//#define USING_COLLIDERS

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Boids;
using Unity.Physics;
using Unity.Physics.Extensions;
using Debug = UnityEngine.Debug;
using Unity.Physics.Systems;

namespace Boids
{
    public class BoidsSimulationSystem : JobComponentSystem
    {
#if !USING_COLLIDERS
        [BurstCompile]
        private struct NeighborsDetectionJob : IJobForEachWithEntity_EBCC<BoidNeighbors, Translation, BoidVelocity>
        {
            [ReadOnly] public Param.Neighbor neighbor;
            [ReadOnly] public ComponentDataFromEntity<Translation> translationFromEntity;
            [ReadOnly] public NativeArray<Entity> allBoids;

            public void Execute(Entity entity, int index, DynamicBuffer<BoidNeighbors> buffer, [ReadOnly] ref Translation translation, [ReadOnly] ref BoidVelocity velocity)
            {
                buffer.Clear();

                float3 forward = math.normalize(velocity.Value);
                var distanceThreshold = neighbor.distance * neighbor.distance;
                var productThreshold = math.cos(math.radians(neighbor.Fov));

                for(int i = 0; i < allBoids.Length; ++i)
                {
                    if(i == index)
                        continue;

                    var neighbor = allBoids[i];

                    var neighborPosition = translationFromEntity[neighbor].Value;
                    var to = neighborPosition - translation.Value;
                    var distance = math.lengthsq(to);
                    
                    if(distance < distanceThreshold)
                    {
                        var direction = math.normalize(to);
                        var product = math.dot(direction, forward);

                        if(product < productThreshold)
                            buffer.Add(new BoidNeighbors { Value = neighbor });
                    }
                }
            }
        }
#else
        [BurstCompile]
        private struct NeighborsDetectionJob : IJobForEachWithEntity_EBCCC<BoidNeighbors, Translation, BoidVelocity, PhysicsCollider>
        {
            [ReadOnly] public CollisionWorld collisionWorld;
            [ReadOnly] public float distanceThreshold;
            [ReadOnly] public float productThreshold;

            public void Execute(Entity entity, int index, DynamicBuffer<BoidNeighbors> buffer, [ReadOnly] ref Translation translation, [ReadOnly] ref BoidVelocity velocity, [ReadOnly] ref PhysicsCollider collider)
            {
                buffer.Clear();

                var forward = math.normalize(velocity.Value);

                var pointDistanceInput = new PointDistanceInput()
                {
                    Filter = collider.Value.Value.Filter
                    , MaxDistance = distanceThreshold
                    , Position = translation.Value
                };
                var allHits = new NativeList<DistanceHit>(8, Allocator.Temp);
                if(collisionWorld.CalculateDistance(pointDistanceInput, ref allHits))
                {
                    for(int i = 0; i < allHits.Length; ++i)
                    {
                        var hit = allHits[i];
                        var neighbor = collisionWorld.Bodies[hit.RigidBodyIndex].Entity;
                        if(neighbor == entity)
                            continue;

                        var to = hit.Position - translation.Value;
                        var direction = math.normalize(to);
                        var product = math.dot(direction, forward);

                        if(product < productThreshold)
                            buffer.Add(new BoidNeighbors { Value = neighbor });
                    }
                }
                allHits.Dispose();
            }
        }
#endif

        [BurstCompile]
        private struct WallJob : IJobForEach<Translation, BoidAcceleration>
        {
            [ReadOnly] public Param.Wall wall;

            // Calculates how close the boid is to a wall and how that affects its acceleration
            float3 GetAccelerationAgainstWall(float distance, float3 direction, float threshold, float weight) =>
                distance < threshold
                ? direction * (weight / math.abs(distance / threshold))
                : float3.zero;

            public void Execute([ReadOnly] ref Translation translation, [WriteOnly] ref BoidAcceleration acceleration)
            {
                var scale = wall.scale * 0.5f;
                var threshold = wall.distance;
                var weight = wall.weight;

                acceleration.Value +=
                    GetAccelerationAgainstWall(-scale - translation.Value.x, new float3(1, 0, 0), threshold, weight) +
                    GetAccelerationAgainstWall(-scale - translation.Value.y, new float3(0, 1, 0), threshold, weight) +
                    GetAccelerationAgainstWall(-scale - translation.Value.z, new float3(0, 0, 1), threshold, weight) +
                    GetAccelerationAgainstWall(scale - translation.Value.x, new float3(-1, 0, 0), threshold, weight) +
                    GetAccelerationAgainstWall(scale - translation.Value.y, new float3(0, -1, 0), threshold, weight) +
                    GetAccelerationAgainstWall(scale - translation.Value.z, new float3(0, 0, -1), threshold, weight);
            }
        }

        [BurstCompile]
        private struct SeparationJob : IJobForEach_BCC<BoidNeighbors, Translation, BoidAcceleration>
        {
            [ReadOnly] public float separationWeight;
            [ReadOnly] public ComponentDataFromEntity<Translation> neighborPositions;

            public void Execute(DynamicBuffer<BoidNeighbors> buffer, [ReadOnly] ref Translation translation, [WriteOnly] ref BoidAcceleration acceleration)
            {
                var neighbors = buffer.Reinterpret<Entity>();
                if(neighbors.Length == 0)
                    return;

                var force = float3.zero;
                for(int i = 0; i < neighbors.Length; ++i)
                {
                    var neighborPosition = neighborPositions[neighbors[i]].Value;
                    force += math.normalize(translation.Value - neighborPosition);
                }
                force /= neighbors.Length;

                var decceleration = force * separationWeight;
                acceleration.Value += decceleration;
            }
        }

        [BurstCompile]
        private struct AlignmentJob : IJobForEach_BCC<BoidNeighbors, BoidVelocity, BoidAcceleration>
        {
            [ReadOnly] public float alignmentWeight;
            [ReadOnly] public ComponentDataFromEntity<BoidVelocity> neighborVelocities;

            public void Execute(DynamicBuffer<BoidNeighbors> buffer, [ReadOnly] ref BoidVelocity velocity, [WriteOnly] ref BoidAcceleration acceleration)
            {
                var neighbors = buffer.Reinterpret<Entity>();
                if(neighbors.Length == 0)
                    return;

                var averageVelocity = float3.zero;
                for(int i = 0; i < neighbors.Length; ++i)
                    averageVelocity += neighborVelocities[neighbors[i]].Value;
                averageVelocity /= neighbors.Length;

                var decceleration = (averageVelocity - velocity.Value) * alignmentWeight;
                acceleration.Value += decceleration;
            }
        }

        [BurstCompile]
        private struct CohesionJob : IJobForEach_BCC<BoidNeighbors, Translation, BoidAcceleration>
        {
            [ReadOnly] public float cohesionWeight;
            [ReadOnly] public ComponentDataFromEntity<Translation> neighborPositions;

            public void Execute(DynamicBuffer<BoidNeighbors> buffer, [ReadOnly] ref Translation translation, [WriteOnly] ref BoidAcceleration acceleration)
            {
                var neighbors = buffer.Reinterpret<Entity>();
                if(neighbors.Length == 0)
                    return;

                var averagePosition = float3.zero;
                for(int i = 0; i < neighbors.Length; ++i)
                    averagePosition += neighborPositions[neighbors[i]].Value;
                averagePosition /= neighbors.Length;

                var decceleration = (averagePosition - translation.Value) * cohesionWeight;
                acceleration.Value += decceleration;
            }
        }

        [BurstCompile]
        private struct MoveJob : IJobForEach<Translation, Rotation, BoidVelocity, BoidAcceleration>
        {
            [ReadOnly] public float deltaTime;
            [ReadOnly] public Param.Speed speed;

            public void Execute([WriteOnly] ref Translation translation, [WriteOnly] ref Rotation rotation, ref BoidVelocity velocity, ref BoidAcceleration acceleration)
            {
                velocity.Value += acceleration.Value * deltaTime;

                var direction = math.normalize(velocity.Value);
                var speed = math.length(velocity.Value);
                velocity.Value = math.clamp(speed, this.speed.min, this.speed.max) * direction;
                translation.Value += velocity.Value * deltaTime;
                rotation.Value = quaternion.LookRotationSafe(direction, new float3(0, 1, 0));

                acceleration.Value = float3.zero;
            }
        }

#if !USING_COLLIDERS
        private EntityQuery entityQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityQuery = GetEntityQuery(typeof(BoidNeighbors), typeof(Translation), typeof(BoidVelocity));
        }
#endif

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var param = Bootstrap.Param;

#if !USING_COLLIDERS
            var allBoids = entityQuery.ToEntityArray(Allocator.TempJob);

            var neighborsDetectionJob = new NeighborsDetectionJob()
            {
                neighbor = param.neighbor
                , translationFromEntity = GetComponentDataFromEntity<Translation>(true)
                , allBoids = allBoids
            };
#else
            var buildPhysicsWorld = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();

            var neighborsDetectionJob = new NeighborsDetectionJob()
            {
                collisionWorld = buildPhysicsWorld.PhysicsWorld.CollisionWorld
                , distanceThreshold = param.neighbor.distance
                , productThreshold = math.cos(math.radians(param.neighbor.Fov))
            };
#endif

            var wallJob = new WallJob()
            {
                wall = param.wall
            };

            var separationJob = new SeparationJob()
            {
                separationWeight = param.shoal.seperationWeight
                , neighborPositions = GetComponentDataFromEntity<Translation>(true)
            };

            var alignmentJob = new AlignmentJob()
            {
                alignmentWeight = param.shoal.alignmentWeight
                , neighborVelocities = GetComponentDataFromEntity<BoidVelocity>(true)
            };

            var cohesionJob = new CohesionJob()
            {
                cohesionWeight = param.shoal.cohesionWeight
                , neighborPositions = GetComponentDataFromEntity<Translation>(true)
            };

            var moveJob = new MoveJob()
            {
                deltaTime = Time.DeltaTime
                , speed = param.speed
            };

#if !USING_COLLIDERS
            inputDeps = neighborsDetectionJob.Schedule(this, inputDeps);
            inputDeps.Complete();
            allBoids.Dispose();
#else
            inputDeps = JobHandle.CombineDependencies(
                buildPhysicsWorld.FinalJobHandle
                , inputDeps
            );

            inputDeps = neighborsDetectionJob.Schedule(this, inputDeps);
#endif

            inputDeps = wallJob.Schedule(this, inputDeps);

            inputDeps = separationJob.Schedule(this, inputDeps);
            inputDeps = alignmentJob.Schedule(this, inputDeps);
            inputDeps = cohesionJob.Schedule(this, inputDeps);

            inputDeps = moveJob.Schedule(this, inputDeps);

            return inputDeps;
        }
    }

}

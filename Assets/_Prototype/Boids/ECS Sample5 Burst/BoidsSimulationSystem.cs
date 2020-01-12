using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Boids.DOTS.Sample1;
using Debug = UnityEngine.Debug;

namespace Boids.DOTS.Sample5
{
    public class BoidsSimulationSystem : JobComponentSystem
    {
        // Another method for counting neighbors which 'could' potentially be faster (depending on how many boids there are) is to give every boid a physics shape, set to trigger, with as little filters as possible. Then, use either a Point Distance, Collider Distance, or Overlap query to determine all its neighbors.
        [BurstCompile]
        private struct NeighborsDetectionJob : IJobForEachWithEntity_EBCC<NeighborsEntityBuffer, Translation, Velocity>
        {
            [ReadOnly] public Param.Neighbor neighbor;
            [ReadOnly] public ComponentDataFromEntity<Translation> translationFromEntity;
            //[ReadOnly] public BufferFromEntity<NeighborsEntityBuffer> bufferFromEntity;
            [ReadOnly] public NativeArray<Entity> allBoids;

            public void Execute(Entity entity, int index, DynamicBuffer<NeighborsEntityBuffer> buffer, [ReadOnly] ref Translation translation, [ReadOnly] ref Velocity velocity)
            {
                buffer.Clear();

                float3 forward = math.normalize(velocity.Value);
                var distanceThreshold = neighbor.distance;
                var productThreshold = math.cos(math.radians(neighbor.Fov));

                for(int i = 0; i < allBoids.Length; ++i)
                {
                    if(i == index)
                        continue;

                    var neighbor = allBoids[i];

                    float3 neighborPosition = translationFromEntity[neighbor].Value;
                    var to = neighborPosition - translation.Value;
                    var distance = math.length(to);

                    if(distance < distanceThreshold)
                    {
                        var direction = math.normalize(to);
                        var product = math.dot(direction, forward);

                        if(product < productThreshold)
                        {
                            buffer.Add(new NeighborsEntityBuffer { Value = neighbor });
                            //bufferFromEntity[neighbor].Add(new NeighborsEntityBuffer { Value = entity });
                            // Impossible because of write restrictions (a Job cannot write to another entity)
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct WallJob : IJobForEach<Translation, Acceleration>
        {
            [ReadOnly] public Param.Wall wall;

            // Calculates how close the boid is to a wall and how that affects its acceleration
            float3 GetAccelerationAgainstWall(float distance, float3 direction, float threshold, float weight) =>
                distance < threshold
                ? direction * (weight / math.abs(distance / threshold))
                : float3.zero;

            public void Execute([ReadOnly] ref Translation translation, [WriteOnly] ref Acceleration acceleration)
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
        private struct SeparationJob : IJobForEach_BCC<NeighborsEntityBuffer, Translation, Acceleration>
        {
            [ReadOnly] public float separationWeight;
            [ReadOnly] public ComponentDataFromEntity<Translation> neighborPositions;

            public void Execute(DynamicBuffer<NeighborsEntityBuffer> buffer, [ReadOnly] ref Translation translation, [WriteOnly] ref Acceleration acceleration)
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
        private struct AlignmentJob : IJobForEach_BCC<NeighborsEntityBuffer, Velocity, Acceleration>
        {
            [ReadOnly] public float alignmentWeight;
            [ReadOnly] public ComponentDataFromEntity<Velocity> neighborVelocities;

            public void Execute(DynamicBuffer<NeighborsEntityBuffer> buffer, [ReadOnly] ref Velocity velocity, [WriteOnly] ref Acceleration acceleration)
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
        private struct CohesionJob : IJobForEach_BCC<NeighborsEntityBuffer, Translation, Acceleration>
        {
            [ReadOnly] public float cohesionWeight;
            [ReadOnly] public ComponentDataFromEntity<Translation> neighborPositions;

            public void Execute(DynamicBuffer<NeighborsEntityBuffer> buffer, [ReadOnly] ref Translation translation, [WriteOnly] ref Acceleration acceleration)
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
        private struct MoveJob : IJobForEach<Translation, Rotation, Velocity, Acceleration>
        {
            [ReadOnly] public float deltaTime;
            [ReadOnly] public Param.Speed speed;

            public void Execute([WriteOnly] ref Translation translation, [WriteOnly] ref Rotation rotation, ref Velocity velocity, ref Acceleration acceleration)
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

        private EntityQuery entityQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityQuery = GetEntityQuery(typeof(NeighborsEntityBuffer), typeof(Translation), typeof(Velocity));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var param = Bootstrap.Param;
            var allBoids = entityQuery.ToEntityArray(Allocator.TempJob);

            var neighborsDetectionJob = new NeighborsDetectionJob()
            {
                neighbor = param.neighbor
                , translationFromEntity = GetComponentDataFromEntity<Translation>(true)
                //, bufferFromEntity = GetBufferFromEntity<NeighborsEntityBuffer>(false)
                , allBoids = allBoids
            };

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
                , neighborVelocities = GetComponentDataFromEntity<Velocity>(true)
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

            inputDeps = neighborsDetectionJob.Schedule(this, inputDeps);
            inputDeps.Complete();
            allBoids.Dispose();

            inputDeps = wallJob.Schedule(this, inputDeps);

            inputDeps = separationJob.Schedule(this, inputDeps);
            inputDeps = alignmentJob.Schedule(this, inputDeps);
            inputDeps = cohesionJob.Schedule(this, inputDeps);
            
            inputDeps = moveJob.Schedule(this, inputDeps);

            return inputDeps;
        }
    }
}
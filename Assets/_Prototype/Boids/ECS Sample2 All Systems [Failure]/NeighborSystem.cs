using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Boids.DOTS.Sample1;

namespace Boids.DOTS.Sample2
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(BoidsSystemGroup))]
    public class NeighborSystem : JobComponentSystem
    {
        //[BurstCompile]
        struct NeighborSystemJob : IJobForEachWithEntity<Translation, Velocity, Acceleration>
        {
            [ReadOnly] public NativeArray<Entity> allBoids;
            [ReadOnly] public ComponentDataFromEntity<Translation> neighborPositions;
            [ReadOnly] public ComponentDataFromEntity<Velocity> neighborVelocities;
            [ReadOnly] public Param.Neighbor neighborParams;
            [ReadOnly] public Param.Shoal shoalParams;

            private Entity[] GetNeighbors([ReadOnly] Entity entity, [ReadOnly] Translation translation, [ReadOnly] Velocity velocity)
            {
                NativeList<Entity> neighbors = new NativeList<Entity>();

                var distanceThreshold = neighborParams.distance;
                var productThreshold = math.cos(math.radians(neighborParams.Fov));
                float3 forward = math.normalize(velocity.Value);

                for(int i = 0; i < allBoids.Length; ++i)
                {
                    var neighbor = allBoids[i];
                    if(neighbor == entity)
                        continue;

                    float3 neighborPosition = neighborPositions[neighbor].Value;
                    var to = neighborPosition - translation.Value;
                    var distance = math.length(to);

                    if(distance < distanceThreshold)
                    {
                        var direction = math.normalize(to);
                        var product = math.dot(direction, forward);

                        if(product < productThreshold)
                            neighbors.Add(neighbor);
                    }
                }

                return neighbors.ToArray();
            }

            public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref Velocity velocity, [WriteOnly] ref Acceleration acceleration)
            {
                var neighbors = GetNeighbors(entity, translation, velocity);
                if(neighbors.Length == 0)
                    return;

                var averageVelocity = float3.zero;
                var force = float3.zero;
                var averagePosition = float3.zero;
                for(int i = 0; i < neighbors.Length; ++i)
                {
                    averageVelocity += neighborVelocities[neighbors[i]].Value;
                    var neighborPosition = neighborPositions[neighbors[i]].Value;
                    force += math.normalize(translation.Value - neighborPosition);
                    averagePosition += neighborPosition;
                }
                averageVelocity /= neighbors.Length;
                force /= neighbors.Length;
                averagePosition /= neighbors.Length;

                var decceleration =
                    ((averageVelocity - velocity.Value) * shoalParams.alignmentWeight) +
                    (force * shoalParams.seperationWeight) +
                    ((averagePosition - translation.Value) * shoalParams.cohesionWeight);
                acceleration.Value += decceleration;
            }
        }

        private NativeArray<Entity> allBoids;
        protected override void OnCreate()
        {
            base.OnCreate();
            var entityQuery = GetEntityQuery(typeof(Translation), typeof(Velocity), typeof(Acceleration));
            allBoids = entityQuery.ToEntityArray(Allocator.TempJob);
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            Param param = Bootstrap.Param;
            var job = new NeighborSystemJob()
            {
                allBoids = allBoids
                , neighborPositions = GetComponentDataFromEntity<Translation>(true)
                , neighborVelocities = GetComponentDataFromEntity<Velocity>(true)
                , neighborParams = param.neighbor
                , shoalParams = param.shoal
            };
            return job.Schedule(this, inputDependencies);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            allBoids.Dispose();
        }
    }
}


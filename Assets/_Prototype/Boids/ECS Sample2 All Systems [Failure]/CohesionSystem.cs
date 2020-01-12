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
    public class CohesionSystem : JobComponentSystem
    {
        [BurstCompile]
        struct CohesionSystemJob : IJobForEach_BCC<NeighborsEntityBuffer, Translation, Acceleration>
        {
            [ReadOnly] public ComponentDataFromEntity<Translation> neighborPositions;
            [ReadOnly] public float cohesionWeight;

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

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            var job = new CohesionSystemJob()
            {
                neighborPositions = GetComponentDataFromEntity<Translation>(true)
                , cohesionWeight = Bootstrap.Param.shoal.cohesionWeight
            };
            return job.Schedule(this, inputDependencies);
        }
    }
}

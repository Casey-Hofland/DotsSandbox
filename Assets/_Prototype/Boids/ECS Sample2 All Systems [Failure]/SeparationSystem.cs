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
    public class SeparationSystem : JobComponentSystem
    {
        [BurstCompile]
        struct SeparationSystemJob : IJobForEach_BCC<NeighborsEntityBuffer, Translation, Acceleration>
        {
            [ReadOnly] public ComponentDataFromEntity<Translation> neighborPositions;
            [ReadOnly] public float seperationWeight;

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

                var decceleration = force * seperationWeight;
                acceleration.Value += decceleration;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            var job = new SeparationSystemJob()
            {
                neighborPositions = GetComponentDataFromEntity<Translation>(true)
                , seperationWeight = Bootstrap.Param.shoal.seperationWeight
            };
            return job.Schedule(this, inputDependencies);
        }
    }
}

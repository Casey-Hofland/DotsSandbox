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
    public class AlignmentSystem : JobComponentSystem
    {
        [BurstCompile]
        struct AlignmentSystemJob : IJobForEach_BCC<NeighborsEntityBuffer, Velocity, Acceleration>
        {
            [NativeDisableParallelForRestriction]
            [ReadOnly] public ComponentDataFromEntity<Velocity> neighborVelocities;
            [ReadOnly] public float alignmentWeight;

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

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            var job = new AlignmentSystemJob()
            {
                neighborVelocities = GetComponentDataFromEntity<Velocity>(true)
                , alignmentWeight = Bootstrap.Param.shoal.alignmentWeight
            };
            return job.Schedule(this, inputDependencies);
        }
    }
}

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class AutoMoverSystem : JobComponentSystem
{
    [BurstCompile]
    struct AutoMoverSystemJob : IJobForEach<Translation, LocalToWorld, AutoMove>
    {
        public float deltaTime;

        public void Execute([WriteOnly] ref Translation translation, [ReadOnly] ref LocalToWorld localToWorld, [ReadOnly] ref AutoMove autoMove)
        {
            translation.Value += localToWorld.Forward * autoMove.speed * deltaTime;
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new AutoMoverSystemJob()
        {
            deltaTime = Time.DeltaTime
        };
        return job.Schedule(this, inputDependencies);
    }
}
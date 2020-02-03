using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

[UpdateAfter(typeof(TransformSystemGroup))]
public class WrapperSystem : JobComponentSystem
{
    [BurstCompile]
    [RequireComponentTag(typeof(AutoMove))]
    struct WrapperSystemJob : IJobForEach<Translation>
    {
        public Aabb wrapperBounds;

        public void Execute(ref Translation translation)
        {
            var min = wrapperBounds.Min;
            var max = wrapperBounds.Max;
            var extents = wrapperBounds.Extents;

            var position = translation.Value;

            for(int axis = 0; axis < 3; ++axis)
            {
                if(position[axis] < min[axis])
                    position[axis] += extents[axis];
                else if(position[axis] > max[axis])
                    position[axis] -= extents[axis];
            }

            translation.Value = position;
        }
    }

    private EntityQuery wrappers;

    protected override void OnCreate()
    {
        wrappers = GetEntityQuery(ComponentType.ReadOnly<Wrapper>(), ComponentType.ReadOnly<PhysicsCollider>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var wrapper = wrappers.GetSingletonEntity();
        var physicsColliderFromEntity = GetComponentDataFromEntity<PhysicsCollider>(true);
        var bounds = physicsColliderFromEntity[wrapper].Value.Value.CalculateAabb();

        var job = new WrapperSystemJob
        {
            wrapperBounds = bounds
        };
        return job.Schedule(this, inputDependencies);
    }
}
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.Profiling;
using Random = Unity.Mathematics.Random;

namespace Boids.Unity
{
    public class BoidSchoolSpawnSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct SetBoidLocalToWorld : IJobParallelFor
        {
            [NativeDisableContainerSafetyRestriction]
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<LocalToWorld> localToWorldFromEntity;
            public NativeArray<Entity> entities;
            public float3 center;
            public float radius;

            public void Execute(int index)
            {
                var entity = entities[index];
                var random = new Random((uint)(entity.Index + index + 1) * 0x9F6ABC1);
                
                var dir = math.normalizesafe(random.NextFloat3() - new float3(0.5f, 0.5f, 0.5f));
                var pos = center + dir * radius;
                var localToWorld = new LocalToWorld
                {
                    Value = float4x4.TRS(pos, quaternion.LookRotationSafe(dir, math.up()), new float3(1.0f))
                };

                localToWorldFromEntity[entity] = localToWorld;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Entities.WithStructuralChanges().ForEach((Entity entity, int entityInQueryIndex, in BoidSchool boidSchool, in LocalToWorld boidSchoolLocalToWorld) =>
            {
                var boidEntities = new NativeArray<Entity>(boidSchool.count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                Profiler.BeginSample("Instantiate");
                EntityManager.Instantiate(boidSchool.prefab, boidEntities);
                Profiler.EndSample();

                var localToWorldFromEntity = GetComponentDataFromEntity<LocalToWorld>();
                var setBoidLocalToWorldJob = new SetBoidLocalToWorld
                {
                    localToWorldFromEntity = localToWorldFromEntity
                    , entities = boidEntities
                    , center = boidSchoolLocalToWorld.Position
                    , radius = boidSchool.initialRadius
                };

                inputDeps = setBoidLocalToWorldJob.Schedule(boidSchool.count, BoidConstants.innerLoopBatchCount, inputDeps);
                inputDeps = boidEntities.Dispose(inputDeps);

                EntityManager.DestroyEntity(entity);
            }).Run();

            return inputDeps;
        }
    }
}

using CaseyDeCoder.KDCollections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace CaseyDeCoder.Boids
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(BoidSystem))]
    public class BoidSchoolSpawnSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct PopulateBoidSchool : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            [WriteOnly] public ComponentDataFromEntity<LocalToWorld> localToWorldFromEntity;
            [NativeDisableParallelForRestriction]
            [WriteOnly] public ComponentDataFromEntity<Boid> boidFromEntity;
            [WriteOnly] public NativeArray<float3> boidPositions;
            [ReadOnly] public NativeArray<Entity> entities;
            [ReadOnly] public Random random;
            [ReadOnly] public float3 center;

            public void Execute(int index)
            {
                var entity = entities[index];

                var dir = random.NextFloat3Direction();
                var pos = center + random.NextFloat3(new float3(-5.0f), new float3(5.0f)); // Used for simplicity, Remove later!

                // Used for testing
                dir = float3.zero;
                pos = new float3(0, 0, index);

                boidPositions[index] = pos;

                localToWorldFromEntity[entity] = new LocalToWorld
                {
                    Value = float4x4.TRS(pos, quaternion.LookRotationSafe(dir, math.up()), 1.0f)
                };
                boidFromEntity[entity] = new Boid
                {
                    speciesIndex = index
                };
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var boidSystem = World.GetOrCreateSystem<BoidSystem>();

            Entities.WithStructuralChanges().ForEach((Entity entity, int entityInQueryIndex, in BoidSchool boidSchool, in LocalToWorld localToWorld) =>
            {
                var boids = EntityManager.Instantiate(boidSchool.prefab, boidSchool.count, Allocator.TempJob);
                var boidPositions = new NativeArray<float3>(boids.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                var populateBoidSchool = new PopulateBoidSchool
                {
                    localToWorldFromEntity = GetComponentDataFromEntity<LocalToWorld>()
                    , boidFromEntity = GetComponentDataFromEntity<Boid>()
                    , boidPositions = boidPositions
                    , entities = boids
                    , random = new Random((uint)(Time.ElapsedTime + 1) * BoidConstants.randomSeedMultiplier)
                    , center = localToWorld.Position
                };

                inputDeps = populateBoidSchool.Schedule(boidSchool.count, BoidConstants.innerloopBatchCount, inputDeps);
                inputDeps.Complete();

                var tree = new KDTree(boidPositions.ToArray(), BoidConstants.maxPointsPerLeaveNode);
                inputDeps = tree.Rebuild(inputDeps);
                boidSystem.AddKDTree(tree);

                inputDeps = boidPositions.Dispose(inputDeps);
                inputDeps = boids.Dispose(inputDeps);

                inputDeps.Complete();

                EntityManager.DestroyEntity(entity);
            }).Run();

            return inputDeps;
        }
    }
}

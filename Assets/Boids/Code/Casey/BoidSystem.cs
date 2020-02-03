using CaseyDeCoder.KDCollections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace CaseyDeCoder.Boids
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public class BoidSystem : JobComponentSystem
    {
        private EntityQuery boidQuery = null;
        private List<BoidSpecies> uniqueBoidTypes = new List<BoidSpecies>();

        private KDQuery kdQuery = new KDQuery(BoidConstants.maxPointsPerLeaveNode);
        private List<KDTree> kdTrees = new List<KDTree>()
        {
            default
        };

        public void AddKDTree(KDTree tree)
        {
            kdTrees.Add(tree);
        }
        public void RemoveKDTree(int index)
        {
            kdTrees.RemoveAtSwapBack(index);
        }

        protected override void OnCreate()
        {
            var boidQueryDescription = new EntityQueryDesc
            {
                All = new []
                {
                    ComponentType.ReadOnly<BoidSpecies>()
                    , ComponentType.ReadOnly<Boid>()
                    , ComponentType.ReadWrite<LocalToWorld>()
                }
            };

            boidQuery = GetEntityQuery(boidQueryDescription);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            EntityManager.GetAllUniqueSharedComponentData(uniqueBoidTypes);

            boidQuery.SetChangedVersionFilter(ComponentType.ReadWrite<LocalToWorld>());

            var boidType = GetArchetypeChunkComponentType<Boid>(false);
            var boidNeighborType = GetArchetypeChunkBufferType<BoidNeighbor>(false);
            var localToWorldType = GetArchetypeChunkComponentType<LocalToWorld>(false);
            var deltaTime = Time.DeltaTime;

            for(int uniqueBoidTypeIndex = 1; uniqueBoidTypeIndex < uniqueBoidTypes.Count; ++uniqueBoidTypeIndex)
            {
                var uniqueBoidType = uniqueBoidTypes[uniqueBoidTypeIndex];

                boidQuery.SetSharedComponentFilter(uniqueBoidType);
                if(boidQuery.CalculateEntityCount() == 0)
                    continue;

                var kdTree = kdTrees[uniqueBoidTypeIndex];

                var resultIndexes = new NativeArray<int>(uniqueBoidType.maxNeighbors, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var resultDistancesSquared = new NativeArray<float>(uniqueBoidType.maxNeighbors, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var countNeighbors = new CountNeighbors
                {
                    kdQuery = kdQuery
                    , kdTree = kdTree
                    , boidType = boidType
                    , boidNeighborType = boidNeighborType
                    , boidSpecies = uniqueBoidType
                    , resultIndexes = resultIndexes
                    , resultDistancesSquared = resultDistancesSquared
                };

                var move = new Move
                {
                    deltaTime = deltaTime
                    , localToWorldType = localToWorldType
                    , boidSpecies = uniqueBoidType
                };

                var updateKDTreePoints = new UpdateKDTreePoints
                {
                    points = kdTree.Points
                    , localToWorldType = localToWorldType
                    , boidType = boidType
                };

                var countNeighborsDeps = countNeighbors.Schedule(boidQuery, inputDeps);
                //var moveDeps = move.Schedule(boidQuery, inputDeps);
                //var updateKDTreePointsDeps = updateKDTreePoints.Schedule(boidQuery, moveDeps);
                //var rebuildKDTreeDeps = kdTree.Rebuild(updateKDTreePointsDeps);
                //inputDeps = rebuildKDTreeDeps;
                inputDeps = countNeighborsDeps;

                var disposeDeps = inputDeps;
                inputDeps = disposeDeps;

                kdTrees[uniqueBoidTypeIndex] = kdTree;

                //boidQuery.AddDependency(moveDeps);
            }

            uniqueBoidTypes.Clear();

            return inputDeps;
        }

        //[BurstCompile]
        private struct CountNeighbors : IJobChunk
        {
            public KDQuery kdQuery;
            [ReadOnly] public KDTree kdTree;
            public ArchetypeChunkComponentType<Boid> boidType;
            [WriteOnly] public ArchetypeChunkBufferType<BoidNeighbor> boidNeighborType;
            [ReadOnly] public BoidSpecies boidSpecies;

            [DeallocateOnJobCompletion]
            public NativeArray<int> resultIndexes;
            [DeallocateOnJobCompletion]
            public NativeArray<float> resultDistancesSquared;

            private int results;
            private BoidNeighbor boidNeighbor;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var chunkBoids = chunk.GetNativeArray(boidType);
                var chunkBoidNeighborBuffers = chunk.GetBufferAccessor(boidNeighborType);

                for(int i = 0; i < chunk.Count; ++i)
                {
                    var boid = chunkBoids[i];
                    var boidNeighborBuffer = chunkBoidNeighborBuffers[i];

                    kdQuery.Radius(kdTree, boid.speciesIndex, boidSpecies.perceptionDistanceSquared, boidSpecies.maxNeighbors, resultIndexes, resultDistancesSquared, out results);

                    boid.currentNeighbors = results;
                    boidNeighborBuffer.EnsureCapacity(boidSpecies.maxNeighbors);
                    for(int result = 0; result < results; ++result)
                    {
                        boidNeighbor = boidNeighborBuffer[result];
                        boidNeighbor.speciesIndex = resultIndexes[result];
                        boidNeighborBuffer[result] = boidNeighbor;
                    }

                    chunkBoids[i] = boid;
                }
            }
        }

        [BurstCompile]
        private struct Move : IJobChunk
        {
            [ReadOnly] public float deltaTime;
            public ArchetypeChunkComponentType<LocalToWorld> localToWorldType;
            [ReadOnly] public BoidSpecies boidSpecies;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var chunkLocalToWorlds = chunk.GetNativeArray(localToWorldType);

                for(int i = 0; i < chunk.Count; ++i)
                {
                    var localToWorld = chunkLocalToWorlds[i];

                    var newPosition = localToWorld.Position + localToWorld.Forward * boidSpecies.moveSpeed * deltaTime;
                    localToWorld.Value = float4x4.TRS(newPosition, localToWorld.Rotation, 1.0f);

                    chunkLocalToWorlds[i] = localToWorld;
                }
            }
        }

        [BurstCompile]
        private struct UpdateKDTreePoints : IJobChunk
        {
            [WriteOnly] public NativeArray<float3> points;
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> localToWorldType;
            [ReadOnly] public ArchetypeChunkComponentType<Boid> boidType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var chunkLocalToWorlds = chunk.GetNativeArray(localToWorldType);
                var chunkBoids = chunk.GetNativeArray(boidType);

                for(int i = 0; i < chunk.Count; ++i)
                {
                    var localToWorld = chunkLocalToWorlds[i];
                    var boid = chunkBoids[i];

                    points[boid.speciesIndex] = localToWorld.Position;
                }
            }
        }
    }
}

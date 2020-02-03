using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Boids.Unity
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public class BoidSystem : JobComponentSystem
    {
        EntityQuery boidQuery;
        EntityQuery targetQuery;
        EntityQuery obstacleQuery;

        List<Boid> uniqueTypes = new List<Boid>(3);

        [BurstCompile]
        private struct MergeCells : IJobNativeMultiHashMapMergedSharedKeyIndices
        {
            public NativeArray<int> cellIndices;
            public NativeArray<float3> cellAlignment;
            public NativeArray<float3> cellSeparation;
            public NativeArray<int> cellObstaclePositionIndex;
            public NativeArray<float> cellObstacleDistance;
            public NativeArray<int> cellTargetPositionIndex;
            public NativeArray<int> cellCount;
            [ReadOnly] public NativeArray<float3> targetPositions;
            [ReadOnly] public NativeArray<float3> obstaclePositions;

            private void NearestPosition(NativeArray<float3> targets, float3 position, out int nearestPositionIndex, out float nearestDistance)
            {
                nearestPositionIndex = 0;
                nearestDistance = math.lengthsq(position - targets[0]);

                for(int i = 1; i < targets.Length; ++i)
                {
                    var targetPosition = targets[i];
                    var distance = math.lengthsq(position - targetPosition);

                    if(distance < nearestDistance)
                    {
                        nearestPositionIndex = i;
                        nearestDistance = distance;
                    }
                }
                nearestDistance = math.sqrt(nearestDistance);
            }

            // Resolves the distance of the nearest obstacle and target and stores the cell index.
            public void ExecuteFirst(int index)
            {
                var position = cellSeparation[index] / cellCount[index];

                int obstaclePositionIndex;
                float obstacleDistance;
                NearestPosition(obstaclePositions, position, out obstaclePositionIndex, out obstacleDistance);
                cellObstaclePositionIndex[index] = obstaclePositionIndex;
                cellObstacleDistance[index] = obstacleDistance;

                int targetPositionIndex;
                float targetDistance;
                NearestPosition(targetPositions, position, out targetPositionIndex, out targetDistance);
                cellTargetPositionIndex[index] = targetPositionIndex;

                cellIndices[index] = index;
            }

            // Sums the alignment and seperation of the actual index being considered and stores the index of this first value where we're storing cells.
            // Note: these items are summed so that in 'Steer' their average for the cell can be resolved.
            public void ExecuteNext(int firstIndex, int index)
            {
                cellCount[firstIndex] += 1;
                cellAlignment[firstIndex] += cellAlignment[index];
                cellSeparation[firstIndex] += cellSeparation[index];
                cellIndices[index] = firstIndex;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var obstacleCount = obstacleQuery.CalculateEntityCount();
            var targetCount = targetQuery.CalculateEntityCount();

            EntityManager.GetAllUniqueSharedComponentData(uniqueTypes);

            // Each variant of the boid represents a different value of the SharedComponentData and is self-contained, meaning Boids of the same variant only interact with one another. Thus, this loop processes each variant type individually.
            for(int boidVariantIndex = 0; boidVariantIndex < uniqueTypes.Count; ++boidVariantIndex)
            {
                var settings = uniqueTypes[boidVariantIndex];
                boidQuery.AddSharedComponentFilter(settings);

                var boidCount = boidQuery.CalculateEntityCount();

                // Early out. If the given variant includes no boids, move on to the next loop. For example, variant 0 will always exit early because it represents a default, uninitialized boid struct, which does not appear in this sample.
                if(boidCount == 0)
                {
                    boidQuery.ResetFilter();
                    continue;
                }

                // The following calculates spatial cells of neighboring boids.
                // Note: working with a sparse grid and not a dense bounded grid so there are no predefined borders of the space.
                var hashMap = new NativeMultiHashMap<int, int>(boidCount, Allocator.TempJob);
                var cellIndices = new NativeArray<int>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var cellObstaclePositionIndex = new NativeArray<int>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var cellTargetPositionIndex = new NativeArray<int>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var cellCount = new NativeArray<int>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var cellObstacleDistance = new NativeArray<float>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var cellAlignment = new NativeArray<float3>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var cellSeparation = new NativeArray<float3>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                var copyTargetPositions = new NativeArray<float3>(targetCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var copyObstaclePositions = new NativeArray<float3>(obstacleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                // The following jobs all run in parallel because the same JobHandle is passed for their input dependancies when the jobs are scheduled; thus, they can run in any order (or concurrently). The concurrency is property of how they are scheduled, not of the job structs themselves.

                // These jobs extract the relevant position, heading component to NativeArrays so they can be randomly accessed by the 'MergeCells' and 'Steer' jobs. These jobs are defined inline using the Entities.ForEach lambda syntax.
                var initialCellAlignmentJobHandle = Entities
                    .WithSharedComponentFilter(settings)
                    .WithName("InitialCellAlignmentJob")
                    .ForEach((int entityInQueryIndex, in LocalToWorld localToWorld) =>
                    {
                        cellAlignment[entityInQueryIndex] = localToWorld.Forward;
                    }).Schedule(inputDeps);

                var initialCellSeparationJobHandle = Entities
                    .WithSharedComponentFilter(settings)
                    .WithName("InitialCellSeparationJob")
                    .ForEach((int entityInQueryIndex, in LocalToWorld localToWorld) =>
                    {
                        cellSeparation[entityInQueryIndex] = localToWorld.Position;
                    }).Schedule(inputDeps);

                var copyTargetPositionsJobHandle = Entities
                    .WithName("CopyTargetPositionsJob")
                    .WithAll<BoidTarget>()
                    .WithStoreEntityQueryInField(ref targetQuery)
                    .ForEach((int entityInQueryIndex, in LocalToWorld localToWorld) =>
                    {
                        copyTargetPositions[entityInQueryIndex] = localToWorld.Position;
                    }).Schedule(inputDeps);

                var copyObstaclePositionsJobHandle = Entities
                    .WithName("CopyObstaclePositionsJob")
                    .WithAll<BoidObstacle>()
                    .WithStoreEntityQueryInField(ref obstacleQuery)
                    .ForEach((int entityInQueryIndex, in LocalToWorld localToWorld) =>
                    {
                        copyObstaclePositions[entityInQueryIndex] = localToWorld.Position;
                    }).Schedule(inputDeps);

                // Populates a hash map, where each bucket contains the indices of all boids whose positions quantize to the same value for a given cell radius so that the information can be randomly accessed by the 'MergeCells' and 'Steer' jobs. This is useful in terms of the algorithm because it limits the number of comparisons that will actually occur between the different boids. Instead of for each boid, searching through all boids for those within a certain radius, this limits those by the hash-to-bucket simplification.
                var parallelHashMap = hashMap.AsParallelWriter();
                var hashPositionsJobHandle = Entities
                    .WithName("HashPositionsJob")
                    .WithAll<Boid>()
                    .ForEach((int entityInQueryIndex, in LocalToWorld localToWorld) =>
                    {
                        int hash = (int)math.hash(new int3(math.floor(localToWorld.Position / settings.cellRadius)));
                        parallelHashMap.Add(hash, entityInQueryIndex);
                    }).Schedule(inputDeps);

                var initialCellCountJob = new MemsetNativeArray<int>
                {
                    Source = cellCount
                    , Value = 1
                };
                var initialCellCountJobHandle = initialCellCountJob.Schedule(boidCount, BoidConstants.innerLoopBatchCount, inputDeps);

                var initialCellBarrierJobHandle = JobHandle.CombineDependencies(initialCellAlignmentJobHandle, initialCellSeparationJobHandle, initialCellCountJobHandle);
                var copyTargetObstacleBarrierJobHandle = JobHandle.CombineDependencies(copyTargetPositionsJobHandle, copyObstaclePositionsJobHandle);
                var mergeCellsBarrierJobHandle = JobHandle.CombineDependencies(hashPositionsJobHandle, initialCellBarrierJobHandle, copyTargetObstacleBarrierJobHandle);

                var mergeCellsJob = new MergeCells
                {
                    cellIndices = cellIndices
                    , cellAlignment = cellAlignment
                    , cellSeparation = cellSeparation
                    , cellObstacleDistance = cellObstacleDistance
                    , cellObstaclePositionIndex = cellObstaclePositionIndex
                    , cellTargetPositionIndex = cellTargetPositionIndex
                    , cellCount = cellCount
                    , targetPositions = copyTargetPositions
                    , obstaclePositions = copyObstaclePositions
                };
                var mergeCellsJobHandle = mergeCellsJob.Schedule(hashMap, BoidConstants.innerLoopBatchCount, mergeCellsBarrierJobHandle);

                // This reads the previously calculated boid information for all the boids of each cell to update the 'LocalToWorld' of each of the boids based on their newly calculated headings using the standard boid flocking algorithm.
                float deltaTime = math.min(0.05f, Time.DeltaTime);
                var steerJobHandle = Entities
                    .WithName("Steer")
                    .WithSharedComponentFilter(settings)
                    .WithReadOnly(cellIndices)
                    .WithReadOnly(cellCount)
                    .WithReadOnly(cellAlignment)
                    .WithReadOnly(cellSeparation)
                    .WithReadOnly(cellObstacleDistance)
                    .WithReadOnly(cellObstaclePositionIndex)
                    .WithReadOnly(cellTargetPositionIndex)
                    .WithReadOnly(copyObstaclePositions)
                    .WithReadOnly(copyTargetPositions)
                    .ForEach((int entityInQueryIndex, ref LocalToWorld localToWorld) =>
                    {
                        // Temporarily storing the values for code readability
                        var forward = localToWorld.Forward;
                        var currentPosition = localToWorld.Position;
                        var cellIndex = cellIndices[entityInQueryIndex];
                        var neighborCount = cellCount[cellIndex];
                        var alignment = cellAlignment[cellIndex];
                        var separation = cellSeparation[cellIndex];
                        var nearestObstacleDistance = cellObstacleDistance[cellIndex];
                        var nearestObstaclePositionIndex = cellObstaclePositionIndex[cellIndex];
                        var nearestTargetPositionIndex = cellTargetPositionIndex[cellIndex];
                        var nearestObstaclePosition = copyObstaclePositions[nearestObstaclePositionIndex];
                        var nearestTargetPosition = copyTargetPositions[nearestTargetPositionIndex];

                        // Setting up the directions for the three main biocrowds influencing directions adjusted based on the predefined weights:
                        // 1) Alignment - how much should it move in a direction similar to those around it?
                        // Note: we use 'alignment / neighborCount', because we need the average alignment in this case; however alignment is currently the summation of all those of the boids within the cellIndex being considered.
                        var alignmentResult = settings.alignmentWeight * math.normalizesafe(alignment / neighborCount - forward);

                        // 2) Separation - how close it is to other boids and are there too many or too few for comfort?
                        // Note: here separation represents the summed possible center of the cell. We perform the multiplication so that both 'currentPosition' and 'separation' are weighted to represent the cell as a whole and not the current individual boid.
                        var separationResult = settings.separationWeight * math.normalizesafe(currentPosition * neighborCount - separation);

                        // 3) Target - is it still towards its destination?
                        var targetHeading = settings.targetWeight * math.normalizesafe(nearestTargetPosition - currentPosition);

                        var obstacleSteering = currentPosition - nearestObstaclePosition;
                        var avoidObstacleHeading = (nearestObstaclePosition + math.normalizesafe(obstacleSteering)) * settings.obstacleAversionDistance - currentPosition;

                        // The updated heading direction. If not needing to be avoidant (ie obstacle is not within predefined radius) then go with the usual defined heading that uses the amalgamation of the weighted alignment, separation, and target direction vectors.
                        var nearestObstacleDistanceFromRadius = nearestObstacleDistance - settings.obstacleAversionDistance;
                        var normalHeading = math.normalizesafe(alignmentResult + separationResult + targetHeading);
                        var targetForward = math.select(normalHeading, avoidObstacleHeading, nearestObstacleDistanceFromRadius < 0);

                        // Updates using the newly calculated heading direction.
                        var nextHeading = math.normalizesafe(forward, deltaTime * (targetForward - forward));
                        localToWorld = new LocalToWorld
                        {
                            Value = float4x4.TRS(
                                new float3(localToWorld.Position + (nextHeading * settings.moveSpeed * deltaTime))
                                , quaternion.LookRotationSafe(nextHeading, math.up())
                                , new float3(1.0f))
                        };
                    }).Schedule(mergeCellsJobHandle);

                inputDeps = steerJobHandle;

                // Dispose allocated containers with dispose jobs.
                var disposeJobHandle = hashMap.Dispose(inputDeps);
                disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, cellIndices.Dispose(inputDeps));
                disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, cellObstaclePositionIndex.Dispose(inputDeps));
                disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, cellTargetPositionIndex.Dispose(inputDeps));
                disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, cellCount.Dispose(inputDeps));
                disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, cellObstacleDistance.Dispose(inputDeps));
                disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, cellAlignment.Dispose(inputDeps));
                disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, cellSeparation.Dispose(inputDeps));
                disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, copyObstaclePositions.Dispose(inputDeps));
                disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, copyTargetPositions.Dispose(inputDeps));
                inputDeps = disposeJobHandle;

                // We pass the job handle and add the dependancy so that we keep the proper ordering between the jobs as the looping iterates. For our purposes of execution, this ordering isn't necessary; however, without the add dependancy call here, the safety system will throw an error, because we're accessing multiple pieces of boid data and it would think there could be possibly a race condition.

                boidQuery.AddDependency(inputDeps);
                boidQuery.ResetFilter();
            }
            uniqueTypes.Clear();

            return inputDeps;
        }

        protected override void OnCreate()
        {
            boidQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<Boid>(), ComponentType.ReadWrite<LocalToWorld>() }
            });

            RequireForUpdate(boidQuery);
            RequireForUpdate(obstacleQuery);
            RequireForUpdate(targetQuery);
        }
    }
}

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using System;
using Unity.Physics;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CaseyDeCoder.KDCollections
{
    public partial struct KDTree
    {
        private NativeArray<float3> points;
        private NativeArray<int> permutation;
        private NativeList<KDNode> nodes;

        public readonly int MaxPointsPerLeafNode;
        public readonly int Count;

        public NativeArray<float3> Points => points;
        public NativeArray<int> Permutation => permutation;
        public KDNode RootNode => nodes[0];

        private BuildTree GetBuildTree => new BuildTree
        {
            points = points
            , permutation = permutation
            , nodes = nodes
            , MaxPointsPerLeafNode = MaxPointsPerLeafNode
            , Count = Count
        };

        public KDTree(float3[] points, int maxPointsPerLeafNode = 16)
        {
            if(maxPointsPerLeafNode < 1)
                throw new ArgumentOutOfRangeException("maxPointsPerLeafNode", maxPointsPerLeafNode, "Value must be more than 0");

            this.points = new NativeArray<float3>(points, Allocator.Persistent);
            this.MaxPointsPerLeafNode = maxPointsPerLeafNode;
            Count = points.Length;

            permutation = new NativeArray<int>(Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            int expectedMaxNodes = math.ceilpow2((int)math.ceil(Count / (float)maxPointsPerLeafNode)) * 2 - 1;
            nodes = new NativeList<KDNode>(expectedMaxNodes, Allocator.Persistent);

            // This causes Native Collections to be disposed on an Assembly Reload. Unity ECS probably has a better way of dealing with this, but due to ambiguity (+ this package still being in preview) this simpler solution was implemented. Needs to be tested in builds!
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
#endif
        }

#if UNITY_EDITOR
        private void OnBeforeAssemblyReload()
        {
            if(points.IsCreated)
                points.Dispose();
            if(permutation.IsCreated)
                permutation.Dispose();
            if(nodes.IsCreated)
                nodes.Dispose();
        }
#endif

        public void Rebuild()
        {
            GetBuildTree.Run();
        }

        public JobHandle Rebuild(JobHandle jobHandle)
        {
            return GetBuildTree.Schedule(jobHandle);
        }

        public KDNode GetKDNodeAt(int index) => nodes[index];

        [BurstCompile]
        private struct BuildTree : IJob
        {
            [ReadOnly] public NativeArray<float3> points;
            public NativeArray<int> permutation;
            public NativeList<KDNode> nodes;
            [ReadOnly] public int MaxPointsPerLeafNode;
            [ReadOnly] public int Count;

            private int nodeCount;

            private KDNode AddNode()
            {
                if(nodeCount >= nodes.Capacity)
                    nodes.ResizeUninitialized(nodes.Capacity * 2);

                if(nodeCount >= nodes.Length)
                    nodes.AddNoResize(new KDNode());

                KDNode node = nodes[nodeCount];

                node.index = nodeCount;
                node.negativeChildIndex = -1;
                node.positiveChildIndex = -1;
                node.partitionAxis = -1;

                ++nodeCount;

                return node;
            }

            /// <summary>
            /// For calculating root node bounds
            /// </summary>
            /// <returns>Boundary of all points</returns>
            private Aabb CalculateBounds()
            {
                var max = new float3(float.MinValue);
                var min = new float3(float.MaxValue);

                int evenLength = Count & ~1;

                // min, max calculations
                // 3n/2 calculations instead of 2n
                for(int i0 = 0; i0 < evenLength; i0 += 2)
                {
                    int i1 = i0 + 1;

                    var point0 = points[i0];
                    var point1 = points[i1];

                    //loop through all axis of the float3
                    for(int axis = 0; axis < 3; ++axis)
                    {
                        if(point0[axis] > point1[axis])
                        {
                            // point0[axis] is bigger, point1[axis] is smaller
                            if(min[axis] > point1[axis])
                                min[axis] = point1[axis];

                            if(max[axis] < point0[axis])
                                max[axis] = point0[axis];
                        }
                        else
                        {
                            // point0[axis] is smaller, point1[axis] is bigger
                            if(min[axis] > point0[axis])
                                min[axis] = point0[axis];

                            if(max[axis] < point1[axis])
                                max[axis] = point1[axis];
                        }
                    }
                }

                //if the array length was odd, also calculate the min/max for the last element
                if(evenLength != Count)
                {
                    var point = points[evenLength];

                    //loop through all axis of the float3
                    for(int axis = 0; axis < 3; ++axis)
                    {
                        if(min[axis] > point[axis])
                            min[axis] = point[axis];
                        if(max[axis] < point[axis])
                            max[axis] = point[axis];
                    }
                }

                var kdBounds = new Aabb
                {
                    Min = min
                    , Max = max
                };

                return kdBounds;
            }

            private void SplitNodeUntilDone(KDNode node)
            {
                nodes[node.index] = node;

                // Decide if this node should split
                if(node.Count == 0 || node.Count <= MaxPointsPerLeafNode)
                    return;

                // center of bounding box
                var nodeBounds = node.bounds;

                // Find axis where bounds are largest
                int splitAxis = 0;
                float3 boundsSize = nodeBounds.Max - nodeBounds.Min;
                for(int axis = 1; axis < 3; ++axis)
                {
                    if(boundsSize[splitAxis] < boundsSize[axis])
                        splitAxis = axis;
                }
                node.partitionAxis = splitAxis;

                // Calculate the spliting coords
                float splitPivot = CalculatePivot(points, permutation);
                node.partitionCoordinate = splitPivot;

                // 'Spliting' array to two subarrays
                int splittingIndex = Partition(points, permutation);

                // Negative / Left node
                float3 negativeMax = nodeBounds.Max;
                negativeMax[splitAxis] = splitPivot;

                KDNode negativeNode = AddNode();
                negativeNode.bounds = nodeBounds;
                negativeNode.bounds.Max = negativeMax;
                negativeNode.start = node.start;
                negativeNode.end = splittingIndex;
                node.negativeChildIndex = negativeNode.index;
                SplitNodeUntilDone(negativeNode);

                // Positive / Right node
                float3 positiveMin = nodeBounds.Min;
                positiveMin[splitAxis] = splitPivot;

                KDNode positiveNode = AddNode();
                positiveNode.bounds = nodeBounds;
                positiveNode.bounds.Min = positiveMin;
                positiveNode.start = splittingIndex;
                positiveNode.end = node.end;
                node.positiveChildIndex = positiveNode.index;
                SplitNodeUntilDone(positiveNode);

                /// <summary>
                /// Sliding midpoint splitting pivot calculation
                /// 1. First splits node to two equal parts (midPoint)
                /// 2. Checks if elements are in both sides of splitted bounds
                /// 3a. If they are, just return midPoint
                /// 3b. If they are not, then points are only on left or right bound.
                /// 4. Move the splitting pivot so that it shrinks part with points completely (calculate min or max dependent) and return.
                /// </summary>
                float CalculatePivot(NativeArray<float3> points, NativeArray<int> permutation)
                {
                    // Our axis min-max bounds
                    float boundsStart = nodeBounds.Min[splitAxis];
                    float boundsEnd = nodeBounds.Max[splitAxis];

                    //! sliding midpoint rule
                    float midPoint = (boundsStart + boundsEnd) / 2.0f;

                    bool2 balance = false;

                    // this for loop section is used both for sorted and unsorted data
                    for(int i = node.start; i < node.end; ++i)
                    {
                        if(points[permutation[i]][splitAxis] < midPoint)
                            balance.x = true;
                        else
                            balance.y = true;

                        if(math.all(balance))
                            return midPoint;
                    }

                    if(balance.x)
                    {
                        float negativeMaxCoord = float.MinValue;

                        for(int i = node.start; i < node.end; ++i)
                            if(negativeMaxCoord < points[permutation[i]][splitAxis])
                                negativeMaxCoord = points[permutation[i]][splitAxis];

                        return negativeMaxCoord;
                    }
                    else
                    {
                        float positiveMinCoord = float.MaxValue;

                        for(int i = node.start; i < node.end; ++i)
                            if(positiveMinCoord > points[permutation[i]][splitAxis])
                                positiveMinCoord = points[permutation[i]][splitAxis];

                        return positiveMinCoord;
                    }
                }

                /// <summary>
                /// Similar to Hoare partitioning algorithm (used in Quick Sort)
                /// Modification: pivot is not left-most element but is instead argument of function
                /// Calculates splitting index and partially sorts elements (swaps them until they are on correct side - depending on pivot)
                /// Complexity: O(n)
                /// </summary>
                /// <returns>
                /// Returns splitting index that subdivides array into 2 smaller arrays
                /// left = [start, pivot),
                /// right = [pivot, end)
                /// </returns>
                int Partition(NativeArray<float3> points, NativeArray<int> permutation)
                {
                    // note: increasing right pointer is actually decreasing!
                    int LP = node.start - 1;    // left pointer (negative side)
                    int RP = node.end;          // right pointer (positive side)
                    int temp;                   // temporary var for swapping permutation indexes

                    while(true)
                    {
                        // move from left to the right until "out of bounds" value is found
                        do LP++;
                        while(LP < RP && points[permutation[LP]][splitAxis] < splitPivot);

                        // move from right to the left until "out of bounds" value found
                        do RP--;
                        while(LP < RP && points[permutation[RP]][splitAxis] >= splitPivot);

                        if(LP < RP)
                        {
                            // swap
                            temp = permutation[LP];
                            permutation[LP] = permutation[RP];
                            permutation[RP] = temp;
                        }
                        else return LP;
                    }
                }
            }

            public void Execute()
            {
                nodeCount = 0;

                for(int pointIndex = 0; pointIndex < Count; ++pointIndex)
                    permutation[pointIndex] = pointIndex;

                var rootNode = AddNode();
                rootNode.bounds = CalculateBounds();
                rootNode.start = 0;
                rootNode.end = Count;

                SplitNodeUntilDone(rootNode);
            }
        }
    }
}

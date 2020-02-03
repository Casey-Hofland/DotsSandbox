/*MIT License

Copyright(c) 2018 Vili Volčini / viliwonka

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Boids.Casey
{
    public struct KDTree
    {
        private float3[] points;
        private int[] permutation;
        private bool safetyDuplicatesCheck;
        private int maxPointsPerLeafNode;
        private KDNode[] kdNodesStack;
        private int kdNodesCount;
        public KDNode rootNode;

        public int Count { get; private set; }
        public KDNode RootNode => rootNode;
        public float3[] Points => points;  // points on which kd-tree will build on. This array will stay unchanged when re/building kdtree
        public int[] Permutation => permutation;  // index aray, that will be permuted

        public KDTree(float3[] points, int maxPointsPerLeafNode = 16) 
            : this(points, false, maxPointsPerLeafNode) 
        { }

        public KDTree(float3[] points, bool safetyDuplicatesCheck, int maxPointsPerLeafNode = 16)
        {
            this.points = points;
            this.permutation = new int[points.Length];
            this.safetyDuplicatesCheck = safetyDuplicatesCheck;
            this.maxPointsPerLeafNode = maxPointsPerLeafNode;

            //TODO: Calculate in advance how large the stack will need to be
            kdNodesStack = new KDNode[BoidConstants.initialKDNodeStackSize];
            kdNodesCount = default;

            rootNode = default;
            Count = points.Length;

            Rebuild();
        }

        public void Build(IList<float3> points)
        {
            Resize(points.Count);

            for(int i = 0; i < Count; ++i)
                this.points[i] = points[i];

            Rebuild();
        }
        #region Build overloads
        public void Build(IList<float3> points, bool safetyDuplicatesCheck)
        {
            this.safetyDuplicatesCheck = safetyDuplicatesCheck;
            Build(points);
        }

        public void Build(IList<float3> points, int maxPointsPerLeafNode)
        {
            this.maxPointsPerLeafNode = maxPointsPerLeafNode;
            Build(points);
        }

        public void Build(IList<float3> points, bool safetyDuplicatesCheck, int maxPointsPerLeafNode)
        {
            this.safetyDuplicatesCheck = safetyDuplicatesCheck;
            this.maxPointsPerLeafNode = maxPointsPerLeafNode;
            Build(points);
        }
        #endregion

        public void Rebuild()
        {
            for(int permutationIndex = 0; permutationIndex < Count; ++permutationIndex)
                permutation[permutationIndex] = permutationIndex;

            BuildTree();
        }
        #region Rebuild overloads
        public void Rebuild(bool safetyDuplicatesCheck)
        {
            this.safetyDuplicatesCheck = safetyDuplicatesCheck;
            Rebuild();
        }

        public void Rebuild(int maxPointsPerLeafNode)
        {
            this.maxPointsPerLeafNode = maxPointsPerLeafNode;
            Rebuild();
        }

        public void Rebuild(bool safetyDuplicatesCheck, int maxPointsPerLeafNode)
        {
            this.safetyDuplicatesCheck = safetyDuplicatesCheck;
            this.maxPointsPerLeafNode = maxPointsPerLeafNode;
            Rebuild();
        }
        #endregion

        public void Resize(int size)
        {
            Count = size;

            // upsize internal arrays
            if(Count > points.Length)
            {
                Array.Resize(ref points, Count);
                Array.Resize(ref permutation, Count);
            }
        }

        private void BuildTree()
        {
            ResetKDNodeStack();

            rootNode = GetKDNode();
            rootNode.bounds = CalculateBounds();
            rootNode.start = 0;
            rootNode.end = Count;

            SplitNode(rootNode);
        }

        private KDNode GetKDNode()
        {
            if(kdNodesCount >= kdNodesStack.Length)
            {
                // automatic resize of KDNode pool array
                Array.Resize(ref kdNodesStack, kdNodesStack.Length * 2);
            }

            KDNode node = kdNodesStack[kdNodesCount];
            node.partitionAxis = -1;
            node.index = kdNodesCount;

            ++kdNodesCount;

            return node;
        }

        public KDNode GetKDNodeAt(int index) => kdNodesStack[index];

        private void ResetKDNodeStack()
        {
            kdNodesCount = 0;
        }

        /// <summary>
        /// For calculating root node bounds
        /// </summary>
        /// <returns>Boundary of all points</returns>
        private KDBounds CalculateBounds()
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
                        if(point1[axis] < min[axis])
                            min[axis] = point1[axis];

                        if(point0[axis] > max[axis])
                            max[axis] = point0[axis];
                    }
                    else
                    {
                        // point0[axis] is smaller, point1[axis] is bigger
                        if(point0[axis] < min[axis])
                            min[axis] = point0[axis];

                        if(point1[axis] > max[axis])
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

            var kdBounds = new KDBounds
            {
                min = min,
                max = max
            };

            return kdBounds;
        }

        /// <summary>
        /// Recursive splitting procedure
        /// </summary>
        /// <param name="parent">This is where the root node goes</param>
        ///
        private void SplitNode(KDNode parent)
        {
            // center of bounding box
            KDBounds parentBounds = parent.bounds;
            float3 parentBoundsSize = parentBounds.Size;

            // find the axis where the bounds are largest
            int splitAxis = 0;
            for(int axis = 1; axis < 3; ++axis)
            {
                if(parentBoundsSize[splitAxis] < parentBoundsSize[axis])
                    splitAxis = axis;
            }

            // Our axis min-max bounds
            float boundsStart = parentBounds.min[splitAxis];
            float boundsEnd = parentBounds.max[splitAxis];

            // Calculate the spliting coords
            float splitPivot = CalculatePivot(parent.start, parent.end, boundsStart, boundsEnd, splitAxis);

            parent.partitionAxis = splitAxis;
            parent.partitionCoordinate = splitPivot;

            // 'Spliting' array to two subarrays
            int splittingIndex = Partition(parent.start, parent.end, splitPivot, splitAxis);

            // Negative / Left node
            float3 negativeMax = parentBounds.max;
            negativeMax[splitAxis] = splitPivot;

            KDNode negativeNode = GetKDNode();
            negativeNode.bounds = parentBounds;
            negativeNode.bounds.max = negativeMax;
            negativeNode.start = parent.start;
            negativeNode.end = splittingIndex;
            parent.negativeChildIndex = negativeNode.index;

            // Positive / Right node
            float3 positiveMin = parentBounds.min;
            positiveMin[splitAxis] = splitPivot;

            KDNode positiveNode = GetKDNode();
            positiveNode.bounds = parentBounds;
            positiveNode.bounds.min = positiveMin;
            positiveNode.start = splittingIndex;
            positiveNode.end = parent.end;
            parent.positiveChildIndex = positiveNode.index;

            // check if we are actually splitting it anything
            // this if check enables duplicate coordinates, but makes construction a bit slower
            if(!safetyDuplicatesCheck && negativeNode.Count != 0 && positiveNode.Count != 0)
            {
                // Constraint function deciding if split should be continued
                if(ContinueSplit(negativeNode))
                    SplitNode(negativeNode);

                if(ContinueSplit(positiveNode))
                    SplitNode(positiveNode);
            }
        }

        /// <summary>
        /// Sliding midpoint splitting pivot calculation
        /// 1. First splits node to two equal parts (midPoint)
        /// 2. Checks if elements are in both sides of splitted bounds
        /// 3a. If they are, just return midPoint
        /// 3b. If they are not, then points are only on left or right bound.
        /// 4. Move the splitting pivot so that it shrinks part with points completely (calculate min or max dependent) and return.
        /// </summary>
        private float CalculatePivot(int start, int end, float boundsStart, float boundsEnd, int axis)
        {
            //! sliding midpoint rule
            float midPoint = (boundsStart + boundsEnd) / 2.0f;

            bool2 balance = false;

            // this for loop section is used both for sorted and unsorted data
            for(int i = start; i < end; ++i)
            {
                if(points[permutation[i]][axis] < midPoint)
                    balance.x = true;
                else
                    balance.y = true;

                if(math.all(balance))
                    return midPoint;
            }

            if(balance.x)
            {
                float negativeMax = float.MinValue;

                for(int i = start; i < end; i++)
                    if(negativeMax < points[permutation[i]][axis])
                        negativeMax = points[permutation[i]][axis];

                return negativeMax;
            }
            else
            {
                float positiveMin = float.MaxValue;

                for(int i = start; i < end; i++)
                    if(positiveMin > points[permutation[i]][axis])
                        positiveMin = points[permutation[i]][axis];

                return positiveMin;
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
        private int Partition(int start, int end, float partitionPivot, int axis)
        {
            // note: increasing right pointer is actually decreasing!
            int LP = start - 1; // left pointer (negative side)
            int RP = end;       // right pointer (positive side)

            int temp;           // temporary var for swapping permutation indexes

            while(true)
            {
                do
                {
                    // move from left to the right until "out of bounds" value is found
                    LP++;
                }
                while(LP < RP && points[permutation[LP]][axis] < partitionPivot);

                do
                {
                    // move from right to the left until "out of bounds" value found
                    RP--;
                }
                while(LP < RP && points[permutation[RP]][axis] >= partitionPivot);

                if(LP < RP)
                {
                    // swap
                    temp = permutation[LP];
                    permutation[LP] = permutation[RP];
                    permutation[RP] = temp;
                }
                else
                {
                    return LP;
                }
            }
        }

        /// <summary>
        /// Constraint function. You can add custom constraints here - if you have some other data/classes binded to points
        /// </summary>
        private bool ContinueSplit(KDNode node) => (node.Count > maxPointsPerLeafNode);
    }
}

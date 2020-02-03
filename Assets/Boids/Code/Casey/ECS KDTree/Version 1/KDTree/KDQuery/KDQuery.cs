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

/*
The object used for querying. This object should be persistent - re-used for querying.
Contains internal array for pooling, so that it doesn't generate (too much) garbage.
The array never down-sizes, only up-sizes, so the more you use this object, the less garbage it will make over time.

Should only be used by 1 thread,
which means each thread should have its own KDQuery object in order for querying to be thread safe.

KDQuery can query different KDTrees.
*/

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;

namespace Boids.Casey
{
    public partial struct KDQuery
    {
        private KDQueryNode[] queueArray;  // queue array
        private MinHeap<KDQueryNode> minHeap; //heap for k-nearest
        private int count;             // size of queue
        private int queryIndex;        // current index at stack
        //private NativeKeyValueArrays<int, KSmallestHeap<int>> heaps;

        private int LeftToProcess => count - queryIndex;

        public KDQuery(int queryNodesContainersInitialSize = BoidConstants.defaultInitialHeapSize)
        {
            queueArray = new KDQueryNode[queryNodesContainersInitialSize];
            minHeap = new MinHeap<KDQueryNode>(queryNodesContainersInitialSize);
            count = default;
            queryIndex = default;
            //heaps = new SortedList<int, KSmallestHeap<int>>();
        }

        /// <summary>
        /// Returns initialized node from stack that also acts as a pool
        /// The returned reference to node stays in stack
        /// </summary>
        /// <returns>Reference to pooled node</returns>
        private KDQueryNode PushGetQueue()
        {
            if(count >= queueArray.Length)
            {
                // automatic resize of pool
                Array.Resize(ref queueArray, queueArray.Length * 2);
            }

            KDQueryNode node = queueArray[count];

            ++count;

            return node;
        }

        private void PushToQueue(KDNode node, float3 tempClosestPoint)
        {
            var queryNode = PushGetQueue();
            queryNode.node = node;
            queryNode.tempClosestPoint = tempClosestPoint;
        }

        private void PushToHeap(KDNode node, float3 tempClosestPoint, float3 queryPosition)
        {
            var queryNode = PushGetQueue();
            queryNode.node = node;
            queryNode.tempClosestPoint = tempClosestPoint;

            float distanceSquared = math.lengthsq(tempClosestPoint - queryPosition);
            queryNode.distance = distanceSquared;
            minHeap.PushObj(queryNode, distanceSquared);
        }

        private KDQueryNode PopFromQueue()
        {
            var node = queueArray[queryIndex];
            queryIndex++;

            return node;
        }

        private KDQueryNode PopFromHeap()
        {
            KDQueryNode heapNode = minHeap.PopObj();

            queueArray[queryIndex] = heapNode;
            queryIndex++;

            return heapNode;
        }

        private void Reset()
        {
            count = 0;
            queryIndex = 0;
            minHeap.Clear();
        }
    }
}

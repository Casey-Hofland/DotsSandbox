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

using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Boids.Casey
{
    public partial struct KDQuery
    {
        /*
        /// <summary>
        /// Returns indices to k closest points, and optionaly can return distances
        /// </summary>
        /// <param name="tree">Tree to do search on</param>
        /// <param name="indice">Position indice</param>
        /// <param name="maxResults">Max number of points</param>
        /// <param name="resultIndices">List where resulting indices will be stored</param>
        public void KNearest(KDTree tree, int indice, int maxResults, NativeList<int> resultIndices)
        {
            if(!heaps.TryGetValue(maxResults, out var kHeap))
            {
                kHeap = new KSmallestHeap<int>(maxResults);
                heaps.Add(maxResults, kHeap);
            }

            kHeap.Clear();
            Reset();

            float3[] points = tree.Points;
            int[] permutation = tree.Permutation;

            ///Biggest Smallest Squared Radius
            float BSSR = float.PositiveInfinity;

            var rootNode = tree.rootNode;
            var queryPosition = points[indice];

            float3 rootClosestPoint = rootNode.bounds.ClosestPoint(queryPosition);

            PushToHeap(rootNode, rootClosestPoint, queryPosition);

            KDQueryNode queryNode;
            KDNode node;
            int partitionAxis;
            float partitionCoord;
            float3 tempClosestPoint;
            KDNode firstChild;
            KDNode secondChild;

            //Searching
            while(minHeap.Count > 0)
            {
                queryNode = PopFromHeap();
                if(queryNode.distance > BSSR)
                    continue;

                node = queryNode.node;

                if(!node.Leaf)
                {
                    partitionAxis = node.partitionAxis;
                    partitionCoord = node.partitionCoordinate;

                    tempClosestPoint = queryNode.tempClosestPoint;

                    if((tempClosestPoint[partitionAxis] - partitionCoord) < 0)
                    {
                        firstChild = tree.GetKDNodeAt(node.negativeChildIndex);
                        secondChild = tree.GetKDNodeAt(node.positiveChildIndex);
                    }
                    else
                    {
                        firstChild = tree.GetKDNodeAt(node.positiveChildIndex);
                        secondChild = tree.GetKDNodeAt(node.negativeChildIndex);
                    }

                    // we already know we are on the side of firstchild bound/node, so we don't need to test for distance. push to stack for later querying.
                    PushToHeap(firstChild, tempClosestPoint, queryPosition);

                    // project the tempClosestPoint to other bound
                    tempClosestPoint[partitionAxis] = partitionCoord;

                    // FIX: Test if this works - strange there is no distance check here
                    if(secondChild.Count != 0)
                    {
                        PushToHeap(secondChild, tempClosestPoint, queryPosition);
                    }
                }
                else
                {
                    float distanceSquared;

                    // LEAF
                    for(int i = node.start; i < node.end; i++)
                    {
                        int index = permutation[i];
                        if(index == indice)
                            continue;

                        distanceSquared = math.lengthsq(points[index] - queryPosition);

                        if(distanceSquared <= BSSR)
                        {
                            kHeap.PushObj(index, distanceSquared);

                            if(kHeap.Full)
                            {
                                BSSR = kHeap.HeadValue;
                            }
                        }
                    }

                }
            }

            kHeap.FlushResult(resultIndices);
        }
        */
    }
}

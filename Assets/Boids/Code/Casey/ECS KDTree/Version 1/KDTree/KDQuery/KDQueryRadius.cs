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

using Unity.Collections;
using Unity.Mathematics;

namespace Boids.Casey
{
    public partial struct KDQuery
    {
        /// <summary>
        /// Search by radius method.
        /// </summary>
        /// <param name="tree">Tree to do search on</param>
        /// <param name="indice">Position Indice</param>
        /// <param name="queryRadiusSquared">Radius Squared</param>
        /// <param name="resultIndices">Initialized list, cleared.</param>
        public void Radius(KDTree tree, int indice, float queryRadiusSquared, NativeList<int> resultIndices)
        {
            Reset();

            float3[] points = tree.Points;
            int[] permutation = tree.Permutation;
            var rootNode = tree.rootNode;

            var queryPosition = points[indice];
            PushToQueue(rootNode, rootNode.bounds.ClosestPoint(queryPosition));

            KDQueryNode queryNode;
            KDNode node;
            int partitionAxis;
            float partitionCoord;
            float3 tempClosestPoint;
            KDNode firstChild;
            KDNode secondChild;
            float distanceSquared;

            // KD search with pruning (don't visit areas with larger distance than range)
            // Recursion done on Stack
            while(LeftToProcess > 0)
            {
                queryNode = PopFromQueue();
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

                    //We already know we are inside firstchild bound/node, so we don't need to test for distance. Push to stack for later querying

                    //tempClosestPoint is inside negative side. Assign it to firstChild.
                    PushToQueue(firstChild, tempClosestPoint);
                    tempClosestPoint[partitionAxis] = partitionCoord;
                    distanceSquared = math.lengthsq(tempClosestPoint - queryPosition);

                    //Testing other side
                    if(secondChild.Count != 0
                            && distanceSquared <= queryRadiusSquared)
                    {
                        PushToQueue(secondChild, tempClosestPoint);
                    }
                }
                else
                {
                    //LEAF
                    for(int i = node.start; i < node.end; i++)
                    {
                        int index = permutation[i];
                        if(index != indice)
                            continue;

                        if(math.lengthsq(points[index] - queryPosition) <= queryRadiusSquared)
                        {
                            resultIndices.Add(index);
                        }
                    }

                }
            }
        }
    }
}

using Unity.Collections;
using Unity.Mathematics;

namespace CaseyDeCoder.KDCollections
{
    public partial struct KDQuery
    {
        /// <summary>
        /// Queries a radius inside a given KDTree.
        /// </summary>
        /// <param name="queryPosition">The position from where you want to query from.</param>
        /// <param name="resultIndexes">The result indexes list to be updated. The list does not have to be cleared or initialized beforehand.</param>
        /// <param name="results">An integer containing the Length of the results that were found.</param>
        public void Radius(KDTree tree, float3 queryPosition, float radiusSquared, NativeList<int> resultIndexes, out int results)
        {
            results = -1;

            var points = tree.Points;
            var permutation = tree.Permutation;
            var rootNode = tree.RootNode;

            Enqueue(rootNode, queryPosition);

            KDQueryNode queryNode;
            KDNode node;
            while(!QueueEmpty)
            {
                queryNode = Dequeue();
                node = queryNode.node;

                if(node.Leaf)
                {
                    //This code updates the results, as well as the max checking distance if appropriate
                    int index;
                    for(int nodeIndex = node.start; nodeIndex < node.end; ++nodeIndex)
                    {
                        index = permutation[nodeIndex];

                        var distanceSquared = math.lengthsq(points[index] - queryPosition);
                        if(distanceSquared >= radiusSquared)
                            continue;

                        ++results;

                        //Check if results can be overridden before adding new results into the list
                        if(results >= resultIndexes.Length)
                        {
                            if(results < resultIndexes.Capacity)
                                resultIndexes.AddNoResize(index);
                            else
                                resultIndexes.Add(index);
                        }
                        else
                            resultIndexes[results] = index;
                    }
                }
                else
                {
                    int partitionAxis = node.partitionAxis;
                    float partitionCoordinate = node.partitionCoordinate;

                    float3 tempClosestPoint = queryNode.tempClosestPoint;

                    (KDNode firstChild, KDNode secondChild) = ((tempClosestPoint[partitionAxis] - partitionCoordinate) < 0)
                        ? (tree.GetKDNodeAt(node.negativeChildIndex), tree.GetKDNodeAt(node.positiveChildIndex))
                        : (tree.GetKDNodeAt(node.positiveChildIndex), tree.GetKDNodeAt(node.negativeChildIndex));

                    //We already know we are inside firstchild bound/node, so we don't need to test for distance. Enqueue immediately.
                    Enqueue(firstChild, tempClosestPoint);

                    //Safetycheck for Empty child.
                    if(secondChild.Count != 0)
                    {
                        //Check to see if the secondchild is inside our radius.
                        tempClosestPoint[partitionAxis] = partitionCoordinate;
                        float distanceSquared = math.lengthsq(tempClosestPoint - queryPosition);
                        if(distanceSquared < radiusSquared)
                            Enqueue(secondChild, tempClosestPoint);
                    }
                }
            }

            ++results;
        }

        /// <summary>
        /// Queries a radius inside a given KDTree.
        /// </summary>
        /// <param name="queryIndex">The node index from the tree you want to query from. Note that the index itself will not be returned in the resultIndexes. If you want to include this index in your results, query for position instead.</param>
        /// <param name="resultIndexes">The result indexes list to be updated. The list does not have to be cleared or initialized beforehand.</param>
        /// <param name="results">An integer containing the Length of the results that were found.</param>
        public void Radius(KDTree tree, int queryIndex, float radiusSquared, NativeList<int> resultIndexes, out int results)
        {
            results = -1;

            var points = tree.Points;
            var permutation = tree.Permutation;
            var rootNode = tree.RootNode;

            var position = points[queryIndex];
            Enqueue(rootNode, position);

            KDQueryNode queryNode;
            KDNode node;
            while(!QueueEmpty)
            {
                queryNode = Dequeue();
                node = queryNode.node;

                if(node.Leaf)
                {
                    //This code updates the results, as well as the max checking distance if appropriate
                    int index;
                    for(int nodeIndex = node.start; nodeIndex < node.end; ++nodeIndex)
                    {
                        index = permutation[nodeIndex];
                        if(index == queryIndex)
                            continue;

                        var distanceSquared = math.lengthsq(points[index] - position);
                        if(distanceSquared >= radiusSquared)
                            continue;

                        ++results;

                        //Check if results can be overridden before adding new results into the list
                        if(results >= resultIndexes.Length)
                        {
                            if(results < resultIndexes.Capacity)
                                resultIndexes.AddNoResize(index);
                            else
                                resultIndexes.Add(index);
                        }
                        else
                            resultIndexes[results] = index;
                    }
                }
                else
                {
                    int partitionAxis = node.partitionAxis;
                    float partitionCoordinate = node.partitionCoordinate;

                    float3 tempClosestPoint = queryNode.tempClosestPoint;

                    (KDNode firstChild, KDNode secondChild) = ((tempClosestPoint[partitionAxis] - partitionCoordinate) < 0)
                        ? (tree.GetKDNodeAt(node.negativeChildIndex), tree.GetKDNodeAt(node.positiveChildIndex))
                        : (tree.GetKDNodeAt(node.positiveChildIndex), tree.GetKDNodeAt(node.negativeChildIndex));

                    //We already know we are inside firstchild bound/node, so we don't need to test for distance. Enqueue immediately.
                    Enqueue(firstChild, tempClosestPoint);

                    //Safetycheck for Empty child.
                    if(secondChild.Count != 0)
                    {
                        //Check to see if the secondchild is inside our radius.
                        tempClosestPoint[partitionAxis] = partitionCoordinate;
                        float distanceSquared = math.lengthsq(tempClosestPoint - position);
                        if(distanceSquared < radiusSquared)
                            Enqueue(secondChild, tempClosestPoint);
                    }
                }
            }

            ++results;
        }

        /// <summary>
        /// Queries a radius inside a given KDTree.
        /// </summary>
        /// <param name="queryPosition">The position from where you want to query from.</param>
        /// <param name="resultIndexes">The result indexes list to be updated. The list does not have to be cleared or initialized beforehand.</param>
        /// <param name="resultDistancesSquared">The result distances squared list to be updated. The list does not have to be cleared or initialized beforehand.</param>
        /// <param name="results">An integer containing the Length of the results that were found.</param>
        public void Radius(KDTree tree, float3 queryPosition, float radiusSquared, NativeList<int> resultIndexes, NativeList<float> resultDistancesSquared, out int results)
        {
            results = -1;

            var points = tree.Points;
            var permutation = tree.Permutation;
            var rootNode = tree.RootNode;

            Enqueue(rootNode, queryPosition);

            KDQueryNode queryNode;
            KDNode node;
            while(!QueueEmpty)
            {
                queryNode = Dequeue();
                node = queryNode.node;

                if(node.Leaf)
                {
                    //This code updates the results, as well as the max checking distance if appropriate
                    int index;
                    for(int nodeIndex = node.start; nodeIndex < node.end; ++nodeIndex)
                    {
                        index = permutation[nodeIndex];

                        var distanceSquared = math.lengthsq(points[index] - queryPosition);
                        if(distanceSquared >= radiusSquared)
                            continue;

                        ++results;

                        //Check if results can be overridden before adding new results into the list
                        if(results >= resultIndexes.Length)
                        {
                            if(results < resultIndexes.Capacity)
                                resultIndexes.AddNoResize(index);
                            else
                                resultIndexes.Add(index);
                        }
                        else
                            resultIndexes[results] = index;

                        if(results >= resultDistancesSquared.Length)
                        {
                            if(results < resultDistancesSquared.Capacity)
                                resultDistancesSquared.AddNoResize(index);
                            else
                                resultDistancesSquared.Add(index);
                        }
                        else
                            resultDistancesSquared[results] = index;
                    }
                }
                else
                {
                    int partitionAxis = node.partitionAxis;
                    float partitionCoordinate = node.partitionCoordinate;

                    float3 tempClosestPoint = queryNode.tempClosestPoint;

                    (KDNode firstChild, KDNode secondChild) = ((tempClosestPoint[partitionAxis] - partitionCoordinate) < 0)
                        ? (tree.GetKDNodeAt(node.negativeChildIndex), tree.GetKDNodeAt(node.positiveChildIndex))
                        : (tree.GetKDNodeAt(node.positiveChildIndex), tree.GetKDNodeAt(node.negativeChildIndex));

                    //We already know we are inside firstchild bound/node, so we don't need to test for distance. Enqueue immediately.
                    Enqueue(firstChild, tempClosestPoint);

                    //Safetycheck for Empty child.
                    if(secondChild.Count != 0)
                    {
                        //Check to see if the secondchild is inside our radius.
                        tempClosestPoint[partitionAxis] = partitionCoordinate;
                        float distanceSquared = math.lengthsq(tempClosestPoint - queryPosition);
                        if(distanceSquared < radiusSquared)
                            Enqueue(secondChild, tempClosestPoint);
                    }
                }
            }

            ++results;
        }

        /// <summary>
        /// Queries a radius inside a given KDTree.
        /// </summary>
        /// <param name="queryIndex">The node index from the tree you want to query from. Note that the index itself will not be returned in the resultIndexes. If you want to include this index in your results, query for position instead.</param>
        /// <param name="resultIndexes">The result indexes list to be updated. The list does not have to be cleared or initialized beforehand.</param>
        /// <param name="resultDistancesSquared">The result distances squared list to be updated. The list does not have to be cleared or initialized beforehand.</param>
        /// <param name="results">An integer containing the Length of the results that were found.</param>
        public void Radius(KDTree tree, int queryIndex, float radiusSquared, NativeList<int> resultIndexes, NativeList<float> resultDistancesSquared, out int results)
        {
            results = -1;

            var points = tree.Points;
            var permutation = tree.Permutation;
            var rootNode = tree.RootNode;

            var position = points[queryIndex];
            Enqueue(rootNode, position);

            KDQueryNode queryNode;
            KDNode node;
            while(!QueueEmpty)
            {
                queryNode = Dequeue();
                node = queryNode.node;

                if(node.Leaf)
                {
                    //This code updates the results, as well as the max checking distance if appropriate
                    int index;
                    for(int nodeIndex = node.start; nodeIndex < node.end; ++nodeIndex)
                    {
                        index = permutation[nodeIndex];
                        if(index == queryIndex)
                            continue;

                        var distanceSquared = math.lengthsq(points[index] - position);
                        if(distanceSquared >= radiusSquared)
                            continue;

                        ++results;

                        //Check if results can be overridden before adding new results into the list
                        if(results >= resultIndexes.Length)
                        {
                            if(results < resultIndexes.Capacity)
                                resultIndexes.AddNoResize(index);
                            else
                                resultIndexes.Add(index);
                        }
                        else
                            resultIndexes[results] = index;

                        if(results >= resultDistancesSquared.Length)
                        {
                            if(results < resultDistancesSquared.Capacity)
                                resultDistancesSquared.AddNoResize(index);
                            else
                                resultDistancesSquared.Add(index);
                        }
                        else
                            resultDistancesSquared[results] = index;
                    }
                }
                else
                {
                    int partitionAxis = node.partitionAxis;
                    float partitionCoordinate = node.partitionCoordinate;

                    float3 tempClosestPoint = queryNode.tempClosestPoint;

                    (KDNode firstChild, KDNode secondChild) = ((tempClosestPoint[partitionAxis] - partitionCoordinate) < 0)
                        ? (tree.GetKDNodeAt(node.negativeChildIndex), tree.GetKDNodeAt(node.positiveChildIndex))
                        : (tree.GetKDNodeAt(node.positiveChildIndex), tree.GetKDNodeAt(node.negativeChildIndex));

                    //We already know we are inside firstchild bound/node, so we don't need to test for distance. Enqueue immediately.
                    Enqueue(firstChild, tempClosestPoint);

                    //Safetycheck for Empty child.
                    if(secondChild.Count != 0)
                    {
                        //Check to see if the secondchild is inside our radius.
                        tempClosestPoint[partitionAxis] = partitionCoordinate;
                        float distanceSquared = math.lengthsq(tempClosestPoint - position);
                        if(distanceSquared < radiusSquared)
                            Enqueue(secondChild, tempClosestPoint);
                    }
                }
            }

            ++results;
        }

        /// <summary>
        /// Queries a radius inside a given KDTree with a maximum number of results.
        /// </summary>
        /// <param name="queryPosition">The position from where you want to query from.</param>
        /// <param name="resultIndexes">The result indexes array to be updated. The array MUST have a length at least equal to the maxResults and does not have to be cleared or initialized beforehand.</param>
        /// <param name="resultDistancesSquared">The result distances squared array to be updated. The array MUST have a length at least equal to the maxResults and does not have to be cleared or initialized beforehand.</param>
        /// <param name="results">An integer containing the Length of the results that were found.</param>
        public void Radius(KDTree tree, float3 queryPosition, float radiusSquared, int maxResults, NativeArray<int> resultIndexes, NativeArray<float> resultDistancesSquared, out int results)
        {
            results = -1;

            var points = tree.Points;
            var permutation = tree.Permutation;
            var rootNode = tree.RootNode;

            Enqueue(rootNode, queryPosition);

            var maxDistanceSquared = radiusSquared;

            KDQueryNode queryNode;
            KDNode node;
            while(!QueueEmpty)
            {
                queryNode = Dequeue();
                node = queryNode.node;

                if(node.Leaf)
                {
                    //This code updates the results, as well as the max checking distance if appropriate
                    int index;
                    for(int nodeIndex = node.start; nodeIndex < node.end; ++nodeIndex)
                    {
                        index = permutation[nodeIndex];

                        var distanceSquared = math.lengthsq(points[index] - queryPosition);
                        if(distanceSquared >= maxDistanceSquared)
                            continue;

                        if(results + 1 < maxResults)
                            ++results;

                        resultIndexes[results] = index;
                        resultDistancesSquared[results] = distanceSquared;

                        //Update both indice- and distance array. This array is continually sorted, so we can stop sorting the moment we find 2 results to be in the right order.
                        float tempResultDistanceSquared;
                        int tempIndice;
                        for(int result = results, nextResult = result - 1;
                            result > 0 && resultDistancesSquared[nextResult] > resultDistancesSquared[result];
                            --result, --nextResult)
                        {
                            //Set temp values
                            tempResultDistanceSquared = resultDistancesSquared[nextResult];
                            tempIndice = resultIndexes[nextResult];

                            //Update next result
                            resultDistancesSquared[nextResult] = resultDistancesSquared[result];
                            resultIndexes[nextResult] = resultIndexes[result];

                            //Update current result
                            resultDistancesSquared[result] = tempResultDistanceSquared;
                            resultIndexes[result] = tempIndice;
                        }

                        if(results + 1 == maxResults)
                            maxDistanceSquared = resultDistancesSquared[results];
                    }
                }
                else
                {
                    int partitionAxis = node.partitionAxis;
                    float partitionCoordinate = node.partitionCoordinate;

                    float3 tempClosestPoint = queryNode.tempClosestPoint;

                    (KDNode firstChild, KDNode secondChild) = ((tempClosestPoint[partitionAxis] - partitionCoordinate) < 0)
                        ? (tree.GetKDNodeAt(node.negativeChildIndex), tree.GetKDNodeAt(node.positiveChildIndex))
                        : (tree.GetKDNodeAt(node.positiveChildIndex), tree.GetKDNodeAt(node.negativeChildIndex));

                    //We already know we are inside firstchild bound/node, so we don't need to test for distance. Enqueue immediately.
                    Enqueue(firstChild, tempClosestPoint);

                    //Safetycheck for Empty child.
                    if(secondChild.Count != 0)
                    {
                        //Check to see if the secondchild is inside our radius.
                        tempClosestPoint[partitionAxis] = partitionCoordinate;
                        float distanceSquared = math.lengthsq(tempClosestPoint - queryPosition);
                        if(distanceSquared < maxDistanceSquared)
                            Enqueue(secondChild, tempClosestPoint);
                    }
                }
            }

            ++results;
        }

        /// <summary>
        /// Queries a radius inside a given KDTree with a maximum number of results.
        /// </summary>
        /// <param name="queryIndex">The node index from the tree you want to query from. Note that the index itself will not be returned in the resultIndexes. If you want to include this index in your results, query for position instead.</param>
        /// <param name="resultIndexes">The result indexes array to be updated. The array MUST have a length at least equal to the maxResults and does not have to be cleared or initialized beforehand.</param>
        /// <param name="resultDistancesSquared">The result distances squared array to be updated. The array MUST have a length at least equal to the maxResults and does not have to be cleared or initialized beforehand.</param>
        /// <param name="results">An integer containing the Length of the results that were found.</param>
        public void Radius(KDTree tree, int queryIndex, float radiusSquared, int maxResults, NativeArray<int> resultIndexes, NativeArray<float> resultDistancesSquared, out int results)
        {
            results = -1;

            var points = tree.Points;
            var permutation = tree.Permutation;
            var rootNode = tree.RootNode;

            var position = points[queryIndex];
            Enqueue(rootNode, position);

            var maxDistanceSquared = radiusSquared;

            KDQueryNode queryNode;
            KDNode node;
            while(!QueueEmpty)
            {
                queryNode = Dequeue();
                node = queryNode.node;

                if(node.Leaf)
                {
                    //This code updates the results, as well as the max checking distance if appropriate
                    int index;
                    for(int nodeIndex = node.start; nodeIndex < node.end; ++nodeIndex)
                    {
                        index = permutation[nodeIndex];
                        if(index == queryIndex)
                            continue;

                        var distanceSquared = math.lengthsq(points[index] - position);
                        if(distanceSquared >= maxDistanceSquared)
                            continue;

                        if(results + 1 < maxResults)
                            ++results;

                        resultIndexes[results] = index;
                        resultDistancesSquared[results] = distanceSquared;

                        //Update both indice- and distance array. This array is continually sorted, so we can stop sorting the moment we find 2 results to be in the right order.
                        float tempResultDistanceSquared;
                        int tempIndice;
                        for(int result = results, nextResult = result - 1; 
                            result > 0 && resultDistancesSquared[nextResult] > resultDistancesSquared[result]; 
                            --result, --nextResult)
                        {
                            //Set temp values
                            tempResultDistanceSquared = resultDistancesSquared[nextResult];
                            tempIndice =                resultIndexes[nextResult];

                            //Update next result
                            resultDistancesSquared[nextResult] =    resultDistancesSquared[result];
                            resultIndexes[nextResult] =             resultIndexes[result];

                            //Update current result
                            resultDistancesSquared[result] =    tempResultDistanceSquared;
                            resultIndexes[result] =             tempIndice;
                        }

                        if(results + 1 == maxResults)
                            maxDistanceSquared = resultDistancesSquared[results];
                    }
                }
                else
                {
                    int partitionAxis = node.partitionAxis;
                    float partitionCoordinate = node.partitionCoordinate;
                    
                    float3 tempClosestPoint = queryNode.tempClosestPoint;

                    (KDNode firstChild, KDNode secondChild) = ((tempClosestPoint[partitionAxis] - partitionCoordinate) < 0)
                        ? (tree.GetKDNodeAt(node.negativeChildIndex), tree.GetKDNodeAt(node.positiveChildIndex))
                        : (tree.GetKDNodeAt(node.positiveChildIndex), tree.GetKDNodeAt(node.negativeChildIndex));

                    //We already know we are inside firstchild bound/node, so we don't need to test for distance. Enqueue immediately.
                    Enqueue(firstChild, tempClosestPoint);

                    //Safetycheck for Empty child.
                    if(secondChild.Count != 0)
                    {
                        //Check to see if the secondchild is inside our radius.
                        tempClosestPoint[partitionAxis] = partitionCoordinate;
                        float distanceSquared = math.lengthsq(tempClosestPoint - position);
                        if(distanceSquared < maxDistanceSquared)
                            Enqueue(secondChild, tempClosestPoint);
                    }
                }
            }

            ++results;
        }
    }
}

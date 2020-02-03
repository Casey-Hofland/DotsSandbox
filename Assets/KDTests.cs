using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System;

using Random = Unity.Mathematics.Random;

namespace CaseyDeCoder.KDCollections
{
    public class KDTests : MonoBehaviour
    {
        public int length = 32;
        public int maxPointsPerLeafNode = 32;

        [ContextMenu("CeilPow2")]
        public void CeilPow2()
        {
            int maxNodesAtBottom = math.ceilpow2((int)math.ceil(length / (float)maxPointsPerLeafNode));
            int halfMaxNodesAtBottom = maxNodesAtBottom / 2;
            int maxNodes = maxNodesAtBottom * 2 - 1
                - math.max((halfMaxNodesAtBottom - (length - halfMaxNodesAtBottom * maxPointsPerLeafNode)) * 2, 0);

            Debug.Log(maxNodes);
        }

        [ContextMenu("Test KDTree")]
        public void TestKDTree()
        {
            var random = new Random(2);

            float3[] points = new float3[length];
            for(int pointIndex = 0; pointIndex < points.Length; ++pointIndex)
                points[pointIndex] = random.NextFloat3(-points.Length / 2.0f, points.Length / 2.0f);

            KDTree kdTree = new KDTree(points, maxPointsPerLeafNode);

            Debug.Log("Success");
        }

        public int[] array = new int[4]
        {
            4,1,2,3
        };
        int head = 0;
        int tail = 0;

        [ContextMenu("Test Queue Resize")]
        public void TestQueueResize()
        {
            int[] tempArray = array;
            head = Array.FindIndex(tempArray, (i => i == 1));
            tail = head;

            foreach(var i in tempArray)
                Debug.Log(i);
            Debug.Log($"Head: {head}");
            Debug.Log($"Tail: {tail}");

            Debug.Log("-----");

            var oldLength = tempArray.Length;
            Array.Resize(ref tempArray, tempArray.Length * 2);

            int oldIndex;
            if(tail * 2 < oldLength)
                for((oldIndex, tail) = (0, oldLength); oldIndex < head; ++oldIndex, ++tail)
                    tempArray[tail] = tempArray[oldIndex];
            else
                for((oldIndex, head) = (oldLength - 1, tempArray.Length); oldIndex >= tail; --oldIndex)
                    tempArray[--head] = tempArray[oldIndex];

            foreach(var i in tempArray)
                Debug.Log(i);
            Debug.Log($"Head: {head}");
            Debug.Log($"Tail: {tail}");
        }

        /*
        // TODO: TEST IF THIS WORKS !!!!!!!!!!!!!!!!!!!!
        resultIndices.Sort(Comparer<int>.Create((a, b) => resultDistancesSquared[a].CompareTo(resultDistancesSquared[b])));
        resultDistancesSquared.Sort();
        */

        int[] indices = new int[8];
        float[] distances = new float[8];

        [ContextMenu("Test Indice Sort")]
        public void TestIndiceSort()
        {
            var random = new Random(216846);

            int results = 0;
            float maxDistance = float.MaxValue;
            for(int i = 0; i < 16; ++i)
            {
                var newDistance = random.NextFloat();

                if(newDistance >= maxDistance)
                    continue;

                indices[results] = i;
                distances[results] = newDistance;

                //Sort both array
                float tempResultDistanceSquared;
                int tempIndice;
                for(int result = results, nextResult = result - 1; result > 0 && distances[nextResult] > distances[result]; --result, --nextResult)
                {
                    tempResultDistanceSquared = distances[nextResult];
                    tempIndice = indices[nextResult];

                    distances[nextResult] = distances[result];
                    indices[nextResult] = indices[result];

                    distances[result] = tempResultDistanceSquared;
                    indices[result] = tempIndice;
                }

                if(results + 1 < 8)
                    ++results;
                else
                    maxDistance = distances[results];
            }

            for(int i = 0; i < 8; ++i)
            {
                Debug.Log($"{indices[i]}: {distances[i]}");
            }
        }
    }
}

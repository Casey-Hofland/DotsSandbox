using UnityEngine;
using System.Collections;
using Unity.Mathematics;
using Unity.Collections;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CaseyDeCoder.KDCollections
{
    public partial struct KDQuery
    {
        [NativeDisableParallelForRestriction]
        private NativeList<KDQueryNode> nodeQueue;
        private int head;
        private int tail;

        private bool QueueEmpty => head == tail;

        public KDQuery(int expectedNodes = 16)
        {
            if(expectedNodes < 1)
                throw new ArgumentOutOfRangeException("expectedNodes", expectedNodes, "Value must be more than 0");

            nodeQueue = new NativeList<KDQueryNode>(expectedNodes, Allocator.Persistent);
            head = 0;
            tail = 0;

            // This causes Native Collections to be disposed on an Assembly Reload. Unity ECS probably has a better way of dealing with this, but due to ambiguity (+ this package still being in preview) this simpler solution was implemented. Needs to be tested in builds!
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
#endif
        }

#if UNITY_EDITOR
        private void OnBeforeAssemblyReload()
        {
            if(nodeQueue.IsCreated)
                nodeQueue.Dispose();
        }
#endif

        private void Enqueue(KDNode node, float3 tempClosestPoint)
        {
            if(tail >= nodeQueue.Length)
                nodeQueue.AddNoResize(new KDQueryNode());

            nodeQueue[tail].Set(node, tempClosestPoint);

            tail = (tail + 1) % nodeQueue.Capacity;
            if(tail == head)
                DoubleQueueSize();
        }

        private KDQueryNode Dequeue()
        {
            if(head == tail)
                throw new IndexOutOfRangeException("nodeQueue does not contain any elements");

            var queryNode = nodeQueue[head];
            head = (head + 1) % nodeQueue.Capacity;

            return queryNode;
        }

        private void DoubleQueueSize()
        {
            var oldCapacity = nodeQueue.Capacity;
            nodeQueue.ResizeUninitialized(oldCapacity * 2);

            int oldIndex;
            if(tail * 2 < oldCapacity)
                for((oldIndex, tail) = (0, oldCapacity); oldIndex < head; ++oldIndex, ++tail)
                    nodeQueue[tail] = nodeQueue[oldIndex];
            else
                for((oldIndex, head) = (oldCapacity - 1, nodeQueue.Capacity); oldIndex >= tail; --oldIndex)
                    nodeQueue[--head] = nodeQueue[oldIndex];
        }
    }
}

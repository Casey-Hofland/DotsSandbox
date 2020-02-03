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

namespace Boids.Casey
{
    public class MinHeap : Heap
    {
        public MinHeap(int initialSize = BoidConstants.defaultInitialHeapSize) : base(initialSize) { }

        public override void PushValue(float h)
        {
            // if heap array is full
            if(nodesCount == maxSize)
            {
                UpsizeHeap();
            }

            ++nodesCount;
            heap[nodesCount] = h;
            BubbleUp(nodesCount);
        }

        public override float PopValue()
        {
            if(nodesCount == 0)
                throw new System.ArgumentException("Heap is empty!");

            float result = heap[1];
            heap[1] = heap[nodesCount];
            --nodesCount;

            if(nodesCount != 0)
                BubbleDown(1);

            return result;
        }

        protected override void BubbleDown(int index)
        {
            int L = Left(index);
            int R = Right(index);

            // bubbling down, 2 kids
            while(R <= nodesCount)
            {
                // if heap property is violated between index and Left child
                if(heap[index] > heap[L])
                {
                    if(heap[L] > heap[R])
                    {
                        Swap(index, R); // right has smaller priority
                        index = R;
                    }
                    else
                    {
                        Swap(index, L); // left has smaller priority
                        index = L;
                    }
                }
                else
                {
                    // if heap property is violated between index and R
                    if(heap[index] > heap[R])
                    {
                        Swap(index, R);
                        index = R;
                    }
                    else
                    {
                        index = L;
                        L = Left(index);
                        break;
                    }
                }

                L = Left(index);
                R = Right(index);
            }

            // only left & last children available to test and swap
            if(L <= nodesCount && heap[index] > heap[L])
            {
                Swap(index, L);
            }
        }

        protected override void BubbleUp(int index)
        {
            int P = Parent(index);

            //swap, until Heap property isn't violated anymore
            while(P > 0 && heap[P] > heap[index])
            {
                Swap(P, index);

                index = P;
                P = Parent(index);
            }
        }
    }

    //Generic MinHeap
    public class MinHeap<T> : MinHeap where T : struct
    {
        T[] objs;

        public T HeadHeapObject => objs[1];

        public override void PushValue(float h) => throw new System.ArgumentException("Use Push(T, float)!");
        public override float PopValue() => throw new System.ArgumentException("Use Push(T, float)!");

        public MinHeap(int maxNodes = BoidConstants.defaultMinHeapMaxNodes) : base(maxNodes)
        {
            objs = new T[maxNodes + 1];
        }

        T tempObjs;
        protected override void Swap(int A, int B)
        {
            tempHeap = heap[A];
            tempObjs = objs[A];

            heap[A] = heap[B];
            objs[A] = objs[B];

            heap[B] = tempHeap;
            objs[B] = tempObjs;
        }

        public void PushObj(T obj, float h)
        {
            // if heap array is full
            if(nodesCount == maxSize)
            {
                UpsizeHeap();
            }

            nodesCount++;
            heap[nodesCount] = h;
            objs[nodesCount] = obj;

            BubbleUp(nodesCount);
        }

        public T PopObj()
        {
            if(nodesCount == 0)
                throw new System.ArgumentException("Heap is empty!");

            T result = objs[1];

            heap[1] = heap[nodesCount];
            objs[1] = objs[nodesCount];

            objs[nodesCount] = default(T);

            nodesCount--;

            if(nodesCount != 0)
                BubbleDown(1);

            return result;
        }

        public T PopObj(ref float heapValue)
        {
            if(nodesCount == 0)
                throw new System.ArgumentException("Heap is empty!");

            heapValue = heap[1];
            T result = PopObj();

            return result;
        }

        protected override void UpsizeHeap()
        {
            maxSize *= 2;
            System.Array.Resize(ref heap, maxSize + 1);
            System.Array.Resize(ref objs, maxSize + 1);
        }

        //flush internal array, returns ordered data
        public void FlushResult(NativeList<T> resultList)
        {
            int count = nodesCount + 1;

            for(int i = 1; i < count; i++)
            {
                resultList.Add(PopObj());
            }
        }

        public void FlushResult(NativeList<T> resultList, NativeList<float> heapList)
        {
            int count = nodesCount + 1;

            float h = 0f;

            for(int i = 1; i < count; i++)
            {
                resultList.Add(PopObj(ref h));
                heapList.Add(h);
            }
        }
    }
}

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
    public class KSmallestHeap : Heap
    {
        public KSmallestHeap(int maxEntries) : base(maxEntries) { }

        public bool Full => maxSize == nodesCount;

        // in lots of cases, max head gets removed
        public override void PushValue(float h)
        {
            // if heap full
            if(nodesCount == maxSize)
            {
                // if Heads priority is smaller than input priority, then ignore that item
                if(HeadValue < h)
                    return;

                heap[1] = h;   // remove top element
                BubbleDown(1); // bubble it down
            }
            else
            {
                ++nodesCount;
                heap[nodesCount] = h;
                BubbleUp(nodesCount);
            }
        }

        public override float PopValue()
        {
            if(nodesCount == 0)
                throw new System.ArgumentException("Heap is empty!");

            float result = heap[1];

            heap[1] = heap[nodesCount];
            --nodesCount;
            BubbleDown(1);

            return result;
        }
    }

    //Generic KSmallestHeap
    public class KSmallestHeap<T> : KSmallestHeap where T : struct
    {
        T[] objs; //objects

        public T HeadHeapObject => objs[1];

        public override void PushValue(float h) => throw new System.ArgumentException("Use Push(T, float)!");
        public override float PopValue() => throw new System.ArgumentException("Use PopObj()!");

        public KSmallestHeap(int maxEntries) : base(maxEntries)
        {
            objs = new T[maxEntries + 1];
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
            // if heap full
            if(nodesCount == maxSize)
            {
                // if Heads priority is smaller than input priority, then ignore that item
                if(HeadValue < h)
                    return;

                heap[1] = h;   // remove top element
                objs[1] = obj;
                BubbleDown(1); // bubble it down
            }
            else
            {
                ++nodesCount;
                heap[nodesCount] = h;
                objs[nodesCount] = obj;
                BubbleUp(nodesCount);
            }
        }

        public T PopObj()
        {
            if(nodesCount == 0)
                throw new System.ArgumentException("Heap is empty!");

            T result = objs[1];

            heap[1] = heap[nodesCount];
            objs[1] = objs[nodesCount];

            nodesCount--;
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

        //flush internal results, returns ordered data
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

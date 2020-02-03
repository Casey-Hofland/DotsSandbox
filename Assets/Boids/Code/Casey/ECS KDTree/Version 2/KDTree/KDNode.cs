using Unity.Physics;

namespace CaseyDeCoder.KDCollections
{
    public struct KDNode
    {
        public int index;
        public int negativeChildIndex;
        public int positiveChildIndex;

        public float partitionCoordinate;
        public int partitionAxis;

        public int start;
        public int end;

        //public KDBounds bounds;
        public Aabb bounds;

        public int Count => end - start;
        public bool Leaf => partitionAxis == -1;
    }
}

using Unity.Mathematics;

namespace Boids.Casey
{
    public static class BoidConstants
    {
        // NOTE: these values do no belong in BoidConstants! Move to KDConstants instead!
        public const int initialKDNodeStackSize = 64;
        public const int defaultInitialHeapSize = 2048;
        public const int defaultMinHeapMaxNodes = 2048;

        // Note: Higher maxPointsPerLeafNode makes construction of KDtree faster, but querying slower. And true is inverse: Lower maxPointsPerLeafNode makes construction of KDtree slower, but querying faster.
        public const int maxPointsPerLeaveNode = 16;
        public const int innerloopBatchCount = 64;
        public const int internalBufferCapacity = 64;
        public const uint randomSeedMultiplier = 0x9F6ABC1;
    }
}

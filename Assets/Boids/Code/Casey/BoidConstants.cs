namespace CaseyDeCoder.Boids
{
    public static class BoidConstants
    {
        // Note: Higher maxPointsPerLeafNode makes construction of KDtree faster, but querying slower. And true is inverse: Lower maxPointsPerLeafNode makes construction of KDtree slower, but querying faster.
        public const int maxPointsPerLeaveNode = 16;
        public const int innerloopBatchCount = 64;
        public const int internalBufferCapacity = 64;
        public const uint randomSeedMultiplier = 0x9F6ABC1;
    }
}

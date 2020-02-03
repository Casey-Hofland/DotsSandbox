using Unity.Entities;

namespace CaseyDeCoder.Boids
{
    [InternalBufferCapacity(BoidConstants.internalBufferCapacity)]
    public struct BoidNeighbor : IBufferElementData 
    {
        public int speciesIndex;
    }
}

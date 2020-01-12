using Unity.Entities;

namespace Boids.DOTS.Sample2
{
    [InternalBufferCapacity(8)]
    public unsafe struct NeighborsEntityBuffer : IBufferElementData
    {
        public Entity Value;
    }
}

using System;
using Unity.Entities;

namespace Boids.DOTS.Sample5
{
    [Serializable]
    [InternalBufferCapacity(4)]
    public unsafe struct NeighborsEntityBuffer : IBufferElementData
    {
        public Entity Value;
    }
}

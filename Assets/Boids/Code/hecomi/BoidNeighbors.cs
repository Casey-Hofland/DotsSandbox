using System;
using Unity.Entities;

namespace Boids
{
    [Serializable]
    [InternalBufferCapacity(4)]
    public struct BoidNeighbors : IBufferElementData
    {
        public Entity Value;
    }
}

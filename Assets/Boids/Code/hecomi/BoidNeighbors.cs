using System;
using Unity.Entities;

[Serializable]
[InternalBufferCapacity(4)]
public struct BoidNeighbors : IBufferElementData
{
    public Entity Value;
}

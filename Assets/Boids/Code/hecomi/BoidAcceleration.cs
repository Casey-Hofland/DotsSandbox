using System;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct BoidAcceleration : IComponentData
{
    public float3 Value;
}

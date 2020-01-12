using System;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct BoidVelocity : IComponentData
{
    public float3 Value;
}

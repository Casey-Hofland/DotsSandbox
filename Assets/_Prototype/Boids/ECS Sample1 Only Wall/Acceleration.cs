using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Boids.DOTS.Sample1
{
    [Serializable]
    public struct Acceleration : IComponentData
    {
        public float3 Value;
    }
}

using System;
using Unity.Entities;

namespace Boids.Unity
{
    [Serializable]
    public struct BoidSchool : IComponentData
    {
        public Entity prefab;
        public float initialRadius;
        public int count;
    }
}

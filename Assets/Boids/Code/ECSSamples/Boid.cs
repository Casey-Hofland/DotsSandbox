using System;
using Unity.Entities;
using Unity.Transforms;

namespace Boids.Unity
{
    [Serializable]
    [WriteGroup(typeof(LocalToWorld))]
    public struct Boid : ISharedComponentData
    {
        public float cellRadius;
        public float separationWeight;
        public float alignmentWeight;
        public float targetWeight;
        public float obstacleAversionDistance;
        public float moveSpeed;
    }
}

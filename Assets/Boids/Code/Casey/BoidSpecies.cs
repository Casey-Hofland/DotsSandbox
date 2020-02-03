using Unity.Entities;
using Unity.Transforms;

namespace CaseyDeCoder.Boids
{
    [WriteGroup(typeof(LocalToWorld))]
    public struct BoidSpecies : ISharedComponentData
    {
        public float perceptionDistanceSquared;
        public float perceptionAngleRadians;
        public float cohesionWeight;
        public float moveSpeed;
        public int maxNeighbors;
    }
}

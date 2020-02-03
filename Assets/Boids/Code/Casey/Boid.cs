using Unity.Entities;
using Unity.Transforms;

namespace CaseyDeCoder.Boids
{
    [WriteGroup(typeof(LocalToWorld))]
    public struct Boid : IComponentData
    {
        public int speciesIndex;
        public int currentNeighbors;
    }
}

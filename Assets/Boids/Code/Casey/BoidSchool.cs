using Unity.Entities;

namespace CaseyDeCoder.Boids
{
    public struct BoidSchool : IComponentData
    {
        public int count;
        public Entity prefab;
    }
}

using System;
using Unity.Entities;

namespace Boids.Unity
{
    [Serializable]
    [GenerateAuthoringComponent]
    public struct BoidObstacle : IComponentData { }
}

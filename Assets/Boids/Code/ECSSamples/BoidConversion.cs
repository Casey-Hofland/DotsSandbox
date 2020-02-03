#if UNITY_EDITOR

using Unity.Entities;
using Unity.Transforms;

namespace Boids.Unity
{
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    [ConverterVersion("macton", 5)]
    public class BoidConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((BoidAuthoring boidAuthoring) =>
            {
                var entity = GetPrimaryEntity(boidAuthoring);

                DstEntityManager.AddSharedComponentData(entity, new Boid
                {
                    cellRadius = boidAuthoring.CellRadius
                    , separationWeight = boidAuthoring.SeparationWeight
                    , alignmentWeight = boidAuthoring.AlignmentWeight
                    , targetWeight = boidAuthoring.TargetWeight
                    , obstacleAversionDistance = boidAuthoring.ObstacleAversionDistance
                    , moveSpeed = boidAuthoring.MoveSpeed
                });

                // Remove default transform system components
                DstEntityManager.RemoveComponent<Translation>(entity);
                DstEntityManager.RemoveComponent<Rotation>(entity);
            });
        }
    }
}

#endif
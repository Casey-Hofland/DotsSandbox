#if UNITY_EDITOR

using UnityEngine;

namespace Boids.Unity
{
    [AddComponentMenu("DOTS Samples/Boids/Boid")]
    public class BoidAuthoring : MonoBehaviour
    {
        [SerializeField] private float cellRadius = 8.0f;
        [SerializeField] private float separationWeight = 1.0f;
        [SerializeField] private float alignmentWeight = 1.0f;
        [SerializeField] private float targetWeight = 2.0f;
        [SerializeField] private float obstacleAversionDistance = 30.0f;
        [SerializeField] private float moveSpeed = 25.0f;

        public float CellRadius => cellRadius;
        public float SeparationWeight => separationWeight;
        public float AlignmentWeight => alignmentWeight;
        public float TargetWeight => targetWeight;
        public float ObstacleAversionDistance => obstacleAversionDistance;
        public float MoveSpeed => moveSpeed;
    }
}

#endif
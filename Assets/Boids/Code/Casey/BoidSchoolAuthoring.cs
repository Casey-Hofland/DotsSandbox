#if UNITY_EDITOR

using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace CaseyDeCoder.Boids
{
    [DisallowMultipleComponent]
    [RequiresEntityConversion]
    public class BoidSchoolAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        [SerializeField] private int count = 0;
        [SerializeField] private GameObject prefab = null;

        [Header("Boid Settings")]
        [SerializeField] private float perceptionDistance = 1.0f;
        [Range(0.0f, 360.0f)]
        [SerializeField] private float perceptionAngle = 90.0f;
        [SerializeField] private float cohesionWeight = 2.0f;
        [SerializeField] private float moveSpeed = 1.0f;
        [SerializeField] private int maxNeighbors = 8;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            Debug.LogWarning($"Unique boid data is decided by the BoidSchoolAuthorings prefab. Be aware that multiple BoidSchoolAuthoring Components with the same prefab may lead to breaking behaviour.");
            var prefab = conversionSystem.GetPrimaryEntity(this.prefab);
            dstManager.AddSharedComponentData(prefab, new BoidSpecies
            {
                perceptionDistanceSquared = perceptionDistance * perceptionDistance
                , perceptionAngleRadians = math.radians(perceptionAngle)
                , cohesionWeight = cohesionWeight
                , moveSpeed = moveSpeed
                , maxNeighbors = maxNeighbors
            });
            dstManager.AddComponentData(prefab, new Boid { });
            dstManager.AddBuffer<BoidNeighbor>(prefab);

            dstManager.RemoveComponent<Translation>(prefab);
            dstManager.RemoveComponent<Rotation>(prefab);

            dstManager.AddComponentData(entity, new BoidSchool
            {
                count = count
                , prefab = prefab
            });
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(prefab);
        }

        private void OnDrawGizmos()
        {
            var lastGizmosColor = Gizmos.color;
            var lastHandlesColor = Handles.color;

            var center = transform.position;
            var right = prefab.transform.right;
            var up = prefab.transform.up;
            var forward = prefab.transform.forward;
            var perceptionEndPosition = forward * perceptionDistance;

            Gizmos.color = Handles.color = Color.green;

            // Draw prefab
            Gizmos.DrawMesh(prefab.GetComponent<MeshFilter>().sharedMesh, center);

            if(Selection.Contains(gameObject))
            {
                // Draw Sight
                Handles.DrawLine(center, center + perceptionEndPosition);

                // Draw arc from upper view
                Handles.DrawLine(center, center + Quaternion.AngleAxis(-perceptionAngle / 2, up) * perceptionEndPosition);
                Handles.DrawLine(center, center + Quaternion.AngleAxis(perceptionAngle / 2, up) * perceptionEndPosition);
                Handles.DrawWireArc(center, up, Quaternion.AngleAxis(-perceptionAngle / 2, up) * perceptionEndPosition, perceptionAngle, perceptionDistance);

                // Draw arc from side view
                Handles.DrawLine(center, center + Quaternion.AngleAxis(-perceptionAngle / 2, right) * perceptionEndPosition);
                Handles.DrawLine(center, center + Quaternion.AngleAxis(perceptionAngle / 2, right) * perceptionEndPosition);
                Handles.DrawWireArc(center, right, Quaternion.AngleAxis(-perceptionAngle / 2, right) * perceptionEndPosition, perceptionAngle, perceptionDistance);

                // Draw closing arc
                Handles.DrawWireArc(center, forward, Quaternion.AngleAxis(-perceptionAngle / 2, up) * perceptionEndPosition, 360.0f, perceptionDistance);
            }

            Gizmos.color = lastGizmosColor;
            Handles.color = lastHandlesColor;
        }
    }
}

#endif
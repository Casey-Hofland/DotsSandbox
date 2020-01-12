//#define USING_COLLIDERS

using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

#if USING_COLLIDERS
using Collider = Unity.Physics.Collider;
#endif

namespace Boids
{
    public class Bootstrap : MonoBehaviour
    {
        private static Bootstrap instance;
        private static Bootstrap Instance => instance ? instance : (instance = FindObjectOfType<Bootstrap>());

        public static bool IsValid => Instance;
        public static Param Param => Instance.param;

        [SerializeField] private int boidCount = 100;
        [SerializeField] private GameObject prefab = null;
        [SerializeField] private Param param = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by RuntimeInitializeOnLoadMethodAttribute")]
        private void Init()
        {
            instance = null;
        }

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            var conversionSettings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, null);
            var sourceEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, conversionSettings);
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var random = new Random((uint)Guid.NewGuid().GetHashCode() + 1);

#if USING_COLLIDERS
            BlobAssetReference<Collider> sourceCollider = entityManager.GetComponentData<PhysicsCollider>(sourceEntity).Value;
#endif

            for(int i = 0; i < boidCount; ++i)
            {
                var instance = entityManager.Instantiate(sourceEntity);
                var position = (float3)transform.position + random.NextFloat3(-param.wall.scale / 2, param.wall.scale / 2);
                var rotation = random.NextQuaternionRotation();
                var direction = random.NextFloat3Direction() * param.speed.initial;

                entityManager.SetComponentData(instance, new Translation { Value = position });
                entityManager.SetComponentData(instance, new Rotation { Value = rotation });

#if !USING_COLLIDERS
                entityManager.RemoveComponent(instance, typeof(PhysicsCollider));
#else
                entityManager.SetComponentData(instance, new PhysicsCollider { Value = sourceCollider });
#endif

                entityManager.AddComponentData(instance, new BoidVelocity { Value = direction });
                entityManager.AddComponentData(instance, new BoidAcceleration { Value = float3.zero });
                entityManager.AddBuffer<BoidNeighbors>(instance);
            }

            entityManager.DestroyEntity(sourceEntity);
        }

        private void OnDrawGizmos()
        {
            if(!param)
                return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one * param.wall.scale);
        }
    }

}

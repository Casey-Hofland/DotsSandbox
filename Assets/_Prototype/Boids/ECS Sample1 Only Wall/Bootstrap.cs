using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Boids.DOTS.Sample1
{
    public class Bootstrap : MonoBehaviour
    {
        // TODO: Remove Singleton Accessor
        public static Bootstrap Instance { get; private set; }
        public static Param Param => Instance.param;

        [SerializeField]
        private int boidCount = 100;
        [SerializeField]
        private Vector3 boidScale = new Vector3(0.1f, 0.1f, 0.3f);
        [SerializeField]
        private Param param = null;
        [SerializeField]
        private RenderMesh renderMesh = new RenderMesh();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by RuntimeInitializeOnLoadMethodAttribute")]
        private void Init()
        {
            Instance = null;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Setup the data for creating new boid entities
            var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var archetype = manager.CreateArchetype(
                typeof(Translation)
                , typeof(Rotation)
                , typeof(NonUniformScale)
                , typeof(LocalToWorld)
                , typeof(Velocity)
                , typeof(Acceleration)
                , typeof(RenderMesh)
            );
            var random = new Unity.Mathematics.Random((uint)Guid.NewGuid().GetHashCode() + 1);

            // Create boid entities from scratch
            for(int i = 0; i < boidCount; ++i)
            {
                var entity = manager.CreateEntity(archetype);
                manager.SetComponentData(entity, new Translation { Value = (float3)transform.position + random.NextFloat3(-param.wall.scale / 2, param.wall.scale / 2) });
                manager.SetComponentData(entity, new Rotation { Value = quaternion.identity });
                manager.SetComponentData(entity, new NonUniformScale { Value = boidScale });
                manager.SetComponentData(entity, new LocalToWorld { Value = float4x4.identity });
                manager.SetComponentData(entity, new Velocity { Value = random.NextFloat3Direction() * param.speed.initial });
                manager.SetComponentData(entity, new Acceleration { Value = float3.zero });
                manager.SetSharedComponentData(entity, renderMesh);
            }
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

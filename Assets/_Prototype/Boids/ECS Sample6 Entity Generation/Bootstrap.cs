using System;
using Unity.Rendering;
using UnityEngine;

namespace Boids.DOTS.Sample6
{
    public class Bootstrap : MonoBehaviour
    {
        private static Bootstrap instance;
        private static Bootstrap Instance => instance ? instance : (instance = FindObjectOfType<Bootstrap>());

        public static bool IsValid => Instance;
        public static Param Param => Instance.param;
        public static BoidInfo Boid => Instance.boidInfo;

        [SerializeField] private Param param = null;
        [SerializeField]
        private BoidInfo boidInfo = new BoidInfo()
        {
            count = 100
            , scale = new Vector3(0.1f, 0.1f, 0.3f)
        };

        [Serializable]
        public struct BoidInfo
        {
            public int count;
            public Vector3 scale;
            public RenderMesh renderMesh;
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

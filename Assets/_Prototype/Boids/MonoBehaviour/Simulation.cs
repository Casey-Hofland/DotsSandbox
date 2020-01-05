using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

using Random = UnityEngine.Random;

namespace Boids.OOP
{
    // This script creates an OOP simulation of boids (without raycasts). I've implemented some magic to make it work inside the editor (untested).
    [ExecuteAlways]
    public class Simulation : MonoBehaviour
    {
        [SerializeField]
        private int boidCount = 0;
        [SerializeField]
        private GameObject boidPrefab = null;
        [SerializeField]
        private Param param = null;

        private List<Boid> boids = new List<Boid>();

        public ReadOnlyCollection<Boid> Boids => boids.AsReadOnly();

        // Create a new boid somewhere inside a sphere inside our box.
        private void InstantiateBoid()
        {
            if(!boidPrefab || !param)
                return;

            var gameObject = Instantiate(boidPrefab, Random.insideUnitSphere * param.wall.scale * Mathf.PI / 6f, Random.rotation);  // Magic!
            gameObject.transform.SetParent(transform);
            if(gameObject.TryGetComponent<Boid>(out var boid))
                boid = gameObject.AddComponent<Boid>();
            boid.Init(this, param);
            boids.Add(boid);
        }

        private void DestroyBoid()
        {
            var lastIndex = boids.Count - 1;
            if(lastIndex == -1)
                return;

            var boid = boids[lastIndex];
            Destroy(boid.gameObject);
        }

        public void RemoveBoid(Boid boid)
        {
            boids.Remove(boid);
            --boidCount;
        }

        private void Update()
        {
            if(!UnityEditor.EditorApplication.isPlaying)
                return;

            while(boidPrefab && param && boids.Count < boidCount)
                InstantiateBoid();
            while((!boidPrefab || !param || boids.Count > boidCount) && boids.Count > 0)
                DestroyBoid();
        }

#if UNITY_EDITOR
        [SerializeField]
        private bool editMode = false;

        private void DestroyBoidImmediate()
        {
            var lastIndex = boids.Count - 1;
            if(lastIndex == -1)
                return;

            var boid = boids[lastIndex];
            DestroyImmediate(boid.gameObject);
        }

        // Works inside the Editor
        private void OnRenderObject()
        {
            while(boidPrefab && param && boids.Count < boidCount)
                InstantiateBoid();
            while((!boidPrefab || !param || boids.Count > boidCount) && boids.Count > 0)
                DestroyBoidImmediate();

            if(!UnityEditor.EditorApplication.isPlaying && param && editMode)
                foreach(var boid in boids)
                    boid.Update();
        }

        private void OnDrawGizmos()
        {
            if(!param)
                return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * param.wall.scale);
        }
#endif
    }
}
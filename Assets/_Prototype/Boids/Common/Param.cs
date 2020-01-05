using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Boids
{
    [CreateAssetMenu(menuName = "Boid/Param")]
    public class Param : ScriptableObject
    {
        public Speed speed;
        public Neighbor neighbor;
        public Wall wall;
        public Shoal shoal;

        [Serializable]
        public class Speed
        {
            public float initial = 2f;
            public float min = 2f;
            public float max = 5f;
        }

        [Serializable]
        public class Neighbor
        {
            public float distance = 1f;
            public float Fov = 90f;
        }

        [Serializable]
        public class Wall
        {
            public float scale = 5f;
            public float distance = 3f;
            public float weight = 1f;
        }

        [Serializable]
        public class Shoal
        {
            public float seperationWeight = 5f;
            public float alignmentWeight = 2f;
            public float cohesionWeight = 3f;
        }
    }
}

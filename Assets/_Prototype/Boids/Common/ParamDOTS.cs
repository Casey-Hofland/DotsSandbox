using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Boids.DOTS
{
    public class Param : ScriptableObject
    {
        public Speed speed = new Speed()
        {
            initial = 2f
            , min = 2f
            , max = 5f
        };
        public Neighbor neighbor = new Neighbor()
        {
            distance = 1f
            , Fov = 90f
        };
        public Wall wall = new Wall()
        {
            scale = 5f
            , distance = 3f
            , weight = 1f
        };
        public Shoal shoal = new Shoal()
        {
            seperationWeight = 5f
            , alignmentWeight = 2f
            , cohesionWeight = 3f
        };

        [Serializable]
        public struct Speed
        {
            public float initial;
            public float min;
            public float max;
        }

        [Serializable]
        public struct Neighbor
        {
            public float distance;
            public float Fov;
        }

        [Serializable]
        public struct Wall
        {
            public float scale;
            public float distance;
            public float weight;
        }

        [Serializable]
        public struct Shoal
        {
            public float seperationWeight;
            public float alignmentWeight;
            public float cohesionWeight;
        }
    }
}

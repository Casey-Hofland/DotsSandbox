using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Boids.OOP
{
    [ExecuteAlways]
    public class Boid : MonoBehaviour
    {
        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }

        private Simulation simulation;
        private Param param = null;
        private Vector3 acceleration = Vector3.zero;
        private List<Boid> neighbors = new List<Boid>();

        public void Init(Simulation simulation, Param param)
        {
            this.simulation = simulation;
            this.param = param;

            Position = transform.position;
            Velocity = transform.forward * param.speed.initial;
        }

        public void Update()
        {
            if(!simulation || !param)
                return;

            // Simulation Updates
            UpdateNeighbors();
            UpdateWalls();

            // Neighbor Updates
            UpdateSeperation();
            UpdateAlignment();
            UpdateCohesion();

            // Self Updates
            UpdateMove();
        }

        private void UpdateNeighbors()
        {
            neighbors.Clear();

            var prodThres = Mathf.Cos(param.neighbor.Fov * Mathf.Deg2Rad);
            var distThres = param.neighbor.distance;

            foreach(var other in simulation.Boids)
            {
                if(other == this)
                    continue;

                var to = other.Position - Position;
                var dist = to.magnitude;
                if(dist < distThres)
                {
                    var dir = to.normalized;
                    var fwd = Velocity.normalized;
                    var prod = Vector3.Dot(fwd, dir);
                    if(prod > prodThres)
                        neighbors.Add(other);
                }
            }
        }

        private void UpdateWalls()
        {
            var scale = param.wall.scale * 0.5f;
            acceleration +=
                AccelerationAgainstWall(-scale - Position.x, Vector3.right) +
                AccelerationAgainstWall(-scale - Position.y, Vector3.up) +
                AccelerationAgainstWall(-scale - Position.z, Vector3.forward) +
                AccelerationAgainstWall(scale - Position.x, Vector3.left) +
                AccelerationAgainstWall(scale - Position.y, Vector3.down) +
                AccelerationAgainstWall(scale - Position.z, Vector3.back);

            Vector3 AccelerationAgainstWall(float distance, Vector3 direction)
            {
                return distance < param.wall.distance
                    ? direction * (param.wall.weight / Mathf.Abs(distance / param.wall.distance))
                    : Vector3.zero;
            }
        }

        private void UpdateSeperation()
        {
            if(neighbors.Count == 0)
                return;

            var force = Vector3.zero;
            foreach(var neighbor in neighbors)
                force += (Position - neighbor.Position).normalized;

            force /= neighbors.Count;
            acceleration += force * param.shoal.seperationWeight;
        }

        private void UpdateAlignment()
        {
            if(neighbors.Count == 0)
                return;

            var averageVelocity = Vector3.zero;
            foreach(var neighbor in neighbors)
                averageVelocity += neighbor.Velocity;

            averageVelocity /= neighbors.Count;
            acceleration += (averageVelocity - Velocity) * param.shoal.alignmentWeight;
        }

        private void UpdateCohesion()
        {
            if(neighbors.Count == 0)
                return;

            var averagePosition = Vector3.zero;
            foreach(var neighbor in neighbors)
                averagePosition += neighbor.Position;

            averagePosition /= neighbors.Count;
            acceleration += (averagePosition - Position) * param.shoal.cohesionWeight;
        }

        private void UpdateMove()
        {
            var deltaTime = Time.deltaTime;

            Velocity += acceleration * deltaTime;
            var direction = Velocity.normalized;
            var speed = Velocity.magnitude;

            Velocity = Mathf.Clamp(speed, param.speed.min, param.speed.max) * direction;
            Position += Velocity * deltaTime;

            var rotation = Quaternion.LookRotation(Velocity);
            transform.SetPositionAndRotation(Position, rotation);

            acceleration = Vector3.zero;
        }

        private void OnDestroy()
        {
            simulation.RemoveBoid(this);
        }
    }

}

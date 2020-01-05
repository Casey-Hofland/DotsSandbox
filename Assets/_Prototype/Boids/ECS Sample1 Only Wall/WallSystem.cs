using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Boids.DOTS.Sample1
{
    public class WallSystem : JobComponentSystem
    {
        [BurstCompile]
        struct WallSystemJob : IJobForEach<Translation, Acceleration>
        {
            // Data used by all jobs
            private float scale;
            private float threshold;
            private float weight;

            private float3 right;
            private float3 up;
            private float3 forward;
            private float3 left;
            private float3 down;
            private float3 back;

            public WallSystemJob(Param param)
            {
                scale = param.wall.scale * 0.5f;
                threshold = param.wall.distance;
                weight = param.wall.weight;

                right = new float3(1, 0, 0);
                up = new float3(0, 1, 0);
                forward = new float3(0, 0, 1);
                left = new float3(-1, 0, 0);
                down = new float3(0, -1, 0);
                back = new float3(0, 0, -1);
            }

            // Calculates how close the boid is to a wall and how that affects its acceleration
            float3 GetAccelerationAgainstWall(float distance, float3 direction, float threshold, float weight) =>
                distance < threshold
                ? direction * (weight / math.abs(distance / threshold))
                : float3.zero;

            // Magic!
            public void Execute([ReadOnly] ref Translation translation, [WriteOnly] ref Acceleration acceleration)
            {
                acceleration.Value +=
                    GetAccelerationAgainstWall(-scale - translation.Value.x, right, threshold, weight) +
                    GetAccelerationAgainstWall(-scale - translation.Value.y, up, threshold, weight) +
                    GetAccelerationAgainstWall(-scale - translation.Value.z, forward, threshold, weight) +
                    GetAccelerationAgainstWall(scale - translation.Value.x, left, threshold, weight) +
                    GetAccelerationAgainstWall(scale - translation.Value.y, down, threshold, weight) +
                    GetAccelerationAgainstWall(scale - translation.Value.z, back, threshold, weight);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            var job = new WallSystemJob(Bootstrap.Param);
            return job.Schedule(this, inputDependencies);
        }
    }
}

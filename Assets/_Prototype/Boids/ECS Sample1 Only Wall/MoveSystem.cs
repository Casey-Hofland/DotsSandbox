using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Boids.DOTS.Sample1
{
    [UpdateAfter(typeof(WallSystem))]
    public class MoveSystem : JobComponentSystem
    {
        [BurstCompile]
        struct MoveSystemJob : IJobForEach<Translation, Rotation, Velocity, Acceleration>
        {
            // Data used by all jobs
            public float deltaTime;
            public float minSpeed;
            public float maxSpeed;
            public float3 up;

            // Move every boid based on its acceleration.
            public void Execute([WriteOnly] ref Translation translation, [WriteOnly] ref Rotation rotation, ref Velocity velocity, ref Acceleration acceleration)
            {
                velocity.Value += acceleration.Value * deltaTime;

                var direction = math.normalize(velocity.Value);
                var speed = math.length(velocity.Value);
                velocity.Value = math.clamp(speed, minSpeed, maxSpeed) * direction;
                translation.Value += velocity.Value * deltaTime;
                rotation.Value = quaternion.LookRotationSafe(direction, up);

                acceleration.Value = float3.zero;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            var job = new MoveSystemJob()
            {
                deltaTime = Time.DeltaTime
                , minSpeed = Bootstrap.Param.speed.min
                , maxSpeed = Bootstrap.Param.speed.max
                , up = new float3(0, 1, 0)
            };
            return job.Schedule(this, inputDependencies);
        }
    }
}

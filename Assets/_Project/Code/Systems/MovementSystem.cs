using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public class MovementSystem : JobComponentSystem
{
    [BurstCompile]
    struct MovementJob : IJobForEach<Movement, PhysicsMass, Controls, PhysicsVelocity, Translation>
    {
        public void Execute([ReadOnly] ref Movement movement, ref PhysicsMass mass, [ReadOnly] ref Controls controls
            , [WriteOnly] ref PhysicsVelocity velocity, [ReadOnly] ref Translation translation)
        {
            // Make sure the actor doesn't fall over when moving.
            mass.InverseInertia[0] = 0f;
            mass.InverseInertia[2] = 0f;

            // Move the actor based on our inputs.
            var target = new float3()
            {
                x = translation.Value.x + controls.x
                , z = translation.Value.z + controls.z
            };

            var normalizedTarget = math.normalizesafe(target - translation.Value);
            var vel = new float2(normalizedTarget.x, normalizedTarget.z) * movement.moveSpeed;

            velocity.Linear.x = vel.x;
            velocity.Linear.z = vel.y;

            // TODO:
            // Cap max speed
            if(math.SQRT2 * velocity.Linear.x * velocity.Linear.z < movement.maxSpeed)
            {
                //velocity.Linear = clamp some value;
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var movementJob = new MovementJob();
        return movementJob.Schedule(this, inputDependencies);
    }
}
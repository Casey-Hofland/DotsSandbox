using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using Debug = UnityEngine.Debug;
using Input = UnityEngine.Input;

public class JumpSystem : JobComponentSystem
{
    [BurstCompile]
    struct JumpJob : IJobForEach<Translation, Jump, PhysicsCollider, PhysicsVelocity, PhysicsMass>
    {
        public bool jumpPressed;

        [ReadOnly]
        public CollisionWorld collisionWorld;

        // Cast a ray to check if this actor is grounded.
        private bool IsGrounded(Translation translation, float groundCheckDistance, PhysicsCollider collider)
        {
            // Hardcoded filter for testing purposes, might not behave as expected in different scenes.
            var filter = new CollisionFilter()
            {
                BelongsTo = 0b_0100
                , CollidesWith = 0b_0010
                , GroupIndex = 1
            };

            var aabb = collider.Value.Value.CalculateAabb();    // <= could be cached once on initialization.
            var feetPosition = translation.Value + new float3(0f, aabb.Min.y + Math.Constants.Eps * 2, 0f);
            var groundCheckPosition = feetPosition - new float3(0f, groundCheckDistance, 0f);

            var raycast = new RaycastInput()
            {
                Start = feetPosition
                , End = groundCheckPosition
                , Filter = filter
            };

            return collisionWorld.CastRay(raycast);
        }

        // Jump based on if the jump button was pressed, if the actor is grounded and on the actors jump force.
        public void Execute([ReadOnly] ref Translation translation, [ReadOnly] ref Jump jump, [ReadOnly] ref PhysicsCollider collider
            , [WriteOnly] ref PhysicsVelocity velocity, ref PhysicsMass mass)
        {
            if(jumpPressed && IsGrounded(translation, jump.groundCheckDistance, collider))
            {
                var impulse = new float3(0f, jump.force, 0f);
                velocity.Linear.y = 0;
                velocity.ApplyLinearImpulse(mass, impulse);
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var jumpJob = new JumpJob()
        {
            jumpPressed = Input.GetButtonDown("Jump")
            , collisionWorld = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld.CollisionWorld
        };

        // We need the dependancies from the PhysicsWorld as well in order to accurately check if our actor is grounded.
        var combinedDependencies = JobHandle.CombineDependencies(
            World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>().FinalJobHandle
            , inputDependencies
            );

        return jumpJob.Schedule(this, combinedDependencies);
    }
}
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

// Receives Input from the Horizontal and Vertical inputs. For simplification we are making this part of the component system (don't use for long-term solutions).
public class ControlsSystem : JobComponentSystem
{
    [BurstCompile]
    struct ControlsJob : IJobForEach<Controls>
    {
        public float x;
        public float z;

        public void Execute([WriteOnly] ref Controls controls)
        {
            controls.x = x;
            controls.z = z;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new ControlsJob()
        {
            x = Input.GetAxis("Horizontal")
            , z = Input.GetAxis("Vertical")
        };
        
        return job.Schedule(this, inputDependencies);
    }
}
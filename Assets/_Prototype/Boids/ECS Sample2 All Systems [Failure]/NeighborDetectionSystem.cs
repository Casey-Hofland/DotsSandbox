using Boids.DOTS.Sample1;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Boids.DOTS.Sample2
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(BoidsSystemGroup))]
    public class NeighborDetectionSystem : JobComponentSystem
    {
        [BurstCompile]
        struct NeighborDetectionSystemJob : IJobForEachWithEntity_EBCC<NeighborsEntityBuffer, Translation, Velocity>
        {
            [ReadOnly] public float distanceThreshold;
            [ReadOnly] public float productThreshold;
            [ReadOnly] public ComponentDataFromEntity<Translation> translationFromEntity;
            [NativeDisableParallelForRestriction]
            [ReadOnly] public BufferFromEntity<NeighborsEntityBuffer> neighborsFromEntity;
            [ReadOnly] public NativeArray<Entity> allBoids;

            public void Execute(Entity entity, int index, DynamicBuffer<NeighborsEntityBuffer> buffer, [ReadOnly] ref Translation translation, [ReadOnly] ref Velocity velocity)
            {
                buffer.Clear();

                float3 forward = math.normalize(velocity.Value);

                for(int i = 0; i < allBoids.Length; ++i)
                {
                    var neighbor = allBoids[i];
                    if(neighbor == entity)
                        continue;

                    float3 neighborPosition = translationFromEntity[neighbor].Value;
                    var to = neighborPosition - translation.Value;
                    var distance = math.length(to);

                    if(distance < distanceThreshold)
                    {
                        var direction = math.normalize(to);
                        var product = math.dot(direction, forward);

                        if(product < productThreshold)
                            neighborsFromEntity[entity].Add(new NeighborsEntityBuffer { Value = neighbor });
                    }
                }
            }
        }

        private NativeArray<Entity> allBoids;
        protected override void OnCreate()
        {
            base.OnCreate();
            var entityQuery = GetEntityQuery(typeof(NeighborsEntityBuffer), typeof(Translation), typeof(Velocity));
            allBoids = entityQuery.ToEntityArray(Allocator.TempJob);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new NeighborDetectionSystemJob()
            {
                distanceThreshold = Bootstrap.Param.neighbor.distance
                , productThreshold = math.cos(math.radians(Bootstrap.Param.neighbor.Fov))
                , translationFromEntity = GetComponentDataFromEntity<Translation>(true)
                , neighborsFromEntity = GetBufferFromEntity<NeighborsEntityBuffer>(false)
                , allBoids = allBoids
            };
            return job.Schedule(this, inputDeps);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            allBoids.Dispose();
        }

        /*
        [BurstCompile]
        struct NeighborDetectionSystemJob : IJobForEachWithEntity<Translation, Velocity>
        {
            [ReadOnly] private readonly float productThreshold;
            [ReadOnly] private readonly float distanceThreshold;
            [ReadOnly] public ComponentDataFromEntity<Translation> translationFromEntity;
            [ReadOnly] public BufferFromEntity<NeighborsEntityBuffer> neighborsFromEntity;
            [ReadOnly] public NativeArray<Entity> allBoids;

            public NeighborDetectionSystemJob(Param param, ComponentDataFromEntity<Translation> componentData, BufferFromEntity<NeighborsEntityBuffer> buffer, NativeArray<Entity> allBoids)
            {
                productThreshold = math.cos(math.radians(param.neighbor.Fov));
                distanceThreshold = param.neighbor.distance;
                translationFromEntity = componentData;
                neighborsFromEntity = buffer;
                this.allBoids = allBoids;
            }

            public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref Velocity velocity)
            {
                neighborsFromEntity[entity].Clear();

                float3 position = translation.Value;
                float3 forward = math.normalize(velocity.Value);

                for(int i = 0; i < allBoids.Length; ++i)
                {
                    var neighbor = allBoids[i];
                    if(neighbor == entity)
                        continue;

                    float3 neighborPosition = translationFromEntity[neighbor].Value;
                    var to = neighborPosition - position;
                    var distance = math.length(to);

                    if(distance < distanceThreshold)
                    {
                        var direction = math.normalize(to);
                        var product = math.dot(direction, forward);

                        if(product < productThreshold)
                            neighborsFromEntity[entity].Add(new NeighborsEntityBuffer { Value = neighbor });
                    }
                }
            }
        }

        private EntityQuery entityQuery;
        protected override void OnCreate()
        {
            base.OnCreateManager();
            entityQuery = GetEntityQuery(typeof(Translation), typeof(Velocity), typeof(NeighborsEntityBuffer));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new NeighborDetectionSystemJob(
                Bootstrap.Param
                , GetComponentDataFromEntity<Translation>(true)
                , GetBufferFromEntity<NeighborsEntityBuffer>(false)
                , entityQuery.ToEntityArray(Allocator.TempJob)
            );
            return job.Schedule(this, inputDeps);
        }
        */
    }
}

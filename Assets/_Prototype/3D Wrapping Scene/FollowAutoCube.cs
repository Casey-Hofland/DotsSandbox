using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;

public class FollowAutoCube : MonoBehaviour
{
    [SerializeField]
    private Vector3 offset = default;

    private EntityManager entityManager;
    private Entity autoCube;

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var entityQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<AutoMove>());
        autoCube = entityQuery.GetSingletonEntity();
    }

    private void LateUpdate()
    {
        var autoCubeTransform = entityManager.GetComponentData<LocalToWorld>(autoCube);
        transform.position = (Vector3)autoCubeTransform.Position + (Quaternion)autoCubeTransform.Rotation * offset;
    }
}

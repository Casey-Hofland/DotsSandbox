#if UNITY_EDITOR

using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Boids.Unity
{
    [RequiresEntityConversion]
    [AddComponentMenu("DOTS Samples/Boids/BoidSchool")]
    [ConverterVersion("macton", 4)]
    public class BoidSchoolAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        [SerializeField] private GameObject prefab = null;
        [SerializeField] private float initialRadius = 0.0f;
        [SerializeField] private int count = 0;

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(prefab);
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new BoidSchool
            {
                prefab = conversionSystem.GetPrimaryEntity(prefab)
                , initialRadius = initialRadius
                , count = count
            });
        }
    }
}

#endif
using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
[GenerateAuthoringComponent]
public struct Controls : IComponentData
{
    [HideInInspector]
    public float x;
    [HideInInspector]
    public float z;
}

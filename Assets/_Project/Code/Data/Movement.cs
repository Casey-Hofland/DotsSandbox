using System;
using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct Movement : IComponentData
{
    public float moveSpeed;
    public float maxSpeed;
}

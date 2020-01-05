using System;
using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct Jump : IComponentData
{
    public float force;
    public float groundCheckDistance;
}

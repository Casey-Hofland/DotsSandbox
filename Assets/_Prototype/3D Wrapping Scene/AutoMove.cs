using System;
using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct AutoMove : IComponentData
{
    public float speed;
}

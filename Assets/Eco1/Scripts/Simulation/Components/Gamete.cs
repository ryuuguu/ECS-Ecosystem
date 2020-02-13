using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;


[GenerateAuthoringComponent]
public struct Gamete : IComponentData {
    public Entity pollen;
    public bool fertilized;
}
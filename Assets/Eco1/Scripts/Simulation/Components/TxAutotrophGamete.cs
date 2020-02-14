using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;


[GenerateAuthoringComponent]
public struct TxAutotrophGamete : IComponentData {
    public TxAutotrophChrome1AB txAutotrophChrome1AB;
    public TxAutotrophChrome2AB txAutotrophChrome2AB;
    public bool isFertilized;
}
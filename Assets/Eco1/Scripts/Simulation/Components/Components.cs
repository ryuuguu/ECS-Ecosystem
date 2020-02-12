﻿using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace EcoSim {

    
    #region Misc
    
    public struct RandomComponent : IComponentData {
        public Unity.Mathematics.Random random;
    }
    
    #endregion
    
    #region Life
    
    public struct  EnergyStore : IComponentData {
        public float Value;
    }


    public struct Unfertilized : IComponentData {
        public PhysicsCollider femaleCollider; //should be moved out
        public Entity pollen;
        public Random randoml;
    }
    
    #endregion
    
    #region Taxon Autotroph
    
    public struct Shade : IComponentData {
        public float Value;
    }
    
    public struct TxAutotrophParts : IComponentData {
        public Entity stem;
        public Entity leaf;
        public Entity petal0;
        public Entity petal1;
        public Entity petal2;
        public Entity petal3;
        public Entity petal4;
        public Entity petal5;
        public Entity pollen;
    }


    public struct TxAutotrophPhenotype : IComponentData {
        public float leaf;
        public float height;
        public float seed;
        public float age;
    }
   
    public struct TxAutotrophSprout : IComponentData {
        public float3 location;
        public float energy;
    }

    #endregion 
}

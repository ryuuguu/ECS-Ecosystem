﻿using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace EcoSim {

    
    #region Misc
    
    
    #endregion
    
    #region Life
    
    public struct  EnergyStore : IComponentData {
        public float Value;
    }


    public struct Age : IComponentData {
        public float Value;
    }
    #endregion
    
    #region Taxon Autotroph
   
    
    public struct TxAutotrophMaintenance : IComponentData {
        public float baseValue;
        public float leafMultiple;
        public float heightMultiple;
        public float ageMultiple;
    }

    public struct TxAutotrophParts : IComponentData {
        public Entity stem;
        public float3 stemScale;
        public Entity leaf;
        public float3 leafScale;
        public Entity seedPod;
        public float3 seedPodScale;
    }
    
    public struct Leaf : IComponentData {
        public float Value;
    }
    
    public struct Height : IComponentData {
        public float Value;
    }
    
    public struct Seed : IComponentData {
        public float Value;
    }

    public struct TxAutotrophGenome : IComponentData {
        public float nrg2Leaf; 
        public float nrg2Seed;
        public float nrg2Height;
        public float nrg2Storage;
        public float maxLeaf;  //max leaf energy
        public float maxHeight; //max Height
        public float seedSize;  //Size of one seed
        public float ageRate;
    }

    
    public struct TxAutotrophSprout : IComponentData {
        public float3 location;
        public float energy;
    }

    #endregion 
}

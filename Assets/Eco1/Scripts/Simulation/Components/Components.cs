using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

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
        //public Entity seedPod;

    }


    public struct TxAutotrophPhenotype : IComponentData {
        public float leaf;
        public float height;
        public float seed;
        public float age;
    }
    
    [System.Serializable]
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

    [System.Serializable]
    public struct TxAutotrophChrome1A {
        public TxAutotrophChrome1 Value;
    }

    [System.Serializable]
    public struct TxAutotrophChrome1B {
        public TxAutotrophChrome1 Value;
    }

    [System.Serializable]
    public struct TxAutotrophChrome1W {
        public TxAutotrophChrome1 Value;
    }

    [System.Serializable]
    public struct TxAutotrophChrome1 {
        public float nrg2Leaf; 
        public float nrg2Seed;
        public float nrg2Height;
        public float nrg2Storage;
        public float maxLeaf;  //max leaf energy
        public float maxHeight; //max Height
        public float seedSize;  //Size of one seed
        public float ageRate;
    }
    
    [System.Serializable]
    public struct TxAutotrophColorGenome : IComponentData {
        public float r0; 
        public float g0;
        public float b0;
        public float r1;
        public float g1;  
        public float b1; 
        public float r2;  
        public float g2;
        public float b2;
        public float dr0; 
        public float dg0;
        public float db0;
        public float dr1;
        public float dg1;  
        public float db1; 
        public float dr2;  
        public float dg2;
        public float db2;
        
        
    }
    
    public struct TxAutotrophSprout : IComponentData {
        public float3 location;
        public float energy;
    }

    #endregion 
}

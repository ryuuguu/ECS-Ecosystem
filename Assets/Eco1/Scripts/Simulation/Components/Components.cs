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
        public float stemScale;
        //public Entity leaf;
        public float leafScale;
        //public Entity seedPod;
        public float seedPodScale;
    }


    public struct TxAutotrophPhenotype : IComponentData {
        public float leaf;
        public float height;
        public float seed;
        public float age;
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

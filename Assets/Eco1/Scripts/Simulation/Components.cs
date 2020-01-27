using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

namespace EcoSim {

    
    #region Misc
    
    public struct TxInitialize : IComponentData{}
    
    #endregion
    
    #region Life
    
    public struct  EnergyStore : IComponentData {
        public float Value;
    }
    #endregion
    
    #region Taxon Autotroph
   
    public struct TxAutotroph : IComponentData{}
    
    public struct TxAutotrophMaintenance : IComponentData {
        public float baseValue;
        public float leafMultiple;
        public float heightMultiple;
    }

    public struct TxAutotrophParts : IComponentData {
        public Entity stem;
        public Entity leaf;
        public Entity seedPod;
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
    }

    
    public struct LeafMesh : IComponentData{}
    public struct SeedPodMesh : IComponentData{}
    #endregion 
}

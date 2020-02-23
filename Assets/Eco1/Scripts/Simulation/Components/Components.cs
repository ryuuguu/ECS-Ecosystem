
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


    public struct DebugDistances : IComponentData {
        public float seedRange;
        public float pollenRange;
    }
    
    #endregion
    
    #region Taxon Autotroph
    
    public struct Shade : IComponentData {
        public float Value;
    }

    
    public struct TxAutotrophMeshes : IComponentData {
        public Entity stem;
        public Entity leaf;
        public Entity petal0;
        public Entity petal1;
        public Entity petal2;
        public Entity petal3;
        public Entity petal4;
        public Entity petal5;
       // public Entity pollen;
    }
    

    public struct TxAutotrophParts : IComponentData {
        public Entity pollen;
    }
    
    public struct TxAutotrophPetal : IComponentData {
        public Entity Value;
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

    public struct TxAutotrophCacheYPos : IComponentData {
        public float y;
    }
    
    #endregion 
}

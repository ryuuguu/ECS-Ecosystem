using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

namespace EcoSim {

   
    
    #region Life
    
    public struct  EnergyStore : IComponentData {
        public float Value;
    }
    
    public struct BaseMaintenanceEnergy {
        public float Value;  
    }

    #endregion
    
    #region Taxon Autotroph
    
    public struct TxAutotroph : IComponentData{}
    
    public struct Leaf : IComponentData {
        public float Value;
    }

    public struct SeedEnergy : IComponentData {
        public float Value;
    }

    public struct Height : IComponentData {
        public float Value;
    }

    public struct Genome : IComponentData {
        public float nrg2Leaf; //% of energy to leafs
        public float nrg2Seed;
        public float nrg2Height;
        public float maxLeaf;  //max leaf energy
        public float maxHieght; //max Height
        public float seedSize;  //Size of one seed
    }
    #endregion 
}

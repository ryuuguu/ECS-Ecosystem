using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

/* 
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
*/
[System.Serializable]
public struct TxAutotrophChrome1A : IComponentData{
    public TxAutotrophChrome1 Value;
}

[System.Serializable]
public struct TxAutotrophChrome1B : IComponentData{
    public TxAutotrophChrome1 Value;
}

[System.Serializable]
public struct TxAutotrophChrome1W : IComponentData{
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
    /// <summary>Returns the float element at a specified index.</summary>
    unsafe public float this[int index]
    {
        get
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if ((uint)index >= 24)
                throw new System.ArgumentException("get index must be between[0...23] was "+ index );
#endif
            fixed (TxAutotrophChrome1* array = &this) { return ((float*)array)[index]; }
        }
        set
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if ((uint)index >= 24)
                throw new System.ArgumentException("set index must be between[0...23] was " + index);
#endif
            fixed (float* array = &nrg2Leaf) { array[index] = value; }
        }
    }

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


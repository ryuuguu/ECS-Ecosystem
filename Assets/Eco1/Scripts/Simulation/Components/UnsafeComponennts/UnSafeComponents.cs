﻿using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Assertions.Comparers;
using UnityEngine.UIElements;


[System.Serializable]
public struct TxAutotrophChrome1AB : IComponentData{
    public TxAutotrophChrome1 ValueA;
    public TxAutotrophChrome1 ValueB;

    public TxAutotrophChrome1W GetChrome1W() {
        var result = new TxAutotrophChrome1W();
        for (int i = 0; i < TxAutotrophChrome1.LENGTH; i++) {
            result.Value[i] = (ValueA[i] + ValueB[i])/2;
        }
        return result;
    }
    
    public TxAutotrophChrome1AB Copy () {
        var result = new TxAutotrophChrome1AB();
        for (int i = 0; i < TxAutotrophChrome1.LENGTH; i++) {
            result.ValueA[i] = ValueA[i];
            result.ValueB[i] = ValueB[i];
        }
        return result;
    }

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
    public float pollenRange;

    public const int LENGTH = 9;
    
    /// <summary>Returns the float element at a specified index.</summary>
    unsafe public float this[int index]
    {
        get
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if ((uint)index >= LENGTH)
                throw new System.ArgumentException("get index must be between[0...23] was "+ index );
#endif
            fixed (TxAutotrophChrome1* array = &this) { return ((float*)array)[index]; }
        }
        set
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if ((uint)index >= LENGTH)
                throw new System.ArgumentException("set index must be between[0...23] was " + index);
#endif
            fixed (float* array = &nrg2Leaf) { array[index] = value; }
        }
    }

    public TxAutotrophChrome1 Copy() {
        var result = new TxAutotrophChrome1();
        for (int i = 0; i < LENGTH; i++) {
            result[i] = this[i];
        }
        return result;
    }
}
   


[System.Serializable]
public struct TxAutotrophChrome2AB : IComponentData{
    public TxAutotrophChrome2 ValueA;
    public TxAutotrophChrome2 ValueB;

    public TxAutotrophChrome2W GetChrome1W() {
        var result = new TxAutotrophChrome2W();
        for (int i = 0; i < TxAutotrophChrome2.LENGTH; i++) {
            result.Value[i] = (ValueA[i] + ValueB[i])/2;
        }
        return result;
    }
    
    public TxAutotrophChrome2AB Copy () {
        var result = new TxAutotrophChrome2AB();
        for (int i = 0; i < TxAutotrophChrome2.LENGTH; i++) {
            result.ValueA[i] = ValueA[i];
            result.ValueB[i] = ValueB[i];
        }
        return result;
    }

}

[System.Serializable]
public struct TxAutotrophChrome2W : IComponentData{
    public TxAutotrophChrome2 Value;
}


[System.Serializable]
public struct TxAutotrophChrome2 : IComponentData {
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
        
    public const int LENGTH = 18;
    
    /// <summary>Returns the float element at a specified index.</summary>
    unsafe public float this[int index]
    {
        get
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if ((uint)index >= LENGTH)
                throw new System.ArgumentException("get index must be between[0...23] was "+ index );
#endif
            fixed (TxAutotrophChrome2* array = &this) { return ((float*)array)[index]; }
        }
        set
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if ((uint)index >= LENGTH)
                throw new System.ArgumentException("set index must be between[0...23] was " + index);
#endif
            fixed (float* array = &r0) { array[index] = value; }
        }
    }

    public TxAutotrophChrome2 Copy() {
        var result = new TxAutotrophChrome2();
        for (int i = 0; i < LENGTH; i++) {
            result[i] = this[i];
        }
        return result;
    }     
}


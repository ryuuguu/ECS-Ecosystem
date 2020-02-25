
using Unity.Entities;
using Unity.Mathematics;



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

    public TxAutotrophChrome1AB RandomRange (ref Random random) {
        var result = new TxAutotrophChrome1AB();
        for (int i = 0; i < TxAutotrophChrome1.LENGTH; i++) {
            result.ValueA[i] =random.NextFloat(ValueA[i],ValueB[i]);
            result.ValueB[i] = random.NextFloat(ValueA[i],ValueB[i]);
        }
        return result;
    }
    
    public TxAutotrophChrome1 Crossover(ref Random random) {
        int a = random.NextInt(0, TxAutotrophChrome1.LENGTH);
        int b = random.NextInt(0, TxAutotrophChrome1.LENGTH);
        if (a > b) {
            var temp = a;
            a = b;
            b = temp;
        }
        var result = new TxAutotrophChrome1();
        for (int i = 0; i < a; i++) {
            result[i] = ValueA[i];
        }
        for (int i = a; i < b; i++) {
            result[i] = ValueB[i];
        }
        for (int i = b; i < TxAutotrophChrome1.LENGTH; i++) {
            result[i] = ValueA[i];
        }

        return result;
    }
    
    public TxAutotrophChrome1 MaxNorm() {
        var result = new TxAutotrophChrome1();
        float max = 0;
        for (int i = 0; i < TxAutotrophChrome1.LENGTH; i++) {
            max = math.max( ValueA[i]+ ValueB[i],max);
        }

        if (max == 0)
            return result;
        for (int i = 0; i < TxAutotrophChrome1.LENGTH; i++) {
            result[i] =  (ValueA[i]+ ValueB[i])/max;
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
                throw new System.ArgumentException("get index must be between[0...9] was "+ index );
#endif
            fixed (TxAutotrophChrome1* array = &this) { return ((float*)array)[index]; }
        }
        set
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if ((uint)index >= LENGTH)
                throw new System.ArgumentException("set index must be between[0...9] was " + index);
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
    public const float MAX = 100;
    public const float MIN = 0;
    
    public TxAutotrophChrome2AB Copy () {
        var result = new TxAutotrophChrome2AB();
        for (int i = 0; i < TxAutotrophChrome2.LENGTH; i++) {
            result.ValueA[i] = ValueA[i];
            result.ValueB[i] = ValueB[i];
        }
        return result;
    }

    public TxAutotrophChrome2 Crossover(ref Random random) {
        int a = random.NextInt(0, TxAutotrophChrome2.LENGTH);
        int b = random.NextInt(0, TxAutotrophChrome2.LENGTH);
        if (a > b) {
            var temp = a;
            a = b;
            b = temp;
        }
        var result = new TxAutotrophChrome2();
        for (int i = 0; i < a; i++) {
            result[i] = ValueA[i];
        }
        for (int i = a; i < b; i++) {
            result[i] = ValueB[i];
        }
        for (int i = b; i < TxAutotrophChrome2.LENGTH; i++) {
            result[i] = ValueA[i];
        }
        return result;
    }

    public TxAutotrophChrome2 MaxNorm() {
        var result = new TxAutotrophChrome2();
        for (int i = 0; i < TxAutotrophChrome2.LENGTH; i++) {
           result[i] =  (ValueA[i]+ ValueB[i])/MAX;
        }
        return result; 
    }
    
    public float DistanceSq(TxAutotrophChrome2AB other, float maxDistanceSq ) {
        float sum = 0;
        for (int i = 0; i < TxAutotrophChrome2.LENGTH; i++) {
            var t=   (ValueA[i] + ValueB[i]) - (other.ValueA[i] + other.ValueB[i]);
            sum += t * t;
        }
        var d = math.min(1,math.max(0,  sum/maxDistanceSq));
        return d;
    }
}


[System.Serializable]
public struct TxAutotrophChrome2Stats : IComponentData{
    public TxAutotrophChrome2 total;

    public TxAutotrophChrome2Stats Add(TxAutotrophChrome2AB other) {
        var result = new TxAutotrophChrome2Stats();
        for (int i = 0; i < TxAutotrophChrome2.LENGTH; i++) {
            result.total[i] = (total[i] + other.ValueA[i] + other.ValueB[i]) ;
        }
        return result;
    }
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
    public float r3; 
    public float g3;
    public float b3;
    public float r4;
    public float g4;  
    public float b4; 
    public float r5;  
    public float g5;
    public float b5;
        
    public const int LENGTH = 18;
    
    /// <summary>Returns the float element at a specified index.</summary>
    unsafe public float this[int index]
    {
        get
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if ((uint)index >= LENGTH)
                throw new System.ArgumentException("get index must be between[0...18] was "+ index );
#endif
            fixed (TxAutotrophChrome2* array = &this) { return ((float*)array)[index]; }
        }
        set
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if ((uint)index >= LENGTH)
                throw new System.ArgumentException("set index must be between[0...18] was " + index);
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


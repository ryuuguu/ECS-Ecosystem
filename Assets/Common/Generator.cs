using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Generator {

	static public int SelectRandom(IEnumerable<float> frequencies){
		int result = 0;
		float total = frequencies.Sum ();
		float rValue = Random.value * total;
		float incr = 0;
		foreach (var f in frequencies) {
			if (rValue < f + incr)
				return result; 
			incr += f;
			result++;
		}
		return result = frequencies.Count() - 1;
	}


    //used if freqPairs will not be modfified
	public static string SelectRandomOptimized(List<FreqPair> freqPairs){
		var freqs = freqPairs.Select(x => x.freq);
		var index = SelectRandom (freqs);
		var result = freqPairs[index].name;
		
		return result;
	}
	
    //
    public static string SelectRandom(ref List<FreqPair> freqPairs, bool remove = true) {
        Dictionary<string, float> totals = new Dictionary<string, float>();
        foreach (var fp in freqPairs) {
            if (totals.ContainsKey(fp.name)) {
                totals[fp.name] += fp.freq;
            } else {
                totals[fp.name] = fp.freq;
            }
        }
        freqPairs.Clear();
        foreach (var vp in totals) {
            freqPairs.Add(new FreqPair(vp.Key, vp.Value));
        }
        var freqs = freqPairs.Select(x => x.freq);
        var index = SelectRandom(freqs);
        var result = freqPairs[index].name;
        if (remove) {
            freqPairs.RemoveAt(index);
        }
        return result;
    }
    
    [System.Serializable]
	public class FreqPair{
		public string name;
		public float freq;

		public FreqPair(){}
        public FreqPair(FreqPair fp){
            name = fp.name;
            freq = fp.freq;
        }
		public FreqPair(string aName, float aFreq){
			name = aName;
			freq = aFreq;
        }
		
		public override string ToString (){
			return name + " : " + freq;
		}
	}

}

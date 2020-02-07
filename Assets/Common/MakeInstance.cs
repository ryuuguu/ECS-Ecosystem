using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MakeInstance : MonoBehaviour {

    [ContextMenu("MakeInstance")]
    public void Make() {
        var x = Instantiate<GameObject>(gameObject);
        x.name += "Inst";
    }
   
}

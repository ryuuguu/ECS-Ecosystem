using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class DevUitls : MonoBehaviour
{
    public Vector3 globalPos;
    public bool debug = false;
    public bool set = false;

    private void Update() {
        if(debug){
            globalPos = transform.position;
            debug= false;
        }

        if (set) {
            transform.position = globalPos ;
            set = false;
        }
    }
}

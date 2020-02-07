using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Camera))]
public class CameraDebug : MonoBehaviour {

    public float aspect;
    
    private Camera cam;
    void Start() { 
        cam = GetComponent<Camera>();
    }

    void Update() {
        aspect = cam.aspect;
    }
    
    
    
}

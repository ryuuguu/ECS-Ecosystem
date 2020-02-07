using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
public class MeshInfo : MonoBehaviour {
    public int verticesCount;
    public int triangleCount;
    public int normalsCount;
    public int tangentsCount;
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;
    public Vector3 boundsCenter;
    public Vector3 boundsMax;
    public Vector3 boundsMin;


    public void Start() {
        GetInfo();
    }

    public void GetInfo() {
        var mesh = GetComponent<MeshFilter>().mesh;
        boundsCenter = mesh.bounds.center;
        boundsMax = mesh.bounds.max;
        boundsMin = mesh.bounds.min;
        

        verticesCount = mesh.vertexCount;
        triangleCount= mesh.triangles.Length;
        normalsCount = mesh.normals.Length;
        tangentsCount = mesh.tangents.Length;
        minY = minX = float.MaxValue;
        maxY = maxX = float.MinValue;
        foreach (var v in mesh.vertices) {
            minX = Mathf.Min(minX, v.x);
            maxX = Mathf.Max(maxX, v.x);
            minY = Mathf.Min(minY, v.y);
            maxY = Mathf.Max(maxY, v.y);
        }
    }
    
    
}

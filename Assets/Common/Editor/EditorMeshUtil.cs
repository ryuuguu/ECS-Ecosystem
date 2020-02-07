using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EditorMeshUtil : MonoBehaviour
{
    [MenuItem("GameObject/SaveMesh")]
    static  void MakeMesh() {
        var go = Selection.activeGameObject;
        var mf = go.GetComponent<MeshFilter>().mesh;
        AssetDatabase.CreateAsset(mf , "Assets/ATest/"+go.name +"mesh.asset");
        AssetDatabase.SaveAssets();
    }


}

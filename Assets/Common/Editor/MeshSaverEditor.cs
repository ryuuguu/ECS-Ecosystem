using UnityEditor;

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class MeshSaverEditor {

	[MenuItem("CONTEXT/MeshFilter/Save Mesh...")]
	public static void SaveMeshInPlace (MenuCommand menuCommand) {
		MeshFilter mf = menuCommand.context as MeshFilter;
		Mesh m = mf.sharedMesh;
		SaveMesh(m, m.name, false, true);
	}

	[MenuItem("CONTEXT/MeshFilter/Save Mesh As New Instance...")]
	public static void SaveMeshNewInstanceItem (MenuCommand menuCommand) {
		MeshFilter mf = menuCommand.context as MeshFilter;
		Mesh m = mf.sharedMesh;
		SaveMesh(m, m.name, true, true);
	}

	public static void SaveMesh (Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh) {
		string path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", name, "asset");
		if (string.IsNullOrEmpty(path)) return;
        
		path = FileUtil.GetProjectRelativePath(path);

		Mesh meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;

        if (optimizeMesh)
		     MeshUtility.Optimize(meshToSave);
        
		AssetDatabase.CreateAsset(meshToSave, path);
		AssetDatabase.SaveAssets();
	}

    [MenuItem("CONTEXT/MeshFilter/WeldMesh")]
    public static void WeldMesh(MenuCommand menuCommand) {
        MeshFilter mf = menuCommand.context as MeshFilter;
        var mesh = mf.mesh;
        var mw = new B83.MeshHelper.MeshWelder();
        mw.Weld(mesh);
    }


    [MenuItem("CONTEXT/MeshFilter/OptimizeMesh")]
    public static void OptimizeMesh(MenuCommand menuCommand) {
        MeshFilter mf = menuCommand.context as MeshFilter;
        var mesh =mf.mesh;
        MeshUtility.Optimize(mesh);
    }
/*
    [MenuItem("CONTEXT/MeshFilter/SimplifyMesh")]
    public static void SimplifyMesh(MenuCommand menuCommand) {
        MeshFilter mf = menuCommand.context as MeshFilter;
        var mesh = mf.mesh;
       

        float quality = 0.5f;
        var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
        meshSimplifier.Initialize(mesh);
        meshSimplifier.SimplifyMesh(quality);
        mesh = meshSimplifier.ToMesh();
    }
*/

}

/*using UnityEngine;


public static void WeldVertices(Mesh aMesh, float aMaxDelta = 0.001f) {
    var verts = aMesh.vertices;
    var normals = aMesh.normals;
    var uvs = aMesh.uv;
    List<int> newVerts = new List<int>();
    int[] map = new int[verts.Length];
    // create mapping and filter duplicates.
    for (int i = 0; i < verts.Length; i++) {
        var p = verts[i];
        var n = normals[i];
        var uv = uvs[i];
        bool duplicate = false;
        for (int i2 = 0; i2 < newVerts.Count; i2++) {
            int a = newVerts[i2];
            if (
                (verts[a] - p).sqrMagnitude <= aMaxDelta && // compare position
                Vector3.Angle(normals[a], n) <= aMaxDelta && // compare normal
                (uvs[a] - uv).sqrMagnitude <= aMaxDelta // compare first uv coordinate
                ) {
                map[i] = i2;
                duplicate = true;
                break;
            }
        }
        if (!duplicate) {
            map[i] = newVerts.Count;
            newVerts.Add(i);
        }
    }
    // create new vertices
    var verts2 = new Vector3[newVerts.Count];
    var normals2 = new Vector3[newVerts.Count];
    var uvs2 = new Vector2[newVerts.Count];
    for (int i = 0; i < newVerts.Count; i++) {
        int a = newVerts[i];
        verts2[i] = verts[a];
        normals2[i] = normals[a];
        uvs2[i] = uvs[a];
    }
    // map the triangle to the new vertices
    var tris = aMesh.triangles;
    for (int i = 0; i < tris.Length; i++) {
        tris[i] = map[tris[i]];
    }
    aMesh.vertices = verts2;
    aMesh.normals = normals2;
    aMesh.uv = uvs2;
    aMesh.triangles = tris;
}
*/

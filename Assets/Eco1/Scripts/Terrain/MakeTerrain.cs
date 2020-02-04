using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class MakeTerrain : MonoBehaviour {
    //public TerrainData terrainData;
    public Terrain terrain;
    public Environment environment;

    [ContextMenu("make Terrain")]
    public void MakeTerrainSine() {
        var td = terrain.terrainData;
        var size = td.heightmapResolution - 1;
        var heights = new float[size,size];


        float maxlight =float.NegativeInfinity, minlight = float.PositiveInfinity ;
        for (int i = 0; i< size; i++){
            for (int j = 0; j < size; j++) {
                var light = Environment.LightEnergy(new float3(i, 0, j),
                    environment.environmentSettingsInput.environmentConsts.ambientLight,
                    environment.environmentSettingsInput.environmentConsts.variableLight
                );
                maxlight = Mathf.Max(light, maxlight);
                minlight = Mathf.Min(light, minlight);
                heights[i, j] = light;
            }
        }
        
        var range = maxlight - minlight; 
        Debug.Log(""+maxlight +" : " + minlight+" : " + range);
        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                heights[i, j] = (heights[i, j] - minlight) / range;

            }
        }

        td.SetHeights(0,0,heights);
        
    }

}

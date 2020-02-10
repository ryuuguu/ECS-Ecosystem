using System;
using System.Collections;
using System.Collections.Generic;
using EcoSim;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine.Serialization;
using Random = Unity.Mathematics.Random;

public class Environment : MonoBehaviour,IDeclareReferencedPrefabs {
    
    public Terrain terrain;
    
    public static NativeArray< EnvironmentSettings> environmentSettings;
    public static  NativeArray<float> terrainHeight;
    public static NativeArray<float> terrainLight;

    [HideInInspector]
    public float[] localTerrainHeight;
    [HideInInspector]
    public float[] localTerrainLight;
    
    public static float defaultLightEnergy = 20;

    public static float Fitness(float val) {
        return math.select (-0.3f + 1 / (1 + 1 / val),0 ,val==0);
    }
    
    public static float LightEnergy_old(float3 position, float ambientLight, float variableLight) {
        return ambientLight+ (variableLight/200)*(math.abs(position.x+position.z));
    }
    
    public static float LightEnergySine(float3 position, float ambientLight, float variableLight) {
        return ambientLight+ (variableLight/2)*(math.sin(position.x/50)+math.sin(position.z/50));
    }

    public static float LightEnergy(float3 position, float ambientLight, float variableLight) {
        return ambientLight+ (variableLight/2)*((position.x+position.z)/200);
    }
    
    public static float TerrainValue(float3 position, NativeArray<float> valueArray, float4 bounds) {
        var x = (int) (position.x - bounds.x); //I think this truncates towards 0
        var y = (int) (position.z - bounds.y);
        var index = x * (int) (bounds.w - bounds.y+1) + y;
 //       Debug.Log("TerrainValue " + position + " : "+ x +":"+ y + " size: "
 //                 + (int) (bounds.w - bounds.y+1) + " : " +index +  " : "+valueArray[index]);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if (index< 0 || index >=valueArray.Length ) {
            throw new System.ArgumentException("get index must be in "+ bounds +  " was " + index + ":" +position);
        }
#endif

    return  valueArray[index];
}
    
    public EnvironmentSettings environmentSettingsInput;
    
    //public static float4 bounds;
    
    public Vector2[] startPositions ;
   // public float4 boundsInput;
    public GameObject prefabPlant;
    public TxAutotrophChrome1AB txAutotrophChrome1Ab;
    
    protected Random random;
    

    public void Start() {
        var esa = new EnvironmentSettings[1] {environmentSettingsInput};
        environmentSettings = new NativeArray<EnvironmentSettings>(esa,Allocator.Persistent);
        terrainHeight = new NativeArray<float>(localTerrainHeight, Allocator.Persistent);
        terrainLight = new NativeArray<float>(localTerrainLight, Allocator.Persistent);
        //bounds = boundsInput;
        
        random = new Random(environmentSettingsInput.randomSeed);
        //DebugTerrain();
        InitialPlants();
    }
    
    private void OnDestroy() {
        environmentSettings.Dispose();
        terrainHeight.Dispose();
        terrainLight.Dispose();
    }

    public void InitialPlants() {
        float c = -1000;
        foreach (var startPos in startPositions) {
            var position = new Vector3(startPos.x, 0, startPos.y);
            position.y = environmentSettings[0].environmentConsts.terrainHeightScale.y *
                         TerrainValue(position, terrainHeight, environmentSettings[0].environmentConsts.bounds);
            var sh = terrain.SampleHeight(position);
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var entity = em.CreateEntity();
            em.AddComponentData(entity, new RandomComponent {random = new Random(random.NextUInt())});
            em.AddComponentData(entity, new TxAutotrophSprout {location = position, energy = 5});
            em.AddComponentData(entity, txAutotrophChrome1Ab);
            em.AddComponentData(entity, txAutotrophChrome1Ab.GetChrome1W());
            em.AddComponentData(entity, new TxAutotrophColorGenome{b2 = c});
            c = 1000;
        }
    }
    
    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) {
        referencedPrefabs.Add(prefabPlant);
    }
    
    [Serializable]
    public struct EnvironmentSettings {
        public uint randomSeed;
        public TxAutotrophConsts txAutotrophConsts;
        public EnvironmentConsts environmentConsts;
    }
    
    [Serializable]
    public struct TxAutotrophConsts {
        [FormerlySerializedAs("baseValue")] public float baseCost ;
        [FormerlySerializedAs("leafMultiple")] public float leafCostMultiple ;
        [FormerlySerializedAs("heightMultiple")] public float heightCostMultiple ;
        public float pollenCostMultiple ;
        public float ageMultiple ;
        public float seedDivisor;
        [FormerlySerializedAs("LeafShadeRadiusMultiplier")] public float leafShadeRadiusMultiplier;
        [FormerlySerializedAs("LeafShadeEffectMultiplier")] public float leafShadeEffectMultiplier;
        public float pollenRadiusMultiplier;
        public float leafScale;
        public float stemScale;
        public float seedRangeMultiplier;
        public float mutationRate;
        public float mutationRange;

        public float colorGeneScale; 

    }
    [Serializable]
    public struct EnvironmentConsts {
        public float ambientLight;
        public float variableLight;
        public float4 bounds;
        public int terrainResolution;
        public float3 terrainHeightScale;
    }
    
    
    [ContextMenu("make Terrain")]
    public void MakeTerrainSine() {
        var td = terrain.terrainData;
        environmentSettingsInput.environmentConsts.terrainHeightScale = td.heightmapScale;
        environmentSettingsInput.environmentConsts.terrainResolution = td.heightmapResolution;
        var size = td.heightmapResolution ;
        localTerrainLight = new float[size*size];
        localTerrainHeight = new float[size*size];
        float maxlight =float.NegativeInfinity, minlight = float.PositiveInfinity ;
        
        for (int i = 0; i< size; i++){
            for (int j = 0; j < size; j++) {
                var lightEnergy = Environment.LightEnergySine(new float3(i, 0, j),
                    environmentSettingsInput.environmentConsts.ambientLight,
                    environmentSettingsInput.environmentConsts.variableLight
                );
                maxlight = Mathf.Max(lightEnergy, maxlight);
                minlight = Mathf.Min(lightEnergy, minlight);
                localTerrainLight[i*size+j] = lightEnergy;
            }
        }
        float[,] forTerrainData = new float[size,size];
        var range = maxlight - minlight;
        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                localTerrainHeight[i*size+j] =
                    (localTerrainLight[i*size+j] - minlight) / range;
                forTerrainData[i,j] =  (localTerrainLight[i*size+j] - minlight) / range;
            }
        }
        td.SetHeights(0,0, forTerrainData);
    }

}

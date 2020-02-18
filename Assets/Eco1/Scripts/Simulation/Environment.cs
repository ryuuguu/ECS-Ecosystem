using System;
using System.Collections;
using System.Collections.Generic;
using EcoSim;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine.Assertions.Must;
using UnityEngine.Serialization;
using Random = Unity.Mathematics.Random;

public class Environment : MonoBehaviour,IDeclareReferencedPrefabs {
    
    public Terrain terrain;
    
    
    public static NativeArray< EnvironmentSettings> environmentSettings;
    public static  NativeArray<float> terrainHeight;
    public static NativeArray<float> terrainLight;
    public NativeArray<Entity> statsFlowers;

    [HideInInspector]
    public float[] localTerrainHeight;
    [HideInInspector]
    public float[] localTerrainLight;
    
    public static float defaultLightEnergy = 20;

    public static float Fitness_old(float val) {
        return math.select (-0.3f + 1 / (1 + 1 / val),0 ,val==0);
    }
    
    public static float Fitness(float val) {
        return val;
    }
    
    public static float LightEnergy_old(float3 position, float ambientLight, float variableLight) {
        return ambientLight+ (variableLight/200)*(math.abs(position.x+position.z));
    }
    
    public static float LightEnergySine(float3 position, float ambientLight, float variableLight) {
        return ambientLight+(position.x/512) *(variableLight/2)*(math.sin(position.x/50)+math.sin(position.z/50));
    }

    public static float LightEnergy(float3 position, float ambientLight, float variableLight) {
        return ambientLight+ (variableLight/2)*((position.x+position.z)/200);
    }
    
    public static float TerrainValue(float3 position, NativeArray<float> valueArray, float4 bounds, float3 scale) {
        var x = (int) ((position.x - bounds.x)/scale.x); //I think this truncates towards 0
        var y = (int) ((position.z - bounds.y)/scale.z);
        var index = x * (int) ((bounds.w - bounds.y+1)/scale.z) + y;
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
        var es = environmentSettings[0];
        es.random = new Random(random.NextUInt());
        environmentSettings[0] = es;
        MakeTerrainSine();
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        InitialPlants(em);
        statsFlowers =TxAutotrophStats.MakeFlowerStats(em,
            es.environmentConsts.bounds,
            es.environmentConsts.flowerStatsSize);
    }
    
    private void OnDestroy() {
        environmentSettings.Dispose();
        terrainHeight.Dispose();
        terrainLight.Dispose();
        statsFlowers.Dispose(); 
    }

    public void InitialPlants( EntityManager em) {
        float3[]  colors = new [] {
            new float3(1,-1,-1),
            new float3(-1,1,-1),
            new float3(-1,-1,1),
            new float3(1,1,-1),
        };
        int i = 0;
        
        foreach (var startPos in startPositions) {
            i++;
            i %= TxAutotrophChrome2.LENGTH;
            var position = new Vector3(startPos.x, 0, startPos.y);
            position.y = TerrainValue(position, terrainHeight, environmentSettings[0].environmentConsts.bounds,
                             environmentSettings[0].environmentConsts.terrainHeightScale
                             );
            
            var entity = em.CreateEntity();
            em.AddComponentData(entity, new RandomComponent {random = new Random(random.NextUInt())});
            em.AddComponentData(entity, new TxAutotrophSprout {location = position, energy = 5});

            var chrome1AB = txAutotrophChrome1Ab.RandomRange(ref random);
            em.AddComponentData(entity, new TxAutotrophGamete {isFertilized = true,txAutotrophChrome1AB = chrome1AB});
            em.AddComponentData(entity, chrome1AB);
            em.AddComponentData(entity, chrome1AB.GetChrome1W());
            var chrome2 = new TxAutotrophChrome2();
            for (int j = 0; j < TxAutotrophChrome2.LENGTH; j++) {
                chrome2[j] = 50;
            }
            //chrome2[i] = 25;
            
            em.AddComponentData(entity, new TxAutotrophChrome2AB{ValueA = chrome2, ValueB = chrome2} );
        }
    }
    
    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) {
        referencedPrefabs.Add(prefabPlant);
    }
    
    [Serializable]
    public struct EnvironmentSettings {
        public uint randomSeed;
        public Random random;
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
        public float minShadeRadius;
        public float pollenRadiusMultiplier;
        public float leafScale;
        public float stemScale;
        public float seedRangeMultiplier;
        public float mutationRate;
        public float mutationRange;
        public float crossBreedDistance;

         

        public float colorGeneMaxDistanceSq; 

    }
    [Serializable]
    public struct EnvironmentConsts {
        public float ambientLight;
        public float variableLight;
        public float4 bounds;
        public float terrainMaxHeight;
        public float3 terrainHeightScale;
        public int2 flowerStatsSize;
    }
    
    
    [ContextMenu("make Terrain")]
    public void MakeTerrainSine() {
        var td = terrain.terrainData;
        var size = td.heightmapResolution ;
        var bounds = environmentSettingsInput.environmentConsts.bounds;
        var worldSizeX = (bounds.z - bounds.x);
        var worldSizeY = (bounds.w - bounds.y);
        var mapScalingX = worldSizeX / size;
        var mapScalingZ = worldSizeY / size;
        terrain.gameObject.transform.localPosition = new Vector3(bounds.x, 0, bounds.y);
        
        localTerrainLight = new float[size*size];
        localTerrainHeight = new float[size*size];
        float maxLight =float.NegativeInfinity, minLight = float.PositiveInfinity ;
        
        for (int i = 0; i< size; i++){
            for (int j = 0; j < size; j++) {
                var lightEnergy = Environment.LightEnergySine(new float3(i*mapScalingX, 0, j*mapScalingZ),
                    environmentSettingsInput.environmentConsts.ambientLight,
                    environmentSettingsInput.environmentConsts.variableLight
                );
                maxLight = Mathf.Max(lightEnergy, maxLight);
                minLight = Mathf.Min(lightEnergy, minLight);
                localTerrainLight[i*size+j] = lightEnergy;
            }
        }
        
        float[,] forTerrainData = new float[size,size];
        var scale =1/(maxLight - minLight);
        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                forTerrainData[i,j] = (localTerrainLight[i*size+j] - minLight) * scale;
                localTerrainHeight[i * size + j] =
                    forTerrainData[i, j] * environmentSettingsInput.environmentConsts.terrainMaxHeight;


            }
        }
        Debug.Log("map scale: "+ scale + " max: "+(maxLight - minLight) * scale );
        environmentSettingsInput.environmentConsts.terrainHeightScale = new float3(mapScalingX,
            scale, mapScalingZ);
        td.size = new Vector3(worldSizeX,  environmentSettingsInput.environmentConsts.terrainMaxHeight, worldSizeY);
        
        td.SetHeights(0,0, forTerrainData);
    }

}

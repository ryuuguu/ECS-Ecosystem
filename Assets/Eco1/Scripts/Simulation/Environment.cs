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
    public Camera lightNRGCamera;

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
    
    public static float HeightSine(float3 position, EnvironmentConsts ec) {
        return ec.sinXHeight * math.sin(position.x / ec.sinXLength) +
               ec.sinYHeight * math.sin(position.y / ec.sinYLength);
    }

    public static float ResourceValue(float3 position,float ambientResource, float variableResource,
        NativeArray<float> valueArray, float4 bounds, float3 scale) {
        var tv = TerrainValue(position, valueArray, bounds, scale);
        //Debug.Log("ResourceValue: " +tv + " Loc:" + position + " var:" + variableResource);
        return ambientResource+ variableResource*tv;
    }
    
    public static float TerrainValue(float3 position, NativeArray<float> valueArray, float4 bounds, float3 scale) {
        var x = (int) ((position.x - bounds.x)/scale.x); //I think this truncates towards 0
        var y = (int) ((position.z - bounds.y)/scale.z);
        var index = x * (int) ((bounds.w - bounds.y+1)/scale.z) + y;
        // remove the +1 afterbounds.y
 //       Debug.Log("TerrainValue " + position + " : "+ x +":"+ y + " size: "
 //                 + (int) (bounds.w - bounds.y+1) + " : " +index +  " : "+valueArray[index]);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if (index< 0 || index >=valueArray.Length ) {
            throw new System.ArgumentException("get index must be in "+ bounds +  " was " + index 
                                               + ":" +position + " Scale:"+scale+"   " + x + ":"+y +
                                               " b:" +(bounds.w - bounds.y+1) + " b2:" 
                                               + (int) ((bounds.w - bounds.y+1)/scale.z)
                                               
                                               );
        }
#endif

    return  valueArray[index];
}
    
    public EnvironmentSettings environmentSettingsInput;
    
    //public static float4 bounds;
    
    public int2 initialGridSize = new int2(4,4) ;
   // public float4 boundsInput;
    public GameObject prefabPlant;
    public TxAutotrophChrome1AB txAutotrophChrome1Ab;
    
    protected Random random;
    
    protected EntityManager em;
    protected EnvironmentSettings es ;
    public void Start() {
        var esa = new EnvironmentSettings[1] {environmentSettingsInput};
        environmentSettings = new NativeArray<EnvironmentSettings>(esa,Allocator.Persistent);
        
        //bounds = boundsInput;
        
        random = new Random(environmentSettingsInput.randomSeed);
        es = environmentSettings[0];
        es.random = new Random(random.NextUInt());
        environmentSettings[0] = es;
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        MakeTerrainSine();
    }

    private void Update() {
        switch (Time.frameCount) {
            case 0:
                lightNRGCamera.gameObject.SetActive(true);
                break;
            case 2 :
                MakeLightArray();
                break;
            case 4:
                lightNRGCamera.gameObject.SetActive(false);
                break;
            case 6:
                InitPlants();
                break;
        }
    }

    private void MakeLightArray() {
        var prevActive = RenderTexture.active;
        lightNRGCamera.enabled = true;
        RenderTexture tempRT = RenderTexture.GetTemporary(es.environmentConsts.lightArraySize,
            es.environmentConsts.lightArraySize); 
        lightNRGCamera.targetTexture = tempRT;
        lightNRGCamera.Render();
        RenderTexture.active = tempRT;
        var tex = new Texture2D(tempRT.width, tempRT.height,TextureFormat.RGBA32,false);
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        tex.Apply();
        lightNRGCamera.enabled = false;
        RenderTexture.ReleaseTemporary(tempRT);
        var colorArray = tex.GetPixels();
        
        // scale later after it working with matching scales
        if (localTerrainLight.Length != colorArray.Length) {
            Debug.LogWarning("localTerrainLight.Length: "+ localTerrainLight.Length + 
                             " colorArray.Length: " + colorArray.Length);
        }

        float minDebug = 1;
        float maxDebug = 0;
        for(int i =0; i< colorArray.Length;i++) {
            var c = colorArray[i];
            localTerrainLight[i] = (c.r + c.b + c.g) /3;
            minDebug = math.min(minDebug, localTerrainLight[i]);
            maxDebug = math.max(maxDebug, localTerrainLight[i]);
        }
        Debug.Log("light: "+minDebug +" : "+ maxDebug );
        terrainLight = new NativeArray<float>(localTerrainLight, Allocator.Persistent);
        RenderTexture.active = prevActive;
    }


    public void InitPlants() {
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
        var bounds = environmentSettings[0].environmentConsts.bounds;
        var startX = bounds.x;
        var startY = bounds.y;
        var incrX = (bounds.z - bounds.x) / initialGridSize.x;
        var incrY = (bounds.w - bounds.y) / initialGridSize.x;

        for (float x = bounds.x; x < bounds.z; x += incrX) {
            for (float y = bounds.y; y < bounds.w; y += incrY) {
                

                var position = new Vector3(x, 0, y);
                position.y = TerrainValue(position, terrainHeight, environmentSettings[0].environmentConsts.bounds,
                    environmentSettings[0].environmentConsts.terrainScale
                );

                var entity = em.CreateEntity();
                em.AddComponentData(entity, new RandomComponent {random = new Random(random.NextUInt())});
                em.AddComponentData(entity, new TxAutotrophSprout {location = position, energy = 5});

                var chrome1AB = txAutotrophChrome1Ab.RandomRange(ref random);
                em.AddComponentData(entity,
                    new TxAutotrophGamete {isFertilized = true, txAutotrophChrome1AB = chrome1AB});
                em.AddComponentData(entity, chrome1AB);
                em.AddComponentData(entity, chrome1AB.GetChrome1W());
                var chrome2 = new TxAutotrophChrome2();
                for (int j = 0; j < TxAutotrophChrome2.LENGTH; j++) {
                    chrome2[j] = 50;
                }
                //chrome2[i] = 25;

                em.AddComponentData(entity, new TxAutotrophChrome2AB {ValueA = chrome2, ValueB = chrome2});
            }
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
        public float3 terrainScale;
        public int2 flowerStatsSize;
        public float sinXHeight;
        public float sinYHeight;
        public float sinXLength;
        public float sinYLength;
        public int heightArraySize;
        public int lightArraySize;
    }
    
    
    [ContextMenu("make Terrain")]
    public void MakeTerrainSine() {
        var td = terrain.terrainData;
        
        td.heightmapResolution = es.environmentConsts.heightArraySize;
        var size = es.environmentConsts.heightArraySize;
        var bounds = es.environmentConsts.bounds;
        var worldSizeX = (bounds.z - bounds.x);
        var worldSizeY = (bounds.w - bounds.y);
        var mapScalingX = (worldSizeX+1) / size;
        var mapScalingZ = (worldSizeY+1) / size;
        terrain.gameObject.transform.localPosition = new Vector3(bounds.x, 0, bounds.y);
        
        localTerrainLight = new float[size*size];
        localTerrainHeight = new float[size*size];
        float maxLight =float.NegativeInfinity, minLight = float.PositiveInfinity ;
        
        for (int i = 0; i< size; i++){
            for (int j = 0; j < size; j++) {
                var lightEnergy = Environment.HeightSine(new float3(i*mapScalingX, 0, j*mapScalingZ),
                    es.environmentConsts
                );
                maxLight = Mathf.Max(lightEnergy, maxLight);
                minLight = Mathf.Min(lightEnergy, minLight);
                localTerrainHeight[i*size+j] = lightEnergy;
            }
        }
        
        float[,] forTerrainData = new float[size,size];
        var scale =1/(maxLight - minLight);
        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                forTerrainData[i,j] = (localTerrainHeight[i*size+j] - minLight) * scale;
                localTerrainHeight[i * size + j] =
                    forTerrainData[i,j] * environmentSettings[0].environmentConsts.terrainMaxHeight;
            }
        }
        Debug.Log("map scale: "+ scale + " max: "+(maxLight - minLight) * scale );
        es.environmentConsts.terrainScale = new float3(mapScalingX,
            scale, mapScalingZ);
        environmentSettings[0] = es;
        td.size = new Vector3(worldSizeX,  es.environmentConsts.terrainMaxHeight, worldSizeY);
        terrainHeight = new NativeArray<float>(localTerrainHeight, Allocator.Persistent);
        td.SetHeights(0,0, forTerrainData);
    }

}

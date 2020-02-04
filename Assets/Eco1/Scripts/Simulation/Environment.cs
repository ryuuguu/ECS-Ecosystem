using System;
using System.Collections;
using System.Collections.Generic;
using EcoSim;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

public class Environment : MonoBehaviour,IDeclareReferencedPrefabs {

    
    public Terrain terrain;
    
    public static NativeArray< EnvironmentSettings> environmentSettings;
    
    public static float defaultLightEnergy = 20;

    public static float Fitness(float val) {

        return math.select (-0.3f + 1 / (1 + 1 / val),0 ,val==0);
    }
    
    public static float LightEnergy_old(float3 position, float ambientLight, float variableLight) {
        return ambientLight+ (variableLight/200)*(math.abs(position.x+position.z));
    }
    
    public static float LightEnergy(float3 position, float ambientLight, float variableLight) {
        return ambientLight+ (variableLight/2)*(math.sin(position.x/50)+math.sin(position.z/50));
    }

    public static float TerrainValue(float3 position, NativeArray<float> valueArray, float4 bounds) {
        var x = (int) (position.x + bounds.x); //I think this truncates towards 0
        var y = (int) (position.z + bounds.y);
        var index = x * (int) (bounds.w - bounds.y) + y;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if (index< 0 || index >=valueArray.Length ) {
            throw new System.ArgumentException("get index must be in "+ bounds +  " was " + index);
        }
#endif

    return  valueArray[index];
}
    
    public static float MapHeight(float3 position, float ambientLight, float variableLight,
        float mapDecrement, float mapRange, float heightScale) {
        var light = LightEnergy(position, ambientLight, variableLight);
        var height = ((light -mapDecrement) /mapRange) * heightScale;
        return  height;
    }
    
    public EnvironmentSettings environmentSettingsInput;
    
    public static float4 bounds;
    
    public Vector2 startPos = Vector2.zero;
    public float4 boundsInput;
    public GameObject prefabPlant;
    public TxAutotrophGenome txAutotrophGenome;
    
    protected Random random;
    

    public void Start() {
        var esa = new EnvironmentSettings[1] {environmentSettingsInput};
        environmentSettings = new NativeArray<EnvironmentSettings>(esa,Allocator.Persistent);
        
        bounds = boundsInput;
        random = new Random(1);
        InitialPlants();
    }

    private void OnDestroy() {
        environmentSettings.Dispose();
    }

    public void InitialPlants() {
        // y should be calculated with MapHeight
        var position =new  Vector3 (startPos.x,0,startPos.y);
        
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var entity = em.CreateEntity();
        em.AddComponentData(entity, new RandomComponent {random = new Random(random.NextUInt())});
        em.AddComponentData(entity, new TxAutotrophSprout {location = position,energy = 5});
        em.AddComponentData(entity, txAutotrophGenome);

    }
    
    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) {
        referencedPrefabs.Add(prefabPlant);
    }
    
    [Serializable]
    public struct EnvironmentSettings {
        public TxAutotrophConsts txAutotrophConsts;
        public EnvironmentConsts environmentConsts;
    }
    
    [Serializable]
    public struct TxAutotrophConsts {
        public float baseValue ;
        public float leafMultiple ;
        public float heightMultiple ;
        public float ageMultiple ;
        public float seedDivisor;
        public float LeafShadeRadiusMultiplier;
        public float LeafShadeEffectMultiplier;
        public float leafScale;
        public float stemScale;
        public float seedRangeMultiplier;
        public float mutationRate;
        public float mutationRange;
        
    }
    [Serializable]
    public struct EnvironmentConsts {
        public float ambientLight;
        public float variableLight;
        public float4 bounds;
        public int terrainSize;

        public int terrainHeight;
        //public NativeArray<float> terrainHeight;
        //public NativeArray<float> terrainLight;
    }
    
    /*
    [ContextMenu("make Terrain")]
    public void MakeTerrainSine() {
        var td = terrain.terrainData;
        var size = td.heightmapResolution - 1;
        environmentSettingsInput.environmentConsts.terrainLight =
            new NativeArray<float>(size*size,Allocator.Persistent);
        environmentSettingsInput.environmentConsts.terrainHeight = 
            new NativeArray<float>(size*size,Allocator.Persistent);
        float maxlight =float.NegativeInfinity, minlight = float.PositiveInfinity ;
        
        for (int i = 0; i< size; i++){
            for (int j = 0; j < size; j++) {
                var lightEnergy = Environment.LightEnergy(new float3(i, 0, j),
                    environmentSettingsInput.environmentConsts.ambientLight,
                    environmentSettingsInput.environmentConsts.variableLight
                );
                maxlight = Mathf.Max(lightEnergy, maxlight);
                minlight = Mathf.Min(lightEnergy, minlight);
                environmentSettingsInput.environmentConsts.terrainLight[i*size+j] = lightEnergy;
               
            }
        }
        float[,]  managedHeight = new float[size,size];
        var range = maxlight - minlight;
        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                environmentSettingsInput.environmentConsts.terrainHeight[i*size+j] =
                    (environmentSettingsInput.environmentConsts.terrainLight[i*size+j] - minlight) / range;
                managedHeight [i, j] = environmentSettingsInput.environmentConsts.terrainHeight[i*size+j];
            }
        }

        td.SetHeights(0,0,managedHeight);
        
    }
*/
}

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

    public static NativeArray< EnvironmentSettings> environmentSettings;
    
    public static float defaultLightEnergy = 20;

    public static float Fitness(float val) {

        return math.select (-0.3f + 1 / (1 + 1 / val),0 ,val==0);
    }
    
    public static float LightEnergy(float3 position, float ambientLight, float variableLight) {
        return ambientLight+ variableLight*(math.abs(position.x+position.z));
    }
    
    public EnvironmentSettings environmentSettingsInput;
    
    public static float4 bounds;
    
    public Vector2 startPos = Vector2.zero;
    public float4 boundsInput;
    public GameObject prefabPlant;

    public void Start() {
        var esa = new EnvironmentSettings[1] {environmentSettingsInput};
        environmentSettings = new NativeArray<EnvironmentSettings>(esa,Allocator.Persistent);
        
        bounds = boundsInput;
        //random = new Random(1);
        InitialPlants();
    }

    public void InitialPlants() {
        var go = Instantiate(prefabPlant);
        go.transform.position =new  Vector3 (startPos.x,0,startPos.y);
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        //masterEntity = em.CreateEntity();
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
        public float seedRangeMultiplier;
        public float mutationRate;
        public float mutationRange;
        
    }
    [Serializable]
    public struct EnvironmentConsts {
        public float ambientLight;
        public float variableLight;
        public float4 bounds;
    }
    
    
}

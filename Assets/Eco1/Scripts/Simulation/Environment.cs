using System;
using System.Collections;
using System.Collections.Generic;
using EcoSim;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

public class Environment : MonoBehaviour,IDeclareReferencedPrefabs{

    public static float defaultLightEnergy = 20;

    public static float Fitness(float val) {

        return math.select (-0.3f + 1 / (1 + 1 / val),0 ,val==0);
    }
    
    public static float LightEnergy(float3 position) {
        return defaultLightEnergy*2*(math.abs(position.x+position.z))/100;
    }

    [Serializable]
    public struct TxAutotrophMaintenance {
        public float baseValue ;
        public float leafMultiple ;
        public float heightMultiple ;
        public float ageMultiple ;
    }

    public TxAutotrophMaintenance txAutotrophMaintenance;
    public static float4 bounds;
    // increase shade collider and sprout distance
    // should change density hope just a visual change.
    
    public static float spread = 5;


    public Vector2 startPos = Vector2.zero;
    public float4 boundsInput;
    public GameObject prefabPlant;

    public void Start() {
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
}

using System.Collections;
using System.Collections.Generic;
using EcoSim;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

public class Environment : MonoBehaviour,IDeclareReferencedPrefabs{

    public static float defaultLightEnergy = 15;

    public static float Fitness(float val) {

        return math.select (-0.3f + 1 / (1 + 1 / val),0 ,val==0);
    }
    
    public static float LightEnergy(float3 position) {
        return defaultLightEnergy;
    }

    public static Entity prefabPlantStatic;
    public static Entity masterEntity;
    public static float4 bounds;
    public static Random random;

    public float4 boundsInput;
    public GameObject prefabPlant;

    public void Start() {
        bounds = boundsInput;
        random = new Random(1);
        InitialPlants();
    }

    public void InitialPlants() {
        var go = Instantiate(prefabPlant);
        go.transform.position = Vector3.zero;
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        masterEntity = em.CreateEntity();
    }


    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) {
        referencedPrefabs.Add(prefabPlant);
    }
}

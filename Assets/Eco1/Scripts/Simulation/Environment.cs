using System.Collections;
using System.Collections.Generic;
using EcoSim;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;

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
    

    public GameObject prefabPlant;

    public void Start() {
       //prefabPlantStatic = prefabPlant;
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

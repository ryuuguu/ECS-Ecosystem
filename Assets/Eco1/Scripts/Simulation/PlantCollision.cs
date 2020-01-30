
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using System;
using EcoSim;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;


[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class TriggerLeafSystem : JobComponentSystem {
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    StepPhysicsWorld m_StepPhysicsWorldSystem;

    NativeArray<int> m_TriggerEntitiesIndex;

    struct ShadePair {
        public Entity entityA; // this entity lower height + translation.y
        public Entity entityB; 
        public float  shade;

        // dev & testing only
        public float3 translationA;
        public float3 translationB;
        public float heightA;
        public float heightB;
        public float leafA;
        public float leafB;
        
    }

    
    protected override void OnCreate() {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
       

        m_TriggerEntitiesIndex = new NativeArray<int>(1, Allocator.Persistent);
        m_TriggerEntitiesIndex[0] = 0;
        
    }

    protected override void OnDestroy() {
        m_TriggerEntitiesIndex.Dispose();
        
    }
    
    struct GetTriggerEventCount : ITriggerEventsJob {
        [ReadOnly]public ComponentDataFromEntity<Translation> translations; 
        
        [NativeFixedLength(1)] public NativeArray<int> pCounter;
        
        // unsafe writing to this?
        public void Execute(TriggerEvent triggerEvent)
        {
            pCounter[0]++;
            
        }
    }
    
    struct MakeShadePairs : ITriggerEventsJob {
        [ReadOnly]public ComponentDataFromEntity<Translation> translations; 
        [ReadOnly]public ComponentDataFromEntity<Height> heights; 
        [ReadOnly]public ComponentDataFromEntity<Leaf> leafs; 

        
        [NativeFixedLength(1)] public NativeArray<int> pCounter;
        public NativeArray<ShadePair> shadePairs;
        
        // need entity a to be lower than entity B
        //  need leaf0 to be less than leaf1 
        // distance squared between translations
        // R0*R0-R0*R1
        // R1*R1
        
        public void Execute(TriggerEvent triggerEvent) {
            var shadePair  = new ShadePair();
            var eA = triggerEvent.Entities.EntityA;
            var eB = triggerEvent.Entities.EntityB;
            
            var swap = translations[eA].Value.y + heights[eA].Value > translations[eB].Value.y + heights[eB].Value;
            if (swap) {
                shadePair.entityA = eB;
                shadePair.entityB = eA;
            }
            else {
                shadePair.entityA = eA;
                shadePair.entityB = eB; 
            }

            shadePair.translationA = translations[shadePair.entityA].Value;
            shadePair.translationB = translations[shadePair.entityB].Value;

            shadePair.heightA = heights[shadePair.entityA].Value;
            shadePair.heightB = heights[shadePair.entityB].Value;
            
            shadePair.leafA = leafs[shadePair.entityA].Value;
            shadePair.leafB = leafs[shadePair.entityB].Value;
            
            // Increment the output counter in a thread safe way.
            var count = ++pCounter[0] - 1;

            shadePairs[count] = shadePair;
        }
    }

    

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        ComponentDataFromEntity<Translation> translations = GetComponentDataFromEntity<Translation>();
        // Get the number of TriggerEvents so that we can allocate a native array
        m_TriggerEntitiesIndex[0] = 0;
        
        
        
        JobHandle getTriggerEventCountJobHandle = new GetTriggerEventCount() {
            pCounter = m_TriggerEntitiesIndex,
            translations =  translations
        }.Schedule(m_StepPhysicsWorldSystem.Simulation, ref m_BuildPhysicsWorldSystem.PhysicsWorld, inputDeps);
        getTriggerEventCountJobHandle.Complete();
        var shadePairs = new NativeArray<ShadePair>(m_TriggerEntitiesIndex[0], Allocator.TempJob);
        m_TriggerEntitiesIndex[0] = 0;
        
        JobHandle makeShadePairsJobHandle = new MakeShadePairs
        {
            translations = GetComponentDataFromEntity<Translation>(),
            heights = GetComponentDataFromEntity<Height>(),
            leafs = GetComponentDataFromEntity<Leaf>(),

            shadePairs = shadePairs,
            pCounter = m_TriggerEntitiesIndex,
        }.Schedule(m_StepPhysicsWorldSystem.Simulation, ref m_BuildPhysicsWorldSystem.PhysicsWorld, getTriggerEventCountJobHandle);
        makeShadePairsJobHandle.Complete();


        if (m_TriggerEntitiesIndex[0] != 0) {
            Debug.Log("Count Triggers: " + UnityEngine.Time.frameCount + " : " + m_TriggerEntitiesIndex[0]);
            Debug.Log("T: " + shadePairs[0].translationA + " : " + shadePairs[0].translationB +
                      "H: " + shadePairs[0].heightA + " : " + shadePairs[0].heightB +
                      "L: " + shadePairs[0].leafA + " : " + shadePairs[0].leafB);
        }

        shadePairs.Dispose();
        return makeShadePairsJobHandle;
    }
}
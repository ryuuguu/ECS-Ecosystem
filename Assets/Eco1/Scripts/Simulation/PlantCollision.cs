
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
using Math = System.Math;


[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class TriggerLeafSystem : JobComponentSystem {
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    StepPhysicsWorld m_StepPhysicsWorldSystem;

    NativeArray<int> m_TriggerEntitiesIndex;
    private EntityQuery m_GroupShade;
    

    struct ShadePair {
        public Entity entity; // this entity lower height + translation.y
        public float  shade;

        // dev & testing only
        public Entity entityB;
    }
    
    protected override void OnCreate() {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
       

        m_TriggerEntitiesIndex = new NativeArray<int>(1, Allocator.Persistent);
        m_TriggerEntitiesIndex[0] = 0;

        m_GroupShade = GetEntityQuery(ComponentType.ReadWrite<Shade>());


    }

    protected override void OnDestroy() {
        m_TriggerEntitiesIndex.Dispose();
        
    }
    
    struct GetTriggerEventCount : ITriggerEventsJob {
        [NativeFixedLength(1)] public NativeArray<int> pCounter;
        
        // unsafe writing to this?
        public void Execute(TriggerEvent triggerEvent)
        {
            pCounter[0]++;
            
        }
    }
    
    struct MakeShadePairs : ITriggerEventsJob {
        [ReadOnly]public ComponentDataFromEntity<Translation> translations; 
        [ReadOnly]public ComponentDataFromEntity<TxAutotrophPhenotype> txAutotrophPhenotype;
        
        public NativeHashMap<Entity, float> shadeDict;
        
        // need entity a to be lower than entity B
        //  need leaf0 to be less than leaf1 
        // distance squared between translations
        // max  (r0+r1 )* (r0 + r1)
        // (R0-R1)*(r0-r1)
        // r
        
        public void Execute(TriggerEvent triggerEvent) {
            var shadePair  = new ShadePair();
            var eA = triggerEvent.Entities.EntityA;
            var eB = triggerEvent.Entities.EntityB;
            Entity e;
            
            var swap = translations[eA].Value.y + txAutotrophPhenotype[eA].height> translations[eB].Value.y 
                       + txAutotrophPhenotype[eB].height;
            if (swap) {
                shadePair.entity = eB;
                e = eB;
                shadePair.entityB = eA;
            }
            else {
                shadePair.entity = eA;
                e = eA;
                shadePair.entityB = eB; 
            }

            
            var dSqr = math.distancesq(translations[shadePair.entity].Value, translations[shadePair.entityB].Value);
            var r0 = math.max(txAutotrophPhenotype[shadePair.entity].leaf, txAutotrophPhenotype[shadePair.entityB].leaf);
            var r1 = math.min(txAutotrophPhenotype[shadePair.entity].leaf, txAutotrophPhenotype[shadePair.entityB].leaf);
            var minD = (r0-r1 )* (r0 - r1);
            var maxD = (r0+r1 )* (r0 + r1)-minD;
            var num = dSqr - minD;
            if (!shadeDict.ContainsKey(e)) {
                shadeDict[e] = 0;
            }
            if(num <= 0 ) {
                
               shadeDict[e]+= r1;
            } else {
                
                shadeDict[e]+= (1-((maxD - num) / maxD)) *r1;
            }
            
            // Increment the output counter in a thread safe way.
            
        }
    }

    struct AddShade : IJobForEachWithEntity<Shade> {
        [ReadOnly] public NativeHashMap<Entity, float> shadeDict;

        public void Execute(Entity entity, int index, ref Shade shade) {
            if (shadeDict.ContainsKey(entity)){
                shade.Value = shadeDict[entity];
            }
            else {
                shade.Value = 0;
            }
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        ComponentDataFromEntity<Translation> translations = GetComponentDataFromEntity<Translation>();
        // Get the number of TriggerEvents so that we can allocate a native array
        m_TriggerEntitiesIndex[0] = 0;
        
        JobHandle getTriggerEventCountJobHandle = new GetTriggerEventCount() {
            pCounter = m_TriggerEntitiesIndex
        }.Schedule(m_StepPhysicsWorldSystem.Simulation, ref m_BuildPhysicsWorldSystem.PhysicsWorld, inputDeps);
        getTriggerEventCountJobHandle.Complete();
        
        //m_TriggerEntitiesIndex[0] is too large could use count of entites with triggers if can get it fast
        var shadeDict = new NativeHashMap<Entity,float>(m_TriggerEntitiesIndex[0], Allocator.TempJob);
        m_TriggerEntitiesIndex[0] = 0;
        
        JobHandle makeShadePairsJobHandle = new MakeShadePairs
        {
            translations = GetComponentDataFromEntity<Translation>(),
            txAutotrophPhenotype = GetComponentDataFromEntity<TxAutotrophPhenotype>(),
            shadeDict = shadeDict
        }.Schedule(m_StepPhysicsWorldSystem.Simulation, ref m_BuildPhysicsWorldSystem.PhysicsWorld, getTriggerEventCountJobHandle);
        makeShadePairsJobHandle.Complete();
        
        
        /*
        if (shadePairs.Length != 0) { 
            Debug.Log("Count Triggers: " + UnityEngine.Time.frameCount + " : " +shadePairs.Length);
           // Debug.Log("T: " + shadePairs[0].translationA + " : " + shadePairs[0].translationB +
           //           " L: " + shadePairs[0].leafA + " : " + shadePairs[0].leafB + " S: "+ shadePairs[0].shade);
        }
        */
        
        JobHandle addShadeJobHandle = new AddShade
        {
            shadeDict = shadeDict,
        }.Schedule(m_GroupShade, makeShadePairsJobHandle);
        addShadeJobHandle.Complete();
        
        shadeDict.Dispose();
        
        return makeShadePairsJobHandle;
    }
}

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


[BurstCompile]
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
    
    struct MakeShadeDict : ITriggerEventsJob {
        [ReadOnly]public ComponentDataFromEntity<Translation> translations; 
        [ReadOnly]public ComponentDataFromEntity<TxAutotrophPhenotype> txAutotrophPhenotype;
        
        [ReadOnly]public NativeArray<Environment.EnvironmentSettings> environmentSettings;
        
        public NativeHashMap<Entity, float> shadeDict;
        
        // need entity a to be lower than entity B
        //  need leaf0 to be less than leaf1 
        // distance squared between translations
        // max  (r0+r1 )* (r0 + r1)
        // (R0-R1)*(r0-r1)
        // r
        
        public void Execute(TriggerEvent triggerEvent) {
            var txAutotrophConsts = environmentSettings[0].txAutotrophConsts;
            var eA = triggerEvent.Entities.EntityA;
            var eB = triggerEvent.Entities.EntityB;
            Entity e;
            Entity eOther;
            
            var swap = translations[eA].Value.y + txAutotrophPhenotype[eA].height> translations[eB].Value.y 
                       + txAutotrophPhenotype[eB].height;
            if (swap) {
                e = eB;
                eOther = eA;
            }
            else {
                e = eA;
                eOther = eB;
            }
            
            var l0 = math.max(txAutotrophConsts.minShadeRadius,
                         txAutotrophPhenotype[e].leaf)* txAutotrophConsts.leafShadeRadiusMultiplier;
            var l1 = math.max(txAutotrophConsts.minShadeRadius,
                         txAutotrophPhenotype[eOther].leaf)* txAutotrophConsts.leafShadeRadiusMultiplier;

            var dSqr = math.distancesq(translations[e].Value, translations[eOther].Value);
            var r0 = math.max(l0, l1);
            var r1 = math.min(l0, l1);
            var rSub = r0 - r1;
            var minD = rSub * rSub;
            var num = dSqr - minD;
            if (!shadeDict.ContainsKey(e)) {
                if(num <= 0 ) {
                    shadeDict[e]= r1;
                } else {
                    var rAdd = r0 + r1;
                    var maxD = rAdd * rAdd-minD;
                    shadeDict[e]= (1-((maxD - num) / maxD)) *r1;
                }
            }
            if(num <= 0 ) {
                shadeDict[e]+= r1;
            } else {
                var rAdd = r0 + r1;
                var maxD = rAdd * rAdd-minD;
                shadeDict[e]+= (1-((maxD - num) / maxD)) *r1;
            }
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
        /*
        JobHandle getTriggerEventCountJobHandle = new GetTriggerEventCount() {
            pCounter = m_TriggerEntitiesIndex
        }.Schedule(m_StepPhysicsWorldSystem.Simulation, ref m_BuildPhysicsWorldSystem.PhysicsWorld, inputDeps);
         getTriggerEventCountJobHandle.Complete();
        */
        
        var shades = GetEntityQuery(ComponentType.ReadOnly<Shade>())
            .ToEntityArray(Allocator.TempJob);
        var shadesCount = shades.Length;
        shades.Dispose();
        
        //m_TriggerEntitiesIndex[0] is too large could use count of entites with triggers if can get it fast
        var shadeDict = new NativeHashMap<Entity,float>(shadesCount, Allocator.TempJob);
       // m_TriggerEntitiesIndex[0] = 0;
        
        JobHandle makeShadePairsJobHandle = new MakeShadeDict
        {
            translations = GetComponentDataFromEntity<Translation>(),
            txAutotrophPhenotype = GetComponentDataFromEntity<TxAutotrophPhenotype>(),
            environmentSettings = Environment.environmentSettings, 
            shadeDict = shadeDict
        }.Schedule(m_StepPhysicsWorldSystem.Simulation, ref m_BuildPhysicsWorldSystem.PhysicsWorld,  inputDeps);
        makeShadePairsJobHandle.Complete();
        
        
        
        JobHandle addShadeJobHandle = new AddShade
        {
            shadeDict = shadeDict,
        }.Schedule(m_GroupShade, makeShadePairsJobHandle);
        addShadeJobHandle.Complete();
        
        shadeDict.Dispose();
        
        return makeShadePairsJobHandle;
    }
}
﻿
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using EcoSim;
using Unity.Mathematics;
using Unity.Transforms;



[BurstCompile]
[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class TriggerLeafSystem : JobComponentSystem {
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    StepPhysicsWorld m_StepPhysicsWorldSystem;
    
    private EntityQuery m_GroupShade;
    private EntityQuery m_GroupGamete;
    
    protected override void OnCreate() {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
        m_GroupShade = GetEntityQuery(ComponentType.ReadWrite<Shade>());
        m_GroupGamete = GetEntityQuery(
            ComponentType.ReadWrite<TxAutotrophGamete>(),
            ComponentType.ReadOnly<TxAutotrophSeed>()
            );
        
    }

    struct MakeShadeDict : ITriggerEventsJob {
        [ReadOnly]public ComponentDataFromEntity<Translation> translations; 
        [ReadOnly]public ComponentDataFromEntity<TxAutotrophPhenotype> txAutotrophPhenotype;
        
        [ReadOnly]public NativeArray<Environment.EnvironmentSettings> environmentSettings;
        
        public NativeHashMap<Entity, float> shadeDict;

        public void Execute(TriggerEvent triggerEvent) {
            var txAutotrophConsts = environmentSettings[0].txAutotrophConsts;
            var eA = triggerEvent.Entities.EntityA;
            var eB = triggerEvent.Entities.EntityB;

            Entity e;
            Entity eOther;
            if (txAutotrophPhenotype.Exists(eA)) {
                    var swap = translations[eA].Value.y + txAutotrophPhenotype[eA].height > translations[eB].Value.y
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
                                 txAutotrophPhenotype[e].leaf) * txAutotrophConsts.leafShadeRadiusMultiplier;
                    var l1 = math.max(txAutotrophConsts.minShadeRadius,
                                 txAutotrophPhenotype[eOther].leaf) * txAutotrophConsts.leafShadeRadiusMultiplier;
                    var dSqr = math.distancesq(translations[e].Value, translations[eOther].Value);
                    var r0 = math.max(l0, l1);
                    var r1 = math.min(l0, l1);
                    var rSub = r0 - r1;
                    var minD = rSub * rSub;
                    var num = dSqr - minD;
                    if (!shadeDict.ContainsKey(e)) {
                        if (num <= 0) {
                            shadeDict[e] = r1;
                        }
                        else {
                            var rAdd = r0 + r1;
                            var maxD = rAdd * rAdd - minD;
                            shadeDict[e] = (1 - ((maxD - num) / maxD)) * r1;
                        }
                    }
                    else {

                        if (num <= 0) {
                            shadeDict[e] += r1;
                        }
                        else {
                            var rAdd = r0 + r1;
                            var maxD = rAdd * rAdd - minD;
                            shadeDict[e] += (1 - ((maxD - num) / maxD)) * r1;
                        }
                    }
            }
            
        }
    }

    
    struct MakeTFertilizerDict : ITriggerEventsJob {
        [ReadOnly] public ComponentDataFromEntity<TxAutotrophChrome2AB> txAutotrophChrome2AB;
        [ReadOnly]public ComponentDataFromEntity<TxAutotrophPollen> TxAutotrophPollen;
        
        
        [ReadOnly]public NativeArray<Environment.EnvironmentSettings> environmentSettings;
        
        public NativeHashMap<Entity, Entity> fertilizeDict;
        public Unity.Mathematics.Random random;
        
        
        public void Execute(TriggerEvent triggerEvent) {
            var txAutotrophConsts = environmentSettings[0].txAutotrophConsts;
            var eA = triggerEvent.Entities.EntityA;
            var eB = triggerEvent.Entities.EntityB;
            
            
            Entity e;
            Entity eOther;
            
            
            if (TxAutotrophPollen.Exists(eA) || TxAutotrophPollen.Exists(eB)) {
                if (TxAutotrophPollen.Exists(eA)) {
                    eOther = eA;
                    e = eB;
                }
                else {
                    e = eA;
                    eOther = eB;
                }
                float distance = txAutotrophChrome2AB[TxAutotrophPollen[eOther].plant].DistanceSq(
                    txAutotrophChrome2AB[e],txAutotrophConsts.crossBreedDistance
                    );
                if (random.NextFloat(0, 1) > distance) {
                    if (!fertilizeDict.ContainsKey(e)) {
                        fertilizeDict[e] = eOther;

                    }
                    else {
                        if (random.NextBool()) {
                            fertilizeDict[e] = eOther;
                        }
                    }
                }
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
    
    struct AutotrophFertilize : IJobForEachWithEntity<TxAutotrophGamete> {
        [ReadOnly] public NativeHashMap<Entity, Entity> fertilizeDict;
        [ReadOnly] public ComponentDataFromEntity<TxAutotrophChrome1AB> txAutotrophChrome1AB;
        [ReadOnly] public ComponentDataFromEntity<TxAutotrophChrome2AB> txAutotrophChrome2AB;
        [ReadOnly] public ComponentDataFromEntity<TxAutotrophPollen> txAutotrophPollen;

        public void Execute(Entity entity, int index, ref TxAutotrophGamete txAutotrophGamete) {
            if (fertilizeDict.ContainsKey(entity)) {
                txAutotrophGamete.isFertilized = true;
                txAutotrophGamete.txAutotrophChrome1AB = 
                    txAutotrophChrome1AB[ txAutotrophPollen[ fertilizeDict[entity]].plant].Copy();
                txAutotrophGamete.txAutotrophChrome2AB = 
                    txAutotrophChrome2AB[ txAutotrophPollen[ fertilizeDict[entity]].plant].Copy();
            }
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        ComponentDataFromEntity<Translation> translations = GetComponentDataFromEntity<Translation>();
        
        var shades = GetEntityQuery(ComponentType.ReadOnly<Shade>())
            .ToEntityArray(Allocator.TempJob);
        var shadesCount = shades.Length;
        shades.Dispose();
        
        var unfertilizeds = GetEntityQuery(ComponentType.ReadOnly<TxAutotrophGamete>(),
                ComponentType.ReadOnly<TxAutotroph>()
                )
            .ToEntityArray(Allocator.TempJob);
        var unfertilizedCount = unfertilizeds.Length;
        unfertilizeds.Dispose();
        
        var shadeDict = new NativeHashMap<Entity,float>(shadesCount, Allocator.TempJob);
        var fertilizeDict = new NativeHashMap<Entity,Entity>(unfertilizedCount, Allocator.TempJob);
        
        JobHandle makeShadeDictJobHandle = new MakeShadeDict
        {
            translations = GetComponentDataFromEntity<Translation>(),
            txAutotrophPhenotype = GetComponentDataFromEntity<TxAutotrophPhenotype>(),
            environmentSettings = Environment.environmentSettings,
            shadeDict = shadeDict,
        }.Schedule(m_StepPhysicsWorldSystem.Simulation, ref m_BuildPhysicsWorldSystem.PhysicsWorld,  inputDeps);
        
        JobHandle makeFertilizeDictJobHandle = new MakeTFertilizerDict()
        {
            txAutotrophChrome2AB = GetComponentDataFromEntity<TxAutotrophChrome2AB>(),
            TxAutotrophPollen = GetComponentDataFromEntity<TxAutotrophPollen>(),
            environmentSettings = Environment.environmentSettings,
            fertilizeDict = fertilizeDict,
            random = new Unity.Mathematics.Random(Environment.environmentSettings[0].random.NextUInt())
        }.Schedule(m_StepPhysicsWorldSystem.Simulation, ref m_BuildPhysicsWorldSystem.PhysicsWorld,  inputDeps);


        makeFertilizeDictJobHandle.Complete();
        makeShadeDictJobHandle.Complete();
        
        /*
        //Debug.Log("ShadeDict: " + shadeDict.Length);
        Debug.Log("FertilizeDict: " + fertilizeDict.Length);
        var keys =  fertilizeDict.GetKeyArray(Allocator.Persistent);
        //if (keys.Length > 0) {
        //    Debug.Log("fertilizeDict gamete: " + keys[0] + " pollen: " + fertilizeDict[keys[0]]);
        //}
        foreach (var c in keys ) {
            Debug.Log("fertilizeDict: " + c + " : " + fertilizeDict[c] );
        }

        keys.Dispose();
        */
        
        
        JobHandle  autotrophFertilize = new AutotrophFertilize()
        {
            fertilizeDict =fertilizeDict,
            txAutotrophChrome1AB = GetComponentDataFromEntity<TxAutotrophChrome1AB>(),
            txAutotrophChrome2AB = GetComponentDataFromEntity<TxAutotrophChrome2AB>(),
            txAutotrophPollen = GetComponentDataFromEntity<TxAutotrophPollen>()
        }.Schedule(m_GroupGamete, makeFertilizeDictJobHandle);
        
        JobHandle addShadeJobHandle = new AddShade
        {
            shadeDict = shadeDict,
        }.Schedule(m_GroupShade, makeShadeDictJobHandle);

        addShadeJobHandle.Complete();
        autotrophFertilize.Complete();
        shadeDict.Dispose();
        fertilizeDict.Dispose();
        
        return  addShadeJobHandle;
    }
}
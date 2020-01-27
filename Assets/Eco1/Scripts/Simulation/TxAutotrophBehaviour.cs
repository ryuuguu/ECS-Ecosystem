﻿using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Transforms;
using Unity.Physics.Authoring;
using Unity.Mathematics;
using Unity.Burst;



namespace EcoSim {
    
    public class TxAutotrophBehaviour : MonoBehaviour, IConvertGameObjectToEntity {
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (enabled) {
                AddComponentDatas(entity, dstManager);
            }
            
        }
        public static void AddComponentDatas(Entity entity, EntityManager dstManager){
            dstManager.AddComponentData(entity, new  TxAutotroph());
            dstManager.AddComponentData(entity, new  EnergyStore(){Value = 0});
            dstManager.AddComponentData(entity, new  TxAutotrophMaintenance() {
                baseValue = 1,
                leafMultiple = 0.1f,
                heightMultiple = 0.1f
            });
            dstManager.AddComponentData(entity, new  Leaf() {Value = 1});
            dstManager.AddComponentData(entity, new  Height() {Value = 1});
            dstManager.AddComponentData(entity, new  Seed() {Value = 0});
            dstManager.AddComponentData(entity, new  TxAutotrophGenome() {
                nrg2Height = 5,
                nrg2Leaf = 5,
                nrg2Seed = 5,
                nrg2Storage = 5,
                maxHeight = 5,
                maxLeaf = 5,
                seedSize = 5
            });
            dstManager.AddComponentData(entity, new TxInitialize());
            dstManager.AddComponentData(entity, new TxAutotrophParts());
            
        }
    }
    
    
    
    
    //[UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
    //[UpdateBefore(typeof(BeginSimulationEntityCommandBufferSystem))]
    public  class TxAutotrophInitialize : JobComponentSystem
    {
      
        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            
            Entities.ForEach((DynamicBuffer<Child> children,ref TxAutotrophParts txAutotrophParts) => {
                txAutotrophParts.stem = children[0].Value;
            }).Run();
            return default;
        }
    }
    /// <summary>
    ///  receive light
    ///  add to other system energy stores
    /// </summary>
    [BurstCompile]
    
    public class TxAutotrophLight : JobComponentSystem {
        EntityQuery m_Group;
        protected override void OnCreate() {
            m_Group = GetEntityQuery(ComponentType.ReadWrite<EnergyStore>(),
                ComponentType.ReadOnly<TxAutotroph>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<Leaf>()
            );
        }

        struct TxGainEnergy : IJobForEach<EnergyStore,
            Translation,
            Leaf> {
            public void Execute(ref EnergyStore energyStore,
                [ReadOnly] ref  Translation translation,
                [ReadOnly] ref Leaf leaf) {
                energyStore.Value += Enviroment.LightEnergy(translation.Value)*Enviroment.Fitness(leaf.Value) ;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            TxGainEnergy job = new TxGainEnergy() { };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            return jobHandle;
        }
    }

    
    
    
    [UpdateAfter(typeof(TxAutotrophLight))]
    [BurstCompile]
    public class TxAutotrophPayMaintenance : JobComponentSystem {
        EntityQuery m_Group;
        protected override void OnCreate() {
            m_Group = GetEntityQuery(ComponentType.ReadWrite<EnergyStore>(),
                ComponentType.ReadOnly<TxAutotroph>(),
                ComponentType.ReadOnly<TxAutotrophMaintenance>(),
                ComponentType.ReadOnly<Leaf>(),
                ComponentType.ReadOnly<Height>()
            );
        }

        struct PayMaintenance : IJobForEach<EnergyStore, TxAutotrophMaintenance, Leaf, Height> {
            public void Execute(ref EnergyStore energyStore,  
                [ReadOnly] ref TxAutotrophMaintenance txAutotrophMaintenance,
                [ReadOnly] ref Leaf leaf,
                [ReadOnly] ref Height height
            ) {
                energyStore.Value -= txAutotrophMaintenance.baseValue + 
                                     txAutotrophMaintenance.leafMultiple * leaf.Value +
                    txAutotrophMaintenance.heightMultiple * height.Value;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            PayMaintenance job = new PayMaintenance() { };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            return jobHandle;
        }
    }

    [UpdateAfter(typeof(TxAutotrophMaintenance))]
    [BurstCompile]
    public class TxAutotrophGrow : JobComponentSystem {
        EntityQuery m_Group;
        protected override void OnCreate() {
            m_Group = GetEntityQuery(ComponentType.ReadWrite<EnergyStore>(),
                ComponentType.ReadWrite<Leaf>(),
                ComponentType.ReadWrite<Height>(),
                ComponentType.ReadWrite<Seed>(),
                ComponentType.ReadWrite<TxAutotroph>(),
                ComponentType.ReadWrite<TxAutotrophGenome>()
            );
        }

        struct Grow : IJobForEach<EnergyStore, Leaf, Height, Seed, TxAutotrophGenome> {
            public void Execute(ref EnergyStore energyStore,
                 ref Leaf leaf,
                 ref Height height,
                 ref Seed seed, 
                [ReadOnly] ref TxAutotrophGenome txAutotrophGenome
                
            ) {
                var sum = txAutotrophGenome.nrg2Height + txAutotrophGenome.nrg2Leaf + txAutotrophGenome.nrg2Seed +
                          txAutotrophGenome.nrg2Storage;
                var heightGrow = energyStore.Value * txAutotrophGenome.nrg2Height / sum;
                var leafGrow = energyStore.Value * txAutotrophGenome.nrg2Leaf / sum;
                var seedGrow = energyStore.Value * txAutotrophGenome.nrg2Seed / sum;
                height.Value += heightGrow;
                leaf.Value += leafGrow;
                seed.Value += seedGrow;
                energyStore.Value -= heightGrow + leafGrow + seedGrow;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            Grow job = new Grow() { };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            return jobHandle;
        }
    }

}

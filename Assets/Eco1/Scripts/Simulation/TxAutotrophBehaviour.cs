using Unity.Physics;
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
                ComponentType.ReadOnly<Translation>()
            );
        }

        struct GainEnergy : IJobForEach<EnergyStore,
            TxAutotroph,
            Translation> {
            public void Execute(ref EnergyStore energyStore, 
                [ReadOnly] ref TxAutotroph txAutotroph, 
                [ReadOnly] ref  Translation translation) {
                energyStore.Value += Enviroment.LightEnergy(translation.Value);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            GainEnergy job = new GainEnergy() { };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            return jobHandle;
        }
    }

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

        struct PayMaintenance : IJobForEach<EnergyStore, TxAutotroph, TxAutotrophMaintenance, Leaf, Height> {
            public void Execute(ref EnergyStore energyStore, 
                [ReadOnly] ref TxAutotroph txAutotroph, 
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

    
}

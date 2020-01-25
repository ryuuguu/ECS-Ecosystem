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
    
    public class TxAutotrophBehaviour : MonoBehaviour, IConvertGameObjectToEntity
    {
        
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (enabled) {
                dstManager.AddComponentData(entity, new  TxAutotroph());
                dstManager.AddComponentData(entity, new  EnergyStore());
            }
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

        struct GainEnergy : IJobForEach<EnergyStore, TxAutotroph, Translation> {
            public void Execute(ref EnergyStore energyStore, [ReadOnly] ref TxAutotroph txAutotroph, 
                [ReadOnly] ref  Translation translation) {
                energyStore.Value += Enviroment.LightEnergy(translation.Value);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            GainEnergy job = new GainEnergy() {
            };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            return jobHandle;
        }
    }
}

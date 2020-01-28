using System.Collections.Generic;
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
    
    public class TxAutotrophBehaviour : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs {
        public GameObject stem;
        public GameObject leaf;
        public GameObject seedPod;
        
        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) {
            referencedPrefabs.Add(stem);
            referencedPrefabs.Add(leaf);
            referencedPrefabs.Add(seedPod);
        }
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager,
            GameObjectConversionSystem conversionSystem) {
            
            var stemEntity = conversionSystem.GetPrimaryEntity(stem);
            var leafEntity = conversionSystem.GetPrimaryEntity(leaf);
            var seedPodEntity = conversionSystem.GetPrimaryEntity(seedPod);
            
            if (enabled) {
                AddComponentDatas(entity, dstManager, stemEntity, leafEntity, seedPodEntity  );
            }
            
        }
        public static void AddComponentDatas(Entity entity, EntityManager dstManager,Entity stemEntity,
            Entity leafEntity, Entity seedPodEntity ){
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
            dstManager.AddComponentData(entity, new TxAutotrophParts() {
                stem = stemEntity,
                stemScale = dstManager.GetComponentData<NonUniformScale>(stemEntity).Value,
                leaf = leafEntity,
                leafScale = dstManager.GetComponentData<NonUniformScale>(leafEntity).Value,
                seedPod = seedPodEntity,
                seedPodScale = dstManager.GetComponentData<NonUniformScale>(seedPodEntity).Value,
            });
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
                energyStore.Value += Environment.LightEnergy(translation.Value)*Environment.Fitness(leaf.Value) ;
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

    [UpdateAfter(typeof(TxAutotrophPayMaintenance))]
    [BurstCompile]
    public class TxAutotrophGrow : JobComponentSystem {
        EntityQuery m_Group;
        protected EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
        
        protected override void OnCreate() {
            m_Group = GetEntityQuery(ComponentType.ReadWrite<EnergyStore>(),
                ComponentType.ReadWrite<Leaf>(),
                ComponentType.ReadWrite<Height>(),
                ComponentType.ReadWrite<Seed>(),
                ComponentType.ReadOnly<TxAutotroph>(),
                ComponentType.ReadOnly<TxAutotrophGenome>(),
                ComponentType.ReadOnly<TxAutotrophParts>()
            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        struct Grow : IJobForEachWithEntity<EnergyStore, Leaf, Height, Seed, TxAutotrophGenome,TxAutotrophParts> {
            
            public EntityCommandBuffer.Concurrent ecb;
            public void Execute(Entity entity, int index,ref EnergyStore energyStore,
                 ref Leaf leaf,
                 ref Height height,
                 ref Seed seed, 
                 [ReadOnly] ref TxAutotrophGenome txAutotrophGenome,
                 [ReadOnly] ref TxAutotrophParts txAutotrophParts
            ) {
                var sum = txAutotrophGenome.nrg2Height + txAutotrophGenome.nrg2Leaf + txAutotrophGenome.nrg2Seed +
                          txAutotrophGenome.nrg2Storage;
                var heightGrow = energyStore.Value * txAutotrophGenome.nrg2Height / sum;
                var leafGrow = energyStore.Value * txAutotrophGenome.nrg2Leaf / sum;
                var seedGrow = energyStore.Value * txAutotrophGenome.nrg2Seed / sum;
                height.Value += heightGrow;
                ecb.SetComponent(index, txAutotrophParts.stem, new NonUniformScale()
                    {Value = txAutotrophParts.stemScale*height.Value});
                leaf.Value += leafGrow;
                ecb.SetComponent(index, txAutotrophParts.leaf, new NonUniformScale()
                    {Value = txAutotrophParts.leafScale*leaf.Value});
                seed.Value += seedGrow;
                ecb.SetComponent(index, txAutotrophParts.seedPod, new NonUniformScale()
                    {Value = txAutotrophParts.seedPodScale*seed.Value});
                energyStore.Value -= heightGrow + leafGrow + seedGrow;
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
            Grow job = new Grow() { ecb=ecb };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            m_EndSimulationEcbSystem.AddJobHandleForProducer(jobHandle);
            return jobHandle;
        }
    }
    
    
  /*  
    [UpdateAfter(typeof(TxAutotrophMakeSproutSystem))]
    [BurstCompile]
    public class TxAutotrophSproutSystem : ComponentSystem {
        
        protected override void OnUpdate() {
            Entities
                .ForEach((ref TxAutotrophSprout txAutotrophSprout) => {
                    var sprout = EntityManager.Instantiate(Environment.prefabPlantStatic);
                    EntityManager.SetComponentData(sprout, new Translation(){Value = txAutotrophSprout.location});
            });
        }
    }
    
    
    
    */
    
    [UpdateAfter(typeof(TxAutotrophMakeSproutSystem))]
    [BurstCompile]
    public class TxAutotrophSproutSystem : JobComponentSystem {
        EntityQuery m_Group;
        
        Entity prefabEntity;
        protected EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
        
        protected override void OnCreate() {
            m_Group = GetEntityQuery(
                ComponentType.ReadOnly<TxAutotrophSprout>()
            );
            
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        
        struct Sprout : IJobForEachWithEntity< TxAutotrophSprout> {
            public Entity prefabEntity;
            public EntityCommandBuffer.Concurrent ecb;

            public void Execute(Entity entity, int index,
                [ReadOnly] ref TxAutotrophSprout txAutotrophSprout
            ) {
                var sprout = ecb.Instantiate(index,prefabEntity);
                var pos = txAutotrophSprout.location;
                ecb.SetComponent(index,sprout, new Translation(){Value = pos});
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            NativeArray<Entity> prefabArray = GetEntityQuery(
                ComponentType.ReadOnly<TxAutotroph>(),
                ComponentType.ReadOnly<Prefab>()
            ).ToEntityArray(Allocator.TempJob);
            if (prefabArray.Length > 0) {
                prefabEntity = prefabArray[0];
                var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
                Sprout job = new Sprout() {ecb = ecb, prefabEntity = prefabEntity};
                JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
                m_EndSimulationEcbSystem.AddJobHandleForProducer(jobHandle);
                prefabArray.Dispose();
                return jobHandle;
            }

            prefabArray.Dispose();
            return inputDeps;
        }
        
        

    }
    

    


    [UpdateAfter(typeof(TxAutotrophPayMaintenance))]
    [BurstCompile]
    public class TxAutotrophMakeSproutSystem : JobComponentSystem {
        EntityQuery m_Group;
        protected EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
        
        protected override void OnCreate() {
            m_Group = GetEntityQuery(
                ComponentType.ReadWrite<Seed>(),
                ComponentType.ReadOnly<TxAutotrophGenome>(),
                ComponentType.ReadOnly<Translation>()
            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        struct Sprout : IJobForEachWithEntity< Seed, TxAutotrophGenome,Translation> {
            
            public EntityCommandBuffer.Concurrent ecb;

            public void Execute(Entity entity, int index,
                 ref Seed seed,
                 [ReadOnly] ref TxAutotrophGenome txAutotrophGenome,
                 [ReadOnly] ref Translation translation
            ) {
                if (seed.Value > txAutotrophGenome.seedSize) {
                    var e = ecb.CreateEntity(index);
                    ecb.AddComponent<TxAutotrophSprout>(index,e,new TxAutotrophSprout(){energy = txAutotrophGenome.seedSize, location = translation.Value + new float3(100,0,100)});
                    seed.Value -= txAutotrophGenome.seedSize;
                }
            }
            
        }
       
        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
            Sprout job = new Sprout() { ecb=ecb };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            m_EndSimulationEcbSystem.AddJobHandleForProducer(jobHandle);
            return jobHandle;
           
        }
    }

    
}

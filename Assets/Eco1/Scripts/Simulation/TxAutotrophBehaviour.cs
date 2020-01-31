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
using UnityEditor.IMGUI.Controls;
using Material = Unity.Physics.Material;


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
                heightMultiple = 0.1f,
                ageMultiple = 0.1f
            });
            dstManager.AddComponentData(entity, new  Leaf() {Value = 1});
            dstManager.AddComponentData(entity, new  Height() {Value = 1});
            dstManager.AddComponentData(entity, new  Seed() {Value = 0});
            dstManager.AddComponentData(entity, new  Age() {Value = 0});
            dstManager.AddComponentData(entity, new  Shade() {Value = 0});
            dstManager.AddComponentData(entity, new  RandomComponent() {random = new Unity.Mathematics.Random(1)});
            dstManager.AddComponentData(entity, new  TxAutotrophGenome() {
                nrg2Height = 5,
                nrg2Leaf = 5,
                nrg2Seed = 5,
                nrg2Storage = 5,
                maxHeight = 5,
                maxLeaf = 5,
                seedSize = 5,
                ageRate = 2.2f
            });
            dstManager.AddComponentData(entity, new TxAutotrophParts() {
                stem = stemEntity,
                stemScale = 1,
                leaf = leafEntity,
                leafScale = 1,
                seedPod = seedPodEntity,
                seedPodScale = 1,
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
                ComponentType.ReadOnly<Leaf>(),
                ComponentType.ReadOnly<Shade>()
            );
        }

        struct TxGainEnergy : IJobForEach<EnergyStore,
            Translation,
            Leaf,
            Shade
            > {
            public void Execute(ref EnergyStore energyStore,
                [ReadOnly] ref  Translation translation,
                [ReadOnly] ref Leaf leaf,
                [ReadOnly] ref Shade shade
                ) {
                energyStore.Value += Environment.LightEnergy(translation.Value)*Environment.Fitness(leaf.Value)
                                                                               *leaf.Value/(leaf.Value+shade.Value) ;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            TxGainEnergy job = new TxGainEnergy() { };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            jobHandle.Complete();
            return jobHandle;
        }
    }
    
    [UpdateAfter(typeof(TxAutotrophLight))]
    [BurstCompile]
    public class TxAutotrophPayMaintenance : JobComponentSystem {
        EntityQuery m_Group;
        protected BeginPresentationEntityCommandBufferSystem m_BeginPresentationEcbSystem;
        protected override void OnCreate() {
            m_Group = GetEntityQuery(ComponentType.ReadWrite<EnergyStore>(),
                ComponentType.ReadWrite<Age>(),
                ComponentType.ReadOnly<TxAutotrophMaintenance>(),
                ComponentType.ReadOnly<Leaf>(),
                ComponentType.ReadOnly<Height>(),
                ComponentType.ReadOnly<TxAutotrophGenome>(),
                ComponentType.ReadOnly<TxAutotrophParts>()
            );
            m_BeginPresentationEcbSystem = World
                .GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }

        struct PayMaintenance : IJobChunk {
            public EntityCommandBuffer.Concurrent ecb;
            public ArchetypeChunkComponentType<EnergyStore> energyStoreType;
            public ArchetypeChunkComponentType<Age> ageType;
            [ReadOnly] public ArchetypeChunkComponentType<TxAutotrophMaintenance> txAutotrophMaintenanceType;
            [ReadOnly] public ArchetypeChunkComponentType<Leaf> leafType;
            [ReadOnly] public ArchetypeChunkComponentType<Height> heightType;
            [ReadOnly] public ArchetypeChunkComponentType<TxAutotrophGenome> txAutotrophGenomeType;
            [ReadOnly] public ArchetypeChunkComponentType<TxAutotrophParts> txAutotrophPartsType;
            [ReadOnly] public ArchetypeChunkEntityType entityType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                for (var i = 0; i < chunk.Count; i++) {
                    var energyStore = chunk.GetNativeArray(energyStoreType);
                    var age = chunk.GetNativeArray(ageType);
                    var txAutotrophMaintenance = chunk.GetNativeArray(txAutotrophMaintenanceType);
                    var leaf = chunk.GetNativeArray(leafType);
                    var height = chunk.GetNativeArray(heightType);
                    var txAutotrophGenome = chunk.GetNativeArray(txAutotrophGenomeType);
                    var txAutotrophParts = chunk.GetNativeArray(txAutotrophPartsType);
                    var entities = chunk.GetNativeArray(entityType);
                    
                    age[i] = new Age() {Value = age[i].Value+1};
                    energyStore[i] = new EnergyStore() {
                        Value =energyStore[i].Value - (txAutotrophMaintenance[i].baseValue +
                               txAutotrophMaintenance[i].leafMultiple * leaf[i].Value +
                               txAutotrophMaintenance[i].heightMultiple * height[i].Value +
                               txAutotrophMaintenance[i].ageMultiple * txAutotrophGenome[i].ageRate +
                                 age[i].Value / txAutotrophGenome[i].ageRate)
                    };
                    if (energyStore[i].Value < 0) {
                        ecb.DestroyEntity(chunkIndex, entities[i]);
                        ecb.DestroyEntity(chunkIndex, txAutotrophParts[i].stem);
                        ecb.DestroyEntity(chunkIndex, txAutotrophParts[i].leaf);
                        ecb.DestroyEntity(chunkIndex, txAutotrophParts[i].seedPod);
                    }
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var ecb = m_BeginPresentationEcbSystem.CreateCommandBuffer().ToConcurrent();
            var energyStoreType = GetArchetypeChunkComponentType<EnergyStore>(false);
            var ageType = GetArchetypeChunkComponentType<Age>(false);
            var txAutotrophMaintenanceType = GetArchetypeChunkComponentType<TxAutotrophMaintenance>(true);
            var leafType = GetArchetypeChunkComponentType<Leaf>(true);
            var heightType = GetArchetypeChunkComponentType<Height>(true);
            var txAutotrophGenomeType = GetArchetypeChunkComponentType<TxAutotrophGenome>(true);
            var txAutotrophPartsType = GetArchetypeChunkComponentType<TxAutotrophParts>(true);
            var entityType = GetArchetypeChunkEntityType();
            
            PayMaintenance job = new PayMaintenance() {
                energyStoreType = energyStoreType,
                ageType = ageType,
                txAutotrophMaintenanceType = txAutotrophMaintenanceType,
                leafType = leafType,
                heightType = heightType,
                txAutotrophGenomeType = txAutotrophGenomeType,
                txAutotrophPartsType = txAutotrophPartsType,
                entityType = entityType,
                ecb = ecb
            };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            jobHandle.Complete();
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

        struct Grow : IJobChunk {
           
            public ArchetypeChunkComponentType<EnergyStore> energyStoreType;
            public ArchetypeChunkComponentType<Leaf> leafType;
            public ArchetypeChunkComponentType<Height> heightType;
            public ArchetypeChunkComponentType<Seed> seedType;
            [ReadOnly] public ArchetypeChunkComponentType<TxAutotrophGenome> txAutotrophGenomeType;
            [ReadOnly] public ArchetypeChunkComponentType<TxAutotrophParts> txAutotrophPartsType;
            [ReadOnly] public ArchetypeChunkEntityType entityType;
            
            public EntityCommandBuffer.Concurrent ecb;
            public void Execute(ArchetypeChunk chunk, int index, int firstEntityIndex) {
                for (var i = 0; i < chunk.Count; i++) {
                    var energyStore = chunk.GetNativeArray(energyStoreType);
                    var seed = chunk.GetNativeArray(seedType);
                    var leaf = chunk.GetNativeArray(leafType);
                    var height = chunk.GetNativeArray(heightType);
                    var txAutotrophGenome = chunk.GetNativeArray(txAutotrophGenomeType);
                    var txAutotrophParts = chunk.GetNativeArray(txAutotrophPartsType);
                    var entities = chunk.GetNativeArray(entityType);
                    var heightShare = math.select(txAutotrophGenome[i].nrg2Height, 0,
                        height[i].Value >txAutotrophGenome[i].maxHeight);
                    var leafShare = math.select(txAutotrophGenome[i].nrg2Leaf, 0,
                        leaf[i].Value >txAutotrophGenome[i].maxLeaf);

                    
                    
                    var sum = heightShare + leafShare + txAutotrophGenome[i].nrg2Seed +
                              txAutotrophGenome[i].nrg2Storage;
                    var heightGrow = energyStore[i].Value * heightShare / sum;
                    var leafGrow = energyStore[i].Value * leafShare / sum;
                    var seedGrow = energyStore[i].Value * txAutotrophGenome[i].nrg2Seed / sum;
                    
                    if (heightGrow != 0) {
                        height[i] = new Height() {Value = height[i].Value + heightGrow};
                        ecb.AddComponent(index, txAutotrophParts[i].stem, new Scale()
                        {Value = txAutotrophParts[i].stemScale * height[i].Value});
                    }
                    
                    if (leafGrow != 0 || heightGrow != 0 ) {
                        leaf[i] = new Leaf() {Value = leaf[i].Value + leafGrow};
                        
                        ecb.AddComponent(index, txAutotrophParts[i].leaf, new Scale()
                            {Value = txAutotrophParts[i].leafScale * leaf[i].Value/height[i].Value});
                        
                        ecb.SetComponent(index, entities[i], new PhysicsCollider {
                            Value = Unity.Physics.SphereCollider.Create(
                                new SphereGeometry {
                                    Center = float3.zero,
                                    Radius = leaf[i].Value,
                                }, CollisionFilter.Default,new Material{Flags = Material.MaterialFlags.IsTrigger})
                        });
                    }
                    
                    seed[i] = new Seed() { Value = seed[i].Value + seedGrow};
                    ecb.AddComponent(index, txAutotrophParts[i].seedPod, new Scale()
                        {Value = txAutotrophParts[i].seedPodScale * seed[i].Value/height[i].Value});
                    energyStore[i] = new EnergyStore()
                        {Value = energyStore[i].Value - (heightGrow + leafGrow + seedGrow)};
                }
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var energyStoreType = GetArchetypeChunkComponentType<EnergyStore>(false);
            var seedType = GetArchetypeChunkComponentType<Seed>(false);
            var heightType = GetArchetypeChunkComponentType<Height>(false);
            var leafType = GetArchetypeChunkComponentType<Leaf>(false);
            
            var txAutotrophGenomeType = GetArchetypeChunkComponentType<TxAutotrophGenome>(true);
            var txAutotrophPartsType = GetArchetypeChunkComponentType<TxAutotrophParts>(true);
            var entityType = GetArchetypeChunkEntityType();
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
            Grow job = new Grow() {
                energyStoreType = energyStoreType,
                seedType = seedType,
                leafType = leafType,
                heightType = heightType,
                txAutotrophGenomeType = txAutotrophGenomeType,
                txAutotrophPartsType = txAutotrophPartsType,
                entityType = entityType,
                ecb=ecb
            };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            m_EndSimulationEcbSystem.AddJobHandleForProducer(jobHandle);
            jobHandle.Complete();
            return jobHandle;
        }
    }
    
    [UpdateAfter(typeof(TxAutotrophMakeSproutSystem))]
    [BurstCompile]
    public class TxAutotrophSproutSystem : JobComponentSystem {
        EntityQuery m_Group;
        
        Entity prefabEntity;
        protected EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
        
        protected override void OnCreate() {
            m_Group = GetEntityQuery(ComponentType.ReadWrite<RandomComponent>(),
                ComponentType.ReadOnly<TxAutotrophSprout>(),
                ComponentType.ReadOnly<TxAutotrophGenome>()
            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        struct Sprout : IJobForEachWithEntity<RandomComponent, TxAutotrophSprout,TxAutotrophGenome> {
            public Entity prefabEntity;
            public EntityCommandBuffer.Concurrent ecb;

            public void Execute(Entity entity, int index,
                ref RandomComponent randomComponent,
                [ReadOnly] ref TxAutotrophSprout txAutotrophSprout,
                [ReadOnly] ref TxAutotrophGenome txAutotrophGenome
            ) {
                var sprout = ecb.Instantiate(index,prefabEntity);
                var pos = txAutotrophSprout.location;
                ecb.SetComponent(index,sprout, new Translation(){Value = pos});
                ecb.AddComponent<RandomComponent>(index,sprout,new RandomComponent()
                    {random = new Unity.Mathematics.Random(randomComponent.random.NextUInt())});
                ecb.SetComponent(index,sprout, new TxAutotrophGenome{
                    nrg2Height = txAutotrophGenome.nrg2Height,
                    nrg2Leaf = txAutotrophGenome.nrg2Leaf,
                    nrg2Seed = txAutotrophGenome.nrg2Seed,
                    nrg2Storage = txAutotrophGenome.nrg2Storage,
                    maxHeight = txAutotrophGenome.maxHeight,
                    maxLeaf = txAutotrophGenome.maxLeaf,
                    ageRate = txAutotrophGenome.ageRate,
                    seedSize = txAutotrophGenome.seedSize}
                        );
                ecb.RemoveComponent<TxAutotrophGenome>(index,entity);
                ecb.DestroyEntity(index,entity);
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
                jobHandle.Complete();
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
                ComponentType.ReadWrite<RandomComponent>(),
                ComponentType.ReadOnly<TxAutotrophGenome>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<Height>()
            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        struct Sprout : IJobForEachWithEntity< Seed,RandomComponent, TxAutotrophGenome,Translation,Height> {
            
            public EntityCommandBuffer.Concurrent ecb;

            private float Mutate(float val, ref Unity.Mathematics.Random random) {
                var mutant = math.max(1,val * random.NextFloat(0.95f, 1.05f));
                return math.select(val, mutant,0.05f<random.NextFloat(0,1));
            }
            
            public void Execute(Entity entity, int index,
                 ref Seed seed,
                 ref RandomComponent randomComponent,
                 [ReadOnly] ref TxAutotrophGenome txAutotrophGenome,
                 [ReadOnly] ref Translation translation,
                 [ReadOnly] ref Height height
            ) {
                while (seed.Value > txAutotrophGenome.seedSize) {
                    seed.Value -= txAutotrophGenome.seedSize;
                    var e = ecb.CreateEntity(index);
                    var loc = randomComponent.random.NextFloat2(-20, 20)*height.Value/txAutotrophGenome.seedSize;
                    
                    var location = translation.Value + new float3(loc.x, 0, loc.y);
                    if (location.x > Environment.bounds.x && location.x < Environment.bounds.z  &&
                        location.z > Environment.bounds.y && location.z < Environment.bounds.w ) {
                        ecb.AddComponent<TxAutotrophSprout>(index, e, new TxAutotrophSprout() {
                            energy = txAutotrophGenome.seedSize,
                            location = location
                        });
                        ecb.AddComponent<RandomComponent>(index,e,new RandomComponent()
                            {random = new Unity.Mathematics.Random(randomComponent.random.NextUInt())});
                    }
                    var newGenome = new TxAutotrophGenome();
                    newGenome.nrg2Height = Mutate(txAutotrophGenome.nrg2Height,ref randomComponent.random);
                    newGenome.nrg2Leaf =Mutate(txAutotrophGenome.nrg2Leaf,ref randomComponent.random);
                    newGenome.nrg2Seed = Mutate(txAutotrophGenome.nrg2Seed,ref randomComponent.random);
                    newGenome.nrg2Storage =Mutate(txAutotrophGenome.nrg2Storage,ref randomComponent.random);
                    newGenome.maxHeight = Mutate(txAutotrophGenome.maxHeight,ref randomComponent.random);
                    newGenome.maxLeaf =Mutate(txAutotrophGenome.maxLeaf,ref randomComponent.random);
                    newGenome.ageRate = Mutate(txAutotrophGenome.ageRate,ref randomComponent.random);
                    newGenome.seedSize =Mutate(txAutotrophGenome.seedSize,ref randomComponent.random);
                    ecb.AddComponent<TxAutotrophGenome>(index,e,newGenome);
                }
            }
        }
       
        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
            Sprout job = new Sprout() { ecb=ecb };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            m_EndSimulationEcbSystem.AddJobHandleForProducer(jobHandle);
            jobHandle.Complete();
            return jobHandle;
            
        }
    }

    
}

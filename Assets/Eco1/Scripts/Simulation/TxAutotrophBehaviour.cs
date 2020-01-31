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
using Collider = UnityEngine.Collider;
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
            dstManager.AddComponentData(entity, new  TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed =  0,
                age = 0
            });
            
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
                ComponentType.ReadOnly<TxAutotrophPhenotype>(),
                ComponentType.ReadOnly<Shade>()
            );
        }

        struct TxGainEnergy : IJobForEach<EnergyStore,
            Translation,
            TxAutotrophPhenotype,
            Shade
            > {
            public void Execute(ref EnergyStore energyStore,
                [ReadOnly] ref  Translation translation,
                [ReadOnly] ref TxAutotrophPhenotype TxAutotrophPhenotype,
                [ReadOnly] ref Shade shade
                ) {
                energyStore.Value += Environment.LightEnergy(translation.Value)*Environment.Fitness(TxAutotrophPhenotype.leaf)
                                                                               *TxAutotrophPhenotype.leaf/(TxAutotrophPhenotype.leaf+shade.Value) ;
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
                ComponentType.ReadWrite<TxAutotrophPhenotype>(),
                ComponentType.ReadOnly<TxAutotrophMaintenance>(),
                ComponentType.ReadOnly<TxAutotrophGenome>(),
                ComponentType.ReadOnly<TxAutotrophParts>()
            );
            m_BeginPresentationEcbSystem = World
                .GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }

        struct PayMaintenance : IJobForEachWithEntity<EnergyStore,TxAutotrophPhenotype,TxAutotrophMaintenance,
            TxAutotrophGenome, TxAutotrophParts> {
            public EntityCommandBuffer.Concurrent ecb;
           
            public void Execute(Entity entity, int index, ref EnergyStore energyStore,
                ref TxAutotrophPhenotype txAutotrophPhenotype,
                [ReadOnly] ref TxAutotrophMaintenance txAutotrophMaintenance,
                [ReadOnly]ref TxAutotrophGenome txAutotrophGenome,
                [ReadOnly]ref TxAutotrophParts txAutotrophParts) {
             
               
                    txAutotrophPhenotype = new TxAutotrophPhenotype() {
                        age = txAutotrophPhenotype.age+1,
                        height = txAutotrophPhenotype.height,
                        leaf = txAutotrophPhenotype.leaf,
                        seed = txAutotrophPhenotype.seed
                    };
                    energyStore = new EnergyStore() {
                        Value =energyStore.Value - (txAutotrophMaintenance.baseValue +
                               txAutotrophMaintenance.leafMultiple * txAutotrophPhenotype.leaf +
                               txAutotrophMaintenance.heightMultiple * txAutotrophPhenotype.height +
                               txAutotrophMaintenance.ageMultiple * txAutotrophGenome.ageRate +
                                 txAutotrophPhenotype.age / txAutotrophGenome.ageRate)
                    };
                    if (energyStore.Value < 0) {
                        ecb.DestroyEntity(index, entity);
                        ecb.DestroyEntity(index, txAutotrophParts.stem);
                        ecb.DestroyEntity(index, txAutotrophParts.leaf);
                        ecb.DestroyEntity(index, txAutotrophParts.seedPod);
                    }
                
            }

           
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var ecb = m_BeginPresentationEcbSystem.CreateCommandBuffer().ToConcurrent();
            
            
            PayMaintenance job = new PayMaintenance() {
                
                ecb = ecb
            };
            JobHandle jobHandle = job.Run(m_Group, inputDeps);
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
                ComponentType.ReadWrite<TxAutotrophPhenotype>(),
                ComponentType.ReadOnly<TxAutotroph>(),
                ComponentType.ReadOnly<TxAutotrophGenome>(),
                ComponentType.ReadOnly<TxAutotrophParts>()
            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        struct Grow : IJobForEachWithEntity<EnergyStore,TxAutotrophPhenotype,TxAutotrophGenome,TxAutotrophParts> {
            public EntityCommandBuffer.Concurrent ecb;
            public void Execute(Entity entity, int index, ref EnergyStore energyStore,
                [ReadOnly] ref TxAutotrophPhenotype txAutotrophPhenotype,
                [ReadOnly] ref TxAutotrophGenome txAutotrophGenome,
                [ReadOnly] ref TxAutotrophParts txAutotrophParts) {
                
                var heightShare = math.select(txAutotrophGenome.nrg2Height, 0,
                    txAutotrophPhenotype.height >txAutotrophGenome.maxHeight);
                var leafShare = math.select(txAutotrophGenome.nrg2Leaf, 0,
                    txAutotrophPhenotype.leaf >txAutotrophGenome.maxLeaf);

                
                
                var sum = heightShare + leafShare + txAutotrophGenome.nrg2Seed +
                          txAutotrophGenome.nrg2Storage;
                var heightGrow = energyStore.Value * heightShare / sum;
                var leafGrow = energyStore.Value * leafShare / sum;
                var seedGrow = energyStore.Value * txAutotrophGenome.nrg2Seed / sum;
                
                txAutotrophPhenotype = new TxAutotrophPhenotype() {
                    age = txAutotrophPhenotype.age,
                    height = txAutotrophPhenotype.height+ heightGrow,
                    leaf = txAutotrophPhenotype.leaf + leafGrow,
                    seed = txAutotrophPhenotype.seed + seedGrow
                };
                
                if (heightGrow != 0) {
                    ecb.AddComponent(index, txAutotrophParts.stem, new Scale()
                        {Value = txAutotrophParts.stemScale * txAutotrophPhenotype.height});
                }
                if (leafGrow != 0 || heightGrow != 0 ) {    
                    ecb.AddComponent(index, txAutotrophParts.leaf, new Scale()
                        {Value = txAutotrophParts.leafScale *
                                 txAutotrophPhenotype.leaf/txAutotrophPhenotype.height});
                    
                    ecb.SetComponent(index, entity, new PhysicsCollider {
                        Value = Unity.Physics.SphereCollider.Create(
                            new SphereGeometry {
                                Center = float3.zero,
                                Radius = txAutotrophPhenotype.leaf,
                            }, CollisionFilter.Default,new Material{Flags = Material.MaterialFlags.IsTrigger})
                    });
                }
                
                ecb.AddComponent(index, txAutotrophParts.seedPod, new Scale()
                    {Value = txAutotrophParts.seedPodScale * txAutotrophPhenotype.seed/txAutotrophPhenotype.height});
                energyStore = new EnergyStore()
                    {Value = energyStore.Value - (heightGrow + leafGrow + seedGrow)};
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
            Grow job = new Grow() {
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
                //Debug.Log("Destroy  TxAutotrophSprout : "+ entity.Index);
                
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
                JobHandle jobHandle = job.Run(m_Group);
                    //Schedule(m_Group, inputDeps);
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
                ComponentType.ReadWrite<TxAutotrophPhenotype>(),
                ComponentType.ReadWrite<RandomComponent>(),
                ComponentType.ReadOnly<TxAutotrophGenome>(),
                ComponentType.ReadOnly<Translation>()
            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        struct Sprout : IJobForEachWithEntity< TxAutotrophPhenotype,RandomComponent, TxAutotrophGenome,Translation> {
            
            public EntityCommandBuffer.Concurrent ecb;

            private float Mutate(float val, ref Unity.Mathematics.Random random) {
                var mutant = math.max(1,val * random.NextFloat(0.95f, 1.05f));
                return math.select(val, mutant,0.05f<random.NextFloat(0,1));
            }
            
            public void Execute(Entity entity, int index,
                 ref TxAutotrophPhenotype txAutotrophPhenotype,
                 ref RandomComponent randomComponent,
                 [ReadOnly] ref TxAutotrophGenome txAutotrophGenome,
                 [ReadOnly] ref Translation translation
            ) {
                while (txAutotrophPhenotype.seed > txAutotrophGenome.seedSize) {
                    txAutotrophPhenotype.seed -= txAutotrophGenome.seedSize;
                    
                    var loc = randomComponent.random.NextFloat2(-20, 20)*txAutotrophPhenotype.height/txAutotrophGenome.seedSize;
                    
                    var location = translation.Value + new float3(loc.x, 0, loc.y);
                    if (location.x > Environment.bounds.x && location.x < Environment.bounds.z &&
                        location.z > Environment.bounds.y && location.z < Environment.bounds.w) {
                        var e = ecb.CreateEntity(index);
                        ecb.AddComponent<TxAutotrophSprout>(index, e, new TxAutotrophSprout() {
                            energy = txAutotrophGenome.seedSize,
                            location = location
                        });
                        ecb.AddComponent<RandomComponent>(index, e, new RandomComponent()
                            {random = new Unity.Mathematics.Random(randomComponent.random.NextUInt())});

                        var newGenome = new TxAutotrophGenome();
                        newGenome.nrg2Height = Mutate(txAutotrophGenome.nrg2Height, ref randomComponent.random);
                        newGenome.nrg2Leaf = Mutate(txAutotrophGenome.nrg2Leaf, ref randomComponent.random);
                        newGenome.nrg2Seed = Mutate(txAutotrophGenome.nrg2Seed, ref randomComponent.random);
                        newGenome.nrg2Storage = Mutate(txAutotrophGenome.nrg2Storage, ref randomComponent.random);
                        newGenome.maxHeight = Mutate(txAutotrophGenome.maxHeight, ref randomComponent.random);
                        newGenome.maxLeaf = Mutate(txAutotrophGenome.maxLeaf, ref randomComponent.random);
                        newGenome.ageRate = Mutate(txAutotrophGenome.ageRate, ref randomComponent.random);
                        newGenome.seedSize = Mutate(txAutotrophGenome.seedSize, ref randomComponent.random);
                        ecb.AddComponent<TxAutotrophGenome>(index, e, newGenome);
                    }
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
/*
    public class TxDebugSystem : JobComponentSystem {
        EntityQuery m_Group;

        protected override void OnCreate() {
            m_Group = GetEntityQuery(ComponentType.ReadOnly<TxAutotrophGenome>());
        }

        struct DebuJob : IJobForEachWithEntity<TxAutotrophGenome> {
            public void Execute(Entity entity, int index,[ReadOnly] ref TxAutotrophGenome c0) {
                Debug.Log("Debug: I:"+entity.Index + " : " + c0.nrg2Height);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            DebuJob job = new DebuJob() {
            };
            JobHandle jobHandle = job.Run(m_Group, inputDeps);
            return jobHandle;
        }
    }
  */  
}

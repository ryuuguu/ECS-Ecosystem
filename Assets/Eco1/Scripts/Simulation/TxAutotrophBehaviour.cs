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
        
        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) {
            referencedPrefabs.Add(stem);
            
        }
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager,
            GameObjectConversionSystem conversionSystem) {
            
            var stemEntity = conversionSystem.GetPrimaryEntity(stem);
            
            if (enabled) {
                AddComponentDatas(entity, dstManager); //, leafEntity, seedPodEntity  );
            }
            
        }
        public static void AddComponentDatas(Entity entity, EntityManager dstManager){
           // ,
           // Entity leafEntity, Entity seedPodEntity ){
            dstManager.AddComponentData(entity, new  TxAutotroph());
            dstManager.AddComponentData(entity, new  EnergyStore(){Value = 0});
            dstManager.AddComponentData(entity, new  TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed =  0,
                age = 0
            });
            dstManager.AddComponentData(entity, new  Scale() {Value = 1});
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
            dstManager.AddComponentData(entity, new TxAutotrophParts {
                stem = entity,
                
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
            
            [ReadOnly]public NativeArray<Environment.EnvironmentSettings> environmentSettings;
            
            public void Execute(ref EnergyStore energyStore,
                [ReadOnly] ref  Translation translation,
                [ReadOnly] ref TxAutotrophPhenotype TxAutotrophPhenotype,
                [ReadOnly] ref Shade shade
                ) {
                energyStore.Value += 
                    Environment.LightEnergy(translation.Value,
                        environmentSettings[0].environmentConsts.ambientLight,
                        environmentSettings[0].environmentConsts.variableLight
                        )
                    *Environment.Fitness(TxAutotrophPhenotype.leaf) 
                    *TxAutotrophPhenotype.leaf/
                    (TxAutotrophPhenotype.leaf+shade.Value*
                     environmentSettings[0].txAutotrophConsts.LeafShadeEffectMultiplier) ;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            TxGainEnergy job = new TxGainEnergy() {
                environmentSettings = Environment.environmentSettings
            };
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
                ComponentType.ReadOnly<TxAutotrophGenome>(),
                ComponentType.ReadOnly<TxAutotrophParts>()
            );
            m_BeginPresentationEcbSystem = World
                .GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }

        struct PayMaintenance : IJobForEachWithEntity<EnergyStore,TxAutotrophPhenotype,
            TxAutotrophGenome, TxAutotrophParts> {
            [ReadOnly]public NativeArray<Environment.EnvironmentSettings> environmentSettings;
            
            public EntityCommandBuffer.Concurrent ecb;
           
            public void Execute(Entity entity, int index, ref EnergyStore energyStore,
                ref TxAutotrophPhenotype txAutotrophPhenotype,
                [ReadOnly]ref TxAutotrophGenome txAutotrophGenome,
                [ReadOnly]ref TxAutotrophParts txAutotrophParts) {
             
                var txAutotrophConsts = environmentSettings[0].txAutotrophConsts;
                
                txAutotrophPhenotype = new TxAutotrophPhenotype() {
                    age = txAutotrophPhenotype.age+1,
                    height = txAutotrophPhenotype.height,
                    leaf = txAutotrophPhenotype.leaf,
                    seed = txAutotrophPhenotype.seed
                };
                energyStore = new EnergyStore() {
                    Value =energyStore.Value
                           - (txAutotrophConsts.baseValue + 
                              txAutotrophConsts.leafMultiple * txAutotrophPhenotype.leaf +
                              txAutotrophConsts.heightMultiple * txAutotrophPhenotype.height +
                              txAutotrophConsts.ageMultiple * txAutotrophGenome.ageRate +
                              txAutotrophPhenotype.age / txAutotrophGenome.ageRate)
                };
                if (energyStore.Value < 0) {
                    ecb.DestroyEntity(index, entity);
                    //ecb.DestroyEntity(index, txAutotrophParts.stem);
                    ecb.DestroyEntity(index, txAutotrophParts.leaf);
                    //ecb.DestroyEntity(index, txAutotrophParts.seedPod);
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var ecb = m_BeginPresentationEcbSystem.CreateCommandBuffer().ToConcurrent();

            PayMaintenance job = new PayMaintenance() {
                environmentSettings =Environment.environmentSettings,
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
                ComponentType.ReadWrite<Scale>(),
                ComponentType.ReadOnly<TxAutotrophGenome>(),
                ComponentType.ReadOnly<TxAutotrophParts>(),
                ComponentType.ReadOnly<Translation>()
                
            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        struct Grow : IJobForEachWithEntity<EnergyStore, TxAutotrophPhenotype, Scale,
            TxAutotrophGenome, TxAutotrophParts, Translation> {
            [ReadOnly]public NativeArray<Environment.EnvironmentSettings> environmentSettings;
            
            public EntityCommandBuffer.Concurrent ecb;
            public void Execute(Entity entity, int index, ref EnergyStore energyStore, 
                ref TxAutotrophPhenotype txAutotrophPhenotype,
                ref Scale scale,
                [ReadOnly] ref TxAutotrophGenome txAutotrophGenome,
                [ReadOnly] ref TxAutotrophParts txAutotrophParts,
                [ReadOnly] ref Translation translation
                ) {
                var txAutotrophConsts = environmentSettings[0].txAutotrophConsts;
                
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
                    seed = txAutotrophPhenotype.seed + seedGrow/environmentSettings[0].txAutotrophConsts.seedDivisor
                };
                
                
                if (heightGrow != 0) {
                   // ecb.SetComponent(index, txAutotrophParts.stem, new Scale()
                   //     {Value = txAutotrophParts.stemScale * txAutotrophPhenotype.height});
                   scale.Value = txAutotrophConsts.stemScale * txAutotrophPhenotype.height;
                    ecb.SetComponent(index, txAutotrophParts.leaf, new Translation
                    {Value = new float3(translation.Value.x,
                                 txAutotrophConsts.stemScale * txAutotrophPhenotype.height*0.8f,
                        translation.Value.z)});
                    
                }
                
                if (leafGrow != 0 ) {    
                   ecb.SetComponent(index, txAutotrophParts.leaf, new Scale()
                   {
                       Value = math.sqrt(txAutotrophPhenotype.leaf)* txAutotrophConsts.leafScale       
                   });
                    
                    ecb.SetComponent(index, entity, new PhysicsCollider {
                        Value = Unity.Physics.SphereCollider.Create(
                            new SphereGeometry {
                                Center = float3.zero,
                                Radius = txAutotrophPhenotype.leaf* txAutotrophConsts.LeafShadeRadiusMultiplier,
                            }, CollisionFilter.Default,new Material{Flags = Material.MaterialFlags.IsTrigger})
                    });
                }
                
                
                //ecb.AddComponent(index, txAutotrophParts.seedPod, new Scale()
                //    {Value = txAutotrophParts.seedPodScale * txAutotrophPhenotype.seed/txAutotrophPhenotype.height});
                
                energyStore = new EnergyStore()
                    {Value = energyStore.Value - (heightGrow + leafGrow + seedGrow)};
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
            Grow job = new Grow() {
                environmentSettings = Environment.environmentSettings,
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
        Entity prefabLeafEntity;
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
            public Entity prefabLeafEntity;
            public EntityCommandBuffer.Concurrent ecb;
            
            public void Execute(Entity entity, int index,
                ref RandomComponent randomComponent,
                [ReadOnly] ref TxAutotrophSprout txAutotrophSprout,
                [ReadOnly] ref TxAutotrophGenome txAutotrophGenome
            ) {
                var sprout = ecb.Instantiate(index,prefabEntity);
                var leaf = ecb.Instantiate(index,prefabLeafEntity);
                var pos = txAutotrophSprout.location;
                ecb.SetComponent(index,sprout, new Translation(){Value = pos});
                ecb.SetComponent(index,leaf, new Translation(){Value = pos + new float3(0,0.8f,0)});
                ecb.AddComponent(index,leaf, new Scale{Value = 1});
                ecb.SetComponent(index,sprout,new TxAutotrophParts {
                    leaf =  leaf
                });
                ecb.AddComponent(index,sprout, new Scale{Value = 1});
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
                ecb.SetComponent(index,sprout,new  EnergyStore{Value =txAutotrophSprout.energy});
                ecb.RemoveComponent<TxAutotrophGenome>(index,entity);
                ecb.DestroyEntity(index,entity);
                
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            
            
            //this could be set once per environment run
            NativeArray<Entity> prefabArray = GetEntityQuery(
                ComponentType.ReadOnly<TxAutotroph>(),
                ComponentType.ReadOnly<Prefab>()
            ).ToEntityArray(Allocator.TempJob);
            NativeArray<Entity> prefabLeafArray = GetEntityQuery(
                ComponentType.ReadOnly<TxAutotrophLeafMeshFlag>(),
                ComponentType.ReadOnly<Prefab>()
            ).ToEntityArray(Allocator.TempJob);
            if (prefabArray.Length > 0) {
                prefabEntity = prefabArray[0];
                prefabLeafEntity = prefabLeafArray[0];
                var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
                Sprout job = new Sprout() {
                    ecb = ecb,
                    prefabEntity = prefabEntity,
                    prefabLeafEntity = prefabLeafEntity
                };
                JobHandle jobHandle = job.Schedule(m_Group,inputDeps);
                    //Schedule(m_Group, inputDeps);
                m_EndSimulationEcbSystem.AddJobHandleForProducer(jobHandle);
                prefabArray.Dispose();
                prefabLeafArray.Dispose();
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
            [ReadOnly]public NativeArray<Environment.EnvironmentSettings> environmentSettings;
            
            
            public void Execute(Entity entity, int index,
                 ref TxAutotrophPhenotype txAutotrophPhenotype,
                 ref RandomComponent randomComponent,
                 [ReadOnly] ref TxAutotrophGenome txAutotrophGenome,
                 [ReadOnly] ref Translation translation
            ) {
                
                float Mutate(float val, ref Unity.Mathematics.Random random, float rate, float rangel, float rangeH) {
                    var mutant = math.max(1,val * random.NextFloat(rangel, rangeH));
                    return math.select(val, mutant,rate<random.NextFloat(0,1));
                }
                var txAutotrophConsts = environmentSettings[0].txAutotrophConsts;
                var mRate = environmentSettings[0].txAutotrophConsts.mutationRate;
                var mRange = environmentSettings[0].txAutotrophConsts.mutationRange;
                var mRangeH = 1 + mRange;
                var mRangeL = 1 - mRange;
                var environmentConsts = environmentSettings[0].environmentConsts;
                
                while (txAutotrophPhenotype.seed > txAutotrophGenome.seedSize) {
                    txAutotrophPhenotype.seed -= txAutotrophGenome.seedSize;

                    var loc =txAutotrophConsts.seedRangeMultiplier * randomComponent.random.NextFloat2(-1, 1)*txAutotrophPhenotype.height/txAutotrophGenome.seedSize;
                    
                    var location = translation.Value + new float3(loc.x, 0, loc.y);
                    if (location.x > environmentConsts.bounds.x && location.x < environmentConsts.bounds.z &&
                        location.z > environmentConsts.bounds.y && location.z < environmentConsts.bounds.w) {
                        var e = ecb.CreateEntity(index);
                        ecb.AddComponent<TxAutotrophSprout>(index, e, new TxAutotrophSprout() {
                            energy = txAutotrophGenome.seedSize,
                            location = location
                            
                        });
                        ecb.AddComponent<RandomComponent>(index, e, new RandomComponent()
                            {random = new Unity.Mathematics.Random(randomComponent.random.NextUInt())});

                        var newGenome = new TxAutotrophGenome();
                        newGenome.nrg2Height = Mutate(txAutotrophGenome.nrg2Height, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH);
                        newGenome.nrg2Leaf = Mutate(txAutotrophGenome.nrg2Leaf, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH);
                        newGenome.nrg2Seed = Mutate(txAutotrophGenome.nrg2Seed, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH);
                        newGenome.nrg2Storage = Mutate(txAutotrophGenome.nrg2Storage, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH);
                        newGenome.maxHeight = Mutate(txAutotrophGenome.maxHeight, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH);
                        newGenome.maxLeaf = Mutate(txAutotrophGenome.maxLeaf, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH);
                        newGenome.ageRate = Mutate(txAutotrophGenome.ageRate, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH);
                        newGenome.seedSize = Mutate(txAutotrophGenome.seedSize, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH);
                        ecb.AddComponent<TxAutotrophGenome>(index, e, newGenome);
                    }
                }
            }
        }
       
        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
            Sprout job = new Sprout() {
                environmentSettings = Environment.environmentSettings,
                ecb=ecb
            };
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

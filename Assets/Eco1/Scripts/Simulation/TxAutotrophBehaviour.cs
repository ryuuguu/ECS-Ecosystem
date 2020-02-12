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
using Unity.Rendering;

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

        public static void AddComponentDatas(Entity entity, EntityManager dstManager) {
            dstManager.AddComponentData(entity, new TxAutotroph());
            dstManager.AddComponentData(entity, new EnergyStore() {Value = 0});
            dstManager.AddComponentData(entity, new TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed = 0,
                age = 0
            });
            dstManager.AddComponentData(entity, new Scale() {Value = 1.1f});
            dstManager.AddComponentData(entity, new Shade() {Value = 0});
            dstManager.AddComponentData(entity, new RandomComponent() {random = new Unity.Mathematics.Random(1)});
            dstManager.AddComponentData(entity, new TxAutotrophChrome1AB());
            dstManager.AddComponentData(entity, new  TxAutotrophChrome1W{ Value = new TxAutotrophChrome1 {
                nrg2Height = 5,
                nrg2Leaf = 5,
                nrg2Seed = 5,
                nrg2Storage = 5,
                maxHeight = 5,
                maxLeaf = 5,
                seedSize = 5,
                ageRate = 2.2f
            }});
            dstManager.AddComponentData(entity, new TxAutotrophColorGenome());
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
                    Environment.LightEnergySine(translation.Value,
                        environmentSettings[0].environmentConsts.ambientLight,
                        environmentSettings[0].environmentConsts.variableLight
                        )
                    *Environment.Fitness(TxAutotrophPhenotype.leaf) 
                    *TxAutotrophPhenotype.leaf/
                    (TxAutotrophPhenotype.leaf+shade.Value*
                     environmentSettings[0].txAutotrophConsts.leafShadeEffectMultiplier) ;
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
                ComponentType.ReadOnly<TxAutotrophChrome1W>(),
                ComponentType.ReadOnly<TxAutotrophParts>()
            );
            m_BeginPresentationEcbSystem = World
                .GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }

        struct PayMaintenance : IJobForEachWithEntity<EnergyStore,TxAutotrophPhenotype,
            TxAutotrophChrome1W, TxAutotrophParts> {
            [ReadOnly]public NativeArray<Environment.EnvironmentSettings> environmentSettings;
            
            public EntityCommandBuffer.Concurrent ecb;
           
            public void Execute(Entity entity, int index, ref EnergyStore energyStore,
                ref TxAutotrophPhenotype txAutotrophPhenotype,
                [ReadOnly]ref TxAutotrophChrome1W txAutotrophChrome1W,
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
                           - (txAutotrophConsts.baseCost + 
                              txAutotrophConsts.leafCostMultiple * txAutotrophPhenotype.leaf +
                              txAutotrophConsts.heightCostMultiple * txAutotrophPhenotype.height +
                              txAutotrophConsts.ageMultiple * txAutotrophChrome1W.Value.ageRate +
                              txAutotrophPhenotype.age / txAutotrophChrome1W.Value.ageRate+
                              txAutotrophConsts.pollenCostMultiple  * txAutotrophChrome1W.Value.pollenRange
                              )
                };
                if (energyStore.Value < 0) {
                    ecb.DestroyEntity(index, entity);
                    //ecb.DestroyEntity(index, txAutotrophParts.stem);
                    //ecb.DestroyEntity(index, txAutotrophParts.leaf);
                    ecb.DestroyEntity(index, txAutotrophParts.petal0);
                    ecb.DestroyEntity(index, txAutotrophParts.petal1);
                    ecb.DestroyEntity(index, txAutotrophParts.petal2);
                    ecb.DestroyEntity(index, txAutotrophParts.petal3);
                    ecb.DestroyEntity(index, txAutotrophParts.petal4);
                    ecb.DestroyEntity(index, txAutotrophParts.petal5);
                    ecb.DestroyEntity(index, txAutotrophParts.pollen);
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var ecb = m_BeginPresentationEcbSystem.CreateCommandBuffer().ToConcurrent();

            PayMaintenance job = new PayMaintenance() {
                environmentSettings =Environment.environmentSettings,
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
                ComponentType.ReadWrite<TxAutotrophPhenotype>(),
                ComponentType.ReadWrite<Scale>(),
                ComponentType.ReadOnly<TxAutotrophChrome1W>(),
                ComponentType.ReadOnly<TxAutotrophParts>(),
                ComponentType.ReadOnly<Translation>()
            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        struct Grow : IJobForEachWithEntity<EnergyStore, TxAutotrophPhenotype, Scale,
            TxAutotrophChrome1W, TxAutotrophParts, Translation> {
            [ReadOnly]public NativeArray<Environment.EnvironmentSettings> environmentSettings;
            
            public EntityCommandBuffer.Concurrent ecb;
            public void Execute(Entity entity, int index, ref EnergyStore energyStore, 
                ref TxAutotrophPhenotype txAutotrophPhenotype,
                ref Scale scale,
                [ReadOnly] ref TxAutotrophChrome1W txAutotrophChrome1W,
                [ReadOnly] ref TxAutotrophParts txAutotrophParts,
                [ReadOnly] ref Translation translation
                ) {
                var txAutotrophConsts = environmentSettings[0].txAutotrophConsts;
                var sum = txAutotrophChrome1W.Value.nrg2Height 
                          + txAutotrophChrome1W.Value.nrg2Leaf
                          + txAutotrophChrome1W.Value.nrg2Seed 
                          + txAutotrophChrome1W.Value.nrg2Storage;
                var heightEnergy = energyStore.Value * txAutotrophChrome1W.Value.nrg2Height/sum;
                var heightGrow  = math.min(heightEnergy, 
                    txAutotrophChrome1W.Value.maxHeight- txAutotrophPhenotype.height );
                
                var leafEnergy =  energyStore.Value * txAutotrophChrome1W.Value.nrg2Leaf/sum;
                var leafGrow = math.min(leafEnergy,
                    txAutotrophChrome1W.Value.maxLeaf - txAutotrophPhenotype.leaf );
                var seedGrow = energyStore.Value * txAutotrophChrome1W.Value.nrg2Seed / sum;
                
                txAutotrophPhenotype = new TxAutotrophPhenotype() {
                    age = txAutotrophPhenotype.age,
                    height = txAutotrophPhenotype.height+ heightGrow,
                    leaf = txAutotrophPhenotype.leaf + leafGrow,
                    seed = txAutotrophPhenotype.seed + seedGrow/environmentSettings[0].txAutotrophConsts.seedDivisor
                };
                
                
                if (heightGrow != 0) {
                    scale.Value = txAutotrophConsts.stemScale * txAutotrophPhenotype.height;
                   
                    ecb.SetComponent(index, txAutotrophParts.petal0, new Translation
                    {Value = new float3(translation.Value.x,
                        translation.Value.y+txAutotrophConsts.stemScale * txAutotrophPhenotype.height*0.9f,
                        translation.Value.z)});
                    
                    ecb.SetComponent(index, txAutotrophParts.petal1, new Translation
                    {Value = new float3(translation.Value.x,
                        translation.Value.y+txAutotrophConsts.stemScale * txAutotrophPhenotype.height*0.9f,
                        translation.Value.z)});
                   
                    
                    ecb.SetComponent(index, txAutotrophParts.petal2, new Translation
                    {Value = new float3(translation.Value.x,
                        translation.Value.y+txAutotrophConsts.stemScale * txAutotrophPhenotype.height*0.9f,
                        translation.Value.z)});
                    

                    ecb.SetComponent(index, txAutotrophParts.petal3, new Translation
                    {Value = new float3(translation.Value.x,
                        translation.Value.y+txAutotrophConsts.stemScale * txAutotrophPhenotype.height*0.9f,
                        translation.Value.z)});
                   

                    ecb.SetComponent(index, txAutotrophParts.petal4, new Translation
                    {Value = new float3(translation.Value.x,
                        translation.Value.y+txAutotrophConsts.stemScale * txAutotrophPhenotype.height*0.9f,
                        translation.Value.z)});
                    
                    ecb.SetComponent(index, txAutotrophParts.petal5, new Translation
                    {Value = new float3(translation.Value.x,
                        translation.Value.y+txAutotrophConsts.stemScale * txAutotrophPhenotype.height*0.9f,
                        translation.Value.z)});
                    
                    ecb.SetComponent(index, txAutotrophParts.pollen, new PhysicsCollider {
                        Value = Unity.Physics.SphereCollider.Create(
                            new SphereGeometry {
                                Center = float3.zero,
                                Radius =txAutotrophPhenotype.height + txAutotrophChrome1W.Value.pollenRange*
                                        txAutotrophConsts.pollenRadiusMultiplier
                            }, new  CollisionFilter{BelongsTo = 2,CollidesWith = 4,GroupIndex = 0},
                            new Material{Flags = Material.MaterialFlags.IsTrigger})
                    });
                    
                    
                }
                
                if (leafGrow != 0 ) {
                    var lScale = math.sqrt(txAutotrophPhenotype.leaf) * txAutotrophConsts.leafScale;
                    
                    ecb.SetComponent(index, txAutotrophParts.petal0, new Scale{Value = lScale });
                    ecb.SetComponent(index, txAutotrophParts.petal1, new Scale{Value = lScale });
                    ecb.SetComponent(index, txAutotrophParts.petal2, new Scale{Value = lScale });
                    ecb.SetComponent(index, txAutotrophParts.petal3, new Scale{Value = lScale });
                    ecb.SetComponent(index, txAutotrophParts.petal4, new Scale{Value = lScale });
                    ecb.SetComponent(index, txAutotrophParts.petal5, new Scale{Value = lScale });

                    ecb.SetComponent(index, entity, new PhysicsCollider {
                        Value = Unity.Physics.SphereCollider.Create(
                            new SphereGeometry {
                                Center = float3.zero,
                                Radius = math.max(txAutotrophConsts.minShadeRadius,
                                    txAutotrophPhenotype.leaf)* txAutotrophConsts.leafShadeRadiusMultiplier
                            }, new  CollisionFilter{BelongsTo = 1,CollidesWith = 1,GroupIndex = 0},
                            new Material{Flags = Material.MaterialFlags.IsTrigger})
                    });
                }
                
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
        Entity prefabPetalEntity;
        Entity prefabPollenEntity;
        protected EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
        
        protected override void OnCreate() {
            m_Group = GetEntityQuery(ComponentType.ReadWrite<RandomComponent>(),
                ComponentType.ReadOnly<TxAutotrophSprout>(),
                ComponentType.ReadOnly<TxAutotrophChrome1AB>(),
                ComponentType.ReadOnly<TxAutotrophChrome1W>(),
                ComponentType.ReadOnly<TxAutotrophColorGenome>(),
                ComponentType.Exclude<Unfertilized>()
            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        struct Sprout : IJobForEachWithEntity<RandomComponent, TxAutotrophSprout, TxAutotrophChrome1AB, 
            TxAutotrophChrome1W,TxAutotrophColorGenome> {
            public Entity prefabEntity;
            public Entity prefabPetalEntity;
            public Entity prefabPollenEntity;
            public EntityCommandBuffer.Concurrent ecb;
            [ReadOnly]public NativeArray<Environment.EnvironmentSettings> environmentSettings;
            
            public void Execute(Entity entity, int index,
                ref RandomComponent randomComponent,
                [ReadOnly] ref TxAutotrophSprout txAutotrophSprout,
                [ReadOnly] ref TxAutotrophChrome1AB txAutotrophChrome1Ab, 
                [ReadOnly] ref TxAutotrophChrome1W txAutotrophChrome1W,
                [ReadOnly] ref TxAutotrophColorGenome txAutotrophColorGenome
            ) {
                var colorGeneScale = environmentSettings[0].txAutotrophConsts.colorGeneScale;
                var sprout = ecb.Instantiate(index,prefabEntity);
                var petal0 = ecb.Instantiate(index,prefabPetalEntity);
                var petal1 = ecb.Instantiate(index,prefabPetalEntity);
                var petal2 = ecb.Instantiate(index,prefabPetalEntity);
                var petal3 = ecb.Instantiate(index,prefabPetalEntity);
                var petal4 = ecb.Instantiate(index,prefabPetalEntity);
                var petal5 = ecb.Instantiate(index,prefabPetalEntity);

                var pos = txAutotrophSprout.location;
                
                var pollen = ecb.Instantiate(index,prefabPollenEntity);
                ecb.SetComponent(index,pollen, new Translation(){Value = pos});
                ecb.SetComponent(index,pollen, new TxAutotrophPollen(){plant = sprout });
                
                ecb.SetComponent(index,sprout, new Translation(){Value = pos});
                
                ecb.SetComponent(index,petal0, new Translation(){Value = pos + new float3(0,0.9f,0)});
                ecb.AddComponent(index,petal0, new Scale{Value = 1});
                
                ecb.SetComponent(index,petal1, new Translation(){Value = pos + new float3(0,0.9f,0)});
                ecb.AddComponent(index,petal1, new Scale{Value = 1});
                ecb.SetComponent(index, petal1, new Rotation {Value = quaternion.Euler(0,math.PI/3,0)});
                
                ecb.SetComponent(index,petal2, new Translation(){Value = pos + new float3(0,0.9f,0)});
                ecb.AddComponent(index,petal2, new Scale{Value = 1});
                ecb.SetComponent(index, petal2, new Rotation {Value = quaternion.Euler(0,2*math.PI/3,0)});
                
                ecb.SetComponent(index,petal3, new Translation(){Value = pos + new float3(0,0.9f,0)});
                ecb.AddComponent(index,petal3, new Scale{Value = 1});
                ecb.SetComponent(index, petal3, new Rotation {Value = quaternion.Euler(0,3*math.PI/3,0)});
                
                ecb.SetComponent(index,petal4, new Translation(){Value = pos + new float3(0,0.9f,0)});
                ecb.AddComponent(index,petal4, new Scale{Value = 1});
                ecb.SetComponent(index, petal4, new Rotation {Value = quaternion.Euler(0,4*math.PI/3,0)});
                
                ecb.SetComponent(index,petal5, new Translation(){Value = pos + new float3(0,0.9f,0)});
                ecb.AddComponent(index,petal5, new Scale{Value = 1});
                ecb.SetComponent(index, petal5, new Rotation {Value = quaternion.Euler(0,5*math.PI/3,0)});
                
                ecb.SetComponent(index,sprout,new TxAutotrophParts {
                    petal0 = petal0,
                    petal1 = petal1,
                    petal2 = petal2,
                    petal3 = petal3,
                    petal4 = petal4,
                    petal5 = petal5,
                    pollen = pollen
                });
                ecb.AddComponent(index,sprout, new Scale{Value = 1});
                ecb.SetComponent<RandomComponent>(index,sprout,new RandomComponent()
                    {random = new Unity.Mathematics.Random(randomComponent.random.NextUInt())});
                ecb.SetComponent(index,sprout, txAutotrophChrome1Ab.Copy());
                ecb.SetComponent(index,sprout, new TxAutotrophChrome1W{Value = txAutotrophChrome1W.Value.Copy()});
                ecb.SetComponent(index,sprout, new TxAutotrophColorGenome(){
                    r0 = txAutotrophColorGenome.r0,
                    g0 = txAutotrophColorGenome.g0,
                    b0 = txAutotrophColorGenome.b0,
                    r1 = txAutotrophColorGenome.r1,
                    g1 = txAutotrophColorGenome.g1,
                    b1 = txAutotrophColorGenome.b1,
                    r2 = txAutotrophColorGenome.r2,
                    g2 = txAutotrophColorGenome.g2,
                    b2 = txAutotrophColorGenome.b2,
                    dr0 = txAutotrophColorGenome.dr0,
                    dg0 = txAutotrophColorGenome.dg0,
                    db0 = txAutotrophColorGenome.db0,
                    dr1 = txAutotrophColorGenome.dr1,
                    dg1 = txAutotrophColorGenome.dg1,
                    db1 = txAutotrophColorGenome.db1,
                    dr2 = txAutotrophColorGenome.dr2,
                    dg2 = txAutotrophColorGenome.dg2,
                    db2 = txAutotrophColorGenome.db2
                    }
                );

                float Normalize(float cg) {
                    return (cg + colorGeneScale / 2) / colorGeneScale;
                }

                var nr0 = Normalize(txAutotrophColorGenome.r0 );
                var ng0 = Normalize(txAutotrophColorGenome.g0 );
                var nb0 = Normalize(txAutotrophColorGenome.b0 );
                var nr1 = Normalize(txAutotrophColorGenome.r1 );
                var ng1 = Normalize(txAutotrophColorGenome.g1 );
                var nb1 = Normalize(txAutotrophColorGenome.b1 );
                var nr2 = Normalize(txAutotrophColorGenome.r2 );
                var ng2 = Normalize(txAutotrophColorGenome.g2 );
                var nb2 = Normalize(txAutotrophColorGenome.b2 );
                var baseC =  Normalize(0);
                
                ecb.SetComponent(index,sprout,new  EnergyStore{Value =txAutotrophSprout.energy});
                ecb.DestroyEntity(index,entity);
                ecb.SetComponent(index, petal0, new MaterialColor {Value = new float4(baseC,baseC,baseC ,1)});
                ecb.SetComponent(index, petal1, new MaterialColor {Value = new float4(nr2 ,ng2,nb2 ,1)});
                ecb.SetComponent(index, petal2, new MaterialColor {Value = new float4(baseC,baseC,baseC ,1)});
                ecb.SetComponent(index, petal3, new MaterialColor {Value = new float4(nr2 ,ng2,nb2 ,1)});
                ecb.SetComponent(index, petal4, new MaterialColor {Value = new float4(baseC,baseC ,baseC ,1)});
                ecb.SetComponent(index, petal5, new MaterialColor {Value = new float4(nr2 ,ng2,nb2 ,1)});

            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            
            //this could be set once per environment run
            NativeArray<Entity> prefabArray = GetEntityQuery(
                ComponentType.ReadOnly<TxAutotroph>(),
                ComponentType.ReadOnly<Prefab>()
            ).ToEntityArray(Allocator.TempJob);
            
            NativeArray<Entity> prefabPollenArray = GetEntityQuery(
                ComponentType.ReadOnly<TxAutotrophPollen>(),
                ComponentType.ReadOnly<Prefab>()
            ).ToEntityArray(Allocator.TempJob);
            NativeArray<Entity> prefabPetalArray = GetEntityQuery(
                ComponentType.ReadOnly<TxAutotrophPetalMeshFlag>(),
                ComponentType.ReadOnly<Prefab>()
            ).ToEntityArray(Allocator.TempJob);

            var jobDeps = inputDeps;
            if (prefabArray.Length > 0) {
                prefabEntity = prefabArray[0];
                prefabPollenEntity = prefabPollenArray[0];
                prefabPetalEntity = prefabPetalArray[0];
                var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
                Sprout job = new Sprout() {
                    environmentSettings = Environment.environmentSettings,
                    ecb = ecb,
                    prefabEntity = prefabEntity,
                    prefabPollenEntity = prefabPollenEntity,
                    prefabPetalEntity = prefabPetalEntity
                };
                JobHandle jobHandle = job.Schedule(m_Group,inputDeps);
                    //Schedule(m_Group, inputDeps);
                m_EndSimulationEcbSystem.AddJobHandleForProducer(jobHandle);
                
                jobHandle.Complete();
                jobDeps =  jobHandle;
            }
            prefabArray.Dispose();
            prefabPollenArray.Dispose();
            prefabPetalArray.Dispose();
            
            return jobDeps;
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
                ComponentType.ReadOnly<TxAutotrophChrome1AB>(),
                ComponentType.ReadOnly<TxAutotrophChrome1W>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<TxAutotrophColorGenome>()
            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        struct MakeSprout : IJobForEachWithEntity< TxAutotrophPhenotype,RandomComponent, TxAutotrophChrome1AB,
            TxAutotrophChrome1W, TxAutotrophColorGenome,Translation> {
            
            public EntityCommandBuffer.Concurrent ecb;
            [ReadOnly]public NativeArray<Environment.EnvironmentSettings> environmentSettings;
            [ReadOnly]public NativeArray<float> terrainHeight;
            
            public void Execute(Entity entity, int index,
                 ref TxAutotrophPhenotype txAutotrophPhenotype,
                 ref RandomComponent randomComponent,
                 [ReadOnly] ref TxAutotrophChrome1AB txAutotrophChrome1AB,
                 [ReadOnly] ref TxAutotrophChrome1W txAutotrophChrome1W,
                 [ReadOnly] ref TxAutotrophColorGenome txAutotrophColorGenome,
                 [ReadOnly] ref Translation translation
            ) {
                float Mutate(float val, ref Unity.Mathematics.Random random, float rate, float rangeL,
                    float rangeH) {
                    bool mutate = rate < random.NextFloat(0, 1);
                    if (mutate) {
                        var mutant = math.max(1,val * random.NextFloat(rangeL, rangeH));
                        return mutant;
                    } else {
                        return val;
                    }
                }
                var txAutotrophConsts = environmentSettings[0].txAutotrophConsts;
                var mRate = environmentSettings[0].txAutotrophConsts.mutationRate;
                var mRange = environmentSettings[0].txAutotrophConsts.mutationRange;
                var bounds = environmentSettings[0].environmentConsts.bounds;
                var heightScale = environmentSettings[0].environmentConsts.terrainHeightScale.y;
                var mRangeH = 1 + mRange;
                var mRangeL = 1 - mRange;

                //Single Seed each frame only
                if (txAutotrophPhenotype.seed >= txAutotrophChrome1W.Value.seedSize) {
                    txAutotrophPhenotype.seed -= txAutotrophChrome1W.Value.seedSize;

                    var loc =txAutotrophConsts.seedRangeMultiplier 
                             * randomComponent.random.NextFloat2(-1, 1)
                             *txAutotrophPhenotype.height/txAutotrophChrome1W.Value.seedSize;
                    
                    var location = translation.Value + new float3(loc.x, 0, loc.y);
                    if (location.x > bounds.x && location.x < bounds.z &&
                        location.z > bounds.y && location.z < bounds.w) {
                        // do not know how to get height scale from terrain
                        var height =heightScale*  Environment.TerrainValue(location,terrainHeight,bounds);
                        location.y = height;
                        var e = ecb.CreateEntity(index);
                        ecb.AddComponent<TxAutotrophSprout>(index, e, new TxAutotrophSprout() {
                            energy = txAutotrophChrome1W.Value.seedSize,
                            location = location
                        });
                        var txCG = new TxAutotrophColorGenome() {
                            r0 = txAutotrophColorGenome.r0,
                            g0 = txAutotrophColorGenome.g0,
                            b0 = txAutotrophColorGenome.b0,
                            r1 = txAutotrophColorGenome.r1,
                            g1 = txAutotrophColorGenome.g1,
                            b1 = txAutotrophColorGenome.b1,
                            r2 = txAutotrophColorGenome.r2,
                            g2 = txAutotrophColorGenome.g2,
                            b2 = txAutotrophColorGenome.b2,
                            dr0 = txAutotrophColorGenome.dr0,
                            dg0 = txAutotrophColorGenome.dg0,
                            db0 = txAutotrophColorGenome.db0,
                            dr1 = txAutotrophColorGenome.dr1,
                            dg1 = txAutotrophColorGenome.dg1,
                            db1 = txAutotrophColorGenome.db1,
                            dr2 = txAutotrophColorGenome.dr2,
                            dg2 = txAutotrophColorGenome.dg2,
                            db2 = txAutotrophColorGenome.db2
                        };
                        
                        ecb.AddComponent<RandomComponent>(index, e, new RandomComponent()
                            {random = new Unity.Mathematics.Random(randomComponent.random.NextUInt())});
                        
                        var chrome1AB = txAutotrophChrome1AB.Copy();
                        for (int i = 0; i < TxAutotrophChrome1.LENGTH; i++) {
                            chrome1AB.ValueA[i] = Mutate(chrome1AB.ValueA[i], ref randomComponent.random
                                , mRate, mRangeL, mRangeH);
                            chrome1AB.ValueB[i] = Mutate(chrome1AB.ValueB[i], ref randomComponent.random
                                , mRate, mRangeL, mRangeH);
                        }
                        var chrome1W = chrome1AB.GetChrome1W();
                        ecb.AddComponent(index,e,chrome1AB);
                        ecb.AddComponent<TxAutotrophChrome1W>(index, e, chrome1W);
                        
                        ecb.AddComponent(index, e , txCG);
                        
                    }
                }
            }
        }
       
        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
            MakeSprout job = new MakeSprout() {
                environmentSettings = Environment.environmentSettings,
                terrainHeight = Environment.terrainHeight,
                ecb=ecb
            };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            m_EndSimulationEcbSystem.AddJobHandleForProducer(jobHandle);
            jobHandle.Complete();
            return jobHandle;
            
        }
    }

}

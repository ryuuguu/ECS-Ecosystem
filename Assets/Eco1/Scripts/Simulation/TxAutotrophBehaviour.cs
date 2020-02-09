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
            // ,
            // Entity leafEntity, Entity seedPodEntity ){
            dstManager.AddComponentData(entity, new TxAutotroph());
            dstManager.AddComponentData(entity, new EnergyStore() {Value = 0});
            dstManager.AddComponentData(entity, new TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed = 0,
                age = 0
            });
            dstManager.AddComponentData(entity, new Scale() {Value = 1});
            dstManager.AddComponentData(entity, new Shade() {Value = 0});
            dstManager.AddComponentData(entity, new RandomComponent() {random = new Unity.Mathematics.Random(1)});
            dstManager.AddComponentData(entity, new TxAutotrophGenome() {
                nrg2Height = 5,
                nrg2Leaf = 5,
                nrg2Seed = 5,
                nrg2Storage = 5,
                maxHeight = 5,
                maxLeaf = 5,
                seedSize = 5,
                ageRate = 2.2f
            });
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
                    //ecb.DestroyEntity(index, txAutotrophParts.leaf);
                    ecb.DestroyEntity(index, txAutotrophParts.petal0);
                    ecb.DestroyEntity(index, txAutotrophParts.petal1);
                    ecb.DestroyEntity(index, txAutotrophParts.petal2);
                    ecb.DestroyEntity(index, txAutotrophParts.petal3);
                    ecb.DestroyEntity(index, txAutotrophParts.petal4);
                    ecb.DestroyEntity(index, txAutotrophParts.petal5);
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
                var sum = txAutotrophGenome.nrg2Height + txAutotrophGenome.nrg2Leaf + txAutotrophGenome.nrg2Seed +
                          txAutotrophGenome.nrg2Storage;
                
                
                var heightEnergy = energyStore.Value * txAutotrophGenome.nrg2Height/sum;
                var heightGrow  = math.min(heightEnergy, 
                    txAutotrophGenome.maxHeight- txAutotrophPhenotype.height );
                
                var leafEnergy =  energyStore.Value * txAutotrophGenome.nrg2Leaf/sum;
                var leafGrow = math.min(leafEnergy,
                    txAutotrophGenome.maxLeaf - txAutotrophPhenotype.leaf );
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
                   
                   // ecb.SetComponent(index, txAutotrophParts.leaf, new Translation
                  //  {Value = new float3(translation.Value.x,
                  //      translation.Value.y+txAutotrophConsts.stemScale * txAutotrophPhenotype.height*0.8f,
                  //      translation.Value.z)});
                  
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
                    
                }
                
                if (leafGrow != 0 ) {
                    var lScale = math.sqrt(txAutotrophPhenotype.leaf) * txAutotrophConsts.leafScale;
                    //ecb.SetComponent(index, txAutotrophParts.leaf, new Scale{Value = lScale });
                    
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
                                Radius = math.max(0.01f,txAutotrophPhenotype.leaf* txAutotrophConsts.leafShadeRadiusMultiplier),
                            }, new  CollisionFilter{BelongsTo = 1,CollidesWith = 1,GroupIndex = 0},
                            new Material{Flags = Material.MaterialFlags.IsTrigger})
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
        //Entity prefabLeafEntity;
        Entity prefabPetalEntity;
        protected EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
        
        protected override void OnCreate() {
            m_Group = GetEntityQuery(ComponentType.ReadWrite<RandomComponent>(),
                ComponentType.ReadOnly<TxAutotrophSprout>(),
                ComponentType.ReadOnly<TxAutotrophGenome>(),
                ComponentType.ReadOnly<TxAutotrophColorGenome>()

            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        struct Sprout : IJobForEachWithEntity<RandomComponent, TxAutotrophSprout,TxAutotrophGenome,TxAutotrophColorGenome> {
            public Entity prefabEntity;
            //public Entity prefabLeafEntity;
            public Entity prefabPetalEntity;
            public EntityCommandBuffer.Concurrent ecb;
            [ReadOnly]public NativeArray<Environment.EnvironmentSettings> environmentSettings;
            
            public void Execute(Entity entity, int index,
                ref RandomComponent randomComponent,
                [ReadOnly] ref TxAutotrophSprout txAutotrophSprout,
                [ReadOnly] ref TxAutotrophGenome txAutotrophGenome,
                [ReadOnly] ref TxAutotrophColorGenome txAutotrophColorGenome
            ) {
                var colorGeneScale = environmentSettings[0].txAutotrophConsts.colorGeneScale;
                var sprout = ecb.Instantiate(index,prefabEntity);
                //var leaf = ecb.Instantiate(index,prefabLeafEntity);
                var petal0 = ecb.Instantiate(index,prefabPetalEntity);
                var petal1 = ecb.Instantiate(index,prefabPetalEntity);
                var petal2 = ecb.Instantiate(index,prefabPetalEntity);
                var petal3 = ecb.Instantiate(index,prefabPetalEntity);
                var petal4 = ecb.Instantiate(index,prefabPetalEntity);
                var petal5 = ecb.Instantiate(index,prefabPetalEntity);
                var pos = txAutotrophSprout.location;
                ecb.SetComponent(index,sprout, new Translation(){Value = pos});
                //ecb.SetComponent(index,leaf, new Translation(){Value = pos + new float3(0,0.8f,0)});
                //ecb.AddComponent(index,leaf, new Scale{Value = 1});
                
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
                    //leaf =  leaf,
                    petal0 = petal0,
                    petal1 = petal1,
                    petal2 = petal2,
                    petal3 = petal3,
                    petal4 = petal4,
                    petal5 = petal5
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

                float normalize(float cg) {
                    return (cg + colorGeneScale / 2) / colorGeneScale;
                }

                var nr0 = normalize(txAutotrophColorGenome.r0 );
                var ng0 = normalize(txAutotrophColorGenome.g0 );
                var nb0 = normalize(txAutotrophColorGenome.b0 );
                var nr1 = normalize(txAutotrophColorGenome.r1 );
                var ng1 = normalize(txAutotrophColorGenome.g1 );
                var nb1 = normalize(txAutotrophColorGenome.b1 );
                var nr2 = normalize(txAutotrophColorGenome.r2 );
                var ng2 = normalize(txAutotrophColorGenome.g2 );
                var nb2 = normalize(txAutotrophColorGenome.b2 );
                
                var red0 = (txAutotrophGenome.maxHeight-1) / (txAutotrophGenome.maxHeight + txAutotrophGenome.maxLeaf-2);
                var green1 = (txAutotrophGenome.nrg2Height-1) / (txAutotrophGenome.nrg2Height + txAutotrophGenome.nrg2Leaf-2);
                var blue2 = (txAutotrophGenome.ageRate-1) / 7;
                var red3 = (txAutotrophGenome.nrg2Seed-1) / (txAutotrophGenome.nrg2Height + txAutotrophGenome.nrg2Leaf-2);
                var green4 = (txAutotrophGenome.nrg2Storage-1) / (txAutotrophGenome.nrg2Height + txAutotrophGenome.nrg2Leaf-2);
                var blue5 = (txAutotrophGenome.seedSize-1) / 7;
                var baseC =  normalize(0);
                
                ecb.SetComponent(index,sprout,new  EnergyStore{Value =txAutotrophSprout.energy});
               // ecb.RemoveComponent<TxAutotrophGenome>(index,entity);
               // ecb.RemoveComponent<TxAutotrophColorGenome>(index,entity);
               // ecb.RemoveComponent<TxAutotrophSprout>(index,entity);
                ecb.DestroyEntity(index,entity);
                //ecb.SetComponent(index, leaf, new MaterialColor {Value = new float4(red,green,blue,1)});
                ecb.SetComponent(index, petal0, new MaterialColor {Value = new float4(nr0,ng0 ,nb0 ,1)});
                ecb.SetComponent(index, petal1, new MaterialColor {Value = new float4(nb2 ,nb2,nb2 ,1)});
                ecb.SetComponent(index, petal2, new MaterialColor {Value = new float4(nr1,ng1 ,nb1 ,1)});
                ecb.SetComponent(index, petal3, new MaterialColor {Value = new float4(nb2 ,nb2,nb2 ,1)});
                ecb.SetComponent(index, petal4, new MaterialColor {Value = new float4(nr2,ng2 ,baseC ,1)});
                ecb.SetComponent(index, petal5, new MaterialColor {Value = new float4(nb2 ,nb2,nb2 ,1)});

            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            
            
            //this could be set once per environment run
            NativeArray<Entity> prefabArray = GetEntityQuery(
                ComponentType.ReadOnly<TxAutotroph>(),
                ComponentType.ReadOnly<Prefab>()
            ).ToEntityArray(Allocator.TempJob);
            
            //NativeArray<Entity> prefabLeafArray = GetEntityQuery(
            //    ComponentType.ReadOnly<TxAutotrophLeafMeshFlag>(),
            //    ComponentType.ReadOnly<Prefab>()
            //).ToEntityArray(Allocator.TempJob);
            NativeArray<Entity> prefabPetalArray = GetEntityQuery(
                ComponentType.ReadOnly<TxAutotrophPetalMeshFlag>(),
                ComponentType.ReadOnly<Prefab>()
            ).ToEntityArray(Allocator.TempJob);
            if (prefabArray.Length > 0) {
                prefabEntity = prefabArray[0];
                //prefabLeafEntity = prefabLeafArray[0];
                prefabPetalEntity = prefabPetalArray[0];
                var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
                Sprout job = new Sprout() {
                    environmentSettings = Environment.environmentSettings,
                    ecb = ecb,
                    prefabEntity = prefabEntity,
                    //prefabLeafEntity = prefabLeafEntity,
                    prefabPetalEntity = prefabPetalEntity
                };
                JobHandle jobHandle = job.Schedule(m_Group,inputDeps);
                    //Schedule(m_Group, inputDeps);
                m_EndSimulationEcbSystem.AddJobHandleForProducer(jobHandle);
                prefabArray.Dispose();
                //prefabLeafArray.Dispose();
                prefabPetalArray.Dispose();
                jobHandle.Complete();
                return jobHandle;
            }
            prefabArray.Dispose();
            //prefabLeafArray.Dispose();
            prefabPetalArray.Dispose();
            
            
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
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<TxAutotrophColorGenome>()
            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        struct Sprout : IJobForEachWithEntity< TxAutotrophPhenotype,RandomComponent, TxAutotrophGenome,TxAutotrophColorGenome,Translation> {
            
            public EntityCommandBuffer.Concurrent ecb;
            [ReadOnly]public NativeArray<Environment.EnvironmentSettings> environmentSettings;
            [ReadOnly]public NativeArray<float> terrainHeight;
            
            public void Execute(Entity entity, int index,
                 ref TxAutotrophPhenotype txAutotrophPhenotype,
                 ref RandomComponent randomComponent,
                 [ReadOnly] ref TxAutotrophGenome txAutotrophGenome,
                 [ReadOnly] ref TxAutotrophColorGenome txAutotrophColorGenome,
                 [ReadOnly] ref Translation translation
            ) {
                
                (float, float, float) Mutate(float val, ref Unity.Mathematics.Random random, float rate, float rangeL,
                    float rangeH, float cg, float dcg) {
                    var mutant = math.max(1,val * random.NextFloat(rangeL, rangeH));
                    bool mutate = rate < random.NextFloat(0, 1);
                    if (mutate) {
                        if (dcg == 0) {
                            if (mutant > val) {
                                dcg = 1;
                            }
                            else {
                                dcg = -1;
                            }
                        }
                        cg += dcg;
                        return (mutant, cg, dcg);
                    }
                    else {
                        return (val, cg, dcg);
                    }
                    
                    //return (math.select(val, mutant,mutate),0,0);
                }
                var txAutotrophConsts = environmentSettings[0].txAutotrophConsts;
                var mRate = environmentSettings[0].txAutotrophConsts.mutationRate;
                var mRange = environmentSettings[0].txAutotrophConsts.mutationRange;
                var bounds = environmentSettings[0].environmentConsts.bounds;
                var heightScale = environmentSettings[0].environmentConsts.terrainHeightScale.y;
                var mRangeH = 1 + mRange;
                var mRangeL = 1 - mRange;

                //Single Seed each frame only
                if (txAutotrophPhenotype.seed >= txAutotrophGenome.seedSize) {
                    txAutotrophPhenotype.seed -= txAutotrophGenome.seedSize;

                    var loc =txAutotrophConsts.seedRangeMultiplier 
                             * randomComponent.random.NextFloat2(-1, 1)
                             *txAutotrophPhenotype.height/txAutotrophGenome.seedSize;
                    
                    var location = translation.Value + new float3(loc.x, 0, loc.y);
                    if (location.x > bounds.x && location.x < bounds.z &&
                        location.z > bounds.y && location.z < bounds.w) {
                        // do not know how to get height scale from terrain
                        var height =heightScale*  Environment.TerrainValue(location,terrainHeight,bounds);
                        location.y = height;
                        var e = ecb.CreateEntity(index);
                        ecb.AddComponent<TxAutotrophSprout>(index, e, new TxAutotrophSprout() {
                            energy = txAutotrophGenome.seedSize,
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

                        var newGenome = new TxAutotrophGenome();
                        (newGenome.nrg2Height,txCG.r0,txCG.dr0) = Mutate(txAutotrophGenome.nrg2Height, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH, txCG.r0,txCG.dr0 );
                        (newGenome.nrg2Leaf, txCG.g0,txCG.dg0) = Mutate(txAutotrophGenome.nrg2Leaf, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH, txCG.g0,txCG.dg0);
                        (newGenome.nrg2Seed, txCG.b0,txCG.db0) = Mutate(txAutotrophGenome.nrg2Seed, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH, txCG.b0,txCG.db0);
                        (newGenome.nrg2Storage, txCG.r1,txCG.dr1) = Mutate(txAutotrophGenome.nrg2Storage, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH, txCG.r1,txCG.dr1);
                        (newGenome.maxHeight, txCG.g1,txCG.dg1) = Mutate(txAutotrophGenome.maxHeight, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH, txCG.g1,txCG.dg1);
                        (newGenome.maxLeaf, txCG.b1,txCG.db1) = Mutate(txAutotrophGenome.maxLeaf, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH, txCG.b1,txCG.db1);
                        (newGenome.ageRate, txCG.r2,txCG.dr2) = Mutate(txAutotrophGenome.ageRate, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH, txCG.r2,txCG.dr2);
                        (newGenome.seedSize, txCG.g2,txCG.dg2) = Mutate(txAutotrophGenome.seedSize, ref randomComponent.random
                            ,mRate, mRangeL, mRangeH, txCG.g2,txCG.dg2);
                        ecb.AddComponent<TxAutotrophGenome>(index, e, newGenome);
                        
                        ecb.AddComponent(index, e , txCG);
                        
                        
                    }
                    
                    
                    
                }
            }
        }
       
        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
            Sprout job = new Sprout() {
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

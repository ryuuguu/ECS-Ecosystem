
using System.Collections.Generic;
using Unity.Physics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
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
                leaf = 1f,
                height = 1f,
                seed = 0,
                age = 0
            });
            dstManager.AddComponentData(entity, new Scale() {Value = 1.1f});
            dstManager.AddComponentData(entity, new Shade() {Value = 0});
            dstManager.AddComponentData(entity, new RandomComponent() {random = new Unity.Mathematics.Random(1)});
            dstManager.AddComponentData(entity, new TxAutotrophChrome1AB());
            dstManager.AddComponentData(entity, new TxAutotrophChrome1W {
                Value = new TxAutotrophChrome1 {
                    nrg2Height = 5,
                    nrg2Leaf = 5,
                    nrg2Seed = 5,
                    nrg2Storage = 5,
                    maxHeight = 5,
                    maxLeaf = 5,
                    seedSize = 5,
                    ageRate = 2.2f
                }
            });
            dstManager.AddComponentData(entity, new TxAutotrophChrome2AB());
            //dstManager.AddComponentData(entity, new TxAutotrophMeshes {stem = entity,});
            dstManager.AddComponentData(entity, new TxAutotrophParts { });
            dstManager.AddComponentData(entity, new TxAutotrophCacheYPos { });
            dstManager.AddComponentData(entity, new DebugDistances { });
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

            [ReadOnly] public NativeArray<Environment.EnvironmentSettings> environmentSettings;
            [ReadOnly] public NativeArray<float> terrainLight;

            public void Execute(ref EnergyStore energyStore,
                [ReadOnly] ref Translation translation,
                [ReadOnly] ref TxAutotrophPhenotype TxAutotrophPhenotype,
                [ReadOnly] ref Shade shade
            ) {
                var bounds = environmentSettings[0].environmentConsts.bounds;
                var heightScale = environmentSettings[0].environmentConsts.terrainScale;
                var ambientLight = environmentSettings[0].environmentConsts.ambientLight;
                var variableLight = environmentSettings[0].environmentConsts.variableLight;

                energyStore.Value +=
                    Environment.ResourceValue(translation.Value, ambientLight, variableLight,
                        terrainLight, bounds, heightScale)
                    * Environment.Fitness(TxAutotrophPhenotype.leaf)
                    * TxAutotrophPhenotype.leaf /
                    (TxAutotrophPhenotype.leaf + shade.Value *
                     environmentSettings[0].txAutotrophConsts.leafShadeEffectMultiplier);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            TxGainEnergy job = new TxGainEnergy() {
                environmentSettings = Environment.environmentSettings,
                terrainLight = Environment.terrainLight,
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
        EntityQuery petalGroup;
        protected BeginPresentationEntityCommandBufferSystem m_BeginPresentationEcbSystem;

        protected override void OnCreate() {
            m_Group = GetEntityQuery(ComponentType.ReadWrite<EnergyStore>(),
                ComponentType.ReadWrite<TxAutotrophPhenotype>(),
                ComponentType.ReadOnly<TxAutotrophChrome1W>(),
                ComponentType.ReadOnly<TxAutotrophParts>()
            );
           petalGroup = GetEntityQuery(
               ComponentType.ReadOnly<EnergyStore>(),
               ComponentType.ReadOnly<TxAutotrophMeshes>()
            );
            m_BeginPresentationEcbSystem = World
                .GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }

        struct PayMaintenance : IJobForEachWithEntity<EnergyStore, TxAutotrophPhenotype,
            TxAutotrophChrome1W, TxAutotrophParts> {
            [ReadOnly] public NativeArray<Environment.EnvironmentSettings> environmentSettings;

            public EntityCommandBuffer.Concurrent ecb;

            public void Execute(Entity entity, int index, ref EnergyStore energyStore,
                ref TxAutotrophPhenotype txAutotrophPhenotype,
                [ReadOnly] ref TxAutotrophChrome1W txAutotrophChrome1W,
                [ReadOnly] ref TxAutotrophParts txAutotrophParts) {

                var txAutotrophConsts = environmentSettings[0].txAutotrophConsts;

                txAutotrophPhenotype = new TxAutotrophPhenotype() {
                    age = txAutotrophPhenotype.age + 1,
                    height = txAutotrophPhenotype.height,
                    leaf = txAutotrophPhenotype.leaf,
                    seed = txAutotrophPhenotype.seed
                };
                energyStore = new EnergyStore() {
                    Value = energyStore.Value
                            - (txAutotrophConsts.baseCost +
                               txAutotrophConsts.leafCostMultiple * txAutotrophPhenotype.leaf
                                                                  * txAutotrophPhenotype.leaf +

                               txAutotrophConsts.heightCostMultiple * txAutotrophPhenotype.height
                                                                    * txAutotrophPhenotype.height +

                               txAutotrophConsts.ageMultiple * txAutotrophChrome1W.Value.ageRate +
                               txAutotrophPhenotype.age / txAutotrophChrome1W.Value.ageRate +
                               txAutotrophConsts.pollenCostMultiple * txAutotrophChrome1W.Value.pollenRange
                            )
                };
                if (energyStore.Value < 0) {
                    ecb.DestroyEntity(index, entity);
                    ecb.DestroyEntity(index, txAutotrophParts.pollen);
                }
            }
        }

        struct DestroyPetals : IJobForEachWithEntity<EnergyStore, TxAutotrophMeshes> {
            [ReadOnly] public NativeArray<Environment.EnvironmentSettings> environmentSettings;

            public EntityCommandBuffer.Concurrent ecb;

            public void Execute(Entity entity, int index,
                [ReadOnly] ref EnergyStore energyStore,
                [ReadOnly] ref TxAutotrophMeshes txAutotrophMeshes) {

                var txAutotrophConsts = environmentSettings[0].txAutotrophConsts;

                if (energyStore.Value < 0) {
                    ecb.DestroyEntity(index, entity);
                    //ecb.DestroyEntity(index, txAutotrophParts.stem);
                    ecb.DestroyEntity(index, txAutotrophMeshes.petal0);
                    ecb.DestroyEntity(index, txAutotrophMeshes.petal1);
                    ecb.DestroyEntity(index, txAutotrophMeshes.petal2);
                    ecb.DestroyEntity(index, txAutotrophMeshes.petal3);
                    ecb.DestroyEntity(index, txAutotrophMeshes.petal4);
                    ecb.DestroyEntity(index, txAutotrophMeshes.petal5);
                }
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var ecb = m_BeginPresentationEcbSystem.CreateCommandBuffer().ToConcurrent();

            PayMaintenance payMaintenance = new PayMaintenance() {
                environmentSettings = Environment.environmentSettings,
                ecb = ecb
            };
            JobHandle jobHandle = payMaintenance.Schedule(m_Group, inputDeps);
            m_BeginPresentationEcbSystem.AddJobHandleForProducer(jobHandle);
            jobHandle.Complete();
            DestroyPetals destroyPetals = new DestroyPetals() {
                environmentSettings = Environment.environmentSettings,
                ecb = ecb
            };
            jobHandle = destroyPetals.Schedule(petalGroup, jobHandle);
            m_BeginPresentationEcbSystem.AddJobHandleForProducer(jobHandle);
            jobHandle.Complete();
            
            return jobHandle;
        }
    }

    [UpdateAfter(typeof(TxAutotrophPayMaintenance))]
    [BurstCompile]
    public class TxAutotrophGrow : JobComponentSystem {
        EntityQuery m_Group;
        EntityQuery petalGroup;
        protected EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;

        protected override void OnCreate() {
            m_Group = GetEntityQuery(ComponentType.ReadWrite<EnergyStore>(),
                ComponentType.ReadWrite<TxAutotrophPhenotype>(),
                ComponentType.ReadWrite<Scale>(),
                ComponentType.ReadOnly<TxAutotrophChrome1W>(),
                ComponentType.ReadOnly<TxAutotrophParts>(),
                ComponentType.ReadOnly<Translation>()
            );
            petalGroup = GetEntityQuery(
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Scale>(),
                ComponentType.ReadOnly<TxAutotrophPetal>()
            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        struct Grow : IJobForEachWithEntity<EnergyStore, TxAutotrophPhenotype, Scale,
            TxAutotrophChrome1W, TxAutotrophParts, Translation> {
            [ReadOnly] public NativeArray<Environment.EnvironmentSettings> environmentSettings;

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
                var heightEnergy = energyStore.Value * txAutotrophChrome1W.Value.nrg2Height / sum;
                var heightGrow = math.min(heightEnergy,
                    txAutotrophChrome1W.Value.maxHeight - txAutotrophPhenotype.height);

                var leafEnergy = energyStore.Value * txAutotrophChrome1W.Value.nrg2Leaf / sum;
                var leafGrow = math.min(leafEnergy,
                    txAutotrophChrome1W.Value.maxLeaf - txAutotrophPhenotype.leaf);
                var seedGrow = energyStore.Value * txAutotrophChrome1W.Value.nrg2Seed / sum;

                txAutotrophPhenotype = new TxAutotrophPhenotype() {
                    age = txAutotrophPhenotype.age,
                    height = txAutotrophPhenotype.height + heightGrow,
                    leaf = txAutotrophPhenotype.leaf + leafGrow,
                    seed = txAutotrophPhenotype.seed + seedGrow / environmentSettings[0].txAutotrophConsts.seedDivisor
                };

                if (heightGrow != 0) {
                    scale.Value = txAutotrophConsts.stemScale * txAutotrophPhenotype.height;

                    var radius = math.max(0.00001f,
                        txAutotrophPhenotype.height + txAutotrophChrome1W.Value.pollenRange *
                        txAutotrophConsts.pollenRadiusMultiplier);

                    ecb.SetComponent(index, txAutotrophParts.pollen, new PhysicsCollider {
                        Value = Unity.Physics.SphereCollider.Create(
                            new SphereGeometry {
                                Center = float3.zero,
                                Radius = radius
                            }, new CollisionFilter {BelongsTo = 2, CollidesWith = 4, GroupIndex = 0},
                            new Material {Flags = Material.MaterialFlags.IsTrigger})
                    });
                }

                if (leafGrow != 0) {
                    
                    ecb.SetComponent(index, entity, new PhysicsCollider {
                        Value = Unity.Physics.SphereCollider.Create(
                            new SphereGeometry {
                                Center = float3.zero,
                                Radius = math.max(txAutotrophConsts.minShadeRadius,
                                             txAutotrophPhenotype.leaf) * txAutotrophConsts.leafShadeRadiusMultiplier
                            }, new CollisionFilter {BelongsTo = 1, CollidesWith = 1, GroupIndex = 0},
                            new Material {Flags = Material.MaterialFlags.IsTrigger})
                    });
                }

                energyStore = new EnergyStore()
                    {Value = energyStore.Value - (heightGrow + leafGrow + seedGrow)};
            }
        }

        struct GrowPetals : IJobForEach<Translation, Scale, TxAutotrophPetal> {
            [ReadOnly] public NativeArray<Environment.EnvironmentSettings> environmentSettings;
            [ReadOnly] public ComponentDataFromEntity<TxAutotrophCacheYPos> txAutoTrophCacheYPos;
            [ReadOnly] public ComponentDataFromEntity<TxAutotrophPhenotype> txAutotrophPhenotypes;

            public EntityCommandBuffer.Concurrent ecb;

            public void Execute(
                ref Translation translation,
                ref Scale scale,
                [ReadOnly] ref TxAutotrophPetal txAutotrophPetal
            ) {
                var txAutotrophConsts = environmentSettings[0].txAutotrophConsts;

                translation.Value = new float3(translation.Value.x,
                    txAutoTrophCacheYPos[txAutotrophPetal.Value].y +
                    txAutotrophConsts.stemScale
                    * txAutotrophPhenotypes[txAutotrophPetal.Value].height * 1.9f,
                    translation.Value.z);

                var lScale = math.sqrt(txAutotrophPhenotypes[txAutotrophPetal.Value].leaf) *
                             txAutotrophConsts.leafScale;

                scale.Value = lScale;
            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
            Grow job = new Grow() {
                environmentSettings = Environment.environmentSettings,
                ecb = ecb
            };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            m_EndSimulationEcbSystem.AddJobHandleForProducer(jobHandle);
            jobHandle.Complete();
            GrowPetals growPetals = new GrowPetals() {
                environmentSettings = Environment.environmentSettings,
                ecb = ecb,
                txAutoTrophCacheYPos = GetComponentDataFromEntity<TxAutotrophCacheYPos>(),
                txAutotrophPhenotypes = GetComponentDataFromEntity<TxAutotrophPhenotype>(),
            };
            jobHandle = growPetals.Schedule(petalGroup, jobHandle);
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
                ComponentType.ReadOnly<TxAutotrophGamete>(),
                ComponentType.ReadOnly<TxAutotrophSprout>(),
                ComponentType.ReadOnly<TxAutotrophChrome1AB>(),
                ComponentType.ReadOnly<TxAutotrophChrome1W>(),
                ComponentType.ReadOnly<TxAutotrophChrome2AB>()
            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        struct Sprout : IJobForEachWithEntity<RandomComponent,
            TxAutotrophGamete,
            TxAutotrophSprout, TxAutotrophChrome1AB,
            TxAutotrophChrome1W, TxAutotrophChrome2AB> {
            public Entity prefabEntity;
            public Entity prefabPetalEntity;
            public Entity prefabPollenEntity;
            public EntityCommandBuffer.Concurrent ecb;
            [ReadOnly] public NativeArray<Environment.EnvironmentSettings> environmentSettings;
            //[ReadOnly] public ComponentDataFromEntity<TxAutotrophChrome1AB> txAutotrophChrome1ABCD;
            //[ReadOnly] public ComponentDataFromEntity<TxAutotrophPollen> txAutotrophPollenCD;

            public void Execute(Entity entity, int index,
                ref RandomComponent randomComponent,
                [ReadOnly] ref TxAutotrophGamete txAutotrophGamete,
                [ReadOnly] ref TxAutotrophSprout txAutotrophSprout,
                [ReadOnly] ref TxAutotrophChrome1AB txAutotrophChrome1Ab,
                [ReadOnly] ref TxAutotrophChrome1W txAutotrophChrome1W,
                [ReadOnly] ref TxAutotrophChrome2AB txAutotrophChrome2AB
            ) {

                if (txAutotrophGamete.isFertilized) {
                    var plant = ecb.Instantiate(index, prefabEntity);
                    

                    var pos = txAutotrophSprout.location;

                    var pollen = ecb.Instantiate(index, prefabPollenEntity);
                    ecb.SetComponent(index, pollen, new Translation() {Value = pos});
                    ecb.SetComponent(index, pollen, new TxAutotrophPollen() {plant = plant});
                    ecb.SetComponent(index, pollen, new PhysicsCollider {
                        Value = Unity.Physics.SphereCollider.Create(
                            new SphereGeometry {
                                Center = float3.zero,
                                Radius = math.max(0.01f, txAutotrophChrome1W.Value.pollenRange *
                                                         environmentSettings[0].txAutotrophConsts
                                                             .pollenRadiusMultiplier)
                            }, new CollisionFilter {BelongsTo = 2, CollidesWith = 4, GroupIndex = 0},
                            new Material {Flags = Material.MaterialFlags.IsTrigger})
                    });

                    ecb.SetComponent(index, plant, new Translation() {Value = pos});
                   
                    ecb.SetComponent(index, plant, new TxAutotrophParts {pollen = pollen});
                    ecb.SetComponent(index, plant, new TxAutotrophCacheYPos {y = pos.y});
                    
                    ecb.AddComponent(index, plant, new Scale {Value = 1});
                    ecb.SetComponent<RandomComponent>(index, plant, new RandomComponent()
                        {random = new Unity.Mathematics.Random(randomComponent.random.NextUInt())});


                    var chrome1A = txAutotrophChrome1Ab.Crossover(ref randomComponent.random);
                    var pollenChrome = txAutotrophGamete.txAutotrophChrome1AB;
                    var chrome1B = pollenChrome.Crossover(ref randomComponent.random);
                    var chrome1AB = new TxAutotrophChrome1AB {ValueA = chrome1A, ValueB = chrome1B};

                    ecb.SetComponent(index, plant, chrome1AB);
                    ecb.SetComponent(index, plant, chrome1AB.GetChrome1W());

                    var chrome2A = txAutotrophChrome2AB.Crossover(ref randomComponent.random);
                    var pollenChrome2 = txAutotrophGamete.txAutotrophChrome2AB;
                    var chrome2B = pollenChrome2.Crossover(ref randomComponent.random);
                    var chrome2AB = new TxAutotrophChrome2AB {ValueA = chrome2A, ValueB = chrome2B};
                    ecb.SetComponent(index, plant, chrome2AB);

                    var norm1 = chrome1AB.MaxNorm();
                    var baseC = 0.5f;
                    var norm2 = chrome2AB.MaxNorm();

                    ecb.SetComponent(index, plant, new EnergyStore {Value = txAutotrophSprout.energy});
                    ecb.DestroyEntity(index, entity);
                    
                     
                    if(environmentSettings[0].graphicsSettings.petals) {
                        var petal0 = ecb.Instantiate(index, prefabPetalEntity);
                        var petal1 = ecb.Instantiate(index, prefabPetalEntity);
                        var petal2 = ecb.Instantiate(index, prefabPetalEntity);
                        var petal3 = ecb.Instantiate(index, prefabPetalEntity);
                        var petal4 = ecb.Instantiate(index, prefabPetalEntity);
                        var petal5 = ecb.Instantiate(index, prefabPetalEntity);

                        ecb.SetComponent(index, petal0, new Translation() {Value = pos + new float3(0, 1.9f, 0)});
                        ecb.AddComponent(index, petal0, new Scale {Value = 1});
                        ecb.AddComponent(index, petal0, new TxAutotrophPetal() {Value = plant});

                        ecb.SetComponent(index, petal1, new Translation() {Value = pos + new float3(0, 1.9f, 0)});
                        ecb.AddComponent(index, petal1, new Scale {Value = 1});
                        ecb.SetComponent(index, petal1, new Rotation {Value = quaternion.Euler(0, math.PI / 3, 0)});
                        ecb.AddComponent(index, petal1, new TxAutotrophPetal() {Value = plant});
                        
                        ecb.SetComponent(index, petal2, new Translation() {Value = pos + new float3(0, 1.9f, 0)});
                        ecb.AddComponent(index, petal2, new Scale {Value = 1});
                        ecb.SetComponent(index, petal2, new Rotation {Value = quaternion.Euler(0, 2 * math.PI / 3, 0)});
                        ecb.AddComponent(index, petal2, new TxAutotrophPetal() {Value = plant});
                        
                        ecb.SetComponent(index, petal3, new Translation() {Value = pos + new float3(0, 1.9f, 0)});
                        ecb.AddComponent(index, petal3, new Scale {Value = 1});
                        ecb.SetComponent(index, petal3, new Rotation {Value = quaternion.Euler(0, 3 * math.PI / 3, 0)});
                        ecb.AddComponent(index, petal3, new TxAutotrophPetal() {Value = plant});
                        
                        ecb.SetComponent(index, petal4, new Translation() {Value = pos + new float3(0, 1.9f, 0)});
                        ecb.AddComponent(index, petal4, new Scale {Value = 1});
                        ecb.SetComponent(index, petal4, new Rotation {Value = quaternion.Euler(0, 4 * math.PI / 3, 0)});
                        ecb.AddComponent(index, petal4, new TxAutotrophPetal() {Value = plant});
                        
                        ecb.SetComponent(index, petal5, new Translation() {Value = pos + new float3(0, 1.9f, 0)});
                        ecb.AddComponent(index, petal5, new Scale {Value = 1});
                        ecb.SetComponent(index, petal5, new Rotation {Value = quaternion.Euler(0, 5 * math.PI / 3, 0)});
                        ecb.AddComponent(index, petal5, new TxAutotrophPetal() {Value = plant});
                        
                        ecb.AddComponent(index, plant, new TxAutotrophMeshes {
                            petal0 = petal0,
                            petal1 = petal1,
                            petal2 = petal2,
                            petal3 = petal3,
                            petal4 = petal4,
                            petal5 = petal5,
                        });
                    

                    
                        ecb.SetComponent(index, petal0, new MaterialColor
                            {Value = new float4(norm2.r0, norm2.g0, norm2.b0, 1)});
                        ecb.SetComponent(index, petal1, new MaterialColor
                            {Value = new float4(norm2.r1, norm2.g1, norm2.b1, 1)});
                        ecb.SetComponent(index, petal2, new MaterialColor
                            {Value = new float4(norm2.r2, norm2.g2, norm2.b2, 1)});
                        ecb.SetComponent(index, petal3, new MaterialColor
                            {Value = new float4(norm2.r3, norm2.g3, norm2.b3, 1)});
                        ecb.SetComponent(index, petal4, new MaterialColor
                            {Value = new float4(norm2.r4, norm2.g4, norm2.b4, 1)});
                        ecb.SetComponent(index, petal5, new MaterialColor
                            {Value = new float4(norm2.r5, norm2.g5, norm2.b5, 1)});
                    }
                }
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
                    prefabPetalEntity = prefabPetalEntity,
                    //txAutotrophChrome1ABCD = GetComponentDataFromEntity<TxAutotrophChrome1AB>(),
                    //txAutotrophPollenCD = GetComponentDataFromEntity<TxAutotrophPollen>()
                };
                JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
                //Schedule(m_Group, inputDeps);
                m_EndSimulationEcbSystem.AddJobHandleForProducer(jobHandle);

                jobHandle.Complete();
                jobDeps = jobHandle;
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
        NativeArray<Entity> prefabSeedArray;

        protected override void OnCreate() {
            m_Group = GetEntityQuery(
                ComponentType.ReadWrite<TxAutotrophPhenotype>(),
                ComponentType.ReadWrite<RandomComponent>(),
                ComponentType.ReadOnly<TxAutotrophChrome1AB>(),
                ComponentType.ReadOnly<TxAutotrophChrome1W>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<TxAutotrophChrome2AB>()
            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        struct MakeSprout : IJobForEachWithEntity<TxAutotrophPhenotype, RandomComponent, TxAutotrophChrome1AB,
            TxAutotrophChrome1W, TxAutotrophChrome2AB, Translation> {
            [ReadOnly] public NativeArray<Entity> prefabSeedArray;


            public EntityCommandBuffer.Concurrent ecb;
            [ReadOnly] public NativeArray<Environment.EnvironmentSettings> environmentSettings;
            [ReadOnly] public NativeArray<float> terrainHeight;

            public void Execute(Entity entity, int index,
                ref TxAutotrophPhenotype txAutotrophPhenotype,
                ref RandomComponent randomComponent,
                [ReadOnly] ref TxAutotrophChrome1AB txAutotrophChrome1AB,
                [ReadOnly] ref TxAutotrophChrome1W txAutotrophChrome1W,
                [ReadOnly] ref TxAutotrophChrome2AB txAutotrophChrome2AB,
                [ReadOnly] ref Translation translation
            ) {
                float MutateMult(float val, ref Unity.Mathematics.Random random, float rate, float rangeL,
                    float rangeH, float min, float max) {
                    bool mutate = rate < random.NextFloat(0, 1);
                    if (mutate) {
                        var mutant = math.min(max, math.max(min, val * random.NextFloat(rangeL, rangeH)));
                        return math.max(1, mutant);
                    }
                    else {
                        return val;
                    }
                }

                float MutateChrome2(float val, ref Unity.Mathematics.Random random, float rate, float rangeL,
                    float rangeH) {
                    bool mutate = rate < random.NextFloat(0, 1);
                    if (mutate) {
                        var mutant = val + random.NextFloat(rangeL, rangeH);
                        return math.max(TxAutotrophChrome2AB.MIN, math.min(TxAutotrophChrome2AB.MAX, mutant));
                    }
                    else {
                        return val;
                    }
                }

                var txAutotrophConsts = environmentSettings[0].txAutotrophConsts;
                var mRate = environmentSettings[0].txAutotrophConsts.mutationRate;
                var mRange = environmentSettings[0].txAutotrophConsts.mutationRange;
                var bounds = environmentSettings[0].environmentConsts.bounds;
                var heightScale = environmentSettings[0].environmentConsts.terrainScale;
                var mRangeH = 1 + mRange;
                var mRangeL = 1 - mRange;

                //Single Seed each frame only
                if (txAutotrophPhenotype.seed >= txAutotrophChrome1W.Value.seedSize) {
                    txAutotrophPhenotype.seed -= txAutotrophChrome1W.Value.seedSize;

                    var loc = txAutotrophConsts.seedRangeMultiplier
                              * randomComponent.random.NextFloat2(-1, 1)
                              * txAutotrophPhenotype.height / txAutotrophChrome1W.Value.seedSize;

                    var location = translation.Value + new float3(loc.x, 0, loc.y);
                    //too close to edge rounding errors can cause out of index  range errors
                    if (location.x > bounds.x+0.5f && location.x < bounds.z-0.5f &&
                        location.z > bounds.y+0.5f && location.z < bounds.w-0.5f) {
                        var height = Environment.TerrainValue(location, terrainHeight, bounds, heightScale);
                        location.y = height;
                        //var e = ecb.CreateEntity(index);
                        var e = ecb.Instantiate(index, prefabSeedArray[0]);
                        ecb.SetComponent(index, e, new Translation {Value = translation.Value});
                        ecb.AddComponent<TxAutotrophGamete>(index, e, new TxAutotrophGamete());
                        ecb.AddComponent<TxAutotrophSprout>(index, e, new TxAutotrophSprout() {
                            energy = txAutotrophChrome1W.Value.seedSize,
                            location = location
                        });

                        ecb.AddComponent<RandomComponent>(index, e, new RandomComponent()
                            {random = new Unity.Mathematics.Random(randomComponent.random.NextUInt())});



                        var chrome1AB = txAutotrophChrome1AB.Copy();
                        for (int i = 0; i < TxAutotrophChrome1.LENGTH; i++) {
                            chrome1AB.ValueA[i] = MutateMult(chrome1AB.ValueA[i], ref randomComponent.random
                                , mRate, mRangeL, mRangeH,
                                txAutotrophConsts.mutationLimitsChrome1.ValueA[i],
                                txAutotrophConsts.mutationLimitsChrome1.ValueB[i]);
                            chrome1AB.ValueB[i] = MutateMult(chrome1AB.ValueB[i], ref randomComponent.random
                                , mRate, mRangeL, mRangeH,
                                txAutotrophConsts.mutationLimitsChrome1.ValueA[i],
                                txAutotrophConsts.mutationLimitsChrome1.ValueB[i]);
                        }

                        var chrome1W = chrome1AB.GetChrome1W();
                        ecb.AddComponent(index, e, chrome1AB);
                        ecb.AddComponent<TxAutotrophChrome1W>(index, e, chrome1W);
                        var chrome2AB = txAutotrophChrome2AB.Copy();
                        for (int i = 0; i < TxAutotrophChrome2.LENGTH; i++) {
                            chrome2AB.ValueA[i] = MutateChrome2(chrome2AB.ValueA[i], ref randomComponent.random
                                , mRate, -2, 2);
                            chrome2AB.ValueB[i] = MutateChrome2(chrome2AB.ValueB[i], ref randomComponent.random
                                , mRate, -2, 2);
                        }

                        ecb.AddComponent(index, e, chrome2AB);

                    }
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
            prefabSeedArray = GetEntityQuery(
                ComponentType.ReadOnly<TxAutotrophSeed>(),
                ComponentType.ReadOnly<Prefab>()
            ).ToEntityArray(Allocator.TempJob);

            MakeSprout job = new MakeSprout() {
                prefabSeedArray = prefabSeedArray,
                environmentSettings = Environment.environmentSettings,
                terrainHeight = Environment.terrainHeight,
                ecb = ecb
            };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            m_EndSimulationEcbSystem.AddJobHandleForProducer(jobHandle);
            jobHandle.Complete();
            prefabSeedArray.Dispose();
            return jobHandle;
        }
    }



    [BurstCompile]

    public class TxAutotrophDebugDistances : JobComponentSystem {
        EntityQuery m_Group;

        protected override void OnCreate() {
            m_Group = GetEntityQuery(ComponentType.ReadWrite<DebugDistances>(),
                ComponentType.ReadOnly<TxAutotrophChrome1W>(),
                ComponentType.ReadOnly<TxAutotrophPhenotype>()
            );
        }

        struct TxGainEnergy : IJobForEach<DebugDistances,TxAutotrophPhenotype, TxAutotrophChrome1W> {

            [ReadOnly] public NativeArray<Environment.EnvironmentSettings> environmentSettings;
            [ReadOnly] public NativeArray<float> terrainLight;

            public void Execute(ref DebugDistances debugDistances,
                [ReadOnly] ref TxAutotrophPhenotype txAutotrophPhenotype,
                [ReadOnly] ref TxAutotrophChrome1W txAutotrophChrome1W
            ) {
                
                var txAutotrophConsts = environmentSettings[0].txAutotrophConsts;
                

                debugDistances.seedRange = txAutotrophConsts.seedRangeMultiplier
                          * txAutotrophPhenotype.height / txAutotrophChrome1W.Value.seedSize;
                
                debugDistances.pollenRange = math.max(0.00001f,
                    txAutotrophPhenotype.height + txAutotrophChrome1W.Value.pollenRange *
                    txAutotrophConsts.pollenRadiusMultiplier);
            }

            
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            TxGainEnergy job = new TxGainEnergy() {
                environmentSettings = Environment.environmentSettings,
                terrainLight = Environment.terrainLight,
            };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            jobHandle.Complete();
            return jobHandle;
        }
    }

}
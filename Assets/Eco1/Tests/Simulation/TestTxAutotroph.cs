using System.Collections;
using System.Collections.Generic;
using EcoSim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Entities;
using Unity.Entities.Tests;
using Unity.Collections;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;



namespace Tests {


    [TestFixture]
    //[Category("ECS Test")]
    public class TxAutotrophTest : ECSTestsFixture {
        protected Entity sprout;
        
        [SetUp]
        public override void Setup() {
            base.Setup();
            
            // start with 
            var leaf = m_Manager.CreateEntity();
            m_Manager.AddComponent<Prefab>(leaf); 
            m_Manager.AddComponentData(leaf, new Translation{ Value = float3.zero});
            m_Manager.AddComponent<TxAutotrophLeafMeshFlag>(leaf);
            
            //prefab plant
            var stem = m_Manager.CreateEntity();
            m_Manager.AddComponent<Prefab>(stem); 
            m_Manager.AddComponent<TxAutotroph>(stem);
            m_Manager.AddComponentData(stem, new Translation{ Value = float3.zero});
            TxAutotrophBehaviour.AddComponentDatas(stem, m_Manager);
            m_Manager.AddComponentData(stem, new PhysicsCollider {
                    Value = Unity.Physics.SphereCollider.Create(
                        new SphereGeometry {
                            Center = float3.zero,
                            Radius = 1,
                        }, CollisionFilter.Default, 
                        new Unity.Physics.Material{Flags = Unity.Physics.Material.MaterialFlags.IsTrigger})});
            
            sprout = m_Manager.CreateEntity();
            m_Manager.AddComponentData(sprout, new RandomComponent {random = new Unity.Mathematics.Random(1)});
            m_Manager.AddComponentData(sprout, new Gamete(){isFertilized = true});
            m_Manager.AddComponentData(sprout, new TxAutotrophSprout {location = new float3(1,2,3),energy = 5});
            m_Manager.AddComponentData(sprout, new  TxAutotrophChrome1AB());
            m_Manager.AddComponentData(sprout, new  TxAutotrophChrome1W{Value = new TxAutotrophChrome1()});
            m_Manager.AddComponentData(sprout, new  TxAutotrophChrome2{});
            
            var petal = m_Manager.CreateEntity();
            m_Manager.AddComponentData(petal, new TxAutotrophPetalMeshFlag());
            m_Manager.AddComponentData(petal, new Prefab());
            m_Manager.AddComponentData(petal, new Translation());
            m_Manager.AddComponentData(petal, new Rotation());
            m_Manager.AddComponentData(petal, new MaterialColor());
            
            var pollenTrigger = m_Manager.CreateEntity();
            m_Manager.AddComponentData(pollenTrigger, new TxAutotrophPollen());
            m_Manager.AddComponentData(pollenTrigger, new Prefab());
            m_Manager.AddComponentData(pollenTrigger, new Translation());
            m_Manager.AddComponentData(pollenTrigger, new PhysicsCollider {
                Value = Unity.Physics.SphereCollider.Create(
                    new SphereGeometry {
                        Center = float3.zero,
                        Radius = 1,
                    }, CollisionFilter.Default, 
                    new Unity.Physics.Material{Flags = Unity.Physics.Material.MaterialFlags.IsTrigger})});
            
            
            var seed = m_Manager.CreateEntity();
            m_Manager.AddComponentData(seed, new TxAutotrophSeed());
            m_Manager.AddComponentData(seed, new Gamete(){isFertilized = true});
            m_Manager.AddComponentData(seed, new Prefab());
            m_Manager.AddComponentData(seed, new Translation());
            m_Manager.AddComponentData(seed, new PhysicsCollider {
                Value = Unity.Physics.SphereCollider.Create(
                    new SphereGeometry {
                        Center = float3.zero,
                        Radius = 1,
                    }, CollisionFilter.Default, 
                    new Unity.Physics.Material{Flags = Unity.Physics.Material.MaterialFlags.IsTrigger})});
            var es = new Environment.EnvironmentSettings[1];
            es[0].txAutotrophConsts.pollenRadiusMultiplier = 1;
            Environment.environmentSettings = new NativeArray<Environment.EnvironmentSettings>(es,Allocator.Persistent);
        }

        [TearDown]
        public override void TearDown() {
            base.TearDown();
            Environment.environmentSettings.Dispose();
        }

        [Test]
        public void Tx_Sprout_Test() {
            var presprouts = m_Manager.CreateEntityQuery(ComponentType.ReadWrite<RandomComponent>(),
                ComponentType.ReadOnly<Gamete>(),
                ComponentType.ReadOnly<TxAutotrophSprout>(),
                ComponentType.ReadOnly<TxAutotrophChrome1AB>(),
                ComponentType.ReadOnly<TxAutotrophChrome1W>(),
                ComponentType.ReadOnly<TxAutotrophChrome2>()
            ).ToEntityArray(Allocator.TempJob);
            Assert.AreEqual(1,presprouts.Length,"Presprouts count");
            presprouts.Dispose();
            
            World.CreateSystem<TxAutotrophSproutSystem>().Update(); 
            World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update();
            
            var sprouts = m_Manager.CreateEntityQuery(ComponentType.ReadWrite<RandomComponent>(),
                ComponentType.ReadOnly<Gamete>(),
                ComponentType.ReadOnly<TxAutotrophSprout>(),
                ComponentType.ReadOnly<TxAutotrophChrome1AB>(),
                ComponentType.ReadOnly<TxAutotrophChrome1W>(),
                ComponentType.ReadOnly<TxAutotrophChrome2>()
            ).ToEntityArray(Allocator.TempJob);
            Assert.AreEqual(0,sprouts.Length,"Sprouts count");
            sprouts.Dispose();
            
            var stems = m_Manager.CreateEntityQuery(ComponentType.ReadOnly<TxAutotroph>(),
                ComponentType.ReadOnly<TxAutotrophParts>()
            ).ToEntityArray(Allocator.TempJob);
            Assert.AreEqual(1,stems.Length,"Stems count");
            
            var petals = m_Manager.CreateEntityQuery(ComponentType.ReadOnly<TxAutotrophPetalMeshFlag>()
            ).ToEntityArray(Allocator.TempJob);
            Assert.AreEqual(6,petals.Length,"Petals count");
            
            stems.Dispose();
            petals.Dispose();
        }

        [Test]
        public void TxAutotrophGrow_Test() {
            //set Environment & genome 
            
            var chrome1 = new TxAutotrophChrome1 {
                nrg2Height = 1,
                nrg2Leaf = 1,
                nrg2Seed = 1,
                nrg2Storage = 1,
                seedSize = 4, // not tested yet
                maxHeight = 5,
                maxLeaf = 5.5f
            };


            m_Manager.SetComponentData(sprout, new TxAutotrophChrome1AB {
                ValueA = chrome1.Copy(),
                ValueB = chrome1.Copy()
            });
           
            var es = Environment.environmentSettings[0];
            es.txAutotrophConsts.seedDivisor = 2;
            es.txAutotrophConsts.stemScale = 1;
            es.txAutotrophConsts.minShadeRadius = 1;
            es.txAutotrophConsts.leafShadeRadiusMultiplier = 10;
            Environment.environmentSettings[0] = es;
            
            World.CreateSystem<TxAutotrophSproutSystem>().Update(); 
            World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update();

            var stems = m_Manager.CreateEntityQuery(ComponentType.ReadOnly<TxAutotroph>(),
                ComponentType.ReadOnly<TxAutotrophParts>(),
                ComponentType.ReadOnly<EnergyStore>(),
                ComponentType.ReadOnly<TxAutotrophPhenotype>(),
                ComponentType.ReadOnly<Scale>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<PhysicsCollider>()
                
            ).ToEntityArray(Allocator.TempJob);

            var plant = stems[0];
            stems.Dispose();
            
            m_Manager.SetComponentData(plant, new EnergyStore(){Value = 10});
            World.CreateSystem<TxAutotrophGrow>().Update();
           
            Assert.AreEqual( 3.5f , m_Manager.GetComponentData<TxAutotrophPhenotype>(plant).height,
                "Height");
            Assert.AreEqual( 3.5f , m_Manager.GetComponentData<TxAutotrophPhenotype>(plant).leaf,
                "Leaf");
            Assert.AreEqual( 1.25f , m_Manager.GetComponentData<TxAutotrophPhenotype>(plant).seed,
                "Seed");
            Assert.AreEqual( 2.5f , m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore");
            Assert.AreEqual(3.5f, m_Manager.GetComponentData<Scale>(plant).Value, "stem Scale");
            
            World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update();
           
            
            //second pass catches missing m_EndSimulationEcbSystem.AddJobHandleForProducer(jobHandle)in TxAutotrophGrow
            m_Manager.SetComponentData(plant, new EnergyStore(){Value = 10f});
            World.CreateSystem<TxAutotrophGrow>().Update();
            World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update();
            Assert.AreEqual( 4f , m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore");
            Assert.AreEqual( 5f , m_Manager.GetComponentData<TxAutotrophPhenotype>(plant).height,
                "Height Max");
            Assert.AreEqual( 5f, m_Manager.GetComponentData<Scale>(plant).Value,
                "stem Scale Max");
            Assert.AreEqual( 5.5f , m_Manager.GetComponentData<TxAutotrophPhenotype>(plant).leaf,
                "leaf Max");
                Assert.AreEqual( 2.5f , m_Manager.GetComponentData<TxAutotrophPhenotype>(plant).seed,
                "seed size ");
        }
    
        
       // [Test]
       // public void TxAutotrophLight_Test() {
       //     World.CreateSystem<TxAutotrophLight>().Update();
       //     var energy = m_Manager.GetComponentData<EnergyStore>(plant).Value;
       //     Assert.AreEqual(Environment.defaultLightEnergy*Environment.Fitness(1), m_Manager.GetComponentData<EnergyStore>(plant).Value,
       //         "EnergyStore");
       // }
      

        [Test]
        public void TxAutotrophPayMaintenance_Test() {
            
            World.CreateSystem<TxAutotrophSproutSystem>().Update(); 
            World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update();

            var stems = m_Manager.CreateEntityQuery(ComponentType.ReadOnly<TxAutotroph>(),
                ComponentType.ReadOnly<TxAutotrophParts>(),
                ComponentType.ReadOnly<EnergyStore>(),
                ComponentType.ReadOnly<TxAutotrophPhenotype>(),
                ComponentType.ReadOnly<Scale>(),
                ComponentType.ReadOnly<Translation>()
                
            ).ToEntityArray(Allocator.TempJob);

            var plant = stems[0];
            stems.Dispose();
            
            m_Manager.SetComponentData(plant, new  TxAutotrophChrome1W{ Value = new TxAutotrophChrome1 {
                ageRate = 1,
                maxHeight = 1,
                maxLeaf = 1,
                nrg2Height = 1,
                nrg2Leaf = 1,
                nrg2Seed = 1,
                nrg2Storage = 1,
                seedSize = 1
            }});
            //age
            var es = Environment.environmentSettings[0];
            es.txAutotrophConsts.baseCost= 0;
            es.txAutotrophConsts.ageMultiple = 0;
            es.txAutotrophConsts.heightCostMultiple= 0;
            es.txAutotrophConsts.leafCostMultiple = 0;
            Environment.environmentSettings[0] = es;
            m_Manager.SetComponentData(plant,new TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed =  0,
                age = 0
            });
            m_Manager.AddComponentData(plant, new  EnergyStore(){Value = 10});
            World.CreateSystem<TxAutotrophPayMaintenance>().Update();
            // age increases during maintenance 
            Assert.AreEqual( 1, m_Manager.GetComponentData<TxAutotrophPhenotype>(plant).age,
                " age");
            Assert.AreEqual( 9f, m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore age");
            
            es.txAutotrophConsts.baseCost= 1;
            Environment.environmentSettings[0] = es;
            m_Manager.SetComponentData(plant,new TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed =  0,
                age = 0
            });
            m_Manager.AddComponentData(plant, new  EnergyStore(){Value = 10});
            World.CreateSystem<TxAutotrophPayMaintenance>().Update();
            // age(1) * ageRate( 1) + base (1)
            Assert.AreEqual( 8f, m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore base + age/ageRate");
            
            es.txAutotrophConsts.ageMultiple= 1;
            Environment.environmentSettings[0] = es;
            m_Manager.SetComponentData(plant,new TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed =  0,
                age = 0
            });
            m_Manager.AddComponentData(plant, new  EnergyStore(){Value = 10});
            World.CreateSystem<TxAutotrophPayMaintenance>().Update();
            // age(1) * ageRate( 1) + ageMultiple (1)/age(1)
            Assert.AreEqual( 7f, m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore base + age/ageRate + (ageMultiple*ageRate)");
            
            
            es.txAutotrophConsts.heightCostMultiple= 1;
            Environment.environmentSettings[0] = es;
            m_Manager.SetComponentData(plant,new TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed =  0,
                age = 0
            });
            m_Manager.AddComponentData(plant, new  EnergyStore(){Value = 10});
            World.CreateSystem<TxAutotrophPayMaintenance>().Update();
            // age(1) * ageRate( 1) + heightMultiple (1) * height
            Assert.AreEqual( 6f, m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore base + age/ageRate + (ageMultiple*ageRate) + (heightMultiple * height)");
            
            
            es.txAutotrophConsts.leafCostMultiple= 1;
            Environment.environmentSettings[0] = es;
            m_Manager.SetComponentData(plant,new TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed =  0,
                age = 0
            });
            m_Manager.AddComponentData(plant, new  EnergyStore(){Value = 10});
            World.CreateSystem<TxAutotrophPayMaintenance>().Update();
            // age(1) * ageRate( 1) + leafMultiple (1) * leaf
            Assert.AreEqual( 5f, m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore base + age/ageRate + (ageMultiple*ageRate) + (heightMultiple * height) + leafMultiple * leaf ");
            
            m_Manager.SetComponentData(plant,new TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed =  0,
                age = 0
            });
            m_Manager.AddComponentData(plant, new  EnergyStore(){Value = 3});
            World.CreateSystem<TxAutotrophPayMaintenance>().Update();
            Assert.AreEqual( -2f, m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore dying");
            //testing death
            World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>().Update();
            var stemDead = m_Manager.CreateEntityQuery(ComponentType.ReadOnly<TxAutotroph>(),
                ComponentType.ReadOnly<TxAutotrophParts>(),
                ComponentType.ReadOnly<EnergyStore>(),
                ComponentType.ReadOnly<TxAutotrophPhenotype>(),
                ComponentType.ReadOnly<Scale>(),
                ComponentType.ReadOnly<Translation>()
                
            ).ToEntityArray(Allocator.TempJob);
            
            Assert.AreEqual(stemDead.Length,0,"Plant should have died");
            stemDead.Dispose();
        }
        
        [Test]
        public void TxAutotrophMakeSprout_Test() {
            
            var th = new float[513*513];
            
            Environment.terrainHeight = new NativeArray<float>(th,Allocator.Persistent);
            
            var es = Environment.environmentSettings[0];
            es.txAutotrophConsts.seedDivisor = 2;
            es.txAutotrophConsts.stemScale = 1;
            es.txAutotrophConsts.baseCost= 0;
            es.txAutotrophConsts.ageMultiple = 0;
            es.txAutotrophConsts.heightCostMultiple= 0;
            es.txAutotrophConsts.leafCostMultiple = 0;
            es.environmentConsts.bounds = new float4(-256,-256,256,256);
            es.environmentConsts.terrainResolution = 513;
            Environment.environmentSettings[0] = es;
            
            
            World.CreateSystem<TxAutotrophSproutSystem>().Update(); 
            World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update();

            var stems = m_Manager.CreateEntityQuery(ComponentType.ReadOnly<TxAutotroph>(),
                ComponentType.ReadOnly<TxAutotrophParts>(),
                ComponentType.ReadOnly<EnergyStore>(),
                ComponentType.ReadOnly<TxAutotrophPhenotype>(),
                ComponentType.ReadOnly<Scale>(),
                ComponentType.ReadOnly<Translation>()
                
            ).ToEntityArray(Allocator.TempJob);

            var plant = stems[0];
            stems.Dispose();
            
            //check making one sprout 
            
            m_Manager.SetComponentData(plant, new  TxAutotrophChrome1W{ Value = new TxAutotrophChrome1 {
                ageRate = 1,
                maxHeight = 1,
                maxLeaf = 1,
                nrg2Height = 1,
                nrg2Leaf = 1,
                nrg2Seed = 1,
                nrg2Storage = 1,
                seedSize = 1
            }});
            //age
           
            m_Manager.SetComponentData(plant,new TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed =  1.5f,
                age = 0
            });
           
            World.CreateSystem<TxAutotrophMakeSproutSystem>().Update();
            Assert.AreEqual( 0.5f, m_Manager.GetComponentData<TxAutotrophPhenotype>(plant).seed,
                "single Seed energy");
            World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update();
             
            
            var sprouts = m_Manager.CreateEntityQuery(ComponentType.ReadOnly<TxAutotrophSprout>()
            ).ToEntityArray(Allocator.TempJob);
            
            Assert.AreEqual(1,sprouts.Length," single sprout");
            sprouts.Dispose();
            
            //check making more sprouts 
            // now only make 1 sprout
            m_Manager.SetComponentData(plant,new TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed =  3f,
                age = 0
            });

            World.CreateSystem<TxAutotrophMakeSproutSystem>().Update();
            Assert.AreEqual( 2f, m_Manager.GetComponentData<TxAutotrophPhenotype>(plant).seed,
                "multi Seed energy");
            World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update();
            
            var sprouts3 = m_Manager.CreateEntityQuery(ComponentType.ReadOnly<TxAutotrophSprout>()
            ).ToEntityArray(Allocator.TempJob);
            
            Assert.AreEqual(2,sprouts3.Length," triple + single sprout");
            sprouts3.Dispose();
            Environment.terrainHeight.Dispose();
        }
    }
}


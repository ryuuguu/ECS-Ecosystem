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
            m_Manager.AddComponentData(stem, new Shade{ Value = 0});
            m_Manager.AddComponentData(stem, new EnergyStore{ Value = 0});
            m_Manager.AddComponentData(stem, new TxAutotrophParts {});
            m_Manager.AddComponentData(stem, new PhysicsCollider {
                    Value = Unity.Physics.SphereCollider.Create(
                        new SphereGeometry {
                            Center = float3.zero,
                            Radius = 1,
                        }, CollisionFilter.Default,new Unity.Physics.Material{Flags = Unity.Physics.Material.MaterialFlags.IsTrigger})});

            
            //physics collider
            
            m_Manager.AddComponentData(stem, new  TxAutotrophGenome {
            });
            m_Manager.AddComponentData(stem, new  TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed =  0,
                age = 0
            });
            
            
            sprout = m_Manager.CreateEntity();
            m_Manager.AddComponentData(sprout, new RandomComponent {random = new Unity.Mathematics.Random(1)});
            m_Manager.AddComponentData(sprout, new TxAutotrophSprout {location = new float3(1,2,3),energy = 5});
            m_Manager.AddComponentData(sprout, new  TxAutotrophGenome { });
            
            
            var es = new Environment.EnvironmentSettings[1]; 
            Environment.environmentSettings = new NativeArray<Environment.EnvironmentSettings>(es,Allocator.Persistent);

        }

        [TearDown]
        public override void TearDown() {
            base.TearDown();
            Environment.environmentSettings.Dispose();
        }

        [Test]
        public void Tx_Sprout_Test() {
            World.CreateSystem<TxAutotrophSproutSystem>().Update(); 
            World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update();
            
            var sprouts = m_Manager.CreateEntityQuery(ComponentType.ReadOnly<RandomComponent>(),
                ComponentType.ReadOnly<TxAutotrophSprout>(),
                ComponentType.ReadOnly<TxAutotrophGenome>()
            ).ToEntityArray(Allocator.TempJob);
            Assert.AreEqual(0,sprouts.Length,"Sprouts count");
            sprouts.Dispose();
            
            var stems = m_Manager.CreateEntityQuery(ComponentType.ReadOnly<TxAutotroph>(),
                ComponentType.ReadOnly<TxAutotrophParts>()
            ).ToEntityArray(Allocator.TempJob);
            Assert.AreEqual(1,stems.Length,"Stems count");
            
            var leafs = m_Manager.CreateEntityQuery(ComponentType.ReadOnly<TxAutotrophLeafMeshFlag>()
            ).ToEntityArray(Allocator.TempJob);
            Assert.AreEqual(1,leafs.Length,"Leafs count");

            var e1 = m_Manager.GetComponentData<TxAutotrophParts>(stems[0]).leaf;
            var e2 = leafs[0];
            Assert.AreEqual(e1,e2,"stem.part.leaf == leaf");
            
            stems.Dispose();
            leafs.Dispose();
        }

        [Test]
        public void TxAutotrophGrow_Test() {
            //set Environment & genome 
            m_Manager.SetComponentData(sprout, new TxAutotrophGenome {
                nrg2Height = 1,
                nrg2Leaf = 1,
                nrg2Seed = 1,
                nrg2Storage = 1,
                seedSize = 4, // not tested yet
                maxHeight = 5,
                maxLeaf = 5.5f
                
            });
            var es = Environment.environmentSettings[0];
            es.txAutotrophConsts.seedDivisor = 2;
            es.txAutotrophConsts.stemScale = 1;
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
            
            m_Manager.SetComponentData(plant, new EnergyStore(){Value = 10});
            World.CreateSystem<TxAutotrophGrow>().Update();
            var energy = m_Manager.GetComponentData<EnergyStore>(plant).Value;
            Assert.AreEqual( 2.5f , m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore");
            Assert.AreEqual( 3.5f , m_Manager.GetComponentData<TxAutotrophPhenotype>(plant).height,
                "Height");
            Assert.AreEqual( 3.5f , m_Manager.GetComponentData<TxAutotrophPhenotype>(plant).leaf,
                "Leaf");
            Assert.AreEqual( 1.25f , m_Manager.GetComponentData<TxAutotrophPhenotype>(plant).seed,
                "Seed");
            World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update();
            Assert.AreEqual(3.5f, m_Manager.GetComponentData<Scale>(plant).Value, "stem Scale");
            
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
    
        /*
       // [Test]
       // public void TxAutotrophLight_Test() {
       //     World.CreateSystem<TxAutotrophLight>().Update();
       //     var energy = m_Manager.GetComponentData<EnergyStore>(plant).Value;
       //     Assert.AreEqual(Environment.defaultLightEnergy*Environment.Fitness(1), m_Manager.GetComponentData<EnergyStore>(plant).Value,
       //         "EnergyStore");
       // }
        
        
        [Test]
        public void TxAutotrophPayMaintenance_Test() {
            m_Manager.SetComponentData(plant,new TxAutotrophGenome() {
                ageRate = 1,
                maxHeight = 1,
                maxLeaf = 1,
                nrg2Height = 1,
                nrg2Leaf = 1,
                nrg2Seed = 1,
                nrg2Storage = 1,
                seedSize = 1
            });
            //age
            m_Manager.SetComponentData(plant,new TxAutotrophMaintenance(){
                baseValue = 0,
                ageMultiple = 0,
                heightMultiple = 0,
                leafMultiple = 0
                });
            m_Manager.SetComponentData(plant,new TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed =  0,
                age = 0
            });
            m_Manager.AddComponentData(plant, new  EnergyStore(){Value = 10});
            World.CreateSystem<TxAutotrophPayMaintenance>().Update();
            // age(1) * ageRate( 1)
            Assert.AreEqual( 9f, m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore age");
            
            m_Manager.SetComponentData(plant,new TxAutotrophMaintenance(){
                baseValue = 1,
                ageMultiple = 0,
                heightMultiple = 0,
                leafMultiple = 0
            });
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
                "EnergyStore base");
            
            m_Manager.SetComponentData(plant,new TxAutotrophMaintenance(){
                baseValue = 0,
                ageMultiple = 1,
                heightMultiple = 0,
                leafMultiple = 0
            });
            m_Manager.SetComponentData(plant,new TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed =  0,
                age = 0
            });
            m_Manager.AddComponentData(plant, new  EnergyStore(){Value = 10});
            World.CreateSystem<TxAutotrophPayMaintenance>().Update();
            // age(1) * ageRate( 1) + ageMultiple (1)/age(1)
            Assert.AreEqual( 8f, m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore ageMultiple");
            
            m_Manager.SetComponentData(plant,new TxAutotrophMaintenance(){
                baseValue = 0,
                ageMultiple = 0,
                heightMultiple = 1,
                leafMultiple = 0
            });
            m_Manager.SetComponentData(plant,new TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed =  0,
                age = 0
            });
            m_Manager.AddComponentData(plant, new  EnergyStore(){Value = 10});
            World.CreateSystem<TxAutotrophPayMaintenance>().Update();
            // age(1) * ageRate( 1) + heightMultiple (1) * height
            Assert.AreEqual( 8f, m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore heightMultiple");
            
            m_Manager.SetComponentData(plant,new TxAutotrophMaintenance(){
                baseValue = 0,
                ageMultiple = 0,
                heightMultiple = 0,
                leafMultiple = 1
            });
            m_Manager.SetComponentData(plant,new TxAutotrophPhenotype {
                leaf = 1,
                height = 1,
                seed =  0,
                age = 0
            });
            m_Manager.AddComponentData(plant, new  EnergyStore(){Value = 10});
            World.CreateSystem<TxAutotrophPayMaintenance>().Update();
            // age(1) * ageRate( 1) + leafMultiple (1) * leaf
            Assert.AreEqual( 8f, m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore leafMultiple");

        }
        
        */
        


    }


}


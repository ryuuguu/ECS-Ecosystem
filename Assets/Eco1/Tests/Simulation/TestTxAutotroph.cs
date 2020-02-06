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
            m_Manager.AddComponentData(stem, new TxAutotrophParts {
            });

            
            //physics collider
            
            m_Manager.AddComponentData(stem, new  TxAutotrophGenome {
            });
            m_Manager.AddComponentData(stem, new  TxAutotrophPhenotype {
                  
            });
            
            
            sprout = m_Manager.CreateEntity();
            m_Manager.AddComponentData(sprout, new RandomComponent {random = new Unity.Mathematics.Random(1)});
            m_Manager.AddComponentData(sprout, new TxAutotrophSprout {location = new float3(1,2,3),energy = 5});
            m_Manager.AddComponentData(sprout, new  TxAutotrophGenome {
                  
            });
            

        }

        [TearDown]
        public override void TearDown() {
            base.TearDown();
        }

        [Test]
        public void Tx_Sprout_Test() {
            World.CreateSystem<TxAutotrophSproutSystem>().Update(); 
            World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update();
            
            var sprouts = m_Manager.CreateEntityQuery(ComponentType.ReadWrite<RandomComponent>(),
                ComponentType.ReadOnly<TxAutotrophSprout>(),
                ComponentType.ReadOnly<TxAutotrophGenome>()
            ).ToEntityArray(Allocator.TempJob);
            Assert.AreEqual(0,sprouts.Length,"Sprouts count");
            sprouts.Dispose();
            
            //entity query to count TxAutotroph == 1
            //entity query to count TxAutotrophLeafMeshFlag == 1
            

            //check stem part == leaf exist and has TxAutotrophLeafMeshFlag


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
        [Test]
        public void TxAutotrophGrow_Test() {
            m_Manager.SetComponentData<EnergyStore>(plant, new EnergyStore(){Value = 10});
            World.CreateSystem<TxAutotrophGrow>().Update();
            var energy = m_Manager.GetComponentData<EnergyStore>(plant).Value;
            Assert.AreEqual( 2.5f , m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore");
            Assert.AreEqual( 3.5f , m_Manager.GetComponentData<TxAutotrophPhenotype>(plant).height,
                "Height");
            Assert.AreEqual( 3.5f , m_Manager.GetComponentData<TxAutotrophPhenotype>(plant).leaf,
                "Leaf");
            Assert.AreEqual( 2.5f , m_Manager.GetComponentData<TxAutotrophPhenotype>(plant).seed,
                "Seed");
            World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update();
            Assert.AreEqual(3.5f, m_Manager.GetComponentData<Scale>(stem).Value, "stem Scale");
            
            //second pass catches missing m_EndSimulationEcbSystem.AddJobHandleForProducer(jobHandle)in TxAutotrophGrow
            m_Manager.SetComponentData<EnergyStore>(plant, new EnergyStore(){Value = 7.5f});
            World.CreateSystem<TxAutotrophGrow>().Update();
            World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update();
            Assert.AreEqual( 5.375f, m_Manager.GetComponentData<Scale>(stem).Value,
                "stem Scale B");
        }

*/
    }


}


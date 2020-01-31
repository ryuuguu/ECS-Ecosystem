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
        protected Entity plant;
        protected Entity stem;
        protected Entity leaf;
        protected Entity seedPod;
        
        [SetUp]
        public override void Setup() {
            base.Setup();
            plant = m_Manager.CreateEntity();
            
            stem = m_Manager.CreateEntity();
            m_Manager.AddComponentData(stem,new Translation(){Value = new float3(2,1,1)});
            m_Manager.AddComponentData(stem,new NonUniformScale(){Value = new float3(2,2,2)});

            leaf = m_Manager.CreateEntity();
            m_Manager.AddComponentData(leaf,new Translation(){Value = new float3(3,1,1)});
            m_Manager.AddComponentData(leaf,new NonUniformScale(){Value = new float3(3,3,3)});
            
            seedPod =  m_Manager.CreateEntity();
            m_Manager.AddComponentData(seedPod,new Translation(){Value = new float3(4,1,1)});
            m_Manager.AddComponentData(seedPod,new NonUniformScale(){Value = new float3(4,4,4)});

            TxAutotrophBehaviour.AddComponentDatas(plant,m_Manager,stem, leaf,seedPod);
            m_Manager.AddComponentData(plant,new Translation{Value = new float3(1,1,1)});
            Environment.defaultLightEnergy = 10;
            
            m_Manager.AddComponentData(plant,new  PhysicsCollider {Value = Unity.Physics.SphereCollider.Create(
                new SphereGeometry
                {
                    Center = float3.zero,
                    Radius = 1
                }, CollisionFilter.Default)});
        }

        [TearDown]
        public override void TearDown() {
            base.TearDown();
        }
        
        [Test]
        public void Tx_AddComponentDatas_Test() {
            Assert.AreEqual(stem, m_Manager.GetComponentData<TxAutotrophParts>(plant).stem,
                "stem Entity");
            
        }

        
        [Test]
        public void TxAutotrophLight_Test() {
            World.CreateSystem<TxAutotrophLight>().Update();
            var energy = m_Manager.GetComponentData<EnergyStore>(plant).Value;
            Assert.AreEqual(Environment.defaultLightEnergy*Environment.Fitness(1), m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore");
        }
        
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

    }


}


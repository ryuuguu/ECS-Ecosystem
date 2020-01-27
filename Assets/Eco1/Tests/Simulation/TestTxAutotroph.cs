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
            m_Manager.AddComponentData(plant,new Translation(){Value = new float3(1,1,1)});
            Enviroment.defualtLightEnergy = 10;
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
            Assert.AreEqual(Enviroment.defualtLightEnergy*Enviroment.Fitness(1), m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore");
        }
        
        [Test]
        public void TxAutotrophPayMaintenance_Test() {
            World.CreateSystem<TxAutotrophPayMaintenance>().Update();
            var energy = m_Manager.GetComponentData<EnergyStore>(plant).Value;
            Assert.AreEqual( -1.2f, m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore");
        }
        [Test]
        public void TxAutotrophGrow_Test() {
            m_Manager.SetComponentData<EnergyStore>(plant, new EnergyStore(){Value = 10});
            World.CreateSystem<TxAutotrophGrow>().Update();
            var energy = m_Manager.GetComponentData<EnergyStore>(plant).Value;
            Assert.AreEqual( 2.5f , m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore");
            Assert.AreEqual( 3.5f , m_Manager.GetComponentData<Height>(plant).Value,
                "Height");
            Assert.AreEqual( 3.5f , m_Manager.GetComponentData<Leaf>(plant).Value,
                "Leaf");
            Assert.AreEqual( 2.5f , m_Manager.GetComponentData<Seed>(plant).Value,
                "Seed");
            World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().Update();
            Assert.AreEqual(new float3(2, 2, 2) * 3.5f, m_Manager.GetComponentData<NonUniformScale>(stem).Value,
                "stem NonUniformScale");
        }

    }


}


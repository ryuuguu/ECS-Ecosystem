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
        
        [SetUp]
        public override void Setup() {
            base.Setup();
            plant = m_Manager.CreateEntity();
            TxAutotrophBehaviour.AddComponentDatas(plant,m_Manager);
            m_Manager.AddComponentData(plant,new Translation(){Value = new float3(1,1,1)});
            Enviroment.defualtLightEnergy = 10;
        }

        [TearDown]
        public override void TearDown() {
            base.TearDown();
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
        
    }


}


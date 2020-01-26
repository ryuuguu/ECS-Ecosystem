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

        [SetUp]
        public override void Setup() {
            base.Setup();
            Enviroment.defualtLightEnergy = 10;
        }

        [TearDown]
        public override void TearDown() {
            base.TearDown();
        }


        [Test]
        public void TxAutotrophLight_Test() {
            var plant = m_Manager.CreateEntity();
            m_Manager.AddComponentData(plant, new TxAutotroph() { });
            m_Manager.AddComponentData(plant, new EnergyStore() {Value = 0});
            m_Manager.AddComponentData(plant,new Translation(){Value = new float3(1,1,1)});
            World.CreateSystem<TxAutotrophLight>().Update();
            var energy = m_Manager.GetComponentData<EnergyStore>(plant).Value;
            Assert.AreEqual(Enviroment.defualtLightEnergy, m_Manager.GetComponentData<EnergyStore>(plant).Value,
                "EnergyStore");
        }
    }


}


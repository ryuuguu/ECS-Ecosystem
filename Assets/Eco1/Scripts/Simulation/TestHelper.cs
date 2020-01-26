using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Physics.Authoring;
using Unity.Mathematics;



namespace EcoSim {

    [GenerateAuthoringComponent]
    public struct TestHelperComponent : IComponentData {
        public Entity prefabGO;
    }

    
    
}
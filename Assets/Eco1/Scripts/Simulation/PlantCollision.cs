
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using System;
using UnityEditor;


[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class TriggerLeafSystem : JobComponentSystem {
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    StepPhysicsWorld m_StepPhysicsWorldSystem;

    NativeArray<int> m_TriggerEntitiesIndex;

    
    protected override void OnCreate() {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
       

        m_TriggerEntitiesIndex = new NativeArray<int>(1, Allocator.Persistent);
        m_TriggerEntitiesIndex[0] = 0;
    }

    protected override void OnDestroy() {
        m_TriggerEntitiesIndex.Dispose();
    }
    
    
    [BurstCompile]
    struct GetTriggerEventCount : ITriggerEventsJob
    {
        [NativeFixedLength(1)] public NativeArray<int> pCounter;
        
        // unsafe writing to this?
        public void Execute(TriggerEvent triggerEvent)
        {
            pCounter[0]++;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        
        // Get the number of TriggerEvents so that we can allocate a native array
        m_TriggerEntitiesIndex[0] = 0;
        JobHandle getTriggerEventCountJobHandle = new GetTriggerEventCount() {
            pCounter = m_TriggerEntitiesIndex,
        }.Schedule(m_StepPhysicsWorldSystem.Simulation, ref m_BuildPhysicsWorldSystem.PhysicsWorld, inputDeps);
        getTriggerEventCountJobHandle.Complete();

        Debug.Log("Count collisions: "+ m_TriggerEntitiesIndex[0]);
        
        return getTriggerEventCountJobHandle;
    }
}
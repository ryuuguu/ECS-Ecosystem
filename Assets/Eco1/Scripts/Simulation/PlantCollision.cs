
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using System;
using Unity.Transforms;
using UnityEditor;


[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class TriggerLeafSystem : JobComponentSystem {
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    StepPhysicsWorld m_StepPhysicsWorldSystem;

    NativeArray<int> m_TriggerEntitiesIndex;
    NativeArray<int> m_CollisionEntitiesIndex;

    
    protected override void OnCreate() {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
       

        m_TriggerEntitiesIndex = new NativeArray<int>(1, Allocator.Persistent);
        m_TriggerEntitiesIndex[0] = 0;
        
        m_CollisionEntitiesIndex = new NativeArray<int>(1, Allocator.Persistent);
        m_CollisionEntitiesIndex[0] = 0;
    }

    protected override void OnDestroy() {
        m_TriggerEntitiesIndex.Dispose();
        m_CollisionEntitiesIndex.Dispose();
    }
    
    struct GetCollisionEventCount : ICollisionEventsJob
    {
        [ReadOnly]public ComponentDataFromEntity<Translation> translations; 
        [NativeFixedLength(1)] public NativeArray<int> pCounter;
        public void Execute(CollisionEvent collisionEvent)
        {
            pCounter[0]++;
            Debug.Log("Collisions " +translations[collisionEvent.Entities.EntityA].Value.ToString() + " : " +
                      translations[collisionEvent.Entities.EntityB].Value.ToString()
            );
        }
    }
   
    struct GetTriggerEventCount : ITriggerEventsJob {
        [ReadOnly]public ComponentDataFromEntity<Translation> translations; 
        
        [NativeFixedLength(1)] public NativeArray<int> pCounter;
        
        // unsafe writing to this?
        public void Execute(TriggerEvent triggerEvent)
        {
            pCounter[0]++;
            Debug.Log("Triggers " +translations[triggerEvent.Entities.EntityA].Value.ToString() + " : " +
                      translations[triggerEvent.Entities.EntityB].Value.ToString()
                      );
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        ComponentDataFromEntity<Translation> translations = GetComponentDataFromEntity<Translation>();
        // Get the number of TriggerEvents so that we can allocate a native array
        m_TriggerEntitiesIndex[0] = 0;
        m_CollisionEntitiesIndex[0] = 0;
        
        
        JobHandle getTriggerEventCountJobHandle = new GetTriggerEventCount() {
            pCounter = m_TriggerEntitiesIndex,
            translations =  translations
        }.Schedule(m_StepPhysicsWorldSystem.Simulation, ref m_BuildPhysicsWorldSystem.PhysicsWorld, inputDeps);
        getTriggerEventCountJobHandle.Complete();
        
        
        JobHandle getCollisionEventCountJobHandle = new GetCollisionEventCount() {
            pCounter = m_TriggerEntitiesIndex,
            translations =  translations
        }.Schedule(m_StepPhysicsWorldSystem.Simulation, ref m_BuildPhysicsWorldSystem.PhysicsWorld, inputDeps);
        getCollisionEventCountJobHandle.Complete();
        
        
        Debug.Log("Count Triggers: "+UnityEngine.Time.frameCount + " : " + m_TriggerEntitiesIndex[0] + 
                  " : " + m_CollisionEntitiesIndex[0]) ;
        

        return getCollisionEventCountJobHandle;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using EcoSim;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.Serialization;
using Unity.Jobs;
using Unity.Rendering;


public struct TxAutotrophStatFlower : IComponentData {
   public Entity petal0;
   public Entity petal1;
   public Entity petal2;
   public Entity petal3;
   public Entity petal4;
   public Entity petal5; 
}

public struct TxAutotrophStatFlowerSprout : IComponentData {
   public float3 location;
   public float scale;
}

public class TxAutotrophStats  {
   public static NativeArray<Entity> MakeFlowerStats( EntityManager em, float4 bounds, int2 statsSize) {
      var width = (bounds.z - bounds.x) / statsSize.x;
      var height = (bounds.w - bounds.y) / statsSize.y;
      NativeArray<Entity> result = new NativeArray<Entity>(statsSize.x*statsSize.y,Allocator.Persistent);
      for (float i = 0; i < statsSize.x; i++) {
         for (float j = 0; j < statsSize.y; j++) {
           result[(int)(i*statsSize.y + j) ] = MakeFlowerSprout(em,
               new float3((i + 0.5f) * width+bounds.x,0,(j+0.5f)*height+bounds.w),
               width/2.1f);
         }
      }
      return result;
   }
   
   public static Entity MakeFlowerSprout(EntityManager em,float3 location, float scale) {
      var entity = em.CreateEntity();
      em.AddComponentData(entity, new TxAutotrophStatFlowerSprout{location = location, scale = scale});
      
      return entity;
   }
}

 public class TxAutotrophSproutFlowerStatsSystem : JobComponentSystem {
        EntityQuery m_Group;
        protected EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
        private NativeArray<Entity> prefabPetalArray;
        
        protected override void OnCreate() {
            m_Group = GetEntityQuery(
                ComponentType.ReadOnly<TxAutotrophStatFlowerSprout>()
            );
            m_EndSimulationEcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        struct SproutFlowerStat : IJobForEachWithEntity<TxAutotrophStatFlowerSprout> {
            [ReadOnly] public Entity prefabPetalEntity;
           
            
            public EntityCommandBuffer.Concurrent ecb;

            public void Execute(Entity entity, int index,
                
                [ReadOnly]ref TxAutotrophStatFlowerSprout txAutotrophStatFlowerSprout
            ) {
                
                var petal0 = ecb.Instantiate(index, prefabPetalEntity);
                var petal1 = ecb.Instantiate(index, prefabPetalEntity);
                var petal2 = ecb.Instantiate(index, prefabPetalEntity);
                var petal3 = ecb.Instantiate(index, prefabPetalEntity);
                var petal4 = ecb.Instantiate(index, prefabPetalEntity);
                var petal5 = ecb.Instantiate(index, prefabPetalEntity);
                
                ecb.SetComponent(index, petal0, new Translation() {Value = txAutotrophStatFlowerSprout.location });
                ecb.AddComponent(index, petal0, new Scale {Value = txAutotrophStatFlowerSprout.scale});
                ecb.SetComponent(index, petal0, new MaterialColor 
                    {Value = new float4(0.5f, 0.5f, 0.5f, 1)});
                
                ecb.SetComponent(index, petal1, new Translation() {Value = txAutotrophStatFlowerSprout.location });
                ecb.AddComponent(index, petal1, new Scale {Value = txAutotrophStatFlowerSprout.scale});
                ecb.SetComponent(index, petal1, new Rotation {Value = quaternion.Euler(0, math.PI / 3, 0)});
                ecb.SetComponent(index, petal1, new MaterialColor 
                    {Value = new float4(0.5f, 0.5f, 0.5f, 1)});

                ecb.SetComponent(index, petal2, new Translation() {Value = txAutotrophStatFlowerSprout.location });
                ecb.AddComponent(index, petal2, new Scale {Value = txAutotrophStatFlowerSprout.scale});
                ecb.SetComponent(index, petal2, new Rotation {Value = quaternion.Euler(0, 2 * math.PI / 3, 0)});
                ecb.SetComponent(index, petal2, new MaterialColor 
                    {Value = new float4(0.5f, 0.5f, 0.5f, 1)});

                ecb.SetComponent(index, petal3, new Translation() {Value = txAutotrophStatFlowerSprout.location });
                ecb.AddComponent(index, petal3, new Scale {Value= txAutotrophStatFlowerSprout.scale});
                ecb.SetComponent(index, petal3, new Rotation {Value = quaternion.Euler(0, 3 * math.PI / 3, 0)});
                ecb.SetComponent(index, petal3, new MaterialColor 
                    {Value = new float4(0.5f, 0.5f, 0.5f, 1)});

                ecb.SetComponent(index, petal4, new Translation() {Value = txAutotrophStatFlowerSprout.location });
                ecb.AddComponent(index, petal4, new Scale {Value = txAutotrophStatFlowerSprout.scale});
                ecb.SetComponent(index, petal4, new Rotation {Value = quaternion.Euler(0, 4 * math.PI / 3, 0)});
                ecb.SetComponent(index, petal4, new MaterialColor 
                    {Value = new float4(0.5f, 0.5f, 0.5f, 1)});

                ecb.SetComponent(index, petal5, new Translation() {Value = txAutotrophStatFlowerSprout.location });
                ecb.AddComponent(index, petal5, new Scale {Value = txAutotrophStatFlowerSprout.scale});
                ecb.SetComponent(index, petal5, new Rotation {Value = quaternion.Euler(0, 5 * math.PI / 3, 0)});
                ecb.SetComponent(index, petal5, new MaterialColor 
                    {Value = new float4(0.5f, 0.5f, 0.5f, 1)});
                
                ecb.AddComponent(index,entity,new TxAutotrophStatFlower {
                    petal0 = petal0,
                    petal1 = petal1,
                    petal2 = petal2,
                    petal3 = petal3,
                    petal4 = petal4,
                    petal5 = petal5
                });
                
                ecb.RemoveComponent<TxAutotrophStatFlowerSprout>(index,entity);
                
            }
        }
       
        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
            prefabPetalArray = GetEntityQuery(
                ComponentType.ReadOnly<TxAutotrophPetalMeshFlag>(),
                ComponentType.ReadOnly<Prefab>()
            ).ToEntityArray(Allocator.TempJob); 
            
            SproutFlowerStat job = new SproutFlowerStat() {
                prefabPetalEntity = prefabPetalArray[0],
                ecb=ecb
            };
            JobHandle jobHandle = job.Schedule(m_Group, inputDeps);
            m_EndSimulationEcbSystem.AddJobHandleForProducer(jobHandle);
            jobHandle.Complete();
            prefabPetalArray.Dispose();
            return jobHandle;
        }
    }


//system that turns sprouts to TxAutotrophStatFlower and destroys sprout
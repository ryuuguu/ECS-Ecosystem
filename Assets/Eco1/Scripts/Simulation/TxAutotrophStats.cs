using System;
using System.Collections;
using System.Collections.Generic;
using EcoSim;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.Serialization;
using Unity.Jobs;
using Unity.Rendering;

public struct TxAutotrophStatFlowerSprout : IComponentData {
   public float3 location;
   public float scale;
   public int index;
}

public struct TxAutotrophStatFlowerPetal : IComponentData {
    public int flowerIndex;
    public int petalIndex;
}


public class TxAutotrophStats  {

    public static int FlowerStatIndex(float3 location, float4 bounds, int2 statsSize) {
        var width = (bounds.z - bounds.x) / statsSize.x;
        var height = (bounds.w - bounds.y) / statsSize.y;
        return ((int) math.floor((location.x - bounds.x) / width)) * statsSize.y +
            ((int) math.floor((location.z - bounds.y) / width));
    }
    
    
   public static NativeArray<Entity> MakeFlowerStats( EntityManager em, float4 bounds, int2 statsSize) {
      var width = (bounds.z - bounds.x) / statsSize.x;
      var height = (bounds.w - bounds.y) / statsSize.y;
      NativeArray<Entity> result = new NativeArray<Entity>(statsSize.x*statsSize.y,Allocator.Persistent);
      for (float i = 0; i < statsSize.x; i++) {
         for (float j = 0; j < statsSize.y; j++) {
             var index = (int) (i * statsSize.y + j);
           result[index]= MakeFlowerSprout(em,
               new float3((i + 0.5f) * width+bounds.x,0,(j+0.5f)*height+bounds.w),
               width/2.1f,index);
         }
      }
      return result;
   }
   
   public static Entity MakeFlowerSprout(EntityManager em,float3 location, float scale, int index ) {
      var entity = em.CreateEntity();
      em.AddComponentData(entity, new TxAutotrophStatFlowerSprout{location = location, scale = scale, index = index});
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
                ecb.AddComponent(index, petal0, new TxAutotrophStatFlowerPetal {
                    flowerIndex = txAutotrophStatFlowerSprout.index,
                     petalIndex = 0
                });
                ecb.SetComponent(index, petal0, new MaterialColor 
                    {Value = new float4(0.5f, 0.5f, 0.5f, 1)});
                
                ecb.SetComponent(index, petal1, new Translation() {Value = txAutotrophStatFlowerSprout.location });
                ecb.AddComponent(index, petal1, new Scale {Value = txAutotrophStatFlowerSprout.scale});
                ecb.SetComponent(index, petal1, new Rotation {Value = quaternion.Euler(0, math.PI / 3, 0)});
                ecb.AddComponent(index, petal0, new TxAutotrophStatFlowerPetal {
                    flowerIndex = txAutotrophStatFlowerSprout.index,
                    petalIndex = 1
                });
                ecb.SetComponent(index, petal1, new MaterialColor 
                    {Value = new float4(0.5f, 0.5f, 0.5f, 1)});

                ecb.SetComponent(index, petal2, new Translation() {Value = txAutotrophStatFlowerSprout.location });
                ecb.AddComponent(index, petal2, new Scale {Value = txAutotrophStatFlowerSprout.scale});
                ecb.SetComponent(index, petal2, new Rotation {Value = quaternion.Euler(0, 2 * math.PI / 3, 0)});
                ecb.AddComponent(index, petal0, new TxAutotrophStatFlowerPetal {
                    flowerIndex = txAutotrophStatFlowerSprout.index,
                    petalIndex = 2
                });
                ecb.SetComponent(index, petal2, new MaterialColor 
                    {Value = new float4(0.5f, 0.5f, 0.5f, 1)});

                ecb.SetComponent(index, petal3, new Translation() {Value = txAutotrophStatFlowerSprout.location });
                ecb.AddComponent(index, petal3, new Scale {Value= txAutotrophStatFlowerSprout.scale});
                ecb.SetComponent(index, petal3, new Rotation {Value = quaternion.Euler(0, 3 * math.PI / 3, 0)});
                ecb.AddComponent(index, petal0, new TxAutotrophStatFlowerPetal {
                    flowerIndex = txAutotrophStatFlowerSprout.index,
                    petalIndex = 3
                });
                ecb.SetComponent(index, petal3, new MaterialColor 
                    {Value = new float4(0.5f, 0.5f, 0.5f, 1)});

                ecb.SetComponent(index, petal4, new Translation() {Value = txAutotrophStatFlowerSprout.location });
                ecb.AddComponent(index, petal4, new Scale {Value = txAutotrophStatFlowerSprout.scale});
                ecb.SetComponent(index, petal4, new Rotation {Value = quaternion.Euler(0, 4 * math.PI / 3, 0)});
                ecb.AddComponent(index, petal0, new TxAutotrophStatFlowerPetal {
                    flowerIndex = txAutotrophStatFlowerSprout.index,
                    petalIndex = 4
                });
                ecb.SetComponent(index, petal4, new MaterialColor 
                    {Value = new float4(0.5f, 0.5f, 0.5f, 1)});

                ecb.SetComponent(index, petal5, new Translation() {Value = txAutotrophStatFlowerSprout.location });
                ecb.AddComponent(index, petal5, new Scale {Value = txAutotrophStatFlowerSprout.scale});
                ecb.SetComponent(index, petal5, new Rotation {Value = quaternion.Euler(0, 5 * math.PI / 3, 0)});
                ecb.AddComponent(index, petal0, new TxAutotrophStatFlowerPetal {
                    flowerIndex = txAutotrophStatFlowerSprout.index,
                    petalIndex = 5
                });
                ecb.SetComponent(index, petal5, new MaterialColor 
                    {Value = new float4(0.5f, 0.5f, 0.5f, 1)});
                
                ecb.RemoveComponent<TxAutotrophStatFlowerSprout>(index,entity);
                
            }
        }
       
        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();
            prefabPetalArray = GetEntityQuery(
                ComponentType.ReadOnly<TxAutotrophPetalMeshFlag>(),
                ComponentType.ReadOnly<Prefab>()
            ).ToEntityArray(Allocator.Persistent);
            if (prefabPetalArray.Length > 0) {
                SproutFlowerStat job = new SproutFlowerStat() {
                    prefabPetalEntity = prefabPetalArray[0],
                    ecb = ecb
                };
                inputDeps = job.Schedule(m_Group, inputDeps);
                m_EndSimulationEcbSystem.AddJobHandleForProducer(inputDeps);
                inputDeps.Complete();
            }

            prefabPetalArray.Dispose();
            return inputDeps;
        }
    }


[BurstCompile]
    
public class DisplayTxAutotrophStats: JobComponentSystem {
    EntityQuery m_Group;
    EntityQuery m_GroupDisplay;


    public struct StatsArea {
        public TxAutotrophChrome2W chrome2W;
        public int count;
    }
    
    protected override void OnCreate() {
        m_Group = GetEntityQuery(
            ComponentType.ReadOnly<TxAutotrophChrome2W>(),
            ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadOnly<TxAutotroph>()
        );
        m_GroupDisplay = GetEntityQuery (
            ComponentType.ReadWrite<MaterialColor>(),
            ComponentType.ReadOnly<TxAutotrophStatFlowerPetal>()
        );
    }

    struct CollectFlowerStats : IJobForEach<TxAutotrophChrome2W, Translation>
    {
        public NativeArray<StatsArea> flowerStats;
        [ReadOnly]public NativeArray<Environment.EnvironmentSettings> environmentSettings;
        public void Execute(
            [ReadOnly] ref TxAutotrophChrome2W txAutotrophChrome2W, 
            [ReadOnly] ref Translation translation
        ) {
            var index =TxAutotrophStats.FlowerStatIndex(translation.Value,
                environmentSettings[0].environmentConsts.bounds,
                environmentSettings[0].environmentConsts.flowerStatsSize
            );
            var chrome2W = flowerStats[index].chrome2W.Add(txAutotrophChrome2W);

            flowerStats[index] = new StatsArea {
                count = flowerStats[index].count + 1,
                chrome2W = chrome2W
            };
        }
    }
    
    struct DisplayFlowerStats : IJobForEach<MaterialColor,TxAutotrophStatFlowerPetal >
    {
        public NativeArray<StatsArea> flowerStats;
        [ReadOnly]public NativeArray<Environment.EnvironmentSettings> environmentSettings;

        public void Execute(
            ref MaterialColor materialColor,
            [ReadOnly] ref TxAutotrophStatFlowerPetal txAutotrophStatFlowerPetal

        ) {
            var count = flowerStats[txAutotrophStatFlowerPetal.flowerIndex].count;
            if (count > 0) {
                var chrome2W = flowerStats[txAutotrophStatFlowerPetal.flowerIndex].chrome2W;
                int start = 3 * (txAutotrophStatFlowerPetal.petalIndex / 2);
                materialColor.Value =
                    new float4(chrome2W.Value[start] / (20 *count), chrome2W.Value[start + 1] / (20 *count),
                        chrome2W.Value[start + 2] / (20 *count), 1);
            }
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps) {

        float2 statSize = Environment.environmentSettings[0].environmentConsts.flowerStatsSize;
        int length = (int) (statSize.x * statSize.y);
        var flowerStats = new NativeArray<StatsArea> (length, Allocator.TempJob);
        for (int i = 0; i < length; i++) {
            flowerStats[i] = new StatsArea();
        }

        var jobHandle = inputDeps;
        /*
        
        CollectFlowerStats collectFlowerStats = new CollectFlowerStats() {
            flowerStats = flowerStats,
            environmentSettings = Environment.environmentSettings
        };
        jobHandle = collectFlowerStats.Schedule(m_Group, jobHandle);
        jobHandle.Complete();
        
        */

        DisplayFlowerStats displayFlowerStats = new DisplayFlowerStats(){
            flowerStats = flowerStats,
            environmentSettings = Environment.environmentSettings
        };
        jobHandle = displayFlowerStats.Schedule(m_GroupDisplay, jobHandle);
        jobHandle.Complete();
        
        flowerStats.Dispose();
        
        return jobHandle;
    }
}


//system that turns sprouts to TxAutotrophStatFlower and destroys sprout
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;

/*
public class GenerateNextStateSystem : JobComponentSystem
{
    
    // For Burst or Schedule (worker thread) jobs to access data outside the a job an explicit struct with a
    // read only variable is needed
    [BurstCompile]
    struct SetLive : IJobForEach<NextState, Live, Neighbors> {
        // liveLookup is a pointer to a native array of live components indexed by entity
        // since allows access outside set of entities being handled a single job o thread that is running 
        // concurrently with other threads accessing the same native array it must be marked read only 
        [ReadOnly]public ComponentDataFromEntity<Live> liveLookup; 
        public void Execute(ref NextState nextState, [ReadOnly] ref Live live,[ReadOnly] ref  Neighbors neighbors){
            
            int numLiveNeighbors = 0;
                
            numLiveNeighbors += liveLookup[neighbors.nw].value;
            numLiveNeighbors += liveLookup[neighbors.n].value;
            numLiveNeighbors += liveLookup[neighbors.ne].value;
            numLiveNeighbors += liveLookup[neighbors.w].value;
            numLiveNeighbors += liveLookup[neighbors.e].value;
            numLiveNeighbors += liveLookup[neighbors.sw].value;
            numLiveNeighbors += liveLookup[neighbors.s].value;
            numLiveNeighbors += liveLookup[neighbors.se].value;
            
            //Note math.Select(falseValue, trueValue, boolSelector)
            // did not want to pass in arrays so change to
            // 3 selects
            int bornValue = math.select(0, 1, numLiveNeighbors == 3);
            int stayValue = math.select(0, 1, numLiveNeighbors == 2);
            stayValue = math.select(stayValue, 1, numLiveNeighbors == 3);
            
            nextState.value = math.select( bornValue,stayValue, live.value== 1);
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps) { 
        // make a native array of live components indexed by entity
        ComponentDataFromEntity<Live> statuses = GetComponentDataFromEntity<Live>();
        
        SetLive neighborCounterJob = new SetLive() {
            liveLookup = statuses,
        };
        JobHandle jobHandle = neighborCounterJob.Schedule(this, inputDeps);
    
        return jobHandle;
    }
}
*/
/// <summary>
/// Grow autotrophs 
/// </summary>

[AlwaysSynchronizeSystem]
[BurstCompile]
public class UpdateGrowAutoTrophSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        var job = 
            Entities
                .ForEach((ref Energy energy, in GrowSpeed growSpeed) => { energy.Value += growSpeed.Value; }).Schedule(inputDeps);
        return job;
    }
}


//cell energy needs all components in same cell but only in same cell 
// so if needs one with same shared component 
// this may not be the same chunk 
// so how to find entities with same shared component to read from 
[AlwaysSynchronizeSystem]
[BurstCompile]
public class UpdateCellEneryAutotrophSystem : JobComponentSystem
{
/*
    struct CellEnery : IJobChunk<Energy, CellEnergyChunk, GrowSpeed> {
        public void Execute(Entity entiy, int index,ref Energy energy, ref CellEnergyChunk cellEnergyChunk,
            [ReadOnly] ref GrowSpeed growSpeed) {
            energy.Value += growSpeed.Value;
            //var entityChunk = entityManager.GetChunk(instance);
            //entityManager.SetChunkComponentData<CellEnergyChunk>(entityChunk, 
            //    new CellEnergyChunk(){Value = 0});
        }

    }
*/
    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        var job = 
            Entities
                .WithAll(ComponentType.ChunkComponentReadOnly<CellEnergyChunk>())
            .ForEach((Entity ent ) =>
            {
                var cellEnergyChunk = EntityManager.GetChunkComponentData<CellEnergyChunk>(ent);
                //energy.Value += growSpeed.Value;
                
            }).Schedule(inputDeps);
                
        return job;
    }
}

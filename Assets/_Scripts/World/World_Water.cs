using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using Mirror;
using UnityEngine;

public partial class World : IWaterWorld
{
    private WaterSimulation waterSimulation;
    private WaterSimulation lavaSimulation;
    private FluidTickScheduler fluidTicks;
    private JavaRandom fluidRandom;
    private int fluidRandomTickState;
    private FluidTickSave fluidRestore;
    public void RestoreFluidTicks(FluidTickSave save){fluidRestore=save;}
    public FluidTickSave CaptureFluidTicks()=>fluidTicks==null?fluidRestore:fluidTicks.Capture(fluidRandom.State,fluidRandomTickState);
    private sealed class LavaWorld : IWaterWorld
    {
        private readonly World world;
        public LavaWorld(World world) { this.world=world; }
        public int GetWaterCell(WaterPosition p) { return world.GetLavaCell(p); }
        public void SetWaterCell(WaterPosition p,int level) { world.SetLavaCell(p,level); }
    }
    private int betaPopulationJobs;
    private bool waitForBetaPopulation;
    [System.NonSerialized] private ConcurrentQueue<Vector3Int> runtimeWaterNotifications;
    private ConcurrentQueue<Vector3Int> waterNotifications => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeWaterNotifications, () => new ConcurrentQueue<Vector3Int>());
    [System.NonSerialized] private Queue<ChunkData> runtimeWaterChunksToActivate;
    private Queue<ChunkData> waterChunksToActivate => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeWaterChunksToActivate, () => new Queue<ChunkData>());
    [System.NonSerialized] private Dictionary<Vector3Int, byte> runtimeWaterChanges;
    private Dictionary<Vector3Int, byte> waterChanges => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeWaterChanges, () => new Dictionary<Vector3Int, byte>());
    [System.NonSerialized] private Dictionary<Vector3Int, Dictionary<Vector3Int, byte>> runtimePendingWaterStates;
    private Dictionary<Vector3Int, Dictionary<Vector3Int, byte>> pendingWaterStates => System.Threading.LazyInitializer.EnsureInitialized(ref runtimePendingWaterStates, () => new Dictionary<Vector3Int, Dictionary<Vector3Int, byte>>());
    [System.NonSerialized] private HashSet<ChunkRenderer> runtimeWaterDirtyChunks;
    private HashSet<ChunkRenderer> waterDirtyChunks => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeWaterDirtyChunks, () => new HashSet<ChunkRenderer>());
    private float waterTickTime;

    // Block.SetType may run on generation workers. Only enqueue here; all water
    // simulation, network messages, and mesh requests run on Unity's main thread.
    public void NotifyWaterBlockChanged(Vector3Int position) { waterNotifications.Enqueue(position); }
    private void QueueWaterChunk(ChunkData chunk) { waterChunksToActivate.Enqueue(chunk); }

    private void ResetWater()
    {
        waterSimulation = null;
        lavaSimulation = null;
        fluidTicks=null;fluidRandom=null;ResetFluidRuntime();
        waterTickTime = 0;
        while (waterNotifications.TryDequeue(out _)) { }
        waterChunksToActivate.Clear();
        waterChanges.Clear();
        pendingWaterStates.Clear();
        waterDirtyChunks.Clear();
    }

    private static WaterPosition WaterPos(Vector3Int p) { return new WaterPosition(p.x, p.y, p.z); }
    private static Vector3Int BlockPos(WaterPosition p) { return new Vector3Int(p.x, p.y, p.z); }

    private bool TryGetWaterBlock(Vector3Int p, out Block block)
    {
        block = default(Block);
        if (p.y < 0 || p.y >= worldHeight) return false;
        var origin = WorldDataHelper.GetChunkPosition(this, p);
        if (!worldData.chunkDataDict.TryGetValue(origin, out var chunk) || !chunk.isGenerated ||
            !worldData.chunkDict.ContainsKey(origin)) return false;
        if (waitForBetaPopulation)
            for (int dx=0;dx<=1;dx++) for (int dz=0;dz<=1;dz++)
            {
                // All four +8 population owners that can write this chunk.
                var source=origin+new Vector3Int(dx*chunkSize,0,-dz*chunkSize);
                if (!worldData.chunkDataDict.TryGetValue(source,out var contributor) || !contributor.betaPopulationDone) return false;
            }
        block = chunk.GetBlock(p - chunk.worldPos);
        return block.type != BlockType.Nothing;
    }

    public int GetWaterCell(WaterPosition p)
    {
        if (p.y < 0 || p.y >= worldHeight) return WaterSimulation.Solid;
        if (!TryGetWaterBlock(BlockPos(p), out var block)) return WaterSimulation.Unloaded;
        if (block.type == BlockType.Water) return block.waterLevel;
        if (block.type == BlockType.Lava) return WaterSimulation.Lava;
        return FluidCanReplace(block.type) ? WaterSimulation.Air : WaterSimulation.Solid;
    }

    public void SetWaterCell(WaterPosition p, int level)
    {
        var position = BlockPos(p);
        if (ApplyWaterState(position, level))
            waterChanges[position] = level < 0 ? byte.MaxValue : (byte)level;
    }

    public int GetLavaCell(WaterPosition p)
    {
        if(p.y<0||p.y>=worldHeight)return WaterSimulation.Solid;
        if(!TryGetWaterBlock(BlockPos(p),out var block))return WaterSimulation.Unloaded;
        if(block.type==BlockType.Lava)return block.waterLevel;
        if(block.type==BlockType.Water)return WaterSimulation.Water;
        return FluidCanReplace(block.type)?WaterSimulation.Air:WaterSimulation.Solid;
    }

    public static bool FluidHasSolidMaterial(BlockType type)
    {
        // Reeds explicitly block displacement despite their non-solid material.
        return type != BlockType.SugarCane && type != BlockType.Water && type != BlockType.Lava && !FluidCanReplace(type);
    }

    private static bool FluidCanReplace(BlockType type)
    {
        // Beta's flow blockers are material-based, not the user's collider or
        // opacity flags. Reeds, ladders, signs and doors specifically block it.
        if(BlockShapesExtensions.IsPlant(type))return type!=BlockType.SugarCane;
        switch(type)
        {
            case BlockType.Air: case BlockType.Fire: case BlockType.NetherPortal:
            case BlockType.Torch: case BlockType.RedstoneTorch: case BlockType.RedstoneTorchOff:
            case BlockType.RedstoneWire: case BlockType.Lever: case BlockType.StoneButton:
            case BlockType.Rail: case BlockType.PoweredRail: case BlockType.DetectorRail:
            case BlockType.RedstoneRepeater: case BlockType.PoweredRepeater: case BlockType.SnowLayer:
                return true;
            default:return false;
        }
    }

    public void SetLavaCell(WaterPosition p,int level)
    {
        var type=level==WaterSimulation.Obsidian?BlockType.Obsidian:level==WaterSimulation.Cobblestone?BlockType.Cobblestone:level<0?BlockType.Air:BlockType.Lava;
        byte code=type==BlockType.Obsidian?(byte)64:type==BlockType.Cobblestone?(byte)65:type==BlockType.Air?byte.MaxValue:(byte)(32+(level&15));
        var pos=BlockPos(p);
        if(ApplyFluidState(pos,type,level<0?0:level))waterChanges[pos]=code;
    }

    // Legacy water batch wire format: 0..15 water, 32..47 lava, 64 obsidian,
    // 65 cobblestone, 80..95 fire age, 255 air. Clients never run authoritative ticks.
    private static bool ValidFluidCode(byte code) => code<=15 || (code>=32&&code<=47) || code==64 || code==65 || (code>=80&&code<=95) || code==255;
    private bool ApplyFluidCode(Vector3Int p,byte code)
    {
        if(code<=15)return ApplyFluidState(p,BlockType.Water,code);
        if(code>=32&&code<=47)return ApplyFluidState(p,BlockType.Lava,code-32);
        if(code==64)return ApplyFluidState(p,BlockType.Obsidian,0);
        if(code==65)return ApplyFluidState(p,BlockType.Cobblestone,0);
        if(code>=80&&code<=95)return ApplyFluidState(p,BlockType.Fire,code-80);
        return code==255&&ApplyFluidState(p,BlockType.Air,0);
    }

    // Network clients apply server results without scheduling their own fluid ticks.
    public void ReceiveWaterState(Vector3Int position, byte level)
    {
        if(!ValidFluidCode(level))return;
        if (ApplyFluidCode(position,level)) return;
        if (TryGetWaterBlock(position, out _)) return; // Already matches.
        var origin = WorldDataHelper.GetChunkPosition(this, position);
        if (!pendingWaterStates.TryGetValue(origin, out var states))
            pendingWaterStates[origin] = states = new Dictionary<Vector3Int, byte>();
        states[position] = level;
    }

    private bool ApplyWaterState(Vector3Int position, int level)
    {
        return ApplyFluidState(position,level<0?BlockType.Air:BlockType.Water,level<0?0:level);
    }

    private bool ApplyFluidState(Vector3Int position,BlockType type,int level)
    {
        if (!TryGetWaterBlock(position, out var block)) return false;
        bool liquid=type==BlockType.Water||type==BlockType.Lava||type==BlockType.Fire;
        BlockType previousType=block.type;
        bool washedCrop=WheatCropRules.IsCrop(block.type)&&!WheatCropRules.IsCrop(type);
        byte cropAge=washedCrop?SpecialBlockGeometry.State(block.type,block.blockShape):(byte)0;
        if (block.type == type && (!liquid || block.waterLevel == level)) return false;
        bool hardening=block.type==BlockType.Lava&&(type==BlockType.Obsidian||type==BlockType.Cobblestone);
        var chunk = block.chunkData;
        if (block.type != type)
        {
            WorldDataHelper.SetBlock(this,position,type);
            QueueSurvivalAround(position);
            if(BlockSurvivalRules.IsLog(previousType)&&!BlockSurvivalRules.IsLog(type))QueueLeavesAroundRemovedLog(position);
            QueuePortalValidation(position);
        }
        if(washedCrop&&NetworkServer.active)DropWheat(position,cropAge);
        var changed = chunk.GetBlock(position - chunk.worldPos);
        changed.waterLevel = liquid ? (byte)(level&15) : (byte)0;
        chunk.modifiedAfterSave = true;
        if(hardening)EmitFluidEffect(position,0);
        CollectRenderUpdatesAround(position, waterDirtyChunks);
        return true;
    }

    private void ActivateWaterChunk(ChunkData chunk)
    {
        if (!worldData.chunkDataDict.TryGetValue(chunk.worldPos, out var current) || current != chunk) return;
        if (pendingWaterStates.TryGetValue(chunk.worldPos, out var states))
        {
            foreach (var state in new List<KeyValuePair<Vector3Int,byte>>(states))
                if(TryGetWaterBlock(state.Key,out _)) { ApplyFluidCode(state.Key,state.Value);states.Remove(state.Key); }
            if(states.Count==0)pendingWaterStates.Remove(chunk.worldPos);
        }
        if (!NetworkServer.active) return;
        RegisterFluidRuntimeChunk(chunk);
        foreach (var section in chunk.sections)
            foreach (var block in section.blocks)
            {
                var p = block.globalWorldPosition;
                QueueSnowSupportOnChunkActivation(block);
                if(block.type==BlockType.Fire)fluidTicks.Schedule(WaterPos(p),2,40);
                if (block.type == BlockType.Water || block.type == BlockType.Lava)
                {
                    bool lava=block.type==BlockType.Lava;
                    bool active = block.waterLevel != 0;
                    if (!active)
                    {
                        foreach (var direction in WaterBlockDirections)
                        {
                            int cell=lava?GetLavaCell(WaterPos(p+direction)):GetWaterCell(WaterPos(p+direction));
                            if(cell==WaterSimulation.Air || (lava&&cell==WaterSimulation.Water)) { active = true; break; }
                        }
                    }
                    if(lava&&GetLavaCell(WaterPos(p+Vector3Int.up))==WaterSimulation.Water)active=true;
                    if (active)
                    {
                        if(lava)lavaSimulation.NotifyChanged(WaterPos(p));else waterSimulation.Schedule(WaterPos(p));
                    }
                }
                // Resume water waiting on the other side of a previously absent chunk.
                if (block.position.x == 0) WakeFluids(WaterPos(p + Vector3Int.left));
                if (block.position.x == chunk.chunkSize - 1) WakeFluids(WaterPos(p + Vector3Int.right));
                if (block.position.z == 0) WakeFluids(WaterPos(p + Vector3Int.back));
                if (block.position.z == chunk.chunkSize - 1) WakeFluids(WaterPos(p + Vector3Int.forward));
            }
    }

    private void WakeFluids(WaterPosition p)
    {
        waterSimulation.Schedule(p);
        // Borders contain thousands of nonfluid cells; only an existing lava
        // cell on the formerly unavailable side needs hardening/flow wake-up.
        if(GetLavaCell(p)>=0)lavaSimulation.NotifyChanged(p);
    }

    private static readonly Vector3Int[] WaterBlockDirections =
        { Vector3Int.left, Vector3Int.right, Vector3Int.back, Vector3Int.forward, Vector3Int.down };

    private void UpdateWater()
    {
        if (!Application.isPlaying || !IsWorldCreated || disabled || GenerationBusy || System.Threading.Volatile.Read(ref betaPopulationJobs) > 0) return;
        if (waterSimulation == null)
        {
            // Runtime hesitation has its own RNG; never consume the terrain or
            // population streams whose seed parity has already been verified.
            fluidTicks=new FluidTickScheduler();fluidRandom=new JavaRandom(betaWorldSeed);
            fluidRandomTickState=unchecked((int)betaWorldSeed);
            waterSimulation=new WaterSimulation(this,scheduler:fluidTicks);
            lavaSimulation=new WaterSimulation(new LavaWorld(this),true,dimension == WorldDimension.Nether,()=>fluidRandom.NextInt(4),fluidTicks);
            InitializeFluidRuntime();
            if(fluidRestore!=null)
            {
                fluidTicks.Restore(fluidRestore);fluidRandom.RestoreState(fluidRestore.randomState);
                fluidRandomTickState=fluidRestore.randomTickState;fluidRestore=null;
            }
        }
        for (int i = 0; i < 2 && waterChunksToActivate.Count > 0; i++)
            ActivateWaterChunk(waterChunksToActivate.Dequeue());
        while (waterNotifications.TryDequeue(out var p))
            if (NetworkServer.active) { waterSimulation.NotifyChanged(WaterPos(p));lavaSimulation.NotifyChanged(WaterPos(p));NotifyFireChange(WaterPos(p));ValidateFarmNeighbors(p); }

        if (NetworkServer.active)
        {
            waterTickTime += Time.deltaTime;
            // 20 game ticks/sec; each material has its own scheduled delay.
            int ticks = 0;
            while (waterTickTime >= 0.05f && ticks++ < 5)
            {
                waterTickTime -= 0.05f;
                fluidTicks.Advance();
                RandomFluidTicks();
            }
            // Bound catch-up after a pause while preserving normal tick spacing.
            waterTickTime = Mathf.Min(waterTickTime, 0.25f);
            if (waterChanges.Count > 0 && WorldServer.instance != null)
            {
                var positions = new List<Vector3Int>(256);
                var levels = new List<byte>(256);
                foreach (var change in waterChanges)
                {
                    positions.Add(change.Key);
                    levels.Add(change.Value);
                    if (positions.Count < 256) continue;
                    WorldServer.instance.BroadcastWaterUpdates(dimension,positions.ToArray(), levels.ToArray());
                    positions.Clear(); levels.Clear();
                }
                if (positions.Count > 0) WorldServer.instance.BroadcastWaterUpdates(dimension,positions.ToArray(), levels.ToArray());
                waterChanges.Clear();
            }
        }
        if (waterDirtyChunks.Count > 0) QueueFluidMeshUpdates();
    }

    // Fluids use the same bounded rebuild scheduler as ordinary block edits.
    private void QueueFluidMeshUpdates()
    {
        foreach (var chunk in waterDirtyChunks) AddChunkToUpdate(chunk);
        waterDirtyChunks.Clear();
    }
}

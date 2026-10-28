using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[Serializable]
public sealed class FallingBlockState
{
    public int id;
    public BlockType type;
    public byte blockState;
    public Vector3 position;
    public Vector3 motion;
    public int age;

    public FallingBlockState Copy() => (FallingBlockState)MemberwiseClone();
}

public struct FallingBlockStateMessage : NetworkMessage
{
    public WorldDimension dimension;
    public FallingBlockState block;
}

public struct FallingBlockSyncRequest : NetworkMessage { }

public partial class World
{
    private const float FallingBlockHalfWidth=.49f;
    private const float FallingBlockHeight=.98f;
    [NonSerialized] private Dictionary<int,FallingBlockState> runtimeFallingBlocks;
    [NonSerialized] private Dictionary<int,FallingBlockVisual> runtimeFallingVisuals;
    [NonSerialized] private Dictionary<BlockType,Mesh> runtimeFallingMeshes;
    [NonSerialized] private List<FallingBlockState> runtimeFallingTickBuffer;
    [NonSerialized] private List<int> runtimeRemovedFallingVisuals;
    [NonSerialized] private FallingBlockState[] fallingRestore;
    private Material fallingMaterial;
    private int nextFallingBlockId=1;
    private float fallingBlockClock;
    private bool fallingBlockSyncRequested;

    private Dictionary<int,FallingBlockState> fallingBlocks => System.Threading.LazyInitializer.EnsureInitialized(
        ref runtimeFallingBlocks,()=>new Dictionary<int,FallingBlockState>());
    private Dictionary<int,FallingBlockVisual> fallingVisuals => System.Threading.LazyInitializer.EnsureInitialized(
        ref runtimeFallingVisuals,()=>new Dictionary<int,FallingBlockVisual>());
    private Dictionary<BlockType,Mesh> fallingMeshes => System.Threading.LazyInitializer.EnsureInitialized(
        ref runtimeFallingMeshes,()=>new Dictionary<BlockType,Mesh>());
    private List<FallingBlockState> fallingTickBuffer => System.Threading.LazyInitializer.EnsureInitialized(
        ref runtimeFallingTickBuffer,()=>new List<FallingBlockState>());
    private List<int> removedFallingVisuals => System.Threading.LazyInitializer.EnsureInitialized(
        ref runtimeRemovedFallingVisuals,()=>new List<int>());

    public static bool IsFallingBlock(BlockType type) => type==BlockType.Sand||type==BlockType.Gravel;

    private void InitializeFallingBlocks()
    {
        NetworkClient.RegisterHandler<FallingBlockStateMessage>(message=>
        {
            if(NetworkServer.active)return;
            var world=FindDimension(message.dimension);
            if(world!=null)world.ApplyFallingBlockState(message.block);
        });
    }

    internal bool TryStartFallingBlock(Vector3Int position,BlockType expected,byte state)
    {
        if(!DropAuthority||!IsFallingBlock(expected)||position.y<=0||nextFallingBlockId==int.MaxValue)return false;
        var current=GetBlock(position);
        var below=GetBlock(position+Vector3Int.down);
        if(current==null||current.type!=expected||below==null||!FallingBlockCanPassThrough(below.type))return false;

        EnsureFallingBlocksRestored();
        if(!SetFallingBlockCell(position,BlockType.Air,0))return false;
        if(NetworkServer.active&&WorldServer.instance!=null)
            WorldServer.instance.BroadcastBlock(this,position,BlockType.Air,0);

        var falling=new FallingBlockState{id=nextFallingBlockId++,type=expected,blockState=(byte)(state&15),
            position=(Vector3)position+new Vector3(.5f,0,.5f),motion=Vector3.zero};
        fallingBlocks.Add(falling.id,falling);
        BroadcastFallingBlock(falling);
        return true;
    }

    // Sand and gravel are intentionally not checked here. Like Beta, they begin
    // falling from a scheduled neighbour/block update, not from a full scan when
    // a chunk streams in. Snow still needs one load-time support validation.
    internal void QueueSnowSupportOnChunkActivation(Block block)
    {
        if(!DropAuthority||block==null||block.type!=BlockType.SnowLayer)return;
        var type=block.type;
        var position=block.globalWorldPosition;
        var below=GetBlock(position+Vector3Int.down);
        if(below==null||below.type==BlockType.Nothing)return;
        if(!BlockSurvivalRules.CanSurvive(type,SpecialBlockGeometry.State(type,block.blockShape),position,p=>GetBlock(p).type))
            QueueSurvivalAt(position);
    }

    public FallingBlockState[] CaptureFallingBlocks()
    {
        EnsureFallingBlocksRestored();
        var result=new FallingBlockState[fallingBlocks.Count];int index=0;
        foreach(var block in fallingBlocks.Values)result[index++]=block.Copy();
        return result;
    }

    public void RestoreFallingBlocks(FallingBlockState[] saved)
    {
        ResetFallingBlocks();
        fallingRestore=DropAuthority?saved:null;
    }

    private void EnsureFallingBlocksRestored()
    {
        if(!DropAuthority||fallingRestore==null)return;
        foreach(var saved in fallingRestore)
        {
            if(!ValidFallingBlock(saved)||saved.id==int.MaxValue||fallingBlocks.ContainsKey(saved.id))continue;
            fallingBlocks.Add(saved.id,saved.Copy());nextFallingBlockId=Math.Max(nextFallingBlockId,saved.id+1);
        }
        fallingRestore=null;
        foreach(var block in fallingBlocks.Values)BroadcastFallingBlock(block);
    }

    private void ApplyFallingBlockState(FallingBlockState state)
    {
        if(state==null)return;
        if(!ValidFallingBlock(state)){fallingBlocks.Remove(state.id);return;}
        fallingBlocks[state.id]=state.Copy();
    }

    private void BroadcastFallingBlock(FallingBlockState state)
    {
        if(NetworkServer.active)NetworkServer.SendToAll(new FallingBlockStateMessage{dimension=dimension,block=state.Copy()});
    }

    private void BroadcastFallingBlockRemoval(int id)
    {
        if(NetworkServer.active)NetworkServer.SendToAll(new FallingBlockStateMessage{dimension=dimension,block=new FallingBlockState{id=id}});
    }

    private void UpdateFallingBlocks()
    {
        if(!Application.isPlaying||disabled||!IsWorldCreated||GenerationBusy)return;
        EnsureFallingBlocksRestored();
        if(!DropAuthority&&!fallingBlockSyncRequested&&NetworkClient.ready)
        {fallingBlockSyncRequested=true;NetworkClient.Send(new FallingBlockSyncRequest());}
        if(DropAuthority&&!isSaving&&fallingBlocks.Count>0)
        {
            fallingBlockClock+=Time.deltaTime;
            int ticks=0;
            while(fallingBlockClock+.000001f>=.05f&&ticks++<5)
            {
                fallingBlockClock=Mathf.Max(0,fallingBlockClock-.05f);
                fallingTickBuffer.Clear();fallingTickBuffer.AddRange(fallingBlocks.Values);
                foreach(var state in fallingTickBuffer)TickFallingBlock(state);
            }
            fallingBlockClock=Mathf.Min(fallingBlockClock,.25f);
        }
        else fallingBlockClock=0;
        if(Instance==this&&(!NetworkServer.active||NetworkClient.active))UpdateFallingBlockVisuals();
    }

    private void TickFallingBlock(FallingBlockState state)
    {
        if(!ValidFallingBlock(state)){RemoveFallingBlock(state==null?0:state.id);return;}
        var cell=Vector3Int.FloorToInt(state.position);
        var loaded=GetBlock(new Vector3Int(cell.x,Mathf.Clamp(cell.y,0,worldHeight-1),cell.z));
        if(loaded==null||loaded.type==BlockType.Nothing)return;

        state.motion.y=(state.motion.y-.04f)*.98f;
        state.motion.x*=.98f;state.motion.z*=.98f;
        var requested=state.motion;
        var moved=BlockCollision.Resolve(this,state.position,FallingBlockHalfWidth,FallingBlockHeight,requested,false);
        state.position+=moved;state.age++;
        bool grounded=requested.y<0&&moved.y>requested.y+.00001f;
        if(grounded){state.motion.y=0;LandFallingBlock(state);return;}
        if(state.position.y<-64||state.age>600){DropFallingBlock(state);return;}
        // Five snapshots per second are enough for the interpolated visual and
        // halve the traffic/copies used by a large collapsing sand column.
        if(state.age%4==0)BroadcastFallingBlock(state);
    }

    private void LandFallingBlock(FallingBlockState state)
    {
        var target=Vector3Int.FloorToInt(state.position+Vector3.up*.001f);
        var targetBlock=GetBlock(target);
        var below=GetBlock(target+Vector3Int.down);
        bool canSet=target.y>=0&&target.y<worldHeight&&targetBlock!=null&&below!=null&&
            FallingBlockCanPassThrough(targetBlock.type)&&below.type!=BlockType.Nothing&&!FallingBlockCanPassThrough(below.type);
        RemoveFallingBlock(state.id);
        if(!canSet){SpawnFallingBlockDrop(state);return;}
        if(!SetFallingBlockCell(target,state.type,state.blockState))
        {SpawnFallingBlockDrop(state);return;}
        if(NetworkServer.active&&WorldServer.instance!=null)
            WorldServer.instance.BroadcastBlock(this,target,state.type,state.blockState);
        GameAudio.BroadcastBlock(state.type,target,false,this);
    }

    // Falling columns used to call World.SetBlock for every start and landing.
    // That async method immediately builds a mesh, so a single column could
    // launch many redundant jobs for the same chunk. Mutate the cell now and
    // feed the affected renderers into the world's deduplicated update queue.
    private bool SetFallingBlockCell(Vector3Int position,BlockType type,byte state)
    {
        if(position.y<0||position.y>=worldHeight)return false;
        var origin=WorldDataHelper.GetChunkPosition(this,position);
        var renderer=WorldDataHelper.GetChunk(this,origin);
        if(renderer==null||renderer.ChunkData==null)return false;
        WorldDataHelper.SetBlock(this,position,type);
        var changed=GetBlock(position);
        if(changed==null||changed.type!=type)return false;
        changed.blockShape=SpecialBlockGeometry.Shape(type,state);
        renderer.ModifiedByPlayer=true;renderer.ChunkData.modifiedAfterSave=true;
        QueueSurvivalAround(position);QueuePortalValidation(position);
        AddChunkToUpdate(renderer);
        if(renderer.ChunkData.IsOnEdge(position))
            foreach(var neighbor in renderer.ChunkData.GetNeighbourChunk(position))
            {
                if(neighbor==null)continue;
                var neighborRenderer=WorldDataHelper.GetChunk(this,neighbor.worldPos);
                if(neighborRenderer!=null)AddChunkToUpdate(neighborRenderer);
            }
        return true;
    }

    private void DropFallingBlock(FallingBlockState state)
    {RemoveFallingBlock(state.id);SpawnFallingBlockDrop(state);}

    private void SpawnFallingBlockDrop(FallingBlockState state)
    {
        var item=ItemCatalog.FromBlock(state.type);
        if(item!=ItemType.Nothing)SpawnItemDrop(new InventoryStack(item,1),state.position,Vector3.zero,10);
    }

    private void RemoveFallingBlock(int id)
    {
        if(id<=0)return;
        if(fallingBlocks.Remove(id))BroadcastFallingBlockRemoval(id);
    }

    private void UpdateFallingBlockVisuals()
    {
        removedFallingVisuals.Clear();
        foreach(var pair in fallingVisuals)if(!fallingBlocks.ContainsKey(pair.Key))removedFallingVisuals.Add(pair.Key);
        foreach(var id in removedFallingVisuals){fallingVisuals[id].Dispose();fallingVisuals.Remove(id);}
        foreach(var state in fallingBlocks.Values)
        {
            var cell=GetBlock(Vector3Int.FloorToInt(state.position));
            bool visible=cell!=null&&cell.type!=BlockType.Nothing;
            if(!fallingVisuals.TryGetValue(state.id,out var visual)&&visible)
            {
                if(!fallingMeshes.TryGetValue(state.type,out var mesh))fallingMeshes[state.type]=mesh=ItemVisualMesh.Build(state.type);
                if(fallingMaterial==null)fallingMaterial=Resources.Load<Material>("DroppedBlockMaterial");
                if(fallingMaterial==null)continue;
                visual=new FallingBlockVisual(worldRenderer.transform,state.type,mesh,fallingMaterial);fallingVisuals.Add(state.id,visual);
            }
            if(visual!=null){visual.root.SetActive(visible);if(visible)visual.Update(state,this);}
        }
    }

    private void ResetFallingBlocks(bool discardSave=false)
    {
        foreach(var visual in fallingVisuals.Values)visual.Dispose();fallingVisuals.Clear();
        foreach(var mesh in fallingMeshes.Values)if(mesh!=null)Destroy(mesh);fallingMeshes.Clear();
        fallingBlocks.Clear();fallingTickBuffer.Clear();removedFallingVisuals.Clear();
        fallingMaterial=null;fallingBlockClock=0;fallingBlockSyncRequested=false;nextFallingBlockId=1;
        if(discardSave)fallingRestore=null;
    }

    private static bool FallingBlockCanPassThrough(BlockType type)
    {return type==BlockType.Air||type==BlockType.Fire||type==BlockType.Water||type==BlockType.Lava;}

    private static bool ValidFallingBlock(FallingBlockState state)
    {
        if(state==null||state.id<=0||!IsFallingBlock(state.type)||state.age<0)return false;
        return DroppedItemRules.Finite(state.position)&&DroppedItemRules.Finite(state.motion);
    }
}

public sealed class FallingBlockVisual
{
    public readonly GameObject root;
    private readonly MeshRenderer renderer;
    private readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
    private bool placed;

    public FallingBlockVisual(Transform parent,BlockType type,Mesh mesh,Material material)
    {
        root=new GameObject("Falling "+type);root.transform.SetParent(parent,false);
        var model=new GameObject("Block",typeof(MeshFilter),typeof(MeshRenderer));model.transform.SetParent(root.transform,false);
        model.transform.localPosition=new Vector3(-.5f,0,-.5f);
        model.GetComponent<MeshFilter>().sharedMesh=mesh;
        renderer=model.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    public void Update(FallingBlockState state,World world)
    {
        root.transform.position=placed?Vector3.Lerp(root.transform.position,state.position,Mathf.Clamp01(Time.deltaTime*20)):state.position;
        placed=true;
        var light=world.GetBlock(Vector3Int.FloorToInt(state.position+Vector3.up*.5f));
        properties.SetVector("_LightLevels",new Vector4(light==null?15:light.GetSkyLight(),light==null?0:light.GetBlockLight(),1,0));
        renderer.SetPropertyBlock(properties);
    }

    public void Dispose(){UnityEngine.Object.Destroy(root);}
}

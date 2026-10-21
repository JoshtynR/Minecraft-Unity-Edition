using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;

public partial class Player
{
    [SyncVar(hook=nameof(OnDimensionChanged))] public WorldDimension dimension;
    [SyncVar] private bool dimensionTransfer;
    public World CurrentWorld => World.FindDimension(dimension) ?? world ?? World.Instance;
    public bool DimensionTransferInProgress => dimensionTransfer;
    private float portalTime,portalCooldown;
    private int transferSequence;
    private bool initialDimensionJoin;
    private System.Diagnostics.Stopwatch joinStopwatch;
    private TaskCompletionSource<WorldServer.SavePlayerMessage> dimensionCheckpoint;
    private Vector3 transferDestination;
    private readonly Dictionary<Vector3Int,string> incomingDimensionChunks=new Dictionary<Vector3Int,string>();
    private Renderer[] dimensionRenderers;
    private Collider[] dimensionColliders;
    private readonly Dictionary<NetworkTransformBase,bool> pausedTransforms=new Dictionary<NetworkTransformBase,bool>();
    private readonly Dictionary<Renderer,bool> hiddenRenderers=new Dictionary<Renderer,bool>();
    private readonly Dictionary<Canvas,bool> hiddenCanvases=new Dictionary<Canvas,bool>();
    private readonly Dictionary<Collider,bool> hiddenColliders=new Dictionary<Collider,bool>();
    private bool otherDimensionHidden,arrivalSoundPending;
    public float PortalOverlay => Mathf.Clamp01(portalTime / 4);

    private void OnDimensionChanged(WorldDimension previous,WorldDimension next)
    {
        world=World.GetDimension(next);
        if(isLocalPlayer||!networkStarted)World.ActivateDimension(world);
    }
    private void TickDimension()
    {
        if(CurrentWorld!=null)world=CurrentWorld;
        RefreshDimensionVisibility();
        if(arrivalSoundPending&&!WorldLoadingScreen.IsLoading&&(isLocalPlayer||!networkStarted))
        {arrivalSoundPending=false;GameAudio.Play("portal/travel",transform.position,.5f);}
        if(dimensionTransfer){PauseDimensionTransforms();return;}
        ResumeDimensionTransforms();
        if(world==null||!world.IsWorldCreated||health<=0)return;
        bool inside=PortalRules.IsPortal(p=>world.GetBlock(p).type,transform.position);
        if(portalCooldown>0)
        {
            portalCooldown=inside?15:Mathf.Max(0,portalCooldown-Time.fixedDeltaTime);
            portalTime=0;return;
        }
        if(!inside){portalTime=Mathf.Max(0,portalTime-Time.fixedDeltaTime*4);return;}
        if(portalTime==0&&(isLocalPlayer||!networkStarted))GameAudio.Play("portal/trigger",transform.position,.3f);
        portalTime+=Time.fixedDeltaTime;
        if(portalTime>=4&&(!networkStarted||isServer))BeginDimensionTravel(dimension==WorldDimension.Nether?WorldDimension.Overworld:WorldDimension.Nether,
            DimensionRules.Destination(transform.position,dimension==WorldDimension.Nether?WorldDimension.Overworld:WorldDimension.Nether),true);
    }
    private void RefreshDimensionVisibility()
    {
        if(isLocalPlayer||!networkStarted)return;
        bool hide=dimensionTransfer||(World.Instance!=null&&World.Instance.dimension!=dimension);
        if(hide==otherDimensionHidden)return;
        otherDimensionHidden=hide;
        if(hide)
        {
            dimensionRenderers=GetComponentsInChildren<Renderer>(true);
            dimensionColliders=GetComponentsInChildren<Collider>(true);
            foreach(var canvas in GetComponentsInChildren<Canvas>(true)){hiddenCanvases[canvas]=canvas.enabled;canvas.enabled=false;}
            foreach(var renderer in dimensionRenderers){hiddenRenderers[renderer]=renderer.enabled;renderer.enabled=false;}
            foreach(var collider in dimensionColliders){hiddenColliders[collider]=collider.enabled;collider.enabled=false;}
        }
        else
        {
            foreach(var pair in hiddenRenderers)if(pair.Key!=null)pair.Key.enabled=pair.Value;
            foreach(var pair in hiddenColliders)if(pair.Key!=null)pair.Key.enabled=pair.Value;
            foreach(var pair in hiddenCanvases)if(pair.Key!=null)pair.Key.enabled=pair.Value;
            hiddenRenderers.Clear();hiddenColliders.Clear();hiddenCanvases.Clear();
        }
    }
    public void PrepareDimensionJoin(){dimensionTransfer=true;}
    public async void SynchronizeDimensionOnJoin()
    {
        try
        {
            while(this!=null&&inventory==null)await Task.Yield();
            if(this==null)return;
            // Startup terrain was prepared before StartWorldMessage. Joining is
            // not a portal departure: there is no source inventory/world to save.
            initialDimensionJoin=true;
            joinStopwatch=System.Diagnostics.Stopwatch.StartNew();
            dimensionTransfer=true;PauseDimensionTransforms();
            transferDestination=transform.position;
            int sequence=++transferSequence;
            TargetPrepareDimension(connectionToClient,sequence,dimension,transferDestination,CurrentWorld.betaWorldSeed);
        }
        catch(Exception error){Debug.LogException(error);}
    }
    private async void BeginDimensionTravel(WorldDimension target,Vector3 desired,bool createPortal)
    {
        if(dimensionTransfer)return;
        dimensionTransfer=true;PauseDimensionTransforms();serverMining=localMining=false;portalTime=0;
        int sequence=++transferSequence;
        var source=CurrentWorld;WorldDimension previous=dimension;Vector3 previousPosition=transform.position;
        if(!networkStarted||isLocalPlayer)WorldLoadingScreen.Begin();

        try
        {
            if(connectionToClient!=null&&WorldServer.instance!=null)
            {WorldServer.instance.CloseContainersForDimensionTravel(connectionToClient);}
            if(networkStarted&&connectionToClient!=null)
            {
                dimensionCheckpoint=new TaskCompletionSource<WorldServer.SavePlayerMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                TargetDimensionLoading(connectionToClient,sequence);
                var state=await dimensionCheckpoint.Task;
                dimensionCheckpoint=null;
                if(this==null)return;
                inventory.LoadInventory(state.inventory,state.crafting,state.cursor,state.recovery,state.equipment);
            }
            while(source!=null&&source.isSaving)await Task.Yield();
            if(source!=null)
            {
                var checkpoint=SavePlayer();
                await source.SaveDimensionAsync();
                if(source!=null)source.SavePlayer(checkpoint);
            }
            if(this==null)return;
            var destination=World.GetDimension(target);
            transferDestination=createPortal?await destination.FindOrCreateExitAsync(desired):desired;
            if(health<=0&&target==WorldDimension.Overworld)
            {
                await destination.EnsureAreaAsync(new Vector3Int(8,0,8),true);
                bool safe=false;
                for(int y=destination.worldHeight-3;y>0;y--)
                {
                    var floor=destination.GetBlock(new Vector3Int(8,y,8)).type;
                    if(!SpecialBlockGeometry.NormalCube(floor)||destination.GetBlock(new Vector3Int(8,y+1,8)).type!=BlockType.Air||destination.GetBlock(new Vector3Int(8,y+2,8)).type!=BlockType.Air)continue;
                    transferDestination=new Vector3(8.5f,y+1,8.5f);safe=true;break;
                }
                if(!safe)throw new InvalidOperationException("No safe Overworld respawn found");
            }
            await destination.EnsureAreaAsync(Vector3Int.FloorToInt(transferDestination),true);
            if(this==null)return;
            dimension=target;world=destination;
            // Set the server position before snapshots or future stream requests.
            transform.position=transferDestination;
            if(NetworkServer.active){var sync=GetComponent<NetworkTransform>();if(sync!=null)sync.RpcTeleport(transferDestination);}
            if(!networkStarted)
            {
                World.ActivateDimension(destination);FinishDimensionTravel(transferDestination);
                await destination.SaveDimensionAsync();return;
            }
            TargetPrepareDimension(connectionToClient,sequence,target,transferDestination,destination.betaWorldSeed);
        }
        catch(OperationCanceledException) { }
        catch(Exception error)
        {
            Debug.LogException(error);
            if(this==null)return;
            dimension=previous;world=source;transform.position=previousPosition;dimensionTransfer=false;ResumeDimensionTransforms();portalCooldown=15;
            if(!networkStarted||isLocalPlayer){World.ActivateDimension(source);WorldLoadingScreen.Cancel();}
            if(connectionToClient!=null)TargetCancelDimension(connectionToClient,previous,previousPosition);
        }
    }
    [TargetRpc] private async void TargetDimensionLoading(NetworkConnection connection,int sequence)
    {
        dimensionTransfer=true;PauseDimensionTransforms();WorldLoadingScreen.Begin();
        while(this!=null&&(inventory==null||loadingSavedPlayer))await Task.Yield();
        if(this==null)return;
        if(inventory.isOpen)inventory.CloseInventory();
        CmdDimensionCheckpoint(sequence,SavePlayer(),knownItemWearRevision);
    }
    [Command] private void CmdDimensionCheckpoint(int sequence,WorldServer.SavePlayerMessage state,int revision)
    {
        if(!dimensionTransfer||sequence!=transferSequence||dimensionCheckpoint==null)return;
        if(!AcceptsItemRevision(revision)||!ValidDimensionInventory(state))
        {
            dimensionCheckpoint.TrySetException(new InvalidOperationException("Invalid dimension inventory checkpoint"));
            if(connectionToClient!=null)connectionToClient.Disconnect();return;
        }
        dimensionCheckpoint.TrySetResult(state);
    }
    private void CancelDimensionTransfer()
    {dimensionCheckpoint?.TrySetCanceled();dimensionCheckpoint=null;}
    [TargetRpc] private async void TargetPrepareDimension(NetworkConnection connection,int sequence,WorldDimension target,Vector3 destination,long seed)
    {
        dimensionTransfer=true;WorldLoadingScreen.Begin();incomingDimensionChunks.Clear();
        try
        {
            transferSequence=sequence;dimension=target;
            var realm=World.GetDimension(target);realm.betaWorldSeed=seed;world=realm;
            World.ActivateDimension(realm);
            // A host shares the authoritative World; preparing it again only
            // repeats lighting/generation bookkeeping. Remote clients still wait.
            if(!NetworkServer.active)await realm.EnsureAreaAsync(Vector3Int.FloorToInt(destination),true);
            if(this==null)return;
            CmdDimensionTerrainReady(sequence);
        }
        catch(Exception error)
        {Debug.LogException(error);if(this!=null)CmdDimensionPreparationFailed(sequence);}
    }
    [Command] private void CmdDimensionTerrainReady(int sequence)
    {
        if(!dimensionTransfer||sequence!=transferSequence)return;
        var realm=CurrentWorld;
        // Reliable ordered messages carry authoritative saved edits, fluid states and
        // portal blocks. Keep each compressed chunk separate from the completion RPC.
        // Local clients use the very same chunk objects and deliberately ignore
        // TargetDimensionChunk. Never serialize/compress their discarded copies.
        if(!(connectionToClient is LocalConnectionToClient))
        {
            foreach(var chunk in realm.worldData.chunkDataDict.Values.ToArray())
            {
                if(Math.Abs(chunk.worldPos.x-transferDestination.x)>(realm.renderDistance+3)*16||
                   Math.Abs(chunk.worldPos.z-transferDestination.z)>(realm.renderDistance+3)*16)continue;
                TargetDimensionChunk(connectionToClient,sequence,chunk.worldPos,World.Compress(JsonUtility.ToJson(ChunkSaveData.Serialize(chunk))));
            }
            realm.SendCowSnapshot(connectionToClient);
            foreach(var item in realm.CaptureDroppedItems())connectionToClient.Send(new DropStateMessage{dimension=dimension,item=item});
        }
        TargetCompleteDimension(connectionToClient,sequence,transferDestination);
    }
    [TargetRpc] private void TargetDimensionChunk(NetworkConnection connection,int sequence,Vector3Int position,string json)
    {if(sequence==transferSequence&&!NetworkServer.active)incomingDimensionChunks[position]=json;}
    [TargetRpc] private async void TargetCompleteDimension(NetworkConnection connection,int sequence,Vector3 destination)
    {
        if(sequence!=transferSequence)return;
        try
        {
            if(!NetworkServer.active)await CurrentWorld.ApplyDimensionSnapshotsAsync(incomingDimensionChunks);
            incomingDimensionChunks.Clear();
            if(this==null)return;
            while(this!=null&&(inventory==null||loadingSavedPlayer))await Task.Yield();
            if(this==null)return;
            FinishDimensionTravel(destination);
            CmdDimensionEntered(sequence,SavePlayer(),knownItemWearRevision);
        }
        catch(Exception error){Debug.LogException(error);if(this!=null)CmdDimensionPreparationFailed(sequence);}
    }
    [Command] private void CmdDimensionEntered(int sequence,WorldServer.SavePlayerMessage state,int revision)
    {
        if(sequence!=transferSequence)return;
        if(!AcceptsItemRevision(revision)||!ValidDimensionInventory(state))
        {if(connectionToClient!=null)connectionToClient.Disconnect();return;}
        // Inventory/crafting are client-driven in the existing project. Reconcile
        // their bounded snapshot before saving an arrival; position/realm/Steam ID
        // are always taken from the authoritative player, never from this message.
        inventory.LoadInventory(state.inventory,state.crafting,state.cursor,state.recovery,state.equipment);
        FinishDimensionTravel(transferDestination);
        CurrentWorld.SavePlayer(SavePlayer());
        if(initialDimensionJoin)
        {
            initialDimensionJoin=false;
            joinStopwatch?.Stop();
            Debug.Log("[WorldStartup] Player ready in "+(joinStopwatch?.ElapsedMilliseconds??0)+"ms; reused prepared terrain without a departure save.");
        }
        else CurrentWorld.SaveWorld();
    }
    private static bool ValidDimensionInventory(WorldServer.SavePlayerMessage state)
    {
        if(state.inventory==null||state.inventory.Length!=36)return false;
        if(state.crafting!=null&&state.crafting.Length!=4)return false;
        if(state.equipment!=null&&state.equipment.Length!=4)return false;
        if(state.recovery!=null&&state.recovery.Length>4096)return false;
        foreach(var slots in new[]{state.inventory,state.crafting,state.equipment,state.recovery})
        {
            if(slots==null)continue;
            foreach(var slot in slots)if(!ValidDimensionSlot(slot,slots==state.recovery))return false;
        }
        return ValidDimensionSlot(state.cursor,false);
    }
    private static bool ValidDimensionSlot(PlayerInventory.Slot slot,bool recovery)
    {
        if(slot==null)return true;
        if(slot.count<0||slot.damage<0)return false;
        if(slot.count==0)return true;
        return ItemCatalog.Valid(slot.type)&&slot.count<=(recovery?1000000:ItemCatalog.StackLimit(slot.type));
    }
    [Command] private void CmdDimensionPreparationFailed(int sequence)
    {
        if(sequence!=transferSequence)return;
        // Failed terrain preparation disconnects cleanly; a partial dimension must
        // never unfreeze a player into missing collision. Saved position is retained.
        if(connectionToClient!=null)connectionToClient.Disconnect();
    }
    [TargetRpc] private void TargetCancelDimension(NetworkConnection connection,WorldDimension target,Vector3 position)
    {
        dimension=target;world=World.GetDimension(target);World.ActivateDimension(world);
        FinishDimensionTravel(position);WorldLoadingScreen.Cancel();
    }
    private void PauseDimensionTransforms()
    {
        foreach(var sync in GetComponentsInChildren<NetworkTransformBase>(true))
        {
            if(pausedTransforms.ContainsKey(sync))continue;
            pausedTransforms.Add(sync,sync.enabled);sync.enabled=false;
        }
    }
    private void ResumeDimensionTransforms()
    {
        if(pausedTransforms.Count==0)return;
        foreach(var pair in pausedTransforms)if(pair.Key!=null){pair.Key.Reset();pair.Key.enabled=pair.Value;}
        pausedTransforms.Clear();
    }
    private void FinishDimensionTravel(Vector3 destination)
    {
        transform.position=destination;dimensionTransfer=false;ResumeDimensionTransforms();portalCooldown=15;portalTime=0;
        if(health<=0)ApplyFluidRespawn(destination);
        velocity=extraForce=Vector3.zero;verticalMomentum=0;jumpRequest=false;
        hasFluidSafePosition=false;ResetFluidMovement();
        survivalPositionKnown=false;localWalkDistance=localSprintDistance=0;
        if(isLocalPlayer||!networkStarted)arrivalSoundPending=true;
        if(isLocalPlayer||!networkStarted)
        {
            World.ActivateDimension(CurrentWorld);
            if(GameManager.Instance!=null)GameManager.Instance.StartCheckingForChunks();
        }
    }
}

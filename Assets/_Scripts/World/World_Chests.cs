using System.Collections.Generic;
using Mirror;
using UnityEngine;

public partial class World
{
    [System.NonSerialized] private ChestStorage runtimeChests;
    public ChestStorage chests => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeChests, () => new ChestStorage());
    [System.NonSerialized] private HashSet<Vector3Int> runtimeOpenChestLids;
    public HashSet<Vector3Int> openChestLids => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeOpenChestLids, () => new HashSet<Vector3Int>());
    private void InitializeChests()
    {
        ModernChestVisual.Prepare();
        chests.LidChanged+=(position,opened)=>
        {
            SetChestLid(position,opened);
            if(NetworkServer.active&&WorldServer.instance!=null)WorldServer.instance.BroadcastChestLid(dimension,position,opened);
        };
    }
    public void SetChestLid(Vector3Int position,bool opened)
    {
        bool changed=opened?openChestLids.Add(position):openChestLids.Remove(position);
        var partner=ChestPartner(position);
        bool soundOwner=!partner.HasValue||position.x<partner.Value.x||position.x==partner.Value.x&&position.z<partner.Value.z;
        if(changed&&soundOwner&&Instance==this)GameAudio.Play(opened?"random/chestopen":"random/chestclosed",(Vector3)position+Vector3.one*.5f,.5f);
    }
    public bool CanReplaceChest(Vector3Int position,BlockType next)
    {
        if(NetworkClient.active&&!NetworkServer.active)return true;
        var block=GetBlock(position);
        if(block!=null&&IsFurnace(block.type)&&!IsFurnace(next))return furnaces.CanRemove(StorageName,position);
        return block==null||block.type!=BlockType.Chest||next==BlockType.Chest||chests.CanRemove(StorageName,position);
    }
    public bool ChestAccessible(Vector3 eye,Vector3Int position,float reach)
    {
        var block=GetBlock(position);if(block==null||block.type!=BlockType.Chest)return false;
        if((eye-((Vector3)position+Vector3.one*.5f)).sqrMagnitude>(reach+1)*(reach+1))return false;
        var above=GetBlock(position+Vector3Int.up);
        if(above!=null&&SpecialBlockGeometry.NormalCube(above.type))return false;
        var partner=ChestPartner(position);
        if(partner.HasValue&&SpecialBlockGeometry.NormalCube(GetBlock(partner.Value+Vector3Int.up).type))return false;
        Vector3 end=(Vector3)position+Vector3.one*.5f;
        int steps=Mathf.Max(1,Mathf.CeilToInt((end-eye).magnitude/.1f));
        for(int i=1;i<steps;i++)
        {
            var p=Vector3Int.FloorToInt(Vector3.Lerp(eye,end,(float)i/steps));if(p==position||partner.HasValue&&p==partner.Value)return true;
            var other=GetBlock(p);
            if(other==null||other.type==BlockType.Nothing||SpecialBlockGeometry.NormalCube(other.type))return false;
        }
        return true;
    }
    private static readonly Vector3Int[] ChestNeighbors={Vector3Int.left,Vector3Int.right,Vector3Int.forward,Vector3Int.back};
    public Vector3Int? ChestPartner(Vector3Int position)
    {
        Vector3Int? result=null;
        foreach(var delta in ChestNeighbors)
            if(GetBlock(position+delta).type==BlockType.Chest)
            {if(result.HasValue)return null;result=position+delta;}
        if(!result.HasValue)return null;
        foreach(var delta in ChestNeighbors)
            if(result.Value+delta!=position&&GetBlock(result.Value+delta).type==BlockType.Chest)return null;
        return result;
    }
    public bool CanPlaceChest(Vector3Int position)
    {
        int neighbors=0;
        foreach(var delta in ChestNeighbors)
        {
            var p=position+delta;if(GetBlock(p).type!=BlockType.Chest)continue;
            if(++neighbors>1||ChestPartner(p).HasValue)return false;
            foreach(int owner in chests.Owners())if(chests.Position(owner)==p)return false;
        }
        return true;
    }
    public byte ChestRenderFacing(Vector3Int position,byte facing)
    {
        var pair=ChestPartner(position);if(!pair.HasValue)return facing;
        var first=pair.Value.x<position.x||pair.Value.x==position.x&&pair.Value.z<position.z?pair.Value:position;
        byte preferred=SpecialBlockGeometry.State(BlockType.Chest,GetBlock(first).blockShape);
        return pair.Value.x!=position.x?(preferred==2||preferred==3?preferred:(byte)2):(preferred==4||preferred==5?preferred:(byte)4);
    }
}

public partial class WorldServer
{
    private float chestCheckTime;
    public void ReceiveChestRequest(NetworkConnectionToClient connection,ChestRequest request)
    {
        if(connection.identity==null||World.Instance==null)return;
        var player=connection.identity.GetComponent<Player>();if(player==null||player.DimensionTransferInProgress)return;
        var world=player.CurrentWorld;
        if(request.action==0&&world.furnaces.HasOwner(connection.connectionId))
        {connection.Send(new ChestReply{error="Close the furnace first."});return;}
        if(request.action==0&&!player.AcceptsItemRevision(request.itemWearRevision))
        {connection.Send(new ChestReply{error="Inventory update pending; open the chest again."});return;}
        if(world.isSaving&&request.action!=2)
        {connection.Send(new ChestReply{error="World is saving; try the chest again in a moment."});return;}
        bool accessible=player.health>0&&world.ChestAccessible(player.transform.position+Vector3.up*1.6f,request.position,player.reach);
        var reply=world.chests.Process(world.StorageName,connection.connectionId,request,accessible,world.ChestPartner(request.position));
        if(reply.accepted&&!reply.dropped.Empty)player.SpawnMenuDrop(reply.dropped);
        if(reply.accepted)player.GetComponent<PlayerInventory>().ApplyChestPlayerSnapshot(reply.inventory,reply.cursor);
        connection.Send(reply);
    }
    public void CloseChestForDisconnect(NetworkConnectionToClient connection)
    {
        foreach(var world in World.LoadedDimensions)
        {
            var reply=world.chests.Close(connection.connectionId);if(!reply.accepted)continue;
            if(connection.identity!=null)connection.identity.GetComponent<PlayerInventory>().ApplyChestPlayerSnapshot(reply.inventory,reply.cursor);
            world.SaveWorld();
        }
    }
    public void BroadcastChestLid(WorldDimension dimension,Vector3Int position,bool opened){RpcChestLid(dimension,position,opened);}
    [ClientRpc] private void RpcChestLid(WorldDimension dimension,Vector3Int position,bool opened)
    {var world=World.FindDimension(dimension);if(world!=null)world.SetChestLid(position,opened);}
    private void Update()
    {
        if(!isServer||World.Instance==null||Time.unscaledTime<chestCheckTime)return;
        chestCheckTime=Time.unscaledTime+.25f;
        foreach(var world in World.LoadedDimensions)
        {
        CheckFurnaceSessions(world);
        foreach(int owner in world.chests.Owners())
        {
            // Local-host sessions are owned and range-checked by the local UI,
            // not by a Mirror connection with ID -1.
            if(owner==-1)continue;
            NetworkConnectionToClient connection;
            bool connected=NetworkServer.connections.TryGetValue(owner,out connection)&&connection!=null&&connection.identity!=null;
            var player=connected?connection.identity.GetComponent<Player>():null;
            bool valid=player!=null&&player.dimension==world.dimension&&!player.DimensionTransferInProgress&&player.health>0&&world.ChestAccessible(player.transform.position+Vector3.up*1.6f,
                world.chests.Position(owner),player.reach);
            if(valid)continue;
            var reply=world.chests.Close(owner);
            if(player!=null)player.GetComponent<PlayerInventory>().ApplyChestPlayerSnapshot(reply.inventory,reply.cursor);
            if(connected)connection.Send(reply);
            if(!connected)world.SaveWorld();
        }
        }
    }
}

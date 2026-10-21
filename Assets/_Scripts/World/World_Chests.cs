using System.Collections.Generic;
using Mirror;
using UnityEngine;

public partial class World
{
    public readonly ChestStorage chests=new ChestStorage();
    public readonly HashSet<Vector3Int> openChestLids=new HashSet<Vector3Int>();
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
        if(changed&&Instance==this)GameAudio.Play(opened?"random/chestopen":"random/chestclosed",(Vector3)position+Vector3.one*.5f,.5f);
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
        Vector3 end=(Vector3)position+Vector3.one*.5f;
        int steps=Mathf.Max(1,Mathf.CeilToInt((end-eye).magnitude/.1f));
        for(int i=1;i<steps;i++)
        {
            var p=Vector3Int.FloorToInt(Vector3.Lerp(eye,end,(float)i/steps));if(p==position)return true;
            var other=GetBlock(p);
            if(other==null||other.type==BlockType.Nothing||SpecialBlockGeometry.NormalCube(other.type))return false;
        }
        return true;
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
        var reply=world.chests.Process(world.StorageName,connection.connectionId,request,accessible);
        if(reply.accepted)player.GetComponent<PlayerInventory>().ApplyChestPlayerSnapshot(reply.inventory,reply.cursor);
        connection.Send(reply);
        if(reply.accepted&&!reply.opened)world.SaveWorld();
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
            NetworkConnectionToClient connection;
            bool connected=NetworkServer.connections.TryGetValue(owner,out connection)&&connection!=null&&connection.identity!=null;
            var player=connected?connection.identity.GetComponent<Player>():null;
            bool valid=player!=null&&player.dimension==world.dimension&&!player.DimensionTransferInProgress&&player.health>0&&world.ChestAccessible(player.transform.position+Vector3.up*1.6f,
                world.chests.Position(owner),player.reach);
            if(valid)continue;
            var reply=world.chests.Close(owner);
            if(player!=null)player.GetComponent<PlayerInventory>().ApplyChestPlayerSnapshot(reply.inventory,reply.cursor);
            if(connected)connection.Send(reply);
            world.SaveWorld();
        }
        }
    }
}

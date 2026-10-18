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
            if(NetworkServer.active&&WorldServer.instance!=null)WorldServer.instance.BroadcastChestLid(position,opened);
        };
    }
    public void SetChestLid(Vector3Int position,bool opened)
    {if(opened)openChestLids.Add(position);else openChestLids.Remove(position);}
    public bool CanReplaceChest(Vector3Int position,BlockType next)
    {
        if(NetworkClient.active&&!NetworkServer.active)return true;
        var block=GetBlock(position);
        return block==null||block.type!=BlockType.Chest||next==BlockType.Chest||chests.CanRemove(worldName,position);
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
        var player=connection.identity.GetComponent<Player>();if(player==null)return;
        var world=World.Instance;
        if(world.isSaving&&request.action!=2)
        {connection.Send(new ChestReply{error="World is saving; try the chest again in a moment."});return;}
        bool accessible=player.health>0&&world.ChestAccessible(player.transform.position+Vector3.up*1.6f,request.position,player.reach);
        var reply=world.chests.Process(world.worldName,connection.connectionId,request,accessible);
        if(reply.accepted)player.GetComponent<PlayerInventory>().ApplyChestPlayerSnapshot(reply.inventory,reply.cursor);
        connection.Send(reply);
        if(reply.accepted&&!reply.opened)world.SaveWorld();
    }
    public void CloseChestForDisconnect(NetworkConnectionToClient connection)
    {
        if(World.Instance==null)return;
        var reply=World.Instance.chests.Close(connection.connectionId);
        if(!reply.accepted)return;
        if(connection.identity!=null)connection.identity.GetComponent<PlayerInventory>().ApplyChestPlayerSnapshot(reply.inventory,reply.cursor);
        World.Instance.SaveWorld();
    }
    public void BroadcastChestLid(Vector3Int position,bool opened){RpcChestLid(position,opened);}
    [ClientRpc] private void RpcChestLid(Vector3Int position,bool opened)
    {if(World.Instance!=null)World.Instance.SetChestLid(position,opened);}
    private void Update()
    {
        if(!isServer||World.Instance==null||Time.unscaledTime<chestCheckTime)return;
        chestCheckTime=Time.unscaledTime+.25f;
        foreach(int owner in World.Instance.chests.Owners())
        {
            NetworkConnectionToClient connection;
            bool connected=NetworkServer.connections.TryGetValue(owner,out connection)&&connection!=null&&connection.identity!=null;
            var player=connected?connection.identity.GetComponent<Player>():null;
            bool valid=player!=null&&player.health>0&&World.Instance.ChestAccessible(player.transform.position+Vector3.up*1.6f,
                World.Instance.chests.Position(owner),player.reach);
            if(valid)continue;
            var reply=World.Instance.chests.Close(owner);
            if(player!=null)player.GetComponent<PlayerInventory>().ApplyChestPlayerSnapshot(reply.inventory,reply.cursor);
            if(connected)connection.Send(reply);
            World.Instance.SaveWorld();
        }
    }
}

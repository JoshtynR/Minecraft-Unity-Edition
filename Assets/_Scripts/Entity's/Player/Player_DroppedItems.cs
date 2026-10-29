using Mirror;
using System.Collections.Generic;
using UnityEngine;

public partial class Player
{
    private int dropRequestId,lastServerDropRequest;
    private InventoryStack[] lastServerDropReply;
    private bool lastServerDropAccepted,awaitingNetworkDrop;
    private float nextDropPickup;
    private readonly List<DroppedItemState> nearbyPickupItems=new List<DroppedItemState>(16);
    private int cursorDropRequestId,lastServerCursorDropRequest;
    private InventoryStack[] lastServerCursorInventory;
    private InventoryStack lastServerCursorReply;
    private bool lastServerCursorAccepted;

    private bool CanRequestItemTransfer => !dimensionTransfer&&!WorldLoadingScreen.IsLoading&&
        (!networkStarted||(NetworkClient.active&&NetworkClient.ready));

    private void UpdateItemDrops()
    {
        if(inventory==null)return;
        if(inventory.DropRequestPending&&awaitingNetworkDrop&&!NetworkClient.active)
        {inventory.DropRequestPending=false;awaitingNetworkDrop=false;return;}
        if(!CanRequestItemTransfer||world==null||!world.IsWorldCreated||health<=0||inventory.isOpen||escapeMenuOpen||inventory.DropRequestPending)return;
        if(Input.GetKeyDown(KeyCode.Q))
        {RequestDropAction(true,inventory.selectedSlotIndex);return;}
        if(Time.unscaledTime<nextDropPickup)return;
        nextDropPickup=Time.unscaledTime+.1f;
        world.droppedItems.QueryNearby(transform.position,2,nearbyPickupItems);
        foreach(var item in nearbyPickupItems)
        {
            if(!DroppedItemRules.CanPickup(item,transform.position))continue;
            var snapshot=inventory.DropInventorySnapshot();
            if(InventoryRules.Capacity(snapshot,item.stack.type,0,36,item.stack.damage)<=0)continue;
            RequestDropAction(false,item.id);break;
        }
    }
    private void RequestDropAction(bool dropping,int id)
    {
        // Do not lock the inventory for a command Mirror cannot send. Pickups
        // are retried by the normal scan after readiness has been restored.
        if(inventory==null||inventory.DropRequestPending||!CanRequestItemTransfer)return;
        inventory.DropRequestPending=true;awaitingNetworkDrop=networkStarted;
        int request=++dropRequestId;
        var snapshot=inventory.DropInventorySnapshot();
        if(networkStarted)CmdDropAction(request,dropping,id,snapshot,knownItemWearRevision);
        else ProcessDropAction(request,dropping,id,snapshot,false,knownItemWearRevision);
    }
    [Command] private void CmdDropAction(int request,bool dropping,int id,InventoryStack[] snapshot,int revision)
    {ProcessDropAction(request,dropping,id,snapshot,true,revision);}
    private void ProcessDropAction(int request,bool dropping,int id,InventoryStack[] snapshot,bool network,int revision=0)
    {
        // Mining wear can finish while an older pickup request is in flight.
        // The reliable wear callback arrives before this rejection; retrying
        // then uses the updated client stack without overwriting its damage.
        if(!AcceptsItemRevision(revision)){CompleteDropAction(request,snapshot,false,network);return;}
        if(request==lastServerDropRequest&&lastServerDropReply!=null)
        {CompleteDropAction(request,lastServerDropReply,lastServerDropAccepted,network);return;}
        bool accepted=false;
        if(request>lastServerDropRequest&&!dimensionTransfer&&world!=null&&world.IsWorldCreated&&!world.isSaving&&health>0&&
            ChestTransactions.Valid(snapshot,new InventoryStack()))
        {
            if(dropping&&id>=0&&id<9&&!snapshot[id].Empty)
            {
                var stack=snapshot[id];stack.count=1;
                // Beta Q throws one item, with a forty-tick pickup delay.
                Vector3 forward=cam!=null?cam.forward:transform.forward;
                accepted=world.SpawnItemDrop(stack,transform.position+Vector3.up*(eyeHeight-.3f),forward*.3f+Vector3.up*.1f,40);
                if(accepted){snapshot[id].count--;snapshot[id].Normalize();}
            }
            else if(!dropping)accepted=world.PickupItemDrop(id,transform.position,snapshot)>0;
            if(accepted)inventory.ApplyDropInventory(snapshot);
        }
        if(request>lastServerDropRequest)
        {
            lastServerDropRequest=request;lastServerDropAccepted=accepted;
            lastServerDropReply=snapshot==null?null:(InventoryStack[])snapshot.Clone();
        }
        CompleteDropAction(request,snapshot,accepted,network);
    }
    private void CompleteDropAction(int request,InventoryStack[] snapshot,bool accepted,bool network)
    {
        if(network)TargetDropAction(connectionToClient,request,snapshot,accepted);
        else ReceiveDropAction(request,snapshot,accepted);
    }
    [TargetRpc] private void TargetDropAction(NetworkConnection target,int request,InventoryStack[] snapshot,bool accepted)
    {ReceiveDropAction(request,snapshot,accepted);}
    private void ReceiveDropAction(int request,InventoryStack[] snapshot,bool accepted)
    {
        if(request!=dropRequestId||!inventory.DropRequestPending)return;
        if(accepted)inventory.ApplyDropInventory(snapshot);
        inventory.DropRequestPending=false;awaitingNetworkDrop=false;
    }

    public void RequestInventoryCursorDrop(bool single)
    {
        if(inventory==null||inventory.DropRequestPending||!CanRequestItemTransfer||inventory.CursorSnapshot().Empty)return;
        inventory.DropRequestPending=true;awaitingNetworkDrop=networkStarted;
        int request=++cursorDropRequestId;
        var snapshot=inventory.DropInventorySnapshot();var cursor=inventory.CursorSnapshot();
        if(networkStarted)CmdInventoryCursorDrop(request,single,snapshot,cursor,knownItemWearRevision);
        else ProcessInventoryCursorDrop(request,single,snapshot,cursor,false,knownItemWearRevision);
    }
    [Command] private void CmdInventoryCursorDrop(int request,bool single,InventoryStack[] snapshot,InventoryStack cursor,int revision)
    {ProcessInventoryCursorDrop(request,single,snapshot,cursor,true,revision);}
    private void ProcessInventoryCursorDrop(int request,bool single,InventoryStack[] snapshot,InventoryStack cursor,bool network,int revision)
    {
        if(request==lastServerCursorDropRequest&&lastServerCursorInventory!=null)
        {CompleteInventoryCursorDrop(request,lastServerCursorInventory,lastServerCursorReply,lastServerCursorAccepted,network);return;}
        bool accepted=false;
        if(request>lastServerCursorDropRequest&&AcceptsItemRevision(revision)&&!dimensionTransfer&&world!=null&&world.IsWorldCreated&&!world.isSaving&&health>0&&ChestTransactions.Valid(snapshot,cursor))
        {
            var updated=cursor;var dropped=InventoryRules.Drop(ref updated,single);
            accepted=SpawnMenuDrop(dropped);
            if(accepted){cursor=updated;inventory.ApplyDropState(snapshot,cursor);}
        }
        if(request>lastServerCursorDropRequest)
        {
            lastServerCursorDropRequest=request;lastServerCursorAccepted=accepted;
            lastServerCursorInventory=snapshot==null?null:(InventoryStack[])snapshot.Clone();lastServerCursorReply=cursor;
        }
        CompleteInventoryCursorDrop(request,snapshot,cursor,accepted,network);
    }
    private void CompleteInventoryCursorDrop(int request,InventoryStack[] snapshot,InventoryStack cursor,bool accepted,bool network)
    {
        if(network)TargetInventoryCursorDrop(connectionToClient,request,snapshot,cursor,accepted);
        else ReceiveInventoryCursorDrop(request,snapshot,cursor,accepted);
    }
    [TargetRpc] private void TargetInventoryCursorDrop(NetworkConnection target,int request,InventoryStack[] snapshot,InventoryStack cursor,bool accepted)
    {ReceiveInventoryCursorDrop(request,snapshot,cursor,accepted);}
    private void ReceiveInventoryCursorDrop(int request,InventoryStack[] snapshot,InventoryStack cursor,bool accepted)
    {
        if(request!=cursorDropRequestId||inventory==null||!inventory.DropRequestPending)return;
        if(accepted)inventory.ApplyDropState(snapshot,cursor);
        inventory.DropRequestPending=false;awaitingNetworkDrop=false;
    }
    public bool SpawnMenuDrop(InventoryStack stack)
    {
        if(world==null||stack.Empty)return false;
        Vector3 forward=cam!=null?cam.forward:transform.forward;
        return world.SpawnItemDrop(stack,transform.position+Vector3.up*(eyeHeight-.3f),forward*.3f+Vector3.up*.1f,40);
    }
}

public partial class PlayerInventory
{
    public bool DropRequestPending {get;set;}
    public InventoryStack[] DropInventorySnapshot()
    {ReadStorage();return (InventoryStack[])storageBuffer.Clone();}
    public InventoryStack CursorSnapshot()=>Stack(pickedUpSlot);
    public void ApplyDropInventory(InventoryStack[] snapshot)
    {
        if(snapshot==null||snapshot.Length!=36)return;
        for(int i=0;i<36;i++)WriteStack(i,snapshot[i]);
    }
    public void ApplyDropState(InventoryStack[] snapshot,InventoryStack cursor)
    {ApplyDropInventory(snapshot);SetCursor(cursor);}
}

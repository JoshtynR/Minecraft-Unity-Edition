using Mirror;
using UnityEngine;

public partial class Player
{
    private int dropRequestId,lastServerDropRequest;
    private InventoryStack[] lastServerDropReply;
    private bool lastServerDropAccepted,awaitingNetworkDrop;
    private float nextDropPickup;

    private void UpdateItemDrops()
    {
        if(inventory==null)return;
        if(inventory.DropRequestPending&&awaitingNetworkDrop&&!NetworkClient.active)
        {inventory.DropRequestPending=false;awaitingNetworkDrop=false;return;}
        if(world==null||!world.IsWorldCreated||health<=0||inventory.isOpen||escapeMenuOpen||inventory.DropRequestPending)return;
        if(Input.GetKeyDown(KeyCode.Q))
        {RequestDropAction(true,inventory.selectedSlotIndex);return;}
        if(Time.unscaledTime<nextDropPickup)return;
        nextDropPickup=Time.unscaledTime+.1f;
        foreach(var item in world.droppedItems.Items)
        {
            if(!DroppedItemRules.CanPickup(item,transform.position))continue;
            var snapshot=inventory.DropInventorySnapshot();
            if(InventoryRules.Capacity(snapshot,item.stack.type,0,36,item.stack.damage)<=0)continue;
            RequestDropAction(false,item.id);break;
        }
    }
    private void RequestDropAction(bool dropping,int id)
    {
        inventory.DropRequestPending=true;awaitingNetworkDrop=networkStarted;
        int request=++dropRequestId;
        var snapshot=inventory.DropInventorySnapshot();
        if(networkStarted)CmdDropAction(request,dropping,id,snapshot);
        else ProcessDropAction(request,dropping,id,snapshot,false);
    }
    [Command] private void CmdDropAction(int request,bool dropping,int id,InventoryStack[] snapshot)
    {ProcessDropAction(request,dropping,id,snapshot,true);}
    private void ProcessDropAction(int request,bool dropping,int id,InventoryStack[] snapshot,bool network)
    {
        if(request==lastServerDropRequest&&lastServerDropReply!=null)
        {CompleteDropAction(request,lastServerDropReply,lastServerDropAccepted,network);return;}
        bool accepted=false;
        if(request>lastServerDropRequest&&world!=null&&world.IsWorldCreated&&!world.isSaving&&health>0&&
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
}

public partial class PlayerInventory
{
    public bool DropRequestPending {get;set;}
    public InventoryStack[] DropInventorySnapshot()
    {ReadStorage();return (InventoryStack[])storageBuffer.Clone();}
    public void ApplyDropInventory(InventoryStack[] snapshot)
    {
        if(snapshot==null||snapshot.Length!=36)return;
        for(int i=0;i<36;i++)WriteStack(i,snapshot[i]);
    }
}

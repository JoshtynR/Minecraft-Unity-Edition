using Mirror;
using UnityEngine;

public partial class Player
{
    private int serverItemWearRevision,knownItemWearRevision;
    public int ItemWearRevision=>knownItemWearRevision;
    public bool AcceptsItemRevision(int revision)=>revision==serverItemWearRevision;
    private void WearItemOnHit(Player target)
    {
        if(inventory.DropRequestPending||ItemDurability.HitWear(inventory.HeldItem().type)==0)return;
        int request=++dropRequestId;
        inventory.DropRequestPending=true;awaitingNetworkDrop=networkStarted;
        var snapshot=inventory.DropInventorySnapshot();
        if(networkStarted)CmdWearItemOnHit(request,target,inventory.selectedSlotIndex,snapshot,knownItemWearRevision);
        else ProcessItemHitWear(request,target,inventory.selectedSlotIndex,snapshot,false,knownItemWearRevision);
    }
    [Command] private void CmdWearItemOnHit(int request,Player target,int slot,InventoryStack[] snapshot,int revision)
    {ProcessItemHitWear(request,target,slot,snapshot,true,revision);}
    private void ProcessItemHitWear(int request,Player target,int slot,InventoryStack[] snapshot,bool network,int revision=0)
    {
        if(!AcceptsItemRevision(revision)){CompleteDropAction(request,snapshot,false,network);return;}
        if(request==lastServerDropRequest&&lastServerDropReply!=null)
        {CompleteDropAction(request,lastServerDropReply,lastServerDropAccepted,network);return;}
        bool accepted=false;
        if(request>lastServerDropRequest&&!dimensionTransfer&&world!=null&&!world.isSaving&&health>0&&target!=null&&target!=this&&target.dimension==dimension&&target.health>0&&
            (target.transform.position-transform.position).sqrMagnitude<=reach*reach&&slot>=0&&slot<9&&ChestTransactions.Valid(snapshot,new InventoryStack()))
        {
            int amount=ItemDurability.HitWear(snapshot[slot].type);
            if(amount>0&&!snapshot[slot].Empty&&ItemDurability.Maximum(snapshot[slot].type)>0)
            {
                // A validated hit ends the mining session before changing its tool.
                serverMining=false;
                ItemDurability.Wear(ref snapshot[slot],amount);
                inventory.ApplyDropInventory(snapshot);accepted=true;
            }
        }
        if(request>lastServerDropRequest)
        {
            lastServerDropRequest=request;lastServerDropAccepted=accepted;
            lastServerDropReply=snapshot==null?null:(InventoryStack[])snapshot.Clone();
        }
        CompleteDropAction(request,snapshot,accepted,network);
    }
}

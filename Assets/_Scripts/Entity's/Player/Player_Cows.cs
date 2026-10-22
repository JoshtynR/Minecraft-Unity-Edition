using Mirror;
using UnityEngine;

public partial class Player
{
    private float nextLocalCowAction;
    private double nextServerCowAction;
    private CowState TargetedCow()
    {
        return world == null || cam == null ? null : world.TargetCow(cam.position,cam.forward,reach);
    }
    private bool TryUseCow(bool milk)
    {
        var cow=TargetedCow();if(cow==null)return false;
        // A cow blocks mining/placement behind it, even when the held item has no interaction.
        if(milk && inventory.HeldItem().type!=ItemType.Bucket && inventory.HeldItem().type!=CowRules.BreedingFood(cow) && inventory.HeldItem().type!=ItemType.Shears && CowRules.DyeColor(inventory.HeldItem().type)<0)return true;
        if(inventory.DropRequestPending || Time.unscaledTime<nextLocalCowAction)return true;
        nextLocalCowAction=Time.unscaledTime+.25f;CancelMining();
        int request=++dropRequestId;inventory.DropRequestPending=true;awaitingNetworkDrop=networkStarted;
        var snapshot=inventory.DropInventorySnapshot();
        if(networkStarted)CmdCowAction(request,cow.id,milk,inventory.selectedSlotIndex,snapshot,knownItemWearRevision);
        else ProcessCowAction(request,cow.id,milk,inventory.selectedSlotIndex,snapshot,false,knownItemWearRevision);
        return true;
    }
    [Command] private void CmdCowAction(int request,int id,bool milk,int slot,InventoryStack[] snapshot,int revision)
    {ProcessCowAction(request,id,milk,slot,snapshot,true,revision);}
    private void ProcessCowAction(int request,int id,bool milk,int slot,InventoryStack[] snapshot,bool network,int revision)
    {
        if(!AcceptsItemRevision(revision)){CompleteDropAction(request,snapshot,false,network);return;}
        if(request==lastServerDropRequest && lastServerDropReply!=null)
        {CompleteDropAction(request,lastServerDropReply,lastServerDropAccepted,network);return;}
        bool accepted=false;
        if(request>lastServerDropRequest && !dimensionTransfer && world!=null && world.IsWorldCreated && !world.isSaving && health>0 &&
            NetworkTime.time>=nextServerCowAction && slot>=0 && slot<9 && ChestTransactions.Valid(snapshot,new InventoryStack()) &&
            world.CowAccessible(id,transform.position+Vector3.up*eyeHeight,reach))
        {
            serverMining=false;
            if(milk)accepted=world.InteractAnimal(id,snapshot,slot);
            else if(world.HitCow(id,CowRules.AttackDamage(snapshot[slot].type),transform.position))
            {ItemDurability.Wear(ref snapshot[slot],ItemDurability.HitWear(snapshot[slot].type));accepted=true;}
            if(accepted){nextServerCowAction=NetworkTime.time+.25;inventory.ApplyDropInventory(snapshot);}
        }
        if(request>lastServerDropRequest)
        {
            lastServerDropRequest=request;lastServerDropAccepted=accepted;
            lastServerDropReply=snapshot==null?null:(InventoryStack[])snapshot.Clone();
        }
        CompleteDropAction(request,snapshot,accepted,network);
    }
}

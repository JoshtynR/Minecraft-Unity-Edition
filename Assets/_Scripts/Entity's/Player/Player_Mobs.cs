using Mirror;
using UnityEngine;

public partial class Player
{
    private float nextLocalMobAction;
    private double nextServerMobAction;
    private MobState TargetedMob()
    {
        return world == null || cam == null ? null : world.TargetMob(cam.position,cam.forward,reach);
    }
    private bool TryUseMob(bool milk)
    {
        var mob=TargetedMob();if(mob==null)return false;
        if(networkStarted&&!NetworkClient.ready)return true;
        // A mob blocks mining/placement behind it, even when the held item has no interaction.
        if(milk && inventory.HeldItem().type!=ItemType.Bucket && inventory.HeldItem().type!=MobRules.BreedingFood(mob) && inventory.HeldItem().type!=ItemType.Shears && MobRules.DyeColor(inventory.HeldItem().type)<0)return true;
        if(inventory.DropRequestPending || Time.unscaledTime<nextLocalMobAction)return true;
        nextLocalMobAction=Time.unscaledTime+.25f;CancelMining();
        int request=++dropRequestId;inventory.DropRequestPending=true;awaitingNetworkDrop=networkStarted;
        var snapshot=inventory.DropInventorySnapshot();
        if(networkStarted)CmdMobAction(request,mob.id,milk,inventory.selectedSlotIndex,snapshot,knownItemWearRevision);
        else ProcessMobAction(request,mob.id,milk,inventory.selectedSlotIndex,snapshot,false,knownItemWearRevision);
        return true;
    }
    [Command] private void CmdMobAction(int request,int id,bool milk,int slot,InventoryStack[] snapshot,int revision)
    {ProcessMobAction(request,id,milk,slot,snapshot,true,revision);}
    private void ProcessMobAction(int request,int id,bool milk,int slot,InventoryStack[] snapshot,bool network,int revision)
    {
        if(!AcceptsItemRevision(revision)){CompleteDropAction(request,snapshot,false,network);return;}
        if(request==lastServerDropRequest && lastServerDropReply!=null)
        {CompleteDropAction(request,lastServerDropReply,lastServerDropAccepted,network);return;}
        bool accepted=false;
        if(request>lastServerDropRequest && !dimensionTransfer && world!=null && world.IsWorldCreated && !world.isSaving && health>0 &&
            NetworkTime.time>=nextServerMobAction && slot>=0 && slot<9 && ChestTransactions.Valid(snapshot,new InventoryStack()) &&
            world.MobAccessible(id,transform.position+Vector3.up*eyeHeight,reach))
        {
            serverMining=false;
            if(milk)accepted=world.InteractMob(id,snapshot,slot);
            else if(world.HitMob(id,MobRules.AttackDamage(snapshot[slot].type),transform.position))
            {ItemDurability.Wear(ref snapshot[slot],ItemDurability.HitWear(snapshot[slot].type));accepted=true;}
            if(accepted){nextServerMobAction=NetworkTime.time+.25;inventory.ApplyDropInventory(snapshot);}
        }
        if(request>lastServerDropRequest)
        {
            lastServerDropRequest=request;lastServerDropAccepted=accepted;
            lastServerDropReply=snapshot==null?null:(InventoryStack[])snapshot.Clone();
        }
        CompleteDropAction(request,snapshot,accepted,network);
    }
}

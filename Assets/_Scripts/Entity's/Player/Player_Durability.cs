using Mirror;
using UnityEngine;

public partial class Player
{
    private int serverItemWearRevision,knownItemWearRevision;
    private double nextPlayerAttack, hurtUntil;
    private int armourDamageRemainder,lastEquipmentRevision=-1;
    private void UpdateArmourEquipment()
    {
        if(inventory==null||!networkStarted||!isLocalPlayer||isServer||!NetworkClient.ready)return;
        if(lastEquipmentRevision==inventory.EquipmentRevision)return;
        CmdEquipment(inventory.EquipmentSnapshot(),knownItemWearRevision);
        lastEquipmentRevision=inventory.EquipmentRevision;
    }
    [Command] private void CmdEquipment(InventoryStack[] equipment,int revision)
    {
        if(!AcceptsItemRevision(revision)||equipment==null||equipment.Length!=4||inventory==null)return;
        for(int i=0;i<4;i++)
        {
            var stack=equipment[i];if(stack.Empty)continue;
            if(stack.count!=1||stack.damage<0||stack.damage>ItemDurability.Maximum(stack.type)||
                !(ItemDurability.ArmorPart(stack.type)==i||i==0&&stack.type==ItemType.Pumpkin))return;
        }
        inventory.ApplyEquipmentSnapshot(equipment);
    }
    public void ApplyArmouredDamage(int damage)
    {
        if(damage<=0||health<=0)return;
        bool wearingArmour=inventory!=null&&inventory.ArmourValue>0;
        int applied=inventory==null?damage:inventory.AbsorbArmourDamage(damage,ref armourDamageRemainder);
        health=Mathf.Max(0,health-applied);
        if(networkStarted&&isServer&&wearingArmour)
        {
            serverItemWearRevision++;
            if(isLocalPlayer)knownItemWearRevision=serverItemWearRevision;
            else TargetArmourWear(connectionToClient,inventory.EquipmentSnapshot(),serverItemWearRevision);
        }
    }
    [TargetRpc] private void TargetArmourWear(NetworkConnection connection,InventoryStack[] equipment,int revision)
    {
        if(inventory==null||revision<=knownItemWearRevision)return;
        inventory.ApplyEquipmentSnapshot(equipment);knownItemWearRevision=revision;
        lastEquipmentRevision=inventory.EquipmentRevision;
    }
    public int ItemWearRevision=>knownItemWearRevision;
    public bool AcceptsItemRevision(int revision)=>revision==serverItemWearRevision;
    private void WearItemOnHit(Player target)
    {
        if(inventory.DropRequestPending)return;
        int request=++dropRequestId;
        inventory.DropRequestPending=true;awaitingNetworkDrop=networkStarted&&!isServer;
        var snapshot=inventory.DropInventorySnapshot();
        if(networkStarted&&!isServer)CmdWearItemOnHit(request,target,inventory.selectedSlotIndex,snapshot,knownItemWearRevision);
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
            if(NetworkTime.time>=nextPlayerAttack&&NetworkTime.time>=target.hurtUntil&&!target.dimensionTransfer)
            {
                nextPlayerAttack=NetworkTime.time+.25;
                target.hurtUntil=NetworkTime.time+.5;
                int damage=MobRules.AttackDamage(snapshot[slot].type);
                target.ApplyArmouredDamage(damage);
                var push=target.transform.position-transform.position+Vector3.up;
                if(networkStarted&&!(target.isServer&&target.isLocalPlayer))target.RpcAddForce(target.connectionToClient,push,6);
                else target.AddForce(push,6);
                GameAudio.Broadcast("damage/hit",target.transform.position,1,1,world);
                // A validated hit ends the mining session before changing its tool.
                serverMining=false;
                ItemDurability.Wear(ref snapshot[slot],amount);
                inventory.ApplyDropInventory(snapshot);accepted=true;
                exhaustion=System.Math.Min(40,exhaustion+.3f);
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

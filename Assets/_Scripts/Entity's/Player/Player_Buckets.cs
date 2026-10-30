using Mirror;
using UnityEngine;

public partial class Player
{
    private bool TryUseBucket()
    {
        if(inventory==null)return false;
        var type=inventory.HeldItem().type;
        if(type!=ItemType.Bucket&&type!=ItemType.WaterBucket&&type!=ItemType.LavaBucket)return false;
        if(inventory.DropRequestPending||dimensionTransfer||WorldLoadingScreen.IsLoading)return true;
        bool remote=networkStarted&&!isServer;
        if(remote&&!NetworkClient.ready)return true;
        var aim=cam!=null?cam.forward:transform.forward;
        int request=++dropRequestId;var snapshot=inventory.DropInventorySnapshot();
        inventory.DropRequestPending=true;awaitingNetworkDrop=remote;
        if(remote)CmdUseBucket(request,aim,inventory.selectedSlotIndex,snapshot,knownItemWearRevision);
        else ProcessBucketUse(request,aim,inventory.selectedSlotIndex,snapshot,false,knownItemWearRevision);
        return true;
    }

    [Command] private void CmdUseBucket(int request,Vector3 aim,int slot,InventoryStack[] snapshot,int revision)
    {ProcessBucketUse(request,aim,slot,snapshot,true,revision);}

    private void ProcessBucketUse(int request,Vector3 aim,int slot,InventoryStack[] snapshot,bool network,int revision)
    {
        if(!AcceptsItemRevision(revision)){CompleteDropAction(request,snapshot,false,network);return;}
        if(request==lastServerDropRequest&&lastServerDropReply!=null)
        {CompleteDropAction(request,lastServerDropReply,lastServerDropAccepted,network);return;}
        bool accepted=false;var realm=CurrentWorld;
        bool finite=!float.IsNaN(aim.x)&&!float.IsNaN(aim.y)&&!float.IsNaN(aim.z)&&
            !float.IsInfinity(aim.x)&&!float.IsInfinity(aim.y)&&!float.IsInfinity(aim.z);
        if(request>lastServerDropRequest&&!dimensionTransfer&&health>0&&realm!=null&&realm.IsWorldCreated&&!realm.isSaving&&
            slot>=0&&slot<9&&ChestTransactions.Valid(snapshot,new InventoryStack())&&finite)
        {
            var held=snapshot[slot];
            var block=BlockTargeting.Raycast(realm,transform.position+Vector3.up*eyeHeight,aim,reach,out var adjacent,out var hit,held.type==ItemType.Bucket);
            if(block!=null)
            {
                var position=held.type==ItemType.Bucket||World.BucketCanReplace(block.type)?block.globalWorldPosition:adjacent;
                if(realm.UseBucket(position,ref held))
                {serverMining=false;snapshot[slot]=held;inventory.ApplyDropInventory(snapshot);accepted=true;}
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

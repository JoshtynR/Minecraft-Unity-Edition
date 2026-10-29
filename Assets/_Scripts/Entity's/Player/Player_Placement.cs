using Mirror;
using UnityEngine;

public partial class Player
{
    private int placementRequestId,pendingPlacementId,pendingPlacementSlot;
    private InventoryStack[] pendingPlacementInventory;
    private int lastServerPlacementId;
    private bool lastServerPlacementAccepted;

    private bool RequestBlockPlacement(Vector3Int position,BlockType type,byte state)
    {
        if(!NetworkClient.active||!NetworkClient.ready||dimensionTransfer||WorldLoadingScreen.IsLoading||inventory.DropRequestPending)return false;
        pendingPlacementSlot=inventory.selectedSlotIndex;
        pendingPlacementInventory=inventory.DropInventorySnapshot();
        pendingPlacementId=++placementRequestId;
        inventory.DropRequestPending=true;
        NetworkClient.Send(new WorldServer.SetBlockMessage(position,type,state,pendingPlacementId));
        return true;
    }

    public bool TryReplayPlacement(int request,out bool accepted)
    {
        accepted=request==lastServerPlacementId&&lastServerPlacementAccepted;
        return request<=lastServerPlacementId;
    }

    private void CancelPendingPlacement()
    {
        if(pendingPlacementInventory==null)return;
        pendingPlacementInventory=null;pendingPlacementId=0;
        if(inventory!=null)inventory.DropRequestPending=false;
    }

    public void CompletePlacementRequest(NetworkConnectionToClient connection,int request,bool accepted)
    {
        if(request>lastServerPlacementId){lastServerPlacementId=request;lastServerPlacementAccepted=accepted;}
        TargetPlacementResult(connection,request,accepted);
    }

    [TargetRpc] private void TargetPlacementResult(NetworkConnection connection,int request,bool accepted)
    {
        if(request!=pendingPlacementId||pendingPlacementInventory==null||inventory==null)return;
        // Apply to the original slot, not whichever hotbar slot is selected
        // when the reply arrives. Rejections leave the inventory untouched.
        if(accepted)
        {
            inventory.ApplyPlacementConsumption(pendingPlacementSlot,pendingPlacementInventory[pendingPlacementSlot]);
        }
        pendingPlacementInventory=null;pendingPlacementId=0;
        inventory.DropRequestPending=false;
    }
}

public partial class PlayerInventory
{
    public void ApplyPlacementConsumption(int slot,InventoryStack expected)
    {
        if(slot<0||slot>=9)return;
        var current=Stack(slots[slot]);
        if(current.type!=expected.type||current.count<=0)return;
        current.count--;current.Normalize();WriteStack(slot,current);
    }
}

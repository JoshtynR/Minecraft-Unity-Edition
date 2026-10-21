using Mirror;
using UnityEngine;

public partial class Player
{
    private bool TryUseFarming(Block block, Vector3Int adjacent)
    {
        if (!FarmingItems.CanUse(inventory.HeldItem().type)) return false;
        if (inventory.DropRequestPending) return true;
        int request = ++dropRequestId;
        inventory.DropRequestPending = true; awaitingNetworkDrop = networkStarted;
        var snapshot = inventory.DropInventorySnapshot();
        if (networkStarted) CmdUseFarmItem(request, block.globalWorldPosition, adjacent, inventory.selectedSlotIndex, snapshot, knownItemWearRevision);
        else ProcessFarmItem(request, block.globalWorldPosition, adjacent, inventory.selectedSlotIndex, snapshot, false, knownItemWearRevision);
        return true;
    }

    [Command] private void CmdUseFarmItem(int request, Vector3Int target, Vector3Int adjacent, int slot, InventoryStack[] snapshot, int revision)
    { ProcessFarmItem(request, target, adjacent, slot, snapshot, true, revision); }

    private void ProcessFarmItem(int request, Vector3Int target, Vector3Int adjacent, int slot, InventoryStack[] snapshot, bool network, int revision)
    {
        if (!AcceptsItemRevision(revision)) { CompleteDropAction(request,snapshot,false,network); return; }
        if (request == lastServerDropRequest && lastServerDropReply != null)
        { CompleteDropAction(request,lastServerDropReply,lastServerDropAccepted,network); return; }
        bool accepted = false;
        if (request > lastServerDropRequest && !dimensionTransfer && world != null && world.IsWorldCreated && !world.isSaving && health > 0 &&
            slot >= 0 && slot < 9 && ChestTransactions.Valid(snapshot,new InventoryStack()) && InMiningReach(target) &&
            MiningLineOfSight(target,(Vector3)target+Vector3.one*.5f))
        {
            var held = snapshot[slot];
            if (world.UseFarmItem(target,adjacent,ref held))
            { serverMining=false; snapshot[slot]=held; inventory.ApplyDropInventory(snapshot); accepted=true; }
        }
        if (request > lastServerDropRequest)
        {
            lastServerDropRequest=request; lastServerDropAccepted=accepted;
            lastServerDropReply=snapshot==null?null:(InventoryStack[])snapshot.Clone();
        }
        CompleteDropAction(request,snapshot,accepted,network);
    }
}

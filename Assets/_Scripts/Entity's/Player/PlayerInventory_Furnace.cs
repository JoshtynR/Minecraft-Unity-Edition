using Mirror;
using UnityEngine;
using UnityEngine.UI;

public partial class PlayerInventory
{
    private bool furnaceOpen, furnacePending, furnaceCloseRequested, furnaceNetworkSession;
    private int queuedFurnaceCollection=-1;
    private int[] queuedFurnaceDrag;
    private bool queuedFurnaceDragRight;
    private Vector3Int furnacePosition;
    private int furnaceSequence;
    private float nextFurnaceRefresh;
    private RectTransform furnacePanel, furnaceFlame, furnaceArrow;
    private readonly Slot[] furnaceSlots = new Slot[3];
    private readonly RectTransform[] furnaceRects = new RectTransform[3];
    public void RequestFurnace(Vector3Int position)
    {
        if (DropRequestPending || isOpen || furnacePending || furnaceOpen || chestPending || chestOpen || creativeRoot == null) return;
        ReadStorage(); furnacePosition = position; furnaceNetworkSession = (NetworkClient.active&&!NetworkServer.active); OpenInventory();
        SendFurnace(new FurnaceRequest { action = 0, position = position, inventory = (InventoryStack[])storageBuffer.Clone(), cursor = Stack(pickedUpSlot), itemWearRevision = player != null ? player.ItemWearRevision : 0 });
    }
    private void SendFurnace(FurnaceRequest request)
    {
        furnacePending = true;
        if ((NetworkClient.active&&!NetworkServer.active)) { NetworkClient.Send(request); return; }
        var world = player.CurrentWorld;
        if (world == null) { ReceiveFurnaceReply(new FurnaceReply { error = "World closed." }); return; }
        if (world.isSaving && request.action != 2) { ReceiveFurnaceReply(new FurnaceReply { error = "World is saving; try again in a moment." }); return; }
        bool accessible = player != null && player.health > 0 && world.FurnaceAccessible(transform.position + Vector3.up * 1.6f, request.position);
        var reply = world.furnaces.Process(world.worldName, -1, request, accessible);
        if (reply.accepted && !reply.dropped.Empty) player.SpawnMenuDrop(reply.dropped);
        ReceiveFurnaceReply(reply);
        if (reply.accepted && !reply.opened) world.SaveWorld();
    }
    public void ReceiveFurnaceReply(FurnaceReply reply)
    {
        furnacePending = false;
        if (!reply.accepted)
        {
            Debug.LogWarning(reply.error);
            if (!furnaceOpen) { furnaceCloseRequested = false; CloseInventory(); }
            return;
        }
        ApplyChestPlayerSnapshot(reply.inventory, reply.cursor); furnaceSequence = reply.sequence;
        nextFurnaceRefresh = Time.unscaledTime + .2f;
        if (!reply.opened) { EndFurnacePanel(); furnaceCloseRequested = false; CloseInventory(); return; }
        if (!furnaceOpen) { furnaceOpen = true; furnacePosition = reply.position; BuildFurnacePanel(); OpenInventory(); }
        for (int i = 0; i < 3; i++)
        {
            var stack = reply.items[i]; var slot = furnaceSlots[i];
            if (slot.type != stack.type) { ClearSlotIcon(furnaceRects[i]); CreateItemIcon(stack.type, furnaceRects[i]); }
            slot.type = stack.type; slot.damage = stack.damage; slot.SetCount(stack.count); DrawDurability(furnaceRects[i], stack);
        }
        SetFurnaceProgress(furnaceFlame, 14, reply.burnTicks > 0 ? Mathf.Clamp(reply.burnTicks * 13 / System.Math.Max(1, reply.fuelTicks) + 1, 0, 14) : 0, true);
        SetFurnaceProgress(furnaceArrow, 24, Mathf.Clamp(reply.cookTicks * 24 / BetaSmelting.CookTicks, 0, 24), false);
        if(queuedFurnaceCollection>=0&&!furnaceCloseRequested)
        {int index=queuedFurnaceCollection;queuedFurnaceCollection=-1;CollectFurnace(index);return;}
        if(queuedFurnaceDrag!=null&&!furnaceCloseRequested)
        {var indices=queuedFurnaceDrag;bool right=queuedFurnaceDragRight;queuedFurnaceDrag=null;DragFurnace(indices,right);return;}
        if (furnaceCloseRequested) CloseInventory();
    }
    private void BuildFurnacePanel()
    {
        furnacePanel = CreativeRect("FurnaceContainer", creativeRoot, inventoryRect.anchoredPosition.x, 0, 176, 166);
        var assets = Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        var sheet = assets != null ? assets.furnace : null;
        if (sheet != null)
        {
            var background = furnacePanel.gameObject.AddComponent<RawImage>(); background.texture = sheet;
            background.uvRect = new Rect(0, 1 - 166 / 256f, 176 / 256f, 166 / 256f); background.raycastTarget = false;
        }
        else Panel(furnacePanel);
        int[] xs = { 56, 56, 116 }, ys = { 17, 53, 35 };
        for (int i = 0; i < 3; i++)
        {
            var rect = CreativeRect("FurnaceSlot" + i, furnacePanel, xs[i] - 80, 75 - ys[i], 18, 18);
            if (sheet != null) rect.gameObject.AddComponent<Image>().color = Color.clear; else SlotBackground(rect);
            var input = rect.gameObject.AddComponent<InventorySlot>(); input.playerInventory = this; input.slotIndex = 300 + i;
            furnaceRects[i] = rect; furnaceSlots[i] = new Slot(300 + i) { countText = SlotCount(rect) };
        }
        ContainerLabel("FurnaceTitle", furnacePanel, 60, 6, "Furnace");
        ContainerLabel("InventoryTitle", furnacePanel, 8, 72, "Inventory");
        furnaceFlame = ProgressImage("FuelFlame", 56, 36, 14, 14, sheet, true);
        furnaceArrow = ProgressImage("CookArrow", 79, 34, 24, 17, sheet, false);
        inventorySlotsRect.SetParent(furnacePanel, false);
        inventoryRect.gameObject.SetActive(false); creativePanel.gameObject.SetActive(false);
        inventoryHighlightRect.SetAsLastSibling(); pickedUpItemRect.SetAsLastSibling(); tooltipRect.SetAsLastSibling();
    }
    private RectTransform ProgressImage(string name, int x, int y, int width, int height, Texture2D sheet, bool flame)
    {
        var rect = CreativeRect(name, furnacePanel, x - 88 + width * .5f, 83 - y - height * .5f, width, height);
        if (sheet != null) { var image = rect.gameObject.AddComponent<RawImage>(); image.texture = sheet; image.raycastTarget = false; }
        else { var image = rect.gameObject.AddComponent<Image>(); image.color = flame ? new Color(1, .5f, .1f, 1) : new Color(.7f, .7f, .7f, 1); image.raycastTarget = false; }
        return rect;
    }
    private void SetFurnaceProgress(RectTransform rect, int full, int value, bool vertical)
    {
        rect.gameObject.SetActive(value > 0);
        if (vertical) { rect.sizeDelta = new Vector2(14, value); rect.anchoredPosition = new Vector2(-25, 33 + value * .5f); }
        else { rect.sizeDelta = new Vector2(value, 17); rect.anchoredPosition = new Vector2(-9 + value * .5f, 40.5f); }
        var image = rect.GetComponent<RawImage>();
        if (image != null) image.uvRect = vertical ? new Rect(176 / 256f, 1 - 14 / 256f, 14 / 256f, value / 256f) : new Rect(176 / 256f, 1 - 31 / 256f, value / 256f, 17 / 256f);
    }
    private void ClickFurnace(int index, bool right, bool shift)
    {
        if (furnacePending) return;
        int target = index >= 300 && index < 303 ? index - 300 : index >= 0 && index < 36 ? index + 3 : -1;
        if (target < 0) return;
        SendFurnace(new FurnaceRequest { action = 1, position = furnacePosition, slot = target, right = right, shift = shift, sequence = furnaceSequence + 1 });
    }
    private int FurnaceTarget(int index) => index>=300&&index<303?index-300:index>=0&&index<36?index+3:-1;
    private void DragFurnace(int[] indices,bool right)
    {
        if(furnacePending){queuedFurnaceDrag=(int[])indices.Clone();queuedFurnaceDragRight=right;return;}var targets=new int[indices.Length];
        for(int i=0;i<indices.Length;i++)targets[i]=FurnaceTarget(indices[i]);
        SendFurnace(new FurnaceRequest{action=4,position=furnacePosition,slots=targets,right=right,sequence=furnaceSequence+1});
    }
    private void CollectFurnace(int index)
    {
        if(furnacePending){queuedFurnaceCollection=index;return;}int target=FurnaceTarget(index);if(target<0)return;
        SendFurnace(new FurnaceRequest{action=5,position=furnacePosition,slot=target,sequence=furnaceSequence+1});
    }
    private void DropFurnaceCursor(bool single)
    {if(!furnacePending)SendFurnace(new FurnaceRequest{action=6,position=furnacePosition,right=single,sequence=furnaceSequence+1});}
    private bool TryCloseFurnace()
    {
        if (furnacePending) { furnaceCloseRequested = true; return true; }
        if (!furnaceOpen) return false;
        SendFurnace(new FurnaceRequest { action = 2, position = furnacePosition }); return true;
    }
    private void EndFurnacePanel()
    {
        furnaceOpen = false;queuedFurnaceCollection=-1;queuedFurnaceDrag=null; HideSlotHover(); inventorySlotsRect.SetParent(inventoryRect, false);
        inventoryRect.gameObject.SetActive(true);
        if (furnacePanel != null) { furnacePanel.gameObject.SetActive(false); Destroy(furnacePanel.gameObject); furnacePanel = null; }
    }
    private void UpdateFurnacePanel()
    {
        if (!furnaceOpen) return;
        if (player.CurrentWorld == null || (furnaceNetworkSession && !(NetworkClient.active&&!NetworkServer.active))) { furnacePending = false; furnaceCloseRequested = false; EndFurnacePanel(); CloseInventory(); return; }
        furnacePanel.anchoredPosition = new Vector2(inventoryRect.anchoredPosition.x, 0);
        if (!(NetworkClient.active&&!NetworkServer.active) && (player.health <= 0 || !player.CurrentWorld.FurnaceAccessible(transform.position + Vector3.up * 1.6f, furnacePosition)))
        { ReceiveFurnaceReply(player.CurrentWorld.furnaces.Close(-1)); return; }
        if (!furnacePending && Time.unscaledTime >= nextFurnaceRefresh)
            SendFurnace(new FurnaceRequest { action = 3, position = furnacePosition });
    }
    private void ReleaseFurnace()
    {
        if (!furnaceOpen && !furnacePending) return;
        if ((NetworkClient.active&&!NetworkServer.active)) NetworkClient.Send(new FurnaceRequest { action = 2, position = furnacePosition });
        else if (player.CurrentWorld != null) player.CurrentWorld.furnaces.Close(-1);
    }
}

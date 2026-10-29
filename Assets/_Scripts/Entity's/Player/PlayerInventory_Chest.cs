using Mirror;
using UnityEngine;
using UnityEngine.UI;

public partial class PlayerInventory
{
    private bool chestOpen,chestPending,chestCloseRequested,chestNetworkSession;
    private int queuedChestCollection=-1;
    private int[] queuedChestDrag;
    private bool queuedChestDragRight;
    private int chestSequence;
    private Vector3Int chestPosition;
    private RectTransform chestPanel;
    private readonly Slot[] chestSlots=new Slot[27];
    private readonly RectTransform[] chestRects=new RectTransform[27];
    public void RequestChest(Vector3Int position)
    {
        if(DropRequestPending||chestPending||chestOpen||furnacePending||furnaceOpen||isOpen)return;
        ReadStorage();chestPosition=position;
        chestNetworkSession=(NetworkClient.active&&!NetworkServer.active);OpenInventory();
        SendChest(new ChestRequest{action=0,position=position,inventory=(InventoryStack[])storageBuffer.Clone(),cursor=Stack(pickedUpSlot),itemWearRevision=player!=null?player.ItemWearRevision:0});
    }
    private void SendChest(ChestRequest request)
    {
        chestPending=true;
        if((NetworkClient.active&&!NetworkServer.active))NetworkClient.Send(request);
        else
        {
            var world=player.CurrentWorld;
            bool accessible=world!=null&&world.ChestAccessible(transform.position+Vector3.up*1.6f,request.position,player.reach);
            if(world==null){chestPending=false;return;}
            if(world.isSaving&&request.action!=2)
            {ReceiveChestReply(new ChestReply{error="World is saving; try the chest again in a moment."});return;}
            var reply=world.chests.Process(world.worldName,-1,request,accessible);
            if(reply.accepted&&!reply.dropped.Empty)player.SpawnMenuDrop(reply.dropped);
            ReceiveChestReply(reply);
            if(reply.accepted&&!reply.opened)world.SaveWorld();
        }
    }
    public void ApplyChestPlayerSnapshot(InventoryStack[] inventory,InventoryStack cursor)
    {
        if(inventory==null||inventory.Length!=36)return;
        InitializeSlotData();
        for(int i=0;i<36;i++)WriteStack(i,inventory[i]);
        SetCursor(cursor);
    }
    public void ReceiveChestReply(ChestReply reply)
    {
        chestPending=false;
        if(!reply.accepted)
        {
            Debug.LogWarning(reply.error);
            if(!chestOpen||chestCloseRequested){chestCloseRequested=false;CloseInventory();}
            return;
        }
        ApplyChestPlayerSnapshot(reply.inventory,reply.cursor);chestSequence=reply.sequence;
        if(!reply.opened){EndChestPanel();chestCloseRequested=false;CloseInventory();return;}
        if(!chestOpen){chestOpen=true;chestPosition=reply.position;BuildChestPanel();OpenInventory();}
        for(int i=0;i<27;i++)
        {
            var stack=reply.chest[i];var slot=chestSlots[i];
            ClearSlotIcon(chestRects[i]);CreateItemIcon(stack.type,chestRects[i]);
            slot.type=stack.type;slot.damage=stack.damage;slot.SetCount(stack.count);
            DrawDurability(chestRects[i],stack);
        }
        if(queuedChestCollection>=0&&!chestCloseRequested)
        {int index=queuedChestCollection;queuedChestCollection=-1;CollectChest(index);return;}
        if(queuedChestDrag!=null&&!chestCloseRequested)
        {var indices=queuedChestDrag;bool right=queuedChestDragRight;queuedChestDrag=null;DragChest(indices,right);return;}
        if(chestCloseRequested)CloseInventory();
    }
    private void BuildChestPanel()
    {
        chestPanel=CreativeRect("ChestContainer",creativeRoot,inventoryRect.anchoredPosition.x,0,176,168);
        var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        if(assets!=null&&assets.chestContainer!=null)
        {
            var upper=CreativeRect("ChestBackground",chestPanel,0,48,176,72).gameObject.AddComponent<RawImage>();
            upper.texture=assets.chestContainer;upper.uvRect=new Rect(0,1-72/256f,176/256f,72/256f);upper.raycastTarget=false;
            var lower=CreativeRect("PlayerBackground",chestPanel,0,-36,176,96).gameObject.AddComponent<RawImage>();
            lower.texture=assets.chestContainer;lower.uvRect=new Rect(0,1-222/256f,176/256f,96/256f);lower.raycastTarget=false;
        }
        else Panel(chestPanel);
        for(int i=0;i<27;i++)
        {
            var rect=CreativeRect("ChestSlot"+i,chestPanel,-72+i%9*18,58-i/9*18,18,18);
            if(assets!=null&&assets.chestContainer!=null)rect.gameObject.AddComponent<Image>().color=Color.clear;
            else SlotBackground(rect);
            var input=rect.gameObject.AddComponent<InventorySlot>();input.playerInventory=this;input.slotIndex=100+i;
            chestRects[i]=rect;chestSlots[i]=new Slot(100+i){countText=SlotCount(rect)};
        }
        ContainerLabel("ChestTitle",chestPanel,8,6,"Chest");
        ContainerLabel("PlayerTitle",chestPanel,8,74,"Inventory");
        inventorySlotsRect.SetParent(chestPanel,false);
        inventoryRect.gameObject.SetActive(false);creativePanel.gameObject.SetActive(false);
        inventoryHighlightRect.SetAsLastSibling();pickedUpItemRect.SetAsLastSibling();tooltipRect.SetAsLastSibling();
    }
    private void ClickChest(int index,bool right,bool shift)
    {
        if(chestPending)return;
        int target=index>=100?index-100:index>=0&&index<36?27+index:-1;
        if(target<0||target>=63)return;
        SendChest(new ChestRequest{action=1,position=chestPosition,slot=target,right=right,shift=shift,sequence=chestSequence+1});
    }
    private void DragChest(int[] indices,bool right)
    {
        if(chestPending){queuedChestDrag=(int[])indices.Clone();queuedChestDragRight=right;return;}var targets=new int[indices.Length];
        for(int i=0;i<indices.Length;i++)targets[i]=indices[i]>=100?indices[i]-100:indices[i]+27;
        SendChest(new ChestRequest{action=4,position=chestPosition,slots=targets,right=right,sequence=chestSequence+1});
    }
    private void CollectChest(int index)
    {
        if(chestPending){queuedChestCollection=index;return;}int target=index>=100?index-100:index+27;
        SendChest(new ChestRequest{action=5,position=chestPosition,slot=target,sequence=chestSequence+1});
    }
    private void DropChestCursor(bool single)
    {if(!chestPending)SendChest(new ChestRequest{action=6,position=chestPosition,right=single,sequence=chestSequence+1});}
    private bool TryCloseChest()
    {
        if(chestPending){chestCloseRequested=true;return true;}
        if(!chestOpen)return false;
        SendChest(new ChestRequest{action=2,position=chestPosition});return true;
    }
    private void EndChestPanel()
    {
        chestOpen=false;queuedChestCollection=-1;queuedChestDrag=null;HideSlotHover();
        inventorySlotsRect.SetParent(inventoryRect,false);
        inventoryRect.gameObject.SetActive(true);
        if(chestPanel!=null){chestPanel.gameObject.SetActive(false);Destroy(chestPanel.gameObject);chestPanel=null;}
    }
    private void UpdateChestPanel()
    {
        if(!chestOpen)return;
        if(chestNetworkSession&&!(NetworkClient.active&&!NetworkServer.active))
        {chestPending=false;EndChestPanel();CloseInventory();return;}
        chestPanel.anchoredPosition=new Vector2(inventoryRect.anchoredPosition.x,0);
        if(!(NetworkClient.active&&!NetworkServer.active)&&!player.CurrentWorld.ChestAccessible(transform.position+Vector3.up*1.6f,chestPosition,player.reach))
            ReceiveChestReply(player.CurrentWorld.chests.Close(-1));
    }
    private void ReleaseChest()
    {
        if(!chestOpen)return;
        if((NetworkClient.active&&!NetworkServer.active))NetworkClient.Send(new ChestRequest{action=2,position=chestPosition});
        else if(player.CurrentWorld!=null)player.CurrentWorld.chests.Close(-1);
    }
}

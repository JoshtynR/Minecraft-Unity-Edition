using Mirror;
using UnityEngine;
using UnityEngine.UI;

public partial class PlayerInventory
{
    private bool chestOpen,chestPending,chestCloseRequested,chestNetworkSession;
    private int chestSequence;
    private Vector3Int chestPosition;
    private RectTransform chestPanel;
    private readonly Slot[] chestSlots=new Slot[27];
    private readonly RectTransform[] chestRects=new RectTransform[27];
    public void RequestChest(Vector3Int position)
    {
        if(chestPending||chestOpen||isOpen)return;
        ReadStorage();chestPosition=position;
        chestNetworkSession=NetworkClient.active;OpenInventory();
        SendChest(new ChestRequest{action=0,position=position,inventory=(InventoryStack[])storageBuffer.Clone(),cursor=Stack(pickedUpSlot)});
    }
    private void SendChest(ChestRequest request)
    {
        chestPending=true;
        if(NetworkClient.active)NetworkClient.Send(request);
        else
        {
            var world=World.Instance;
            bool accessible=world!=null&&world.ChestAccessible(transform.position+Vector3.up*1.6f,request.position,player.reach);
            if(world==null){chestPending=false;return;}
            if(world.isSaving&&request.action!=2)
            {ReceiveChestReply(new ChestReply{error="World is saving; try the chest again in a moment."});return;}
            var reply=world.chests.Process(world.worldName,-1,request,accessible);
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
        }
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
        CreativeText("ChestTitle",chestPanel,-48,73,64,12,"Chest",8,new Color(.25f,.25f,.25f,1));
        CreativeText("PlayerTitle",chestPanel,-48,10,64,12,"Inventory",8,new Color(.25f,.25f,.25f,1));
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
    private bool TryCloseChest()
    {
        if(chestPending){chestCloseRequested=true;return true;}
        if(!chestOpen)return false;
        SendChest(new ChestRequest{action=2,position=chestPosition});return true;
    }
    private void EndChestPanel()
    {
        chestOpen=false;HideSlotHover();
        inventorySlotsRect.SetParent(inventoryRect,false);
        inventoryRect.gameObject.SetActive(true);creativePanel.gameObject.SetActive(true);
        if(chestPanel!=null){chestPanel.gameObject.SetActive(false);Destroy(chestPanel.gameObject);chestPanel=null;}
    }
    private void UpdateChestPanel()
    {
        if(!chestOpen)return;
        if(chestNetworkSession&&!NetworkClient.active)
        {chestPending=false;EndChestPanel();CloseInventory();return;}
        chestPanel.anchoredPosition=new Vector2(inventoryRect.anchoredPosition.x,0);
        if(!NetworkClient.active&&!World.Instance.ChestAccessible(transform.position+Vector3.up*1.6f,chestPosition,player.reach))
            ReceiveChestReply(World.Instance.chests.Close(-1));
    }
    private void ReleaseChest()
    {
        if(!chestOpen)return;
        if(NetworkClient.active)NetworkClient.Send(new ChestRequest{action=2,position=chestPosition});
        else if(World.Instance!=null)World.Instance.chests.Close(-1);
    }
}

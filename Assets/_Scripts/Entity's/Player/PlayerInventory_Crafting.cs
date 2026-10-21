using Mirror;
using UnityEngine;
using UnityEngine.UI;

public partial class PlayerInventory
{
    private bool tableOpen,tableNetworkSession;
    private Vector3Int tablePosition;
    private RectTransform tablePanel,survivalCraftResultRect;
    private readonly Slot[] tableSlots=new Slot[9];
    private readonly RectTransform[] tableRects=new RectTransform[9];

    public void OpenCraftingTable(Vector3Int position)
    {
        if(DropRequestPending||creativeRoot==null||isOpen||chestOpen||chestPending||furnacePending||furnaceOpen||(player!=null&&player.health<=0))return;
        tablePosition=position;
        if(!TableAccessible())return;
        InitializeSlotData();
        tableOpen=true;tableNetworkSession=NetworkClient.active;
        BuildCraftingTable();
        OpenInventory();
    }

    private bool TableAccessible()
    {
        var world=player.CurrentWorld;
        if(world==null||(player!=null&&player.health<=0))return false;
        var block=world.GetBlock(tablePosition);
        // ContainerWorkbench remains usable within eight blocks of its center.
        return block!=null&&block.type==BlockType.CraftingTable&&
            (transform.position-((Vector3)tablePosition+Vector3.one*.5f)).sqrMagnitude<=64;
    }

    private void BuildCraftingTable()
    {
        tablePanel=CreativeRect("CraftingTableContainer",creativeRoot,inventoryRect.anchoredPosition.x,0,176,166);
        var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        bool textured=assets!=null&&assets.craftingTable!=null;
        if(textured)
        {
            var background=tablePanel.gameObject.AddComponent<RawImage>();
            background.texture=assets.craftingTable;
            background.uvRect=new Rect(0,1-166/256f,176/256f,166/256f);
            background.raycastTarget=false;
        }
        else
        {
            Panel(tablePanel);
            CreativeText("CraftArrow",tablePanel,15,40,20,16,">",13,new Color(.45f,.45f,.45f,1));
        }
        for(int i=0;i<9;i++)
        {
            // ContainerWorkbench: upper-left input at (30,17), 18-pixel pitch.
            var rect=CreativeRect("TableCraft"+i,tablePanel,-50+i%3*18,58-i/3*18,18,18);
            if(textured)rect.gameObject.AddComponent<Image>().color=Color.clear;
            else SlotBackground(rect);
            var input=rect.gameObject.AddComponent<InventorySlot>();input.playerInventory=this;input.slotIndex=200+i;
            tableRects[i]=rect;tableSlots[i].countText=SlotCount(rect);
            CreateItemIcon(tableSlots[i].type,rect);tableSlots[i].SetCount(tableSlots[i].count);
        }
        ContainerLabel("CraftingTitle",tablePanel,28,6,"Crafting");
        ContainerLabel("InventoryTitle",tablePanel,8,72,"Inventory");
        inventorySlotsRect.SetParent(tablePanel,false);
        survivalCraftResultRect=craftResultRect;
        craftResultRect=CreativeRect("TableResult",tablePanel,44,40,18,18);
        if(textured)craftResultRect.gameObject.AddComponent<Image>().color=Color.clear;
        else SlotBackground(craftResultRect);
        var resultInput=craftResultRect.gameObject.AddComponent<InventorySlot>();resultInput.playerInventory=this;resultInput.slotIndex=40;
        craftedOutput.countText=SlotCount(craftResultRect);
        CreateItemIcon(craftedOutput.type,craftResultRect);craftedOutput.SetCount(craftedOutput.count);
        inventoryRect.gameObject.SetActive(false);
        inventoryHighlightRect.SetAsLastSibling();pickedUpItemRect.SetAsLastSibling();tooltipRect.SetAsLastSibling();
    }

    private void EndCraftingTable(bool returnIngredients)
    {
        HideSlotHover();
        if(returnIngredients)
        {
            ReadStorage();
            for(int i=0;i<9;i++)
            {
                var stack=Stack(tableSlots[i]);
                int remaining=InventoryRules.Insert(storageBuffer,stack.type,stack.count,0,36,stack.damage);
                QueueRecovery(stack.type,remaining,stack.damage);
                tableSlots[i].type=ItemType.Nothing;tableSlots[i].damage=0;tableSlots[i].SetCount(0);
            }
            CommitStorage();
        }
        else for(int i=0;i<9;i++)tableSlots[i]=new Slot(200+i);
        tableOpen=false;
        inventorySlotsRect.SetParent(inventoryRect,false);
        craftResultRect=survivalCraftResultRect;
        craftedOutput.countText=craftResultRect.Find("Count").GetComponent<TMPro.TextMeshProUGUI>();
        ClearSlotIcon(craftResultRect);CreateItemIcon(craftedOutput.type,craftResultRect);
        craftedOutput.SetCount(craftedOutput.count);
        inventoryRect.gameObject.SetActive(true);
        if(tablePanel!=null){tablePanel.gameObject.SetActive(false);Destroy(tablePanel.gameObject);tablePanel=null;}
        for(int i=0;i<9;i++)tableRects[i]=null;
    }

    private void UpdateCraftingTable()
    {
        if(!tableOpen)return;
        if((tableNetworkSession&&!NetworkClient.active)||!TableAccessible()){CloseInventory();return;}
        tablePanel.anchoredPosition=new Vector2(inventoryRect.anchoredPosition.x,0);
    }
}

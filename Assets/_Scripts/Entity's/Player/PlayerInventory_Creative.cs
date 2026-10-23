using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class PlayerInventory
{
    private readonly List<ItemType> creativeBlocks = new List<ItemType>();
    private readonly List<RectTransform> creativeCells = new List<RectTransform>();
    private readonly RectTransform[] craftingRects = new RectTransform[4];
    private readonly RectTransform[] equipmentRects = new RectTransform[4],equipmentGhosts = new RectTransform[4];
    private RectTransform creativeRoot, creativePanel, inventoryBackdrop, craftResultRect, tooltipRect;
    private TextMeshProUGUI creativePageText, creativeTitleText, creativeNameText;
    private TMP_FontAsset creativeFont;
    private Material inventoryTextMaterial,inventoryLabelMaterial,containerLabelMaterial;
    private Button previousPage, nextPage, recoveryButton;
    private TextMeshProUGUI recoveryText;
    private int creativePage, creativePageSize;
    private float lastMenuWidth=-1,lastMenuHeight=-1;
    private int hoveredSlot=-1;
    private bool hoveredCatalog;
    private RectTransform hoveredRect;
    private ItemType tooltipType=ItemType.Nothing;
    private bool texturedInventory;
    private bool tooManyItemsEnabled; // Opt in with F1; never change the survival inventory itself.
    private bool lastSidebarVisible;

    private void BuildCreativeInventory(Transform parent)
    {
        creativeBlocks.Clear();
        foreach(var item in ItemCatalog.All)if(!WheatCropRules.IsLegacy((BlockType)(int)item.id))creativeBlocks.Add(item.id);
        creativeBlocks.Sort((a,b)=>((int)a).CompareTo((int)b));
        var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        creativeFont=assets!=null&&assets.levelFont!=null?assets.levelFont:SlotTextPrefab.GetComponent<TextMeshProUGUI>().font;
        BuildInventoryTextMaterials();
        creativeRoot=CreativeRect("InventoryMenu",parent,0,0,640,360);
        creativeRoot.localPosition=new Vector3(0,0,-35);
        var backdrop=CreativeRect("DimBackground",creativeRoot,0,0,640,360);
        backdrop.localPosition=new Vector3(0,0,10);
        var dim=backdrop.gameObject.AddComponent<Image>();dim.color=new Color(0,0,0,.48f);
        var outside=backdrop.gameObject.AddComponent<InventoryOutsideClick>();outside.playerInventory=this;
        inventoryBackdrop=CreativeRect("InventoryBackdrop",creativeRoot,0,-2,182,172);
        var inventoryShade=inventoryBackdrop.gameObject.AddComponent<Image>();inventoryShade.color=new Color(0,0,0,.58f);inventoryShade.raycastTarget=false;
        inventoryRect=CreativeRect("SurvivalInventory",creativeRoot,0,0,176,166);
        texturedInventory=assets!=null&&assets.inventory!=null;
        if(texturedInventory)
        {
            var background=inventoryRect.gameObject.AddComponent<RawImage>();
            background.texture=assets.inventory;
            // Atlas coordinates are based on the classic 256x256 layout, including HD multiples.
            background.uvRect=new Rect(0,1-166/256f,176/256f,166/256f);
            background.raycastTarget=false;
        }
        else Panel(inventoryRect);
        inventorySlotsRect=CreativeRect("Storage",inventoryRect,0,0,176,166);
        for(int i=0;i<36;i++)
            slots[i].countText=InventoryCell("Slot"+i,inventorySlotsRect,TmiLayout.SlotX(i),TmiLayout.SlotY(i),i);
        for(int i=0;i<4;i++)
        {
            craftingSlots[i].countText=InventoryCell("Craft"+i,inventoryRect,18+(i%2)*18,57-(i/2)*18,36+i);
            craftingRects[i]=craftingSlots[i].countText.transform.parent.GetComponent<RectTransform>();
        }
        craftedOutput.countText=InventoryCell("CraftResult",inventoryRect,74,47,40);
        craftResultRect=craftedOutput.countText.transform.parent.GetComponent<RectTransform>();
        // The supplied inventory sheet has its 2x2 grid at (98,18).
        ContainerLabel("CraftingLabel",inventoryRect,97,8,"Crafting");
        if(!texturedInventory)CreativeText("CraftArrow",inventoryRect,55,47,18,16,">",13,new Color(.45f,.45f,.45f,1));
        var preview=CreativeRect("PlayerPreview",inventoryRect,-37,40,50,70);
        if(!texturedInventory)preview.gameObject.AddComponent<Image>().color=new Color(.05f,.05f,.05f,1);
        BuildInventoryPreview(preview,assets);
        for(int i=0;i<4;i++)
        {
            equipmentSlots[i].countText=InventoryCell("Armor"+i,inventoryRect,-72,67-i*18,41+i);
            equipmentRects[i]=equipmentSlots[i].countText.transform.parent.GetComponent<RectTransform>();
            equipmentGhosts[i]=CreativeRect("EmptyEquipment",equipmentRects[i],0,0,16,16);
            ArmorHint(equipmentGhosts[i],i);
        }
        recoveryButton=CreativeButton("Recover saved items",inventoryRect,0,-95,176,16,RecoverSavedItems);
        recoveryText=recoveryButton.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        recoveryButton.gameObject.SetActive(false);
        creativePanel=CreativeRect("TooManyItemsSidebar",creativeRoot,225,0,162,360);
        var catalogShade=creativePanel.gameObject.AddComponent<Image>();catalogShade.color=new Color(0,0,0,.58f);catalogShade.raycastTarget=false;
        creativePanel.gameObject.SetActive(false);
        creativeTitleText=CreativeText("Title",creativePanel,0,170,150,16,"Too Many Items",8,Color.white);
        previousPage=CreativeWidgetButton("<",creativePanel,-65,-170,20,20,assets!=null?assets.guiWidgets:null,()=>ChangeCreativePage(-1));
        nextPage=CreativeWidgetButton(">",creativePanel,65,-170,20,20,assets!=null?assets.guiWidgets:null,()=>ChangeCreativePage(1));
        creativePageText=CreativeText("Page",creativePanel,0,-170,100,16,"",8,Color.white);
        inventoryHighlightRect=CreativeRect("SlotHighlight",creativeRoot,0,0,16,16);
        var highlight=inventoryHighlightRect.gameObject.AddComponent<Image>();
        highlight.color=new Color(1,1,1,.35f);highlight.raycastTarget=false;
        inventoryHighlightRect.gameObject.SetActive(false);
        pickedUpItemRect=CreativeRect("CarriedStack",creativeRoot,0,0,18,18);
        pickedUpItemRect.localPosition=new Vector3(0,0,-65);
        SlotCount(pickedUpItemRect);
        tooltipRect=CreativeRect("ItemTooltip",creativeRoot,0,0,110,17);
        tooltipRect.localPosition=new Vector3(0,0,-90);
        var tooltip=tooltipRect.gameObject.AddComponent<Image>();tooltip.color=new Color(.06f,.01f,.1f,.96f);tooltip.raycastTarget=false;
        creativeNameText=CreativeText("Name",tooltipRect,0,0,104,15,"",8,Color.white);
        tooltipRect.gameObject.SetActive(false);
        UpdateInventoryLayout();
    }

    private void UpdateInventoryLayout()
    {
        float width=canvasRect.rect.width,height=canvasRect.rect.height;
        bool sidebarVisible=tooManyItemsEnabled&&!chestOpen&&!furnaceOpen;
        creativePanel.gameObject.SetActive(sidebarVisible);
        if(width!=lastMenuWidth||height!=lastMenuHeight||sidebarVisible!=lastSidebarVisible)
        {
            lastMenuWidth=width;lastMenuHeight=height;lastSidebarVisible=sidebarVisible;
            var layout=TmiLayout.Calculate(width,height);
            creativeRoot.sizeDelta=new Vector2(layout.width,layout.height);
            creativeRoot.localScale=Vector3.one*layout.scale;
            creativeRoot.GetChild(0).GetComponent<RectTransform>().sizeDelta=creativeRoot.sizeDelta;
            inventoryRect.anchoredPosition=new Vector2(sidebarVisible?layout.inventoryX:0,0);
            inventoryBackdrop.anchoredPosition=new Vector2(inventoryRect.anchoredPosition.x,inventoryRect.anchoredPosition.y-2);
            creativePanel.anchoredPosition=new Vector2(layout.sidebarX,0);
            creativePanel.sizeDelta=new Vector2(layout.columns*18+8,layout.height);
            creativeTitleText.rectTransform.anchoredPosition=new Vector2(0,layout.height/2-11);
            creativeTitleText.rectTransform.sizeDelta=new Vector2(layout.columns*18,16);
            previousPage.GetComponent<RectTransform>().anchoredPosition=new Vector2(-layout.columns*9+10,-layout.height/2+12);
            nextPage.GetComponent<RectTransform>().anchoredPosition=new Vector2(layout.columns*9-10,-layout.height/2+12);
            creativePageText.rectTransform.anchoredPosition=new Vector2(0,-layout.height/2+12);
            creativePageText.rectTransform.sizeDelta=new Vector2(Math.Max(20,layout.columns*18-44),16);
            creativePageSize=Math.Max(1,Math.Min(layout.PageSize,creativeBlocks.Count));
            // F1 is off by default. Build catalog cells/icons only when the
            // sidebar is actually shown, rather than during player startup.
            if(sidebarVisible)
            {
                while(creativeCells.Count<creativePageSize)
                {
                    int index=creativeCells.Count;
                    var cell=CreativeRect("Catalog"+index,creativePanel,0,0,18,18);
                    var hit=cell.gameObject.AddComponent<Image>();hit.color=Color.clear;
                    var input=cell.gameObject.AddComponent<InventorySlot>();input.playerInventory=this;input.slotIndex=index;input.isCatalog=true;
                    creativeCells.Add(cell);
                }
                for(int i=0;i<creativeCells.Count;i++)
                {
                    creativeCells[i].anchoredPosition=new Vector2(-layout.columns*9+9+(i%layout.columns)*18,layout.height/2-32-(i/layout.columns)*18);
                    creativeCells[i].gameObject.SetActive(i<creativePageSize);
                }
                creativePage=Math.Min(creativePage,Math.Max(0,(creativeBlocks.Count-1)/creativePageSize));
                RefreshCreativePage();
            }
        }
        UpdateSlotTooltip();
        UpdateChestPanel();
        UpdateCraftingTable();
        UpdateFurnacePanel();
    }

    private void ToggleTooManyItems()
    {
        tooManyItemsEnabled=!tooManyItemsEnabled;
        HideSlotHover();
        UpdateInventoryLayout();
    }

    public void ClickOutsideInventory(UnityEngine.EventSystems.PointerEventData data)
    {
        if(!isOpen||pickedUpSlot==null||data==null)return;
        bool right=data.button==UnityEngine.EventSystems.PointerEventData.InputButton.Right;
        if(!right&&data.button!=UnityEngine.EventSystems.PointerEventData.InputButton.Left)return;
        Camera camera=inventoryCanvas.renderMode==RenderMode.ScreenSpaceOverlay?null:inventoryCanvas.worldCamera;
        RectTransform panel=chestOpen?chestPanel:furnaceOpen?furnacePanel:tableOpen?tablePanel:inventoryRect;
        if(panel!=null&&RectTransformUtility.RectangleContainsScreenPoint(panel,Input.mousePosition,camera))return;
        if(creativePanel!=null&&creativePanel.gameObject.activeSelf&&RectTransformUtility.RectangleContainsScreenPoint(creativePanel,Input.mousePosition,camera))return;
        inventoryDragActive=false;draggedSlots.Clear();
        if(chestOpen)DropChestCursor(right);
        else if(furnaceOpen)DropFurnaceCursor(right);
        else if(player!=null)player.RequestInventoryCursorDrop(right);
    }

    private void ChangeCreativePage(int delta)
    {
        creativePage=Math.Max(0,Math.Min(Math.Max(0,(creativeBlocks.Count-1)/creativePageSize),creativePage+delta));
        RefreshCreativePage();
    }
    private void RefreshCreativePage()
    {
        HideSlotHover();
        int pages=Math.Max(1,(creativeBlocks.Count+creativePageSize-1)/creativePageSize);
        creativePageText.text=(creativePage+1)+" / "+pages;
        previousPage.interactable=creativePage>0;nextPage.interactable=creativePage+1<pages;
        for(int i=0;i<creativeCells.Count;i++)
        {
            var cell=creativeCells[i];ClearSlotIcon(cell);
            int index=creativePage*creativePageSize+i;
            bool active=i<creativePageSize&&index<creativeBlocks.Count;
            cell.gameObject.SetActive(active);
            if(active)CreateItemIcon(creativeBlocks[index],cell);
        }
    }
    public void GiveCatalogItem(int cell,bool single)
    {
        if(!tooManyItemsEnabled||chestPending||chestOpen||furnacePending||furnaceOpen)return;
        int index=creativePage*creativePageSize+cell;
        if(cell<0||cell>=creativePageSize||index>=creativeBlocks.Count)return;
        AddUpTo(creativeBlocks[index],single?1:ItemCatalog.StackLimit(creativeBlocks[index]));
    }
    public void HoverSlot(RectTransform rect,int index,bool catalog)
    {
        hoveredRect=rect;hoveredSlot=index;hoveredCatalog=catalog;tooltipType=ItemType.Nothing;
        inventoryHighlightRect.position=rect.position;
        var p=inventoryHighlightRect.localPosition;p.z=-45;inventoryHighlightRect.localPosition=p;
        inventoryHighlightRect.gameObject.SetActive(true);
        UpdateSlotTooltip();
    }
    public void HideSlotHover()
    {
        hoveredRect=null;hoveredSlot=-1;
        if(inventoryHighlightRect!=null)inventoryHighlightRect.gameObject.SetActive(false);
        if(tooltipRect!=null)tooltipRect.gameObject.SetActive(false);
    }
    private void UpdateSlotTooltip()
    {
        if(hoveredRect==null||hoveredSlot<0)return;
        int index=creativePage*creativePageSize+hoveredSlot;
        var slot=hoveredCatalog?null:GetSlot(hoveredSlot);
        ItemType type=hoveredCatalog?(index<creativeBlocks.Count?creativeBlocks[index]:ItemType.Nothing):slot==null?ItemType.Nothing:slot.type;
        tooltipRect.gameObject.SetActive(type!=ItemType.Nothing);
        if(type==ItemType.Nothing)return;
        if(type!=tooltipType)
        {
            tooltipType=type;
            var definition=ItemCatalog.Get(type);
            creativeNameText.text=definition==null||string.IsNullOrEmpty(definition.displayName)?type.ToString():definition.displayName;
            float width=Math.Max(48,creativeNameText.text.Length*5+10);
            tooltipRect.sizeDelta=new Vector2(width,17);
            creativeNameText.rectTransform.sizeDelta=new Vector2(width-8,15);
        }
        Vector2 point;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(creativeRoot,Input.mousePosition,
            inventoryCanvas.renderMode==RenderMode.ScreenSpaceOverlay?null:inventoryCanvas.worldCamera,out point))
        {
            float x=Mathf.Clamp(point.x+tooltipRect.sizeDelta.x/2+10,-creativeRoot.sizeDelta.x/2+tooltipRect.sizeDelta.x/2,creativeRoot.sizeDelta.x/2-tooltipRect.sizeDelta.x/2);
            float y=Mathf.Clamp(point.y+15,-creativeRoot.sizeDelta.y/2+10,creativeRoot.sizeDelta.y/2-10);
            tooltipRect.anchoredPosition=new Vector2(x,y);
        }
    }
    private void UpdateRecoveryNotice()
    {
        if(recoveryButton==null)return;
        recoveryButton.gameObject.SetActive(recoveryItems.Count>0);
        recoveryText.text="Recover "+recoveryItems.Count+" saved stacks";
    }

    private TextMeshProUGUI InventoryCell(string name,Transform parent,float x,float y,int index)
    {
        var rect=CreativeRect(name,parent,x,y,18,18);
        // The atlas already contains the bevels. Keep an invisible raycast target for input.
        if(texturedInventory)rect.gameObject.AddComponent<Image>().color=Color.clear;
        else SlotBackground(rect);
        var input=rect.gameObject.AddComponent<InventorySlot>();input.playerInventory=this;input.slotIndex=index;
        return SlotCount(rect);
    }
    private TextMeshProUGUI SlotCount(RectTransform parent)
    {
        var text=CreativeText("Count",parent,0,-5,18,8,"",8,Color.white);
        ConfigureCountText(text);
        return text;
    }
    private void ConfigureCountText(TextMeshProUGUI text)
    {
        text.gameObject.name="Count";
        text.font=creativeFont;text.fontSize=8;text.color=Color.white;
        if(inventoryTextMaterial!=null)text.fontSharedMaterial=inventoryTextMaterial;
        text.enableWordWrapping=false;text.enableAutoSizing=false;text.extraPadding=true;
        text.margin=Vector4.zero;text.raycastTarget=false;
        text.alignment=TextAlignmentOptions.BottomRight;
        text.rectTransform.sizeDelta=new Vector2(18,8);
        text.rectTransform.localPosition=new Vector3(0,-5,-30);
        text.transform.SetAsLastSibling();
    }
    private static void RaiseSlotCount(Transform parent)
    {
        var count=parent.Find("Count");if(count!=null)count.SetAsLastSibling();
    }
    private void BuildInventoryTextMaterials()
    {
        inventoryLabelMaterial=MinecraftUIText.CreateMaterial(creativeFont,8,false);
        inventoryTextMaterial=MinecraftUIText.CreateMaterial(creativeFont,8,true);
        containerLabelMaterial=MinecraftUIText.CreateMaterial(creativeFont,8,false);
        if(containerLabelMaterial!=null)
        {
            var gray=new Color(64/255f,64/255f,64/255f,1);
            containerLabelMaterial.SetColor("_FaceColor",QualitySettings.activeColorSpace==ColorSpace.Linear?gray.linear:gray);
        }
    }
    private void SlotBackground(RectTransform rect)
    {
        rect.gameObject.AddComponent<Image>().color=new Color(.22f,.22f,.22f,1);
        var inner=CreativeRect("Inset",rect,1,-1,16,16);
        var image=inner.gameObject.AddComponent<Image>();image.color=new Color(.55f,.55f,.55f,1);image.raycastTarget=false;
        var edge=CreativeRect("LightEdge",rect,1,-8,16,1);
        var light=edge.gameObject.AddComponent<Image>();light.color=Color.white;light.raycastTarget=false;
        edge=CreativeRect("RightEdge",rect,8,-1,1,16);
        light=edge.gameObject.AddComponent<Image>();light.color=Color.white;light.raycastTarget=false;
    }
    private void ArmorHint(RectTransform parent,int part)
    {
        // Empty-equipment silhouettes; the current registry has no armor items.
        // Beta's pumpkin-only helmet exception is handled by InventoryRules.
        string[][] pixels={
            new[]{"01111110","11111111","11000011","11000011","11000011","00000000","00000000","00000000"},
            new[]{"11000011","11111111","11111111","01111110","01111110","01111110","01111110","00000000"},
            new[]{"01111110","01111110","01111110","01100110","01100110","01100110","01100110","00000000"},
            new[]{"00000000","00000000","01100110","01100110","01100110","11100111","11100111","00000000"}};
        for(int y=0;y<8;y++)for(int x=0;x<8;x++)if(pixels[part][y][x]=='1')
        {
            var pixel=CreativeRect("EquipmentPixel",parent,x*1.5f-5.25f,5.25f-y*1.5f,1.5f,1.5f);
            var image=pixel.gameObject.AddComponent<Image>();image.color=new Color(.43f,.43f,.43f,1);image.raycastTarget=false;
        }
    }
    private void Panel(RectTransform rect)
    {
        rect.gameObject.AddComponent<Image>().color=new Color(.12f,.12f,.12f,1);
        var light=CreativeRect("Bevel",rect,-1,1,Math.Max(1,rect.sizeDelta.x-2),Math.Max(1,rect.sizeDelta.y-2));
        var image=light.gameObject.AddComponent<Image>();image.color=Color.white;image.raycastTarget=false;
        var fill=CreativeRect("Face",rect,0,0,Math.Max(1,rect.sizeDelta.x-6),Math.Max(1,rect.sizeDelta.y-6));
        image=fill.gameObject.AddComponent<Image>();image.color=new Color(.776f,.776f,.776f,1);image.raycastTarget=false;
    }
    private TextMeshProUGUI ContainerLabel(string name,RectTransform parent,float x,float y,string value)
    {
        const float width=80,height=10;
        var text=CreativeText(name,parent,x-parent.sizeDelta.x/2+width/2,parent.sizeDelta.y/2-y-height/2,
            width,height,value,8,Color.white);
        // Keep vertices white and supply #404040 through the SDF face material.
        // This avoids washed-out text in linear rendering and Canvas tint conversion differences.
        if(containerLabelMaterial!=null)text.fontSharedMaterial=containerLabelMaterial;
        text.alignment=TextAlignmentOptions.TopLeft;
        text.margin=Vector4.zero;
        return text;
    }
    private RectTransform CreativeRect(string name,Transform parent,float x,float y,float width,float height)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);return rect;
    }
    private TextMeshProUGUI CreativeText(string name,Transform parent,float x,float y,float width,float height,string value,float size,Color color)
    {
        var rect=CreativeRect(name,parent,x,y,width,height);
        var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=creativeFont;text.fontSize=size;text.text=value;
        var material=color.r+color.g+color.b>1?inventoryTextMaterial:inventoryLabelMaterial;
        if(material!=null)text.fontSharedMaterial=material;
        text.extraPadding=true;
        text.color=color;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.enableWordWrapping=false;return text;
    }
    private Button CreativeButton(string label,Transform parent,float x,float y,float width,float height,UnityEngine.Events.UnityAction action)
    {
        var rect=CreativeRect(label,parent,x,y,width,height);Panel(rect);
        var button=rect.gameObject.AddComponent<Button>();button.onClick.AddListener(action);
        button.onClick.AddListener(()=>GameAudio.PlayClip(GameAudio.Find("UI/click"),Vector3.zero,.2f,false));
        CreativeText("Label",rect,0,0,width,height,label,8,Color.white);return button;
    }

    private Button CreativeWidgetButton(string label,Transform parent,float x,float y,float width,float height,Texture2D widgets,UnityEngine.Events.UnityAction action)
    {
        var rect=CreativeRect(label,parent,x,y,width,height);
        Graphic target;
        if(widgets!=null)
        {
            var image=rect.gameObject.AddComponent<RawImage>();image.texture=widgets;
            // Classic widgets.png: normal button row starts at pixel (0,66), size 200x20.
            image.uvRect=new Rect(0,1-86/256f,200/256f,20/256f);target=image;
        }
        else {Panel(rect);target=rect.GetComponent<Image>();}
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=target;
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.12f,1.12f,1.12f,1);
        colors.pressedColor=new Color(.72f,.72f,.72f,1);colors.disabledColor=new Color(.45f,.45f,.45f,.75f);button.colors=colors;
        button.onClick.AddListener(action);button.onClick.AddListener(()=>GameAudio.PlayClip(GameAudio.Find("UI/click"),Vector3.zero,.2f,false));
        CreativeText("Label",rect,0,0,width,height,label,8,Color.white);return button;
    }
}

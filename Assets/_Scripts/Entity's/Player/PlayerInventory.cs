using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class PlayerInventory : MonoBehaviour
{
    private readonly Dictionary<BlockType, Mesh> iconMeshes = new Dictionary<BlockType, Mesh>();
    public GameObject UIBlockPrefab, SlotTextPrefab;
    public Slot[] slots = new Slot[InventoryRules.StorageSlots];
    private const int maxStackSize = InventoryRules.MaxStack;
    private Player player;
    private RectTransform hotbarRect, selectedSlotRect, inventoryRect, inventorySlotsRect, canvasRect, pickedUpItemRect;
    private Canvas inventoryCanvas;
    [HideInInspector] public RectTransform inventoryHighlightRect;
    public bool isOpen;
    [CanBeNull] public Slot pickedUpSlot;
    [HideInInspector] public int selectedSlotIndex;

    private void Awake() { InitializeSlotData(); }

    private void OnDestroy()
    {
        ReleaseChest();
        ReleaseFurnace();
        DisposeInventoryPreview();
        if (creativeRoot != null) Destroy(creativeRoot.gameObject);
        if(inventoryTextMaterial!=null)Destroy(inventoryTextMaterial);
        if(inventoryLabelMaterial!=null)Destroy(inventoryLabelMaterial);
        if(containerLabelMaterial!=null)Destroy(containerLabelMaterial);
        foreach (var mesh in iconMeshes.Values) if (mesh != null) Destroy(mesh);
        iconMeshes.Clear();
    }

    private void Start()
    {
        player = GetComponent<Player>();
        canvasRect = GameObject.Find("PlayerCanvas").GetComponent<RectTransform>();
        inventoryCanvas = canvasRect.GetComponent<Canvas>();
        var gameUI = canvasRect.Find("GameUI");
        PlayerSurvivalHUD.EnsureOn(gameUI);
        hotbarRect = gameUI.Find("Hotbar").GetComponent<RectTransform>();
        selectedSlotRect = hotbarRect.GetChild(0).GetComponent<RectTransform>();
        var previous = gameUI.Find("Inventory");
        if (previous != null) previous.gameObject.SetActive(false);
        InitializeSlotData();
        BuildCreativeInventory(gameUI);
        for (int i=0;i<9;i++)
        {
            var cell=hotbarRect.GetChild(i+1);
            var count=cell.Find("Count");if(count==null&&cell.childCount>0)count=cell.GetChild(0);
            slots[i].hotbarCountText=count!=null?count.GetComponent<TextMeshProUGUI>():null;
            if(slots[i].hotbarCountText==null)slots[i].hotbarCountText=SlotCount(cell.GetComponent<RectTransform>());
            ConfigureCountText(slots[i].hotbarCountText);
        }
        for(int i=0;i<36;i++){SetSlotItemType(i,slots[i].type);slots[i].SetCount(slots[i].count);}
        for(int i=0;i<4;i++){CreateItemIcon(craftingSlots[i].type,craftingRects[i]);craftingSlots[i].SetCount(craftingSlots[i].count);}
        for(int i=0;i<4;i++){CreateItemIcon(equipmentSlots[i].type,equipmentRects[i]);equipmentGhosts[i].gameObject.SetActive(equipmentSlots[i].count<=0);}
        RefreshPreviewEquipment();
        for(int i=0;i<36;i++)RefreshSlotDurability(i);
        for(int i=0;i<4;i++){RefreshSlotDurability(36+i);RefreshSlotDurability(41+i);}
        craftedOutput.type=ItemType.Nothing;RefreshCraftingOutput();
        creativeRoot.gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdateSelectedSlotUI();
        if (!isOpen)
            for (int i=0;i<9;i++) if (Input.GetKeyDown(KeyCode.Alpha1+i)) selectedSlotIndex=i;
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isOpen) CloseInventory();
            else if (player == null || !player.escapeMenuOpen) OpenInventory();
        }
        if (isOpen && Input.GetKeyDown(KeyCode.F1)) ToggleTooManyItems();
        if (isOpen) UpdateInventoryLayout();
        bool carried = isOpen && pickedUpSlot != null;
        pickedUpItemRect.gameObject.SetActive(carried);
        if (carried)
        {
            Vector2 point;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(creativeRoot, Input.mousePosition,
                inventoryCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : inventoryCanvas.worldCamera, out point))
                pickedUpItemRect.anchoredPosition = point;
        }
        if (isOpen) UpdateInventoryPreview();
    }

    public void OpenInventory()
    {
        if(DropRequestPending)return;
        isOpen = true;
        creativeRoot.gameObject.SetActive(true);
        HideSlotHover();
        UpdateInventoryLayout();
        RefreshCraftingOutput();
        UpdatePickedUpSlotVisual();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseInventory()
    {
        if(TryCloseChest())return;
        if(TryCloseFurnace())return;
        if(tableOpen)EndCraftingTable(true);
        ReturnTransientStacks();
        HideSlotHover();
        isOpen = false;
        creativeRoot.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UpdateSelectedSlotUI()
    {
        if (!isOpen && Input.GetAxis("Mouse ScrollWheel") < 0) selectedSlotIndex=(selectedSlotIndex+1)%9;
        else if (!isOpen && Input.GetAxis("Mouse ScrollWheel") > 0) selectedSlotIndex=(selectedSlotIndex+8)%9;
        selectedSlotIndex=Mathf.Clamp(selectedSlotIndex,0,8);
        selectedSlotRect.localPosition=new Vector3(selectedSlotIndex*20-80,-1,0);
    }

    private static void ClearSlotIcon(Transform parent)
    {
        for (int i=parent.childCount-1;i>=0;i--)
            if (parent.GetChild(i).GetComponent<MeshFilter>() != null || (parent.GetChild(i).gameObject.name == "ItemIcon" || parent.GetChild(i).gameObject.name == "DurabilityBar"))
            {
                var child=parent.GetChild(i);
                child.gameObject.SetActive(false);
                // Unity destroys at end of frame. Detach now so a replacement
                // durability bar cannot find and revive the pending old one.
                child.SetParent(null,false);
                Destroy(child.gameObject);
            }
    }

    public void SetSlotItemType(int index, ItemType type)
    {
        if (index<0 || index>=slots.Length) return;
        slots[index].type=type;
        if (index<9 && hotbarRect!=null)
        {
            var hotbar=hotbarRect.GetChild(index+1);
            ClearSlotIcon(hotbar);CreateItemIcon(type,hotbar);
        }
        if(inventorySlotsRect!=null)
        {
            var cell=inventorySlotsRect.GetChild(index);
            ClearSlotIcon(cell);CreateItemIcon(type,cell);
        }
    }

    public void UpdatePickedUpSlotVisual()
    {
        if(pickedUpItemRect==null)return;
        ClearSlotIcon(pickedUpItemRect);
        var text=pickedUpItemRect.Find("Count").GetComponent<TextMeshProUGUI>();
        text.text="";
        if (pickedUpSlot==null) return;
        pickedUpSlot.countText=text;
        CreateItemIcon(pickedUpSlot.type,pickedUpItemRect);
        pickedUpSlot.SetCount(pickedUpSlot.count);
        DrawDurability(pickedUpItemRect,Stack(pickedUpSlot));
    }

    private void CreateItemIcon(ItemType type, Transform parent)
    {
        var item=ItemCatalog.Get(type);if(item==null)return;
        BlockType block;
        if(item.icon==null&&ItemCatalog.TryPlacedBlock(type,out block)){CreateBlockMesh(block,parent);RaiseSlotCount(parent);return;}
        var rect=CreativeRect("ItemIcon",parent,0,0,16,16);
        rect.localPosition=new Vector3(0,0,-20);
        if(item.icon!=null)
        {
            var image=rect.gameObject.AddComponent<RawImage>();image.texture=item.icon;image.raycastTarget=false;
        }
        else
        {
            // Readable placeholder until the user supplies an individual image.
            var text=CreativeText("MissingImage",rect,0,0,16,16,type.ToString().Substring(0,Math.Min(2,type.ToString().Length)),7,Color.white);
            text.raycastTarget=false;
        }
        RaiseSlotCount(parent);
    }

    private void CreateBlockMesh(BlockType type, Transform parent)
    {
        if (type == BlockType.Nothing || type == BlockType.Air) return;
        
        GameObject blockItem = Instantiate(UIBlockPrefab, parent);
        blockItem.name = "InventoryIcon";
        blockItem.layer = parent.gameObject.layer;
        blockItem.transform.localScale = Vector3.one*10;
        blockItem.GetComponent<RectTransform>().localPosition = new Vector3(-7, -5, -20);
        if (iconMeshes.TryGetValue(type, out var cachedMesh))
        {
            blockItem.GetComponent<MeshFilter>().sharedMesh = cachedMesh;
            return;
        }
        var mesh = ItemVisualMesh.Build(type);
        iconMeshes.Add(type, mesh);
        blockItem.GetComponent<MeshFilter>().sharedMesh = mesh;
    }
    
    [Serializable]
    public class Slot
    {
        public ItemType type;
        public int count;
        public int damage;
        public int index;
        [NonSerialized]
        public TextMeshProUGUI countText;
        // do a separate text for the hotbar
        [NonSerialized]
        [CanBeNull] public TextMeshProUGUI hotbarCountText;

        public Slot(int index)
        {
            type = ItemType.Nothing;
            count = 0;
            this.index = index;
        }

        public Slot(Slot slot)
        {
            type = slot.type;
            count = slot.count;
            damage = slot.damage;
            index = slot.index;
        }

        public Slot(Slot slot, ItemType type, int count, int index)
        {
            this.type = type;
            this.count = count;
            damage = slot.type==type ? slot.damage : 0;
            this.index = index;
            
            countText = slot.countText;
            hotbarCountText = slot.hotbarCountText;
        }


        public Slot()
        {
            type = ItemType.Nothing;
            count = 0;
            index = 0;
        }
        
        public void SetCount(int count)
        {
            this.count = count;
            if (count > 1)
            {
                if(countText != null)
                {
                    countText.text = count.ToString();
                }
                if(hotbarCountText != null)
                {
                    hotbarCountText.text = count.ToString();
                }
            }
            else
            {
                if (countText != null)
                {
                    countText.text = "";
                }
                if(hotbarCountText != null)
                {
                    hotbarCountText.text = "";
                }
            }
        }
    }
}

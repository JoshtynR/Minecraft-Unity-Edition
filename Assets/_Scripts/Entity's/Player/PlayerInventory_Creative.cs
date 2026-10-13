using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class PlayerInventory
{
    private const int CreativePageSize = 45;
    private readonly List<BlockType> creativeBlocks = new List<BlockType>();
    private readonly List<RectTransform> creativeCells = new List<RectTransform>();
    private RectTransform creativeRoot, creativePanel;
    private TextMeshProUGUI creativePageText, creativeNameText;
    private TMP_FontAsset creativeFont;
    private Button previousPage, nextPage;
    private int creativePage;

    private void BuildCreativeInventory()
    {
        // Use the registered catalog, including variants and technical blocks.
        // No serialized block definitions or save-slot layout are changed.
        foreach (BlockType type in Enum.GetValues(typeof(BlockType)))
            if (type != BlockType.Nothing && type != BlockType.Air &&
                BlockDataManager.blockTypeDataDictionary != null &&
                (int)type < BlockDataManager.blockTypeDataDictionary.Length &&
                BlockDataManager.blockTypeDataDictionary[(int)type] != null)
                creativeBlocks.Add(type);

        var assets = Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        creativeFont = assets != null && assets.levelFont != null
            ? assets.levelFont : SlotTextPrefab.GetComponent<TextMeshProUGUI>().font;
        creativeRoot = CreativeRect("CreativeInventory", inventoryRect.parent, 0, 0, 258, 250);
        creativeRoot.localPosition = new Vector3(0, 0, -35);
        creativePanel = CreativeRect("BlockCatalog", creativeRoot, 0, 0, 258, 224);
        creativePanel.gameObject.AddComponent<Image>().color = new Color(.18f, .18f, .18f, .98f);
        CreativeText("Title", creativePanel, 0, 99, 240, 14, "Block catalog", 9);
        CreativeButton("Blocks", creativeRoot, -55, 124, 100, 18, () => ShowInventoryTab(true));
        CreativeButton("Inventory", creativeRoot, 55, 124, 100, 18, () => ShowInventoryTab(false));

        for (int i = 0; i < CreativePageSize; i++)
        {
            var cell = CreativeRect("Block" + i, creativePanel,
                (i % 9 - 4) * 26, 75 - (i / 9) * 26, 24, 24);
            cell.gameObject.AddComponent<Image>().color = new Color(.38f, .38f, .38f, 1);
            var button = cell.gameObject.AddComponent<Button>();
            int index = i;
            button.onClick.AddListener(() => SelectCreativeBlock(index));
            creativeCells.Add(cell);
        }
        previousPage = CreativeButton("<", creativePanel, -91, -52, 38, 18, () => ChangeCreativePage(-1));
        nextPage = CreativeButton(">", creativePanel, 91, -52, 38, 18, () => ChangeCreativePage(1));
        creativePageText = CreativeText("Page", creativePanel, 0, -52, 132, 18, "", 8);
        creativeNameText = CreativeText("Selection", creativePanel, 0, -77, 246, 18, "Choose a block", 8);
        CreativeText("Hint", creativePanel, 0, -98, 246, 18,
            "Click: 64 in selected hotbar slot | 1-9: slot", 6);
        RefreshCreativePage();
        creativeRoot.gameObject.SetActive(false);
    }

    private void ShowInventoryTab(bool blocks)
    {
        // Keep carried items accessible until the player drops them into a slot.
        if (blocks && pickedUpSlot != null) return;
        creativePanel.gameObject.SetActive(blocks);
        inventoryRect.gameObject.SetActive(!blocks);
        inventoryHighlightRect.gameObject.SetActive(false);
    }

    private void ChangeCreativePage(int delta)
    {
        int lastPage = Math.Max(0, (creativeBlocks.Count - 1) / CreativePageSize);
        creativePage = Math.Max(0, Math.Min(lastPage, creativePage + delta));
        RefreshCreativePage();
    }

    private void RefreshCreativePage()
    {
        int pages = Math.Max(1, (creativeBlocks.Count + CreativePageSize - 1) / CreativePageSize);
        creativePageText.text = (creativePage + 1) + " / " + pages + " (" + creativeBlocks.Count + " blocks)";
        previousPage.interactable = creativePage > 0;
        nextPage.interactable = creativePage + 1 < pages;
        for (int i = 0; i < creativeCells.Count; i++)
        {
            var cell = creativeCells[i];
            // Meshes are cached and owned by PlayerInventory; destroy only instances.
            for (int child = cell.childCount - 1; child >= 0; child--)
            {
                cell.GetChild(child).gameObject.SetActive(false);
                Destroy(cell.GetChild(child).gameObject);
            }
            int index = creativePage * CreativePageSize + i;
            cell.gameObject.SetActive(index < creativeBlocks.Count);
            if (index < creativeBlocks.Count)
            {
                CreateBlockMesh(creativeBlocks[index], cell);
                var icon = cell.GetChild(cell.childCount - 1);
                var position = icon.localPosition;
                position.z = -20;
                icon.localPosition = position;
            }
        }
    }

    private void SelectCreativeBlock(int cell)
    {
        int index = creativePage * CreativePageSize + cell;
        if (index < 0 || index >= creativeBlocks.Count || pickedUpSlot != null) return;
        BlockType type = creativeBlocks[index];
        SetSlotBlockType(selectedSlotIndex, type);
        slots[selectedSlotIndex].SetCount(maxStackSize);
        creativeNameText.text = Regex.Replace(type.ToString(), "([a-z0-9])([A-Z])", "$1 $2") +
            " -> slot " + (selectedSlotIndex + 1);
    }

    private RectTransform CreativeRect(string name, Transform parent, float x, float y, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    private TextMeshProUGUI CreativeText(string name, Transform parent, float x, float y,
        float width, float height, string value, float size)
    {
        var rect = CreativeRect(name, parent, x, y, width, height);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = creativeFont;
        text.fontSize = size;
        text.text = value;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private Button CreativeButton(string label, Transform parent, float x, float y,
        float width, float height, UnityEngine.Events.UnityAction action)
    {
        var rect = CreativeRect(label, parent, x, y, width, height);
        rect.gameObject.AddComponent<Image>().color = new Color(.4f, .4f, .4f, 1);
        var button = rect.gameObject.AddComponent<Button>();
        button.onClick.AddListener(action);
        CreativeText("Label", rect, 0, 0, width, height, label, 8);
        return button;
    }
}

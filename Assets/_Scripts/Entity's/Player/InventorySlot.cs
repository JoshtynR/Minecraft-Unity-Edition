using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public PlayerInventory playerInventory;
    public int slotIndex;
    public bool isCatalog;
    public PlayerInventory.Slot mySlot => isCatalog ? null : playerInventory.GetSlot(slotIndex);
    public void OnPointerEnter(PointerEventData data)
    {
        playerInventory.HoverSlot(GetComponent<RectTransform>(),slotIndex,isCatalog);
    }
    public void OnPointerExit(PointerEventData data) { playerInventory.HideSlotHover(); }
    public void OnPointerDown(PointerEventData data)
    {
        bool right=data.button==PointerEventData.InputButton.Right;
        if(!right&&data.button!=PointerEventData.InputButton.Left)return;
        if(isCatalog)playerInventory.GiveCatalogItem(slotIndex,right);
        else playerInventory.ClickInventorySlot(slotIndex,right,Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift));
    }
}

using UnityEngine;
using UnityEngine.UI;

public partial class PlayerInventory
{
    public int EquipmentRevision {get;private set;}
    private readonly InventoryStack[] armourValueBuffer=new InventoryStack[4];
    public InventoryStack[] EquipmentSnapshot()
    {InitializeSlotData();var result=new InventoryStack[4];for(int i=0;i<4;i++)result[i]=Stack(equipmentSlots[i]);return result;}
    public int ArmourValue
    {get{InitializeSlotData();for(int i=0;i<4;i++)armourValueBuffer[i]=Stack(equipmentSlots[i]);return ItemDurability.ArmorValue(armourValueBuffer);}}
    public void ApplyEquipmentSnapshot(InventoryStack[] equipment)
    {if(equipment==null||equipment.Length!=4)return;for(int i=0;i<4;i++)WriteStack(41+i,equipment[i]);}
    public int AbsorbArmourDamage(int damage,ref int remainder)
    {
        int result=ItemDurability.ReduceArmorDamage(damage,ArmourValue,ref remainder);
        if(damage>0)for(int i=0;i<4;i++)
        {var stack=Stack(equipmentSlots[i]);if(!stack.Empty&&ItemDurability.ArmorPart(stack.type)==i)ApplyItemWear(41+i,stack,damage);}
        return result;
    }
    public InventoryStack HeldItem()
    {InitializeSlotData();return selectedSlotIndex>=0&&selectedSlotIndex<9?Stack(slots[selectedSlotIndex]):new InventoryStack();}
    public bool ApplyItemWear(int index,InventoryStack expected,int amount)
    {
        var slot=GetSlot(index);var current=Stack(slot);
        if(slot==null||current.Empty||!current.Matches(expected)||amount<=0)return false;
        bool broken=ItemDurability.Wear(ref current,amount);WriteStack(index,current);
        return broken;
    }
    public void ApplyCompletedMiningWear(int index,InventoryStack expected,int amount)
    {
        if(expected.Empty||amount<=0)return;
        var preferred=GetSlot(index);
        if(preferred!=null&&!Stack(preferred).Empty&&Stack(preferred).Matches(expected)){ApplyItemWear(index,expected,amount);return;}
        for(int i=0;i<36;i++)if(!Stack(slots[i]).Empty&&Stack(slots[i]).Matches(expected)){ApplyItemWear(i,expected,amount);return;}
        if(pickedUpSlot!=null&&Stack(pickedUpSlot).Matches(expected))
        {var cursor=Stack(pickedUpSlot);ItemDurability.Wear(ref cursor,amount);SetCursor(cursor);return;}
        for(int i=0;i<4;i++)if(!Stack(craftingSlots[i]).Empty&&Stack(craftingSlots[i]).Matches(expected)){ApplyItemWear(36+i,expected,amount);return;}
        if(tableOpen)for(int i=0;i<9;i++)if(!Stack(tableSlots[i]).Empty&&Stack(tableSlots[i]).Matches(expected)){ApplyItemWear(200+i,expected,amount);return;}
        for(int i=0;i<recoveryItems.Count;i++)if(Stack(recoveryItems[i]).Matches(expected))
        {
            var stack=Stack(recoveryItems[i]);ItemDurability.Wear(ref stack,amount);
            if(stack.Empty)recoveryItems.RemoveAt(i);
            else{recoveryItems[i].damage=stack.damage;recoveryItems[i].count=stack.count;}
            return;
        }
    }
    private void RefreshSlotDurability(int index)
    {
        var slot=GetSlot(index);if(slot==null)return;
        var item=Stack(slot);
        if(index<36)
        {
            if(index<9&&hotbarRect!=null)DrawDurability(hotbarRect.GetChild(index+1),item);
            if(inventorySlotsRect!=null)DrawDurability(inventorySlotsRect.GetChild(index),item);
        }
        else
        {
            var rect=index>=200?tableRects[index-200]:index>=41?equipmentRects[index-41]:index==40?craftResultRect:craftingRects[index-36];
            if(rect!=null)DrawDurability(rect,item);
        }
    }
    private void DrawDurability(Transform parent,InventoryStack item)
    {
        var bar=parent.Find("DurabilityBar");
        bool visible=!item.Empty&&item.damage>0&&ItemDurability.Maximum(item.type)>0;
        if(!visible){if(bar!=null)bar.gameObject.SetActive(false);return;}
        if(bar==null)
        {
            var root=CreativeRect("DurabilityBar",parent,.5f,-6,13,2);root.localPosition=new Vector3(.5f,-6,-25);
            var bg=root.gameObject.AddComponent<Image>();bg.color=new Color(0,0,0,1);bg.raycastTarget=false;
            var shade=CreativeRect("Shade",root,-.5f,.5f,12,1);shade.gameObject.AddComponent<Image>().raycastTarget=false;
            var fill=CreativeRect("Fill",root,0,.5f,13,1);fill.gameObject.AddComponent<Image>().raycastTarget=false;
            bar=root;
        }
        bar.gameObject.SetActive(true);
        int green=ItemDurability.BarGreen(item),width=ItemDurability.BarWidth(item);
        bar.Find("Shade").GetComponent<Image>().color=new Color((255-green)/4/255f,63/255f,0,1);
        var colored=bar.Find("Fill").GetComponent<RectTransform>();
        colored.anchoredPosition=new Vector2(-6.5f+width*.5f,.5f);colored.sizeDelta=new Vector2(width,1);
        colored.GetComponent<Image>().color=new Color((255-green)/255f,green/255f,0,1);
        RaiseSlotCount(parent);
    }
}

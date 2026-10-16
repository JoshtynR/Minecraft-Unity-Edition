using System;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerInventory
{
    private readonly Slot[] craftingSlots = new Slot[4];
    private readonly Slot[] equipmentSlots = new Slot[4];
    private readonly Slot craftedOutput = new Slot(40);
    private readonly List<Slot> recoveryItems = new List<Slot>();
    private readonly InventoryStack[] storageBuffer = new InventoryStack[36];
    private readonly InventoryStack[] craftBuffer = new InventoryStack[4];

    private static InventoryStack Stack(Slot slot) => slot==null ? new InventoryStack() : new InventoryStack(slot.type,slot.count);
    private void InitializeSlotData()
    {
        if(slots==null)slots=new Slot[36];
        if(slots.Length!=36)
        {
            var old=slots;slots=new Slot[36];Array.Copy(old,slots,Math.Min(old.Length,36));
            for(int i=36;i<old.Length;i++)if(old[i]!=null)QueueRecovery(old[i].type,old[i].count);
        }
        for(int i=0;i<36;i++)if(slots[i]==null)slots[i]=new Slot(i);
        for(int i=0;i<4;i++)if(craftingSlots[i]==null)craftingSlots[i]=new Slot(36+i);
        for(int i=0;i<4;i++)if(equipmentSlots[i]==null)equipmentSlots[i]=new Slot(41+i);
    }
    public Slot GetSlot(int index)
    {
        InitializeSlotData();return index>=0&&index<36 ? slots[index] : index>=36&&index<40 ? craftingSlots[index-36] : index==40 ? craftedOutput : index>=41&&index<45 ? equipmentSlots[index-41] : null;
    }
    private void ReadStorage()
    {
        InitializeSlotData();
        for(int i=0;i<36;i++)storageBuffer[i]=Stack(slots[i]);
    }
    private void CommitStorage()
    {
        for(int i=0;i<36;i++)WriteStack(i,storageBuffer[i]);
    }
    private void WriteStack(int index,InventoryStack value)
    {
        value.Normalize();var slot=GetSlot(index);
        if(index<36)
        {
            if(slot.type!=value.type)SetSlotBlockType(index,value.type);
        }
        else if(slot.type!=value.type)
        {
            var rect=index>=41?equipmentRects[index-41]:index==40?craftResultRect:craftingRects[index-36];
            if(rect!=null){ClearSlotIcon(rect);CreateBlockMesh(value.type,rect);}
            slot.type=value.type;
        }
        slot.SetCount(value.count);
        if(index>=41)
        {
            if(equipmentGhosts[index-41]!=null)equipmentGhosts[index-41].gameObject.SetActive(value.Empty);
            RefreshPreviewEquipment();
        }
    }
    private void SetCursor(InventoryStack stack)
    {
        stack.Normalize();pickedUpSlot=stack.Empty?null:new Slot{type=stack.type,count=stack.count};
        UpdatePickedUpSlotVisual();
    }
    public void ClickInventorySlot(int index,bool right,bool shift)
    {
        if(index==40){TakeCraftResult(shift);return;}
        var slot=GetSlot(index);if(slot==null)return;
        if(shift&&index<36)
        {
            ReadStorage();InventoryRules.Shift(storageBuffer,index);CommitStorage();
        }
        else if(shift)
        {
            ReadStorage();var value=Stack(slot);
            value.count=InventoryRules.Insert(storageBuffer,value.type,value.count,0,36);
            CommitStorage();WriteStack(index,value);
        }
        else
        {
            var value=Stack(slot);var cursor=Stack(pickedUpSlot);
            if(index>=41)InventoryRules.ClickEquipment(ref value,ref cursor,index-41);
            else InventoryRules.Click(ref value,ref cursor,right);
            WriteStack(index,value);SetCursor(cursor);
        }
        RefreshCraftingOutput();
    }
    private void ReadCrafting()
    {
        for(int i=0;i<4;i++)craftBuffer[i]=Stack(craftingSlots[i]);
    }
    private void RefreshCraftingOutput()
    {
        ReadCrafting();WriteStack(40,InventoryRules.CraftResult(craftBuffer));
        UpdateRecoveryNotice();
    }
    private void TakeCraftResult(bool shift)
    {
        ReadStorage();ReadCrafting();var cursor=Stack(pickedUpSlot);
        for(int iteration=0;iteration<(shift?64:1);iteration++)
        {
            var result=InventoryRules.CraftResult(craftBuffer);if(result.Empty)break;
            if(shift)
            {
                if(InventoryRules.Capacity(storageBuffer,result.type,0,36)<result.count)break;
                InventoryRules.Insert(storageBuffer,result.type,result.count,0,36);
            }
            else
            {
                if(!cursor.Empty&&(cursor.type!=result.type||cursor.count+result.count>64))break;
                cursor=new InventoryStack(result.type,(cursor.Empty?0:cursor.count)+result.count);
            }
            InventoryRules.ConsumeRecipe(craftBuffer);
        }
        CommitStorage();
        for(int i=0;i<4;i++)WriteStack(36+i,craftBuffer[i]);
        SetCursor(cursor);RefreshCraftingOutput();
    }
    public bool AddItem(BlockType type,int count=1,bool onlyInventory=false,bool onlyHotbar=false)
    {
        if(count<=0||type==BlockType.Nothing||type==BlockType.Air)return false;
        ReadStorage();int start=onlyInventory?9:0,end=onlyHotbar?9:36;
        // The bool API is transactional: callers must never receive false after
        // some items were inserted and then retry/duplicate the original stack.
        if(InventoryRules.Capacity(storageBuffer,type,start,end)<count)return false;
        InventoryRules.Insert(storageBuffer,type,count,start,end);CommitStorage();return true;
    }
    private int AddUpTo(BlockType type,int count)
    {
        ReadStorage();int remaining=InventoryRules.Insert(storageBuffer,type,count,0,36);CommitStorage();return count-remaining;
    }
    public void RemoveHeldItem(int count=1)
    {
        if(selectedSlotIndex<0||selectedSlotIndex>=9||count<=0)return;
        var stack=Stack(slots[selectedSlotIndex]);stack.count=Math.Max(0,stack.count-count);WriteStack(selectedSlotIndex,stack);
    }
    public void RemoveItem(int index) { if(index>=0&&index<36)WriteStack(index,new InventoryStack()); }

    private void ReturnTransientStacks()
    {
        ReadStorage();var cursor=Stack(pickedUpSlot);
        cursor.count=InventoryRules.Insert(storageBuffer,cursor.type,cursor.count,0,36);
        for(int i=0;i<4;i++)
        {
            var value=Stack(craftingSlots[i]);value.count=InventoryRules.Insert(storageBuffer,value.type,value.count,0,36);WriteStack(36+i,value);
        }
        CommitStorage();SetCursor(cursor);RefreshCraftingOutput();
        // There is no dropped-item entity system yet. When storage is full,
        // keep remaining cursor/grid stacks safely persisted instead of deleting.
    }
    public Slot[] SavedInventory() { InitializeSlotData();return CopySlots(slots); }
    public Slot[] SavedCrafting() => CopySlots(craftingSlots);
    public Slot[] SavedEquipment() => CopySlots(equipmentSlots);
    public Slot SavedCursor() => pickedUpSlot==null?null:new Slot(pickedUpSlot);
    public Slot[] SavedRecovery() => CopySlots(recoveryItems.ToArray());
    private static Slot[] CopySlots(Slot[] source)
    {
        var copy=new Slot[source.Length];for(int i=0;i<copy.Length;i++)copy[i]=source[i]==null?new Slot(i):new Slot(source[i]);return copy;
    }
    private void QueueRecovery(BlockType type,int count)
    {
        if(count>0&&type!=BlockType.Nothing&&type!=BlockType.Air)recoveryItems.Add(new Slot{type=type,count=count});
    }
    public void LoadInventory(Slot[] saved,Slot[] crafting=null,Slot cursor=null,Slot[] recovery=null,Slot[] equipment=null)
    {
        InitializeSlotData();
        recoveryItems.Clear();
        for(int i=0;i<36;i++)
        {
            var stack=saved!=null&&i<saved.Length?Stack(saved[i]):new InventoryStack();
            QueueRecovery(stack.type,Math.Max(0,stack.count-64));stack.count=Math.Min(64,stack.count);WriteStack(i,stack);
        }
        if(saved!=null)for(int i=36;i<saved.Length;i++)if(saved[i]!=null)QueueRecovery(saved[i].type,saved[i].count);
        if(recovery!=null)foreach(var stack in recovery)if(stack!=null)QueueRecovery(stack.type,stack.count);
        for(int i=0;i<4;i++)
        {
            var stack=crafting!=null&&i<crafting.Length?Stack(crafting[i]):new InventoryStack();
            QueueRecovery(stack.type,Math.Max(0,stack.count-64));stack.count=Math.Min(64,stack.count);WriteStack(36+i,stack);
        }
        var carried=Stack(cursor);QueueRecovery(carried.type,Math.Max(0,carried.count-64));carried.count=Math.Min(64,carried.count);SetCursor(carried);
        for(int i=0;i<4;i++)
        {
            var stack=equipment!=null&&i<equipment.Length?Stack(equipment[i]):new InventoryStack();
            bool valid=InventoryRules.EquipmentAccepts(i,stack.type);
            QueueRecovery(stack.type,valid?Math.Max(0,stack.count-1):stack.count);
            WriteStack(41+i,valid?new InventoryStack(stack.type,Math.Min(1,stack.count)):new InventoryStack());
        }
        RecoverSavedItems();RefreshCraftingOutput();
    }
    private void RecoverSavedItems()
    {
        ReadStorage();
        for(int i=0;i<recoveryItems.Count;)
        {
            var stack=recoveryItems[i];stack.count=InventoryRules.Insert(storageBuffer,stack.type,stack.count,0,36);
            if(stack.count==0)recoveryItems.RemoveAt(i);else i++;
        }
        CommitStorage();UpdateRecoveryNotice();
    }
}

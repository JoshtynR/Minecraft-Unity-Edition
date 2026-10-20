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
    private readonly InventoryStack[] playerCraftBuffer = new InventoryStack[4];
    private readonly InventoryStack[] tableCraftBuffer = new InventoryStack[9];
    private InventoryStack[] craftBuffer => tableOpen ? tableCraftBuffer : playerCraftBuffer;
    private Slot[] ActiveCrafting => tableOpen ? tableSlots : craftingSlots;

    private static InventoryStack Stack(Slot slot) => slot==null ? new InventoryStack() : new InventoryStack(slot.type,slot.count,slot.damage);
    private void InitializeSlotData()
    {
        if(slots==null)slots=new Slot[36];
        if(slots.Length!=36)
        {
            var old=slots;slots=new Slot[36];Array.Copy(old,slots,Math.Min(old.Length,36));
            for(int i=36;i<old.Length;i++)if(old[i]!=null)QueueRecovery(old[i].type,old[i].count,old[i].damage);
        }
        for(int i=0;i<36;i++)if(slots[i]==null)slots[i]=new Slot(i);
        for(int i=0;i<4;i++)if(craftingSlots[i]==null)craftingSlots[i]=new Slot(36+i);
        for(int i=0;i<9;i++)if(tableSlots[i]==null)tableSlots[i]=new Slot(200+i);
        for(int i=0;i<4;i++)if(equipmentSlots[i]==null)equipmentSlots[i]=new Slot(41+i);
    }
    public Slot GetSlot(int index)
    {
        if(chestOpen&&index>=100&&index<127)return chestSlots[index-100];
        if(tableOpen&&index>=200&&index<209)return tableSlots[index-200];
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
        value.Normalize();var slot=GetSlot(index);if(slot==null)return;
        if(index<36)
        {
            if(slot.type!=value.type)SetSlotItemType(index,value.type);
        }
        else if(slot.type!=value.type)
        {
            var rect=index>=200?tableRects[index-200]:index>=41?equipmentRects[index-41]:index==40?craftResultRect:craftingRects[index-36];
            if(rect!=null){ClearSlotIcon(rect);CreateItemIcon(value.type,rect);}
            slot.type=value.type;
        }
        slot.damage=value.damage;
        slot.SetCount(value.count);
        RefreshSlotDurability(index);
        if(index>=41&&index<45)
        {
            if(equipmentGhosts[index-41]!=null)equipmentGhosts[index-41].gameObject.SetActive(value.Empty);
            RefreshPreviewEquipment();
        }
    }
    private void SetCursor(InventoryStack stack)
    {
        stack.Normalize();pickedUpSlot=stack.Empty?null:new Slot{type=stack.type,count=stack.count,damage=stack.damage};
        UpdatePickedUpSlotVisual();
    }
    public void ClickInventorySlot(int index,bool right,bool shift)
    {
        if(chestPending||DropRequestPending)return;
        if(chestOpen){ClickChest(index,right,shift);return;}
        if(tableOpen&&!TableAccessible()){CloseInventory();return;}
        if(tableOpen&&((index>=36&&index<40)||(index>=41&&index<45)))return;
        if(index==40){TakeCraftResult(shift);return;}
        var slot=GetSlot(index);if(slot==null)return;
        if(shift&&index<36)
        {
            ReadStorage();InventoryRules.Shift(storageBuffer,index);CommitStorage();
        }
        else if(shift)
        {
            ReadStorage();var value=Stack(slot);
            value.count=InventoryRules.Insert(storageBuffer,value.type,value.count,0,36,value.damage);
            CommitStorage();WriteStack(index,value);
        }
        else
        {
            var value=Stack(slot);var cursor=Stack(pickedUpSlot);
            if(index>=41&&index<45)InventoryRules.ClickEquipment(ref value,ref cursor,index-41);
            else InventoryRules.Click(ref value,ref cursor,right);
            WriteStack(index,value);SetCursor(cursor);
        }
        RefreshCraftingOutput();
    }
    private void ReadCrafting()
    {
        for(int i=0;i<ActiveCrafting.Length;i++)craftBuffer[i]=Stack(ActiveCrafting[i]);
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
                if((!cursor.Empty&&!cursor.Matches(result))||(cursor.Empty?0:cursor.count)+result.count>InventoryRules.Limit(result.type))break;
                cursor=new InventoryStack(result.type,(cursor.Empty?0:cursor.count)+result.count,result.damage);
            }
            InventoryRules.ConsumeRecipe(craftBuffer);
        }
        CommitStorage();
        for(int i=0;i<ActiveCrafting.Length;i++)WriteStack(tableOpen?200+i:36+i,craftBuffer[i]);
        SetCursor(cursor);RefreshCraftingOutput();
    }
    // Existing block-drop callers cross the block/item boundary here.
    public bool AddItem(BlockType block,int count=1,bool onlyInventory=false,bool onlyHotbar=false)
        => AddItem(ItemCatalog.FromBlock(block),count,onlyInventory,onlyHotbar);

    public bool AddItem(ItemType type,int count=1,bool onlyInventory=false,bool onlyHotbar=false,int damage=0)
    {
        if(DropRequestPending||count<=0||damage<0||!ItemCatalog.Valid(type))return false;
        ReadStorage();int start=onlyInventory?9:0,end=onlyHotbar?9:36;
        // The bool API is transactional: callers must never receive false after
        // some items were inserted and then retry/duplicate the original stack.
        if(InventoryRules.Capacity(storageBuffer,type,start,end,damage)<count)return false;
        InventoryRules.Insert(storageBuffer,type,count,start,end,damage);CommitStorage();return true;
    }
    private int AddUpTo(ItemType type,int count)
    {
        ReadStorage();int remaining=InventoryRules.Insert(storageBuffer,type,count,0,36);CommitStorage();return count-remaining;
    }
    public void RemoveHeldItem(int count=1)
    {
        if(DropRequestPending||selectedSlotIndex<0||selectedSlotIndex>=9||count<=0)return;
        var stack=Stack(slots[selectedSlotIndex]);stack.count=Math.Max(0,stack.count-count);WriteStack(selectedSlotIndex,stack);
    }
    public void RemoveItem(int index) { if(index>=0&&index<36)WriteStack(index,new InventoryStack()); }

    private void ReturnTransientStacks()
    {
        ReadStorage();var cursor=Stack(pickedUpSlot);
        cursor.count=InventoryRules.Insert(storageBuffer,cursor.type,cursor.count,0,36,cursor.damage);
        for(int i=0;i<4;i++)
        {
            var value=Stack(craftingSlots[i]);value.count=InventoryRules.Insert(storageBuffer,value.type,value.count,0,36,value.damage);WriteStack(36+i,value);
        }
        CommitStorage();SetCursor(cursor);RefreshCraftingOutput();
        // Menu overflow remains persisted when storage is full. Mining and Q use
        // world drops; closing a menu must not silently discard cursor/grid stacks.
    }
    public Slot[] SavedInventory() { InitializeSlotData();return CopySlots(slots); }
    public Slot[] SavedCrafting() => CopySlots(craftingSlots);
    public Slot[] SavedEquipment() => CopySlots(equipmentSlots);
    public Slot SavedCursor() => pickedUpSlot==null?null:new Slot(pickedUpSlot);
    public Slot[] SavedRecovery()
    {
        // A table is a transient container. Save its inputs with the player's
        // recovery stacks so a save taken with this screen open retains its inputs.
        var saved=new List<Slot>(recoveryItems);
        if(tableOpen)foreach(var slot in tableSlots)if(slot!=null&&slot.count>0)saved.Add(slot);
        return CopySlots(saved.ToArray());
    }
    private static Slot[] CopySlots(Slot[] source)
    {
        var copy=new Slot[source.Length];for(int i=0;i<copy.Length;i++)copy[i]=source[i]==null?new Slot(i):new Slot(source[i]);return copy;
    }
    private void QueueRecovery(ItemType type,int count,int damage=0)
    {
        if(count>0&&type!=ItemType.Nothing&&type!=ItemType.Air)recoveryItems.Add(new Slot{type=type,count=count,damage=damage});
    }
    public void LoadInventory(Slot[] saved,Slot[] crafting=null,Slot cursor=null,Slot[] recovery=null,Slot[] equipment=null)
    {
        if(tableOpen)EndCraftingTable(false);
        InitializeSlotData();
        recoveryItems.Clear();
        for(int i=0;i<36;i++)
        {
            var stack=saved!=null&&i<saved.Length?Stack(saved[i]):new InventoryStack();
            QueueRecovery(stack.type,Math.Max(0,stack.count-InventoryRules.Limit(stack.type)),stack.damage);stack.count=Math.Min(InventoryRules.Limit(stack.type),stack.count);WriteStack(i,stack);
        }
        if(saved!=null)for(int i=36;i<saved.Length;i++)if(saved[i]!=null)QueueRecovery(saved[i].type,saved[i].count,saved[i].damage);
        if(recovery!=null)foreach(var stack in recovery)if(stack!=null)QueueRecovery(stack.type,stack.count,stack.damage);
        for(int i=0;i<4;i++)
        {
            var stack=crafting!=null&&i<crafting.Length?Stack(crafting[i]):new InventoryStack();
            QueueRecovery(stack.type,Math.Max(0,stack.count-InventoryRules.Limit(stack.type)),stack.damage);stack.count=Math.Min(InventoryRules.Limit(stack.type),stack.count);WriteStack(36+i,stack);
        }
        var carried=Stack(cursor);QueueRecovery(carried.type,Math.Max(0,carried.count-InventoryRules.Limit(carried.type)),carried.damage);carried.count=Math.Min(InventoryRules.Limit(carried.type),carried.count);SetCursor(carried);
        for(int i=0;i<4;i++)
        {
            var stack=equipment!=null&&i<equipment.Length?Stack(equipment[i]):new InventoryStack();
            bool valid=InventoryRules.EquipmentAccepts(i,stack.type);
            QueueRecovery(stack.type,valid?Math.Max(0,stack.count-1):stack.count,stack.damage);
            WriteStack(41+i,valid?new InventoryStack(stack.type,Math.Min(1,stack.count),stack.damage):new InventoryStack());
        }
        RecoverSavedItems();RefreshCraftingOutput();
    }
    private void RecoverSavedItems()
    {
        ReadStorage();
        for(int i=0;i<recoveryItems.Count;)
        {
            var stack=recoveryItems[i];stack.count=InventoryRules.Insert(storageBuffer,stack.type,stack.count,0,36,stack.damage);
            if(stack.count==0)recoveryItems.RemoveAt(i);else i++;
        }
        CommitStorage();UpdateRecoveryNotice();
    }
}

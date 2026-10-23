using System;
using System.Collections.Generic;

[Serializable]
public struct InventoryStack
{
    public ItemType type;
    public int count,damage;
    public InventoryStack(ItemType type,int count,int damage=0) { this.type=type;this.count=count;this.damage=damage; }
    public bool Empty => count<=0||type==ItemType.Nothing||type==ItemType.Air;
    public void Normalize() { if(Empty) { type=ItemType.Nothing;count=0;damage=0; } else damage=Math.Max(0,damage); }
    public bool Matches(InventoryStack other) => type==other.type&&damage==other.damage;
}

// Slot arithmetic is independent of rendering, input and networking.
public static class InventoryRules
{
    public const int StorageSlots=36, HotbarSlots=9, MaxStack=64;
    public static int Limit(ItemType type) => Math.Max(1,ItemCatalog.StackLimit(type));
    public static void Click(ref InventoryStack slot,ref InventoryStack cursor,bool right)
    {
        slot.Normalize();cursor.Normalize();
        if(cursor.Empty)
        {
            if(slot.Empty)return;
            int take=right?(slot.count+1)/2:slot.count;
            cursor=new InventoryStack(slot.type,take,slot.damage);slot.count-=take;
        }
        else if(slot.Empty||slot.Matches(cursor))
        {
            int move=Math.Min(right?1:cursor.count,Limit(cursor.type)-(slot.Empty?0:slot.count));
            if(move>0){if(slot.Empty)slot=new InventoryStack(cursor.type,0,cursor.damage);slot.count+=move;cursor.count-=move;}
        }
        else { var swap=slot;slot=cursor;cursor=swap; }
        slot.Normalize();cursor.Normalize();
    }
    public static bool Drag(InventoryStack[] slots,ref InventoryStack cursor,int[] indices,bool right)
    {
        cursor.Normalize();
        if(slots==null||indices==null||cursor.Empty)return false;
        var targets=new List<int>();
        var seen=new HashSet<int>();
        int limit=Limit(cursor.type);
        foreach(int index in indices)
        {
            if(index<0||index>=slots.Length||!seen.Add(index))continue;
            var slot=slots[index];slot.Normalize();slots[index]=slot;
            if((slot.Empty||slot.Matches(cursor))&&(slot.Empty?0:slot.count)<limit)targets.Add(index);
        }
        if(targets.Count==0)return false;
        int perSlot=right?1:Math.Max(1,cursor.count/targets.Count);
        bool changed=false;
        foreach(int index in targets)
        {
            if(cursor.Empty)break;
            var slot=slots[index];
            int move=Math.Min(cursor.count,Math.Min(perSlot,limit-(slot.Empty?0:slot.count)));
            if(move<=0)continue;
            if(slot.Empty)slot=new InventoryStack(cursor.type,0,cursor.damage);
            slot.count+=move;cursor.count-=move;slots[index]=slot;changed=true;
        }
        cursor.Normalize();return changed;
    }
    public static int Collect(InventoryStack[] slots,ref InventoryStack cursor)
    {
        cursor.Normalize();
        if(slots==null||cursor.Empty)return 0;
        int moved=0,limit=Limit(cursor.type);
        for(int i=0;i<slots.Length&&cursor.count<limit;i++)
        {
            var slot=slots[i];slot.Normalize();
            if(slot.Empty||!slot.Matches(cursor)){slots[i]=slot;continue;}
            int take=Math.Min(slot.count,limit-cursor.count);
            cursor.count+=take;slot.count-=take;slot.Normalize();slots[i]=slot;moved+=take;
        }
        return moved;
    }
    public static InventoryStack Drop(ref InventoryStack cursor,bool single)
    {
        cursor.Normalize();if(cursor.Empty)return new InventoryStack();
        int count=single?1:cursor.count;
        var dropped=new InventoryStack(cursor.type,count,cursor.damage);
        cursor.count-=count;cursor.Normalize();return dropped;
    }
    public static bool EquipmentAccepts(int index,ItemType type) => index==0&&type==ItemType.Pumpkin;
    public static void ClickEquipment(ref InventoryStack slot,ref InventoryStack cursor,int index)
    {
        slot.Normalize();cursor.Normalize();
        if(cursor.Empty){cursor=slot;slot=new InventoryStack();return;}
        if(!EquipmentAccepts(index,cursor.type)||!slot.Empty)return;
        slot=new InventoryStack(cursor.type,1,cursor.damage);cursor.count--;cursor.Normalize();
    }
    public static int Capacity(InventoryStack[] slots,ItemType type,int start,int end,int damage=0)
    {
        int capacity=0,limit=Limit(type);
        for(int i=start;i<end;i++)capacity+=slots[i].Empty?limit:
            slots[i].type==type&&slots[i].damage==damage?Math.Max(0,limit-slots[i].count):0;
        return capacity;
    }
    // Merge matching ID + damage first, then empty slots. Return uninserted count.
    public static int Insert(InventoryStack[] slots,ItemType type,int count,int start,int end,int damage=0)
    {
        if(type==ItemType.Nothing||type==ItemType.Air||count<=0)return Math.Max(0,count);
        int limit=Limit(type);
        for(int pass=0;pass<2&&count>0;pass++)for(int i=start;i<end&&count>0;i++)
        {
            bool empty=slots[i].Empty;
            if(pass==0?(empty||slots[i].type!=type||slots[i].damage!=damage):!empty)continue;
            int move=Math.Min(count,limit-(empty?0:slots[i].count));
            if(move<=0)continue;
            slots[i]=new InventoryStack(type,(empty?0:slots[i].count)+move,damage);count-=move;
        }
        return count;
    }
    public static void Shift(InventoryStack[] slots,int index)
    {
        if(index<0||index>=StorageSlots||slots[index].Empty)return;
        var stack=slots[index];
        int remaining=Insert(slots,stack.type,stack.count,index<9?9:0,index<9?36:9,stack.damage);
        slots[index]=new InventoryStack(stack.type,remaining,stack.damage);slots[index].Normalize();
    }
    public static InventoryStack CraftResult(InventoryStack[] input) => BetaCraftingRecipes.Result(input);
    public static void ConsumeRecipe(InventoryStack[] input) => BetaCraftingRecipes.Consume(input);
}

// Coordinates use the original 176x166 Beta inventory, with 18-pixel slot pitch.
public struct TmiLayout
{
    public float scale,width,height,inventoryX,sidebarX;
    public int columns,rows;
    public int PageSize => columns*rows;
    public static TmiLayout Calculate(float width,float height)
    {
        width=Math.Max(1,width);height=Math.Max(1,height);
        float scale=Math.Min(1,Math.Min(width/280f,height/206f));
        float w=width/scale,h=height/scale;
        int columns=Math.Max(4,Math.Min(12,(int)((w/2-100-4)/18)));
        int rows=Math.Max(1,(int)((h-38)/18));
        float sidebar=w/2-4-columns*9;
        return new TmiLayout{scale=scale,width=w,height=h,columns=columns,rows=rows,sidebarX=sidebar,inventoryX=Math.Min(0,sidebar-columns*9-100)};
    }
    public static float SlotX(int index) => -72+(index%9)*18;
    public static float SlotY(int index) => index<9?-67:-9-((index-9)/9)*18;
}

using System;

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
    public static InventoryStack CraftResult(InventoryStack[] input)
    {
        int used=0,last=-1;
        for(int i=0;i<4;i++)if(!input[i].Empty){used++;last=i;}
        if(used==1&&(input[last].type==ItemType.Log||input[last].type==ItemType.SpruceLog||input[last].type==ItemType.BirchLog))
            return new InventoryStack(ItemType.WoodenPlanks,4);
        if(used==2)
        {
            for(int row=0;row<2;row++)
            {
                int a=row*2,b=a+1;
                if(input[a].Empty||input[b].Empty||input[a].type!=input[b].type)continue;
                if(input[a].type==ItemType.Stone)return new InventoryStack(ItemType.StonePressurePlate,1);
                if(input[a].type==ItemType.WoodenPlanks)return new InventoryStack(ItemType.WoodenPressurePlate,1);
            }
            for(int column=0;column<2;column++)
            {
                var top=input[column];var bottom=input[column+2];
                if(top.Empty||bottom.Empty)continue;
                if(top.type==ItemType.WoodenPlanks&&bottom.type==ItemType.WoodenPlanks)return new InventoryStack(ItemType.Stick,4);
                if((top.type==ItemType.Coal||top.type==ItemType.Charcoal)&&bottom.type==ItemType.Stick)return new InventoryStack(ItemType.Torch,4);
                if(top.type==ItemType.Stone&&bottom.type==ItemType.Stone)return new InventoryStack(ItemType.StoneButton,1);
                if(top.type==ItemType.Pumpkin&&bottom.type==ItemType.Torch)return new InventoryStack(ItemType.JackOLantern,1);
            }
        }
        if(used==4)
        {
            ItemType type=input[0].type;
            for(int i=1;i<4;i++)if(input[i].type!=type)return new InventoryStack();
            if(type==ItemType.WoodenPlanks)return new InventoryStack(ItemType.CraftingTable,1);
            if(type==ItemType.Sand)return new InventoryStack(ItemType.Sandstone,1);
        }
        return new InventoryStack();
    }
    public static void ConsumeRecipe(InventoryStack[] input)
    {
        for(int i=0;i<4;i++)if(!input[i].Empty){input[i].count--;input[i].Normalize();}
    }
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

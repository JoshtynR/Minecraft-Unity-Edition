using System;

public static class ChestTransactions
{
    public const int Size=27;
    public static bool Valid(InventoryStack[] inventory,InventoryStack cursor)
    {
        if(inventory==null||inventory.Length!=36||!Valid(cursor))return false;
        foreach(var stack in inventory)if(!Valid(stack))return false;
        return true;
    }
    private static bool Valid(InventoryStack stack) => stack.Empty?stack.count==0:
        ItemCatalog.Valid(stack.type)&&stack.count>0&&stack.count<=InventoryRules.Limit(stack.type)&&stack.damage>=0;
    // Chest slots (27 or 54) precede the player's 36 inventory slots.
    public static bool Click(InventoryStack[] chest,InventoryStack[] inventory,ref InventoryStack cursor,int index,bool right,bool shift)
    {
        if(chest==null||(chest.Length!=Size&&chest.Length!=54)||!Valid(inventory,cursor)||index<0||index>=chest.Length+36)return false;
        bool fromChest=index<chest.Length;int slot=fromChest?index:index-chest.Length;
        var source=fromChest?chest:inventory;var target=fromChest?inventory:chest;
        if(shift)
        {
            var stack=source[slot];
            stack.count=InventoryRules.Insert(target,stack.type,stack.count,0,target.Length,stack.damage);
            stack.Normalize();source[slot]=stack;
        }
        else InventoryRules.Click(ref source[slot],ref cursor,right);
        return true;
    }
    public static bool Drag(InventoryStack[] chest,InventoryStack[] inventory,ref InventoryStack cursor,int[] indices,bool right)
    {
        if(chest==null||(chest.Length!=Size&&chest.Length!=54)||!Valid(inventory,cursor)||indices==null)return false;
        var combined=new InventoryStack[chest.Length+36];Array.Copy(chest,combined,chest.Length);Array.Copy(inventory,0,combined,chest.Length,36);
        InventoryRules.Drag(combined,ref cursor,indices,right);
        Array.Copy(combined,chest,chest.Length);Array.Copy(combined,chest.Length,inventory,0,36);return true;
    }
    public static bool Collect(InventoryStack[] chest,InventoryStack[] inventory,ref InventoryStack cursor,int index)
    {
        if(chest==null||(chest.Length!=Size&&chest.Length!=54)||!Valid(inventory,cursor)||index<0||index>=chest.Length+36)return false;
        var combined=new InventoryStack[chest.Length+36];Array.Copy(chest,combined,chest.Length);Array.Copy(inventory,0,combined,chest.Length,36);
        if(cursor.Empty)InventoryRules.Click(ref combined[index],ref cursor,false);
        InventoryRules.Collect(combined,ref cursor);
        Array.Copy(combined,chest,chest.Length);Array.Copy(combined,chest.Length,inventory,0,36);return true;
    }
}

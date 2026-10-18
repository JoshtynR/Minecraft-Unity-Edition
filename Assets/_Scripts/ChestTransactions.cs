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
    // 0..26 are chest slots, 27..62 are the player's existing inventory slots.
    public static bool Click(InventoryStack[] chest,InventoryStack[] inventory,ref InventoryStack cursor,int index,bool right,bool shift)
    {
        if(chest==null||chest.Length!=Size||!Valid(inventory,cursor)||index<0||index>=Size+36)return false;
        bool fromChest=index<Size;int slot=fromChest?index:index-Size;
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
}

using System;

public enum ItemToolKind {None,Pickaxe,Axe,Shovel,Sword,Hoe,Shears,FlintAndSteel,FishingRod}

// Limits come from the existing editable catalog. Damage is wear used, not uses
// remaining. Beta ItemStack.damageItem breaks only after damage > maxDamage.
public static class ItemDurability
{
    public static int Maximum(ItemType type)=>ItemCatalog.Get(type)?.maxDamage??0;
    public static bool Wear(ref InventoryStack item,int amount)
    {
        int maximum=Maximum(item.type);
        if(item.Empty||maximum<=0||amount<=0)return false;
        long damage=(long)Math.Max(0,item.damage)+amount;
        bool broken=damage>maximum;
        if(broken){item.count--;item.damage=0;item.Normalize();}
        else item.damage=(int)damage;
        return broken;
    }
    public static int BarWidth(InventoryStack item)
    {int max=Maximum(item.type);return max<=0?0:Math.Max(0,Math.Min(13,(int)Math.Floor(13-item.damage*13.0/max+.5)));}
    public static int BarGreen(InventoryStack item)
    {int max=Maximum(item.type);return max<=0?0:Math.Max(0,Math.Min(255,(int)Math.Floor(255-item.damage*255.0/max+.5)));}
    public static ItemToolKind Kind(ItemType type)
    {
        switch(type)
        {
            case ItemType.WoodenPickaxe:case ItemType.StonePickaxe:case ItemType.IronPickaxe:case ItemType.DiamondPickaxe:case ItemType.GoldenPickaxe:return ItemToolKind.Pickaxe;
            case ItemType.WoodenAxe:case ItemType.StoneAxe:case ItemType.IronAxe:case ItemType.DiamondAxe:case ItemType.GoldenAxe:return ItemToolKind.Axe;
            case ItemType.WoodenShovel:case ItemType.StoneShovel:case ItemType.IronShovel:case ItemType.DiamondShovel:case ItemType.GoldenShovel:return ItemToolKind.Shovel;
            case ItemType.WoodenSword:case ItemType.StoneSword:case ItemType.IronSword:case ItemType.DiamondSword:case ItemType.GoldenSword:return ItemToolKind.Sword;
            case ItemType.WoodenHoe:case ItemType.StoneHoe:case ItemType.IronHoe:case ItemType.DiamondHoe:case ItemType.GoldenHoe:return ItemToolKind.Hoe;
            case ItemType.Shears:return ItemToolKind.Shears;
            case ItemType.FlintAndSteel:return ItemToolKind.FlintAndSteel;
            case ItemType.FishingRod:return ItemToolKind.FishingRod;
            default:return ItemToolKind.None;
        }
    }
    public static int MiningWear(ItemType type,BlockType block)
    {
        var kind=Kind(type);
        if(kind==ItemToolKind.Pickaxe||kind==ItemToolKind.Axe||kind==ItemToolKind.Shovel)return 1;
        if(kind==ItemToolKind.Sword)return 2;
        return kind==ItemToolKind.Shears&&(IsLeaves(block)||block==BlockType.Cobweb)?1:0;
    }
    public static int HitWear(ItemType type)
    {var kind=Kind(type);return kind==ItemToolKind.Sword?1:kind==ItemToolKind.Pickaxe||kind==ItemToolKind.Axe||kind==ItemToolKind.Shovel?2:0;}
    public static bool IsLeaves(BlockType type)=>type==BlockType.Leaves||type==BlockType.SpruceLeaves||type==BlockType.BirchLeaves;
    public static bool IsWool(BlockType type)=>type==BlockType.WhiteWool||type>=BlockType.OrangeWool&&type<=BlockType.BlackWool;
    // Hooks for gameplay systems that do not exist yet; casting/reeling empty
    // space has no invented rod wear, and armor is not damaged just by carrying it.
    public static int FishingWear(bool caughtEntity,bool caughtFish,bool stuck)=>stuck?2:caughtEntity?3:caughtFish?1:0;
    public static int ArmorPart(ItemType type)=>type>=ItemType.LeatherHelmet&&type<=ItemType.GoldenBoots?((int)type-(int)ItemType.LeatherHelmet)%4:-1;
}

using System;

// Catalog presentation only: never reorder IDs, asset entries or saved slots.
public enum CreativeItemCategory
{
    Building, Natural, Plants, Decoration, Redstone, Transport,
    Tools, Combat, Food, Materials, Dyes
}

public static class CreativeItemOrder
{
    public static CreativeItemCategory Category(ItemType item)
    {
        if(ItemDurability.ArmorPart(item)>=0 || ItemDurability.Kind(item)==ItemToolKind.Sword ||
            item==ItemType.Bow || item==ItemType.Arrow)return CreativeItemCategory.Combat;
        if(ItemDurability.Kind(item)!=ItemToolKind.None)return CreativeItemCategory.Tools;
        if(item>=ItemType.RoseRed&&item<=ItemType.BoneMeal || item==ItemType.InkSac || item==ItemType.LapisLazuli)
            return CreativeItemCategory.Dyes;
        switch(item)
        {
            case ItemType.Dirt:case ItemType.Grass:case ItemType.Stone:case ItemType.Bedrock:
            case ItemType.Sand:case ItemType.Gravel:case ItemType.Clay:case ItemType.SnowBlock:case ItemType.SnowLayer:
            case ItemType.Ice:case ItemType.Water:case ItemType.Lava:case ItemType.Netherrack:case ItemType.SoulSand:
            case ItemType.Obsidian:case ItemType.GoldOre:case ItemType.IronOre:case ItemType.CoalOre:
            case ItemType.LapisOre:case ItemType.DiamondOre:case ItemType.RedstoneOre:case ItemType.LitRedstoneOre:
            case ItemType.Log:case ItemType.SpruceLog:case ItemType.BirchLog:
                return CreativeItemCategory.Natural;
            case ItemType.Leaves:case ItemType.SpruceLeaves:case ItemType.BirchLeaves:
            case ItemType.OakSapling:case ItemType.SpruceSapling:case ItemType.BirchSapling:
            case ItemType.TallGrass:case ItemType.DryTallGrass:case ItemType.Fern:case ItemType.DeadBush:
            case ItemType.Dandelion:case ItemType.Rose:case ItemType.BrownMushroom:case ItemType.RedMushroom:
            case ItemType.Cactus:case ItemType.SugarCane:case ItemType.Pumpkin:case ItemType.WheatCrop0:
            case ItemType.Farmland:case ItemType.MoistFarmland:case ItemType.Seeds:
                return CreativeItemCategory.Plants;
            case ItemType.Torch:case ItemType.GlowStone:case ItemType.JackOLantern:case ItemType.Cobweb:
            case ItemType.Bed:case ItemType.Chest:case ItemType.LockedChest:case ItemType.CraftingTable:
            case ItemType.Furnace:case ItemType.LitFurnace:case ItemType.Bookshelf:case ItemType.Jukebox:
            case ItemType.StandingSign:case ItemType.WallSign:case ItemType.Painting:
            case ItemType.Fire:case ItemType.MonsterSpawner:case ItemType.NetherPortal:
            case ItemType.MusicDisc13:case ItemType.MusicDiscCat:
                return CreativeItemCategory.Decoration;
            case ItemType.Dispenser:case ItemType.NoteBlock:case ItemType.StickyPiston:case ItemType.Piston:
            case ItemType.PistonHead:case ItemType.MovingPiston:case ItemType.TNT:case ItemType.RedstoneWire:
            case ItemType.RedstoneDust:case ItemType.Lever:case ItemType.StonePressurePlate:
            case ItemType.WoodenPressurePlate:case ItemType.StoneButton:case ItemType.RedstoneTorchOff:
            case ItemType.RedstoneTorch:case ItemType.RedstoneRepeater:case ItemType.PoweredRepeater:
            case ItemType.WoodenDoor:case ItemType.IronDoor:case ItemType.Trapdoor:
                return CreativeItemCategory.Redstone;
            case ItemType.Rail:case ItemType.PoweredRail:case ItemType.DetectorRail:
            case ItemType.Minecart:case ItemType.ChestMinecart:case ItemType.FurnaceMinecart:
            case ItemType.Boat:case ItemType.Saddle:
                return CreativeItemCategory.Transport;
            case ItemType.Bucket:case ItemType.WaterBucket:case ItemType.LavaBucket:
            case ItemType.Compass:case ItemType.Clock:case ItemType.Map:
                return CreativeItemCategory.Tools;
            case ItemType.Apple:case ItemType.GoldenApple:case ItemType.MushroomStew:case ItemType.Bread:
            case ItemType.RawPorkchop:case ItemType.CookedPorkchop:case ItemType.RawFish:case ItemType.CookedFish:
            case ItemType.RawBeef:case ItemType.CookedBeef:case ItemType.RawChicken:case ItemType.CookedChicken:
            case ItemType.RottenFlesh:case ItemType.Cookie:case ItemType.Cake:case ItemType.MilkBucket:
                return CreativeItemCategory.Food;
            default:
                return (int)item<1000?CreativeItemCategory.Building:CreativeItemCategory.Materials;
        }
    }

    public static int Compare(ItemType a,ItemType b)
    {
        int category=Category(a).CompareTo(Category(b));
        if(category!=0)return category;
        // Keep tool families together instead of scattering by material.
        int kind=ItemDurability.Kind(a).CompareTo(ItemDurability.Kind(b));
        if(kind!=0)return kind;
        int name=StringComparer.OrdinalIgnoreCase.Compare(ItemCatalog.Get(a)?.displayName,ItemCatalog.Get(b)?.displayName);
        return name!=0?name:((int)a).CompareTo((int)b);
    }
}

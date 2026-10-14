using System;

// Runtime-only definitions for entries omitted from the assigned asset.
// Defaults match Tools/BetaCatalog/catalog.json; user definitions always win.
public static class BlockDataDefaults
{
    public static BlockTypeData[] CreateLookup(int minimumLength = 0)
    {
        int length = minimumLength;
        foreach (BlockType type in Enum.GetValues(typeof(BlockType)))
            length = Math.Max(length, (int)type + 1);
        var result = new BlockTypeData[length];
        for (int i = 0; i < length; i++) result[i] = Create((BlockType)i);
        return result;
    }

    public static BlockTypeData Create(BlockType type)
    {
        var result = new BlockTypeData { blockType = type };
        switch (type)
        {
            case BlockType.Nothing:
            case BlockType.Air:
            case BlockType.OakSapling:
            case BlockType.PoweredRail:
            case BlockType.DetectorRail:
            case BlockType.TallGrass:
            case BlockType.DeadBush:
            case BlockType.MovingPiston:
            case BlockType.Dandelion:
            case BlockType.Rose:
            case BlockType.RedMushroom:
            case BlockType.RedstoneWire:
            case BlockType.WheatCrop0:
            case BlockType.StandingSign:
            case BlockType.Rail:
            case BlockType.WallSign:
            case BlockType.Lever:
            case BlockType.StonePressurePlate:
            case BlockType.WoodenPressurePlate:
            case BlockType.RedstoneTorchOff:
            case BlockType.StoneButton:
            case BlockType.SnowLayer:
            case BlockType.SugarCane:
            case BlockType.SpruceSapling:
            case BlockType.BirchSapling:
            case BlockType.DryTallGrass:
            case BlockType.Fern:
            case BlockType.WheatCrop1:
            case BlockType.WheatCrop2:
            case BlockType.WheatCrop3:
            case BlockType.WheatCrop4:
            case BlockType.WheatCrop5:
            case BlockType.WheatCrop6:
            case BlockType.WheatCrop7:
                result.opacity = 0; result.lightEmission = 0;
                result.isTransparent = true; result.generateCollider = false;
                break;
            case BlockType.Leaves:
            case BlockType.SpruceLeaves:
            case BlockType.BirchLeaves:
                result.opacity = 1; result.lightEmission = 0;
                result.isTransparent = true; result.generateCollider = true;
                break;
            case BlockType.Water:
                result.opacity = 3; result.lightEmission = 0;
                result.isTransparent = true; result.generateCollider = false;
                break;
            case BlockType.Glass:
            case BlockType.Bed:
            case BlockType.StickyPiston:
            case BlockType.Piston:
            case BlockType.PistonHead:
            case BlockType.MonsterSpawner:
            case BlockType.WoodenDoor:
            case BlockType.Ladder:
            case BlockType.IronDoor:
            case BlockType.Cactus:
            case BlockType.Fence:
            case BlockType.Cake:
            case BlockType.RedstoneRepeater:
            case BlockType.Trapdoor:
                result.opacity = 0; result.lightEmission = 0;
                result.isTransparent = true; result.generateCollider = true;
                break;
            case BlockType.GlowStone:
            case BlockType.JackOLantern:
            case BlockType.LockedChest:
                result.opacity = 15; result.lightEmission = 15;
                result.isTransparent = false; result.generateCollider = true;
                break;
            case BlockType.WoodenSlab:
            case BlockType.DoubleStoneSlab:
            case BlockType.StoneSlab:
            case BlockType.WoodenStairs:
            case BlockType.Farmland:
            case BlockType.CobblestoneStairs:
            case BlockType.SandstoneSlab:
            case BlockType.CobblestoneSlab:
            case BlockType.DoubleSandstoneSlab:
            case BlockType.DoubleWoodenSlab:
            case BlockType.DoubleCobblestoneSlab:
            case BlockType.MoistFarmland:
                result.opacity = 15; result.lightEmission = 0;
                result.isTransparent = true; result.generateCollider = true;
                break;
            case BlockType.Lava:
                result.opacity = 15; result.lightEmission = 15;
                result.isTransparent = true; result.generateCollider = false;
                break;
            case BlockType.Ice:
                result.opacity = 3; result.lightEmission = 0;
                result.isTransparent = true; result.generateCollider = true;
                break;
            case BlockType.Cobweb:
                result.opacity = 1; result.lightEmission = 0;
                result.isTransparent = true; result.generateCollider = false;
                break;
            case BlockType.BrownMushroom:
                result.opacity = 0; result.lightEmission = 1;
                result.isTransparent = true; result.generateCollider = false;
                break;
            case BlockType.Torch:
                result.opacity = 0; result.lightEmission = 14;
                result.isTransparent = true; result.generateCollider = false;
                break;
            case BlockType.Fire:
                result.opacity = 0; result.lightEmission = 15;
                result.isTransparent = true; result.generateCollider = false;
                break;
            case BlockType.LitFurnace:
                result.opacity = 15; result.lightEmission = 13;
                result.isTransparent = false; result.generateCollider = true;
                break;
            case BlockType.LitRedstoneOre:
                result.opacity = 15; result.lightEmission = 9;
                result.isTransparent = false; result.generateCollider = true;
                break;
            case BlockType.RedstoneTorch:
                result.opacity = 0; result.lightEmission = 7;
                result.isTransparent = true; result.generateCollider = false;
                break;
            case BlockType.NetherPortal:
                result.opacity = 0; result.lightEmission = 11;
                result.isTransparent = true; result.generateCollider = false;
                break;
            case BlockType.PoweredRepeater:
                result.opacity = 0; result.lightEmission = 9;
                result.isTransparent = true; result.generateCollider = true;
                break;
        }
        return result;
    }
}

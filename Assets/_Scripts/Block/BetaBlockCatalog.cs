using System;

// Texture-ready Beta registry. Source IDs/metadata document vanilla identity;
// local enum IDs are the existing project's save format, not vanilla IDs.
public static class BetaBlockCatalog
{
    public struct Entry
    {
        public readonly BlockType Type, Drop;
        public readonly int SourceId, Metadata;
        public readonly float Hardness;
        public readonly bool HandHarvestable, CornerPass, HalfSlab;
        public Entry(BlockType type, int sourceId, int metadata, float hardness, bool harvestable, bool cornerPass, bool halfSlab, BlockType drop)
        { Type=type; SourceId=sourceId; Metadata=metadata; Hardness=hardness; HandHarvestable=harvestable; CornerPass=cornerPass; HalfSlab=halfSlab; Drop=drop; }
    }
    public static readonly Entry[] Entries =
    {
        new Entry(BlockType.Nothing, -1, 0, -1f, false, true, false, BlockType.Nothing),
        new Entry(BlockType.Air, 0, 0, -1f, false, true, false, BlockType.Nothing),
        new Entry(BlockType.Dirt, 3, 0, 0.5f, true, false, false, BlockType.Dirt),
        new Entry(BlockType.Grass, 2, 0, 0.6f, true, false, false, BlockType.Dirt),
        new Entry(BlockType.Stone, 1, 0, 1.5f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.Log, 17, 0, 2.0f, true, false, false, BlockType.Log),
        new Entry(BlockType.Leaves, 18, 0, 0.2f, true, false, false, BlockType.Nothing),
        new Entry(BlockType.Sand, 12, 0, 0.5f, true, false, false, BlockType.Sand),
        new Entry(BlockType.Water, 9, 0, -1f, false, false, false, BlockType.Water),
        new Entry(BlockType.Glass, 20, 0, 0.3f, true, false, false, BlockType.Nothing),
        new Entry(BlockType.GlowStone, 89, 0, 0.3f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.WoodenSlab, 44, 2, 2.0f, false, false, true, BlockType.Nothing),
        new Entry(BlockType.Bedrock, 7, 0, -1f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.Gravel, 13, 0, 0.6f, true, false, false, BlockType.Gravel),
        new Entry(BlockType.Sandstone, 24, 0, 0.8f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.Lava, 11, 0, -1f, false, false, false, BlockType.Lava),
        new Entry(BlockType.Ice, 79, 0, 0.5f, true, false, false, BlockType.Nothing),
        new Entry(BlockType.SpruceLog, 17, 1, 2.0f, true, false, false, BlockType.SpruceLog),
        new Entry(BlockType.SpruceLeaves, 18, 1, 0.2f, true, false, false, BlockType.Nothing),
        new Entry(BlockType.BirchLog, 17, 2, 2.0f, true, false, false, BlockType.BirchLog),
        new Entry(BlockType.BirchLeaves, 18, 2, 0.2f, true, false, false, BlockType.Nothing),
        new Entry(BlockType.Cobblestone, 4, 0, 2.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.WoodenPlanks, 5, 0, 2.0f, true, false, false, BlockType.WoodenPlanks),
        new Entry(BlockType.OakSapling, 6, 0, 0.0f, true, true, false, BlockType.OakSapling),
        new Entry(BlockType.GoldOre, 14, 0, 3.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.IronOre, 15, 0, 3.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.CoalOre, 16, 0, 3.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.Sponge, 19, 0, 0.6f, true, false, false, BlockType.Sponge),
        new Entry(BlockType.LapisOre, 21, 0, 3.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.LapisBlock, 22, 0, 3.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.Dispenser, 23, 0, 3.5f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.NoteBlock, 25, 0, 0.8f, true, false, false, BlockType.NoteBlock),
        new Entry(BlockType.Bed, 26, 0, 0.2f, true, false, false, BlockType.Bed),
        new Entry(BlockType.PoweredRail, 27, 0, 0.7f, true, true, false, BlockType.PoweredRail),
        new Entry(BlockType.DetectorRail, 28, 0, 0.7f, true, true, false, BlockType.DetectorRail),
        new Entry(BlockType.StickyPiston, 29, 0, 0.5f, true, false, false, BlockType.StickyPiston),
        new Entry(BlockType.Cobweb, 30, 0, 4.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.TallGrass, 31, 1, 0.0f, true, true, false, BlockType.TallGrass),
        new Entry(BlockType.DeadBush, 32, 0, 0.0f, true, true, false, BlockType.DeadBush),
        new Entry(BlockType.Piston, 33, 0, 0.5f, true, false, false, BlockType.Piston),
        new Entry(BlockType.PistonHead, 34, 0, 0.5f, true, false, false, BlockType.Nothing),
        new Entry(BlockType.WhiteWool, 35, 0, 0.8f, true, false, false, BlockType.WhiteWool),
        new Entry(BlockType.MovingPiston, 36, 0, -1f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.Dandelion, 37, 0, 0.0f, true, true, false, BlockType.Dandelion),
        new Entry(BlockType.Rose, 38, 0, 0.0f, true, true, false, BlockType.Rose),
        new Entry(BlockType.BrownMushroom, 39, 0, 0.0f, true, true, false, BlockType.Nothing),
        new Entry(BlockType.RedMushroom, 40, 0, 0.0f, true, true, false, BlockType.Nothing),
        new Entry(BlockType.GoldBlock, 41, 0, 3.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.IronBlock, 42, 0, 5.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.DoubleStoneSlab, 43, 0, 2.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.StoneSlab, 44, 0, 2.0f, false, false, true, BlockType.Nothing),
        new Entry(BlockType.Bricks, 45, 0, 2.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.TNT, 46, 0, 0.0f, true, false, false, BlockType.TNT),
        new Entry(BlockType.Bookshelf, 47, 0, 1.5f, true, false, false, BlockType.Nothing),
        new Entry(BlockType.MossyCobblestone, 48, 0, 2.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.Obsidian, 49, 0, 10.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.Torch, 50, 0, 0.0f, true, true, false, BlockType.Torch),
        new Entry(BlockType.Fire, 51, 0, 0.0f, true, true, false, BlockType.Nothing),
        new Entry(BlockType.MonsterSpawner, 52, 0, 5.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.WoodenStairs, 53, 0, 2f, true, false, false, BlockType.WoodenStairs),
        new Entry(BlockType.Chest, 54, 0, 2.5f, true, false, false, BlockType.Chest),
        new Entry(BlockType.RedstoneWire, 55, 0, 0.0f, true, true, false, BlockType.Nothing),
        new Entry(BlockType.DiamondOre, 56, 0, 3.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.DiamondBlock, 57, 0, 5.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.CraftingTable, 58, 0, 2.5f, true, false, false, BlockType.CraftingTable),
        new Entry(BlockType.WheatCrop0, 59, 0, 0.0f, true, true, false, BlockType.Nothing),
        new Entry(BlockType.Farmland, 60, 0, 0.6f, true, false, false, BlockType.Dirt),
        new Entry(BlockType.Furnace, 61, 0, 3.5f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.LitFurnace, 62, 0, 3.5f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.StandingSign, 63, 0, 1.0f, true, false, false, BlockType.Nothing),
        new Entry(BlockType.WoodenDoor, 64, 0, 3.0f, true, false, false, BlockType.WoodenDoor),
        new Entry(BlockType.Ladder, 65, 0, 0.4f, true, true, false, BlockType.Ladder),
        new Entry(BlockType.Rail, 66, 0, 0.7f, true, true, false, BlockType.Rail),
        new Entry(BlockType.CobblestoneStairs, 67, 0, 2f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.WallSign, 68, 0, 1.0f, true, false, false, BlockType.Nothing),
        new Entry(BlockType.Lever, 69, 0, 0.5f, true, true, false, BlockType.Lever),
        new Entry(BlockType.StonePressurePlate, 70, 0, 0.5f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.IronDoor, 71, 0, 5.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.WoodenPressurePlate, 72, 0, 0.5f, true, false, false, BlockType.WoodenPressurePlate),
        new Entry(BlockType.RedstoneOre, 73, 0, 3.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.LitRedstoneOre, 74, 0, 3.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.RedstoneTorchOff, 75, 0, 0.0f, true, true, false, BlockType.RedstoneTorch),
        new Entry(BlockType.RedstoneTorch, 76, 0, 0.0f, true, true, false, BlockType.RedstoneTorch),
        new Entry(BlockType.StoneButton, 77, 0, 0.5f, true, true, false, BlockType.StoneButton),
        new Entry(BlockType.SnowLayer, 78, 0, 0.1f, false, true, false, BlockType.Nothing),
        new Entry(BlockType.SnowBlock, 80, 0, 0.2f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.Cactus, 81, 0, 0.4f, true, false, false, BlockType.Cactus),
        new Entry(BlockType.Clay, 82, 0, 0.6f, true, false, false, BlockType.Nothing),
        new Entry(BlockType.SugarCane, 83, 0, 0.0f, true, true, false, BlockType.SugarCane),
        new Entry(BlockType.Jukebox, 84, 0, 2.0f, true, false, false, BlockType.Jukebox),
        new Entry(BlockType.Fence, 85, 0, 2.0f, true, false, false, BlockType.Fence),
        new Entry(BlockType.Pumpkin, 86, 0, 1.0f, true, false, false, BlockType.Pumpkin),
        new Entry(BlockType.Netherrack, 87, 0, 0.4f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.SoulSand, 88, 0, 0.5f, true, false, false, BlockType.SoulSand),
        new Entry(BlockType.NetherPortal, 90, 0, -1.0f, false, true, false, BlockType.Nothing),
        new Entry(BlockType.JackOLantern, 91, 0, 1.0f, true, false, false, BlockType.JackOLantern),
        new Entry(BlockType.Cake, 92, 0, 0.5f, true, false, false, BlockType.Nothing),
        new Entry(BlockType.RedstoneRepeater, 93, 0, 0.0f, true, true, false, BlockType.RedstoneRepeater),
        new Entry(BlockType.PoweredRepeater, 94, 0, 0.0f, true, true, false, BlockType.RedstoneRepeater),
        new Entry(BlockType.LockedChest, 95, 0, 0.0f, true, false, false, BlockType.LockedChest),
        new Entry(BlockType.Trapdoor, 96, 0, 3.0f, true, false, false, BlockType.Trapdoor),
        new Entry(BlockType.SpruceSapling, 6, 1, 0.0f, true, true, false, BlockType.SpruceSapling),
        new Entry(BlockType.BirchSapling, 6, 2, 0.0f, true, true, false, BlockType.BirchSapling),
        new Entry(BlockType.DryTallGrass, 31, 0, 0.0f, true, true, false, BlockType.TallGrass),
        new Entry(BlockType.Fern, 31, 2, 0.0f, true, true, false, BlockType.TallGrass),
        new Entry(BlockType.SandstoneSlab, 44, 1, 2.0f, false, false, true, BlockType.Nothing),
        new Entry(BlockType.CobblestoneSlab, 44, 3, 2.0f, false, false, true, BlockType.Nothing),
        new Entry(BlockType.DoubleSandstoneSlab, 43, 1, 2.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.DoubleWoodenSlab, 43, 2, 2.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.DoubleCobblestoneSlab, 43, 3, 2.0f, false, false, false, BlockType.Nothing),
        new Entry(BlockType.MoistFarmland, 60, 7, 0.6f, true, false, false, BlockType.Dirt),
        new Entry(BlockType.OrangeWool, 35, 1, 0.8f, true, false, false, BlockType.OrangeWool),
        new Entry(BlockType.MagentaWool, 35, 2, 0.8f, true, false, false, BlockType.MagentaWool),
        new Entry(BlockType.LightBlueWool, 35, 3, 0.8f, true, false, false, BlockType.LightBlueWool),
        new Entry(BlockType.YellowWool, 35, 4, 0.8f, true, false, false, BlockType.YellowWool),
        new Entry(BlockType.LimeWool, 35, 5, 0.8f, true, false, false, BlockType.LimeWool),
        new Entry(BlockType.PinkWool, 35, 6, 0.8f, true, false, false, BlockType.PinkWool),
        new Entry(BlockType.GrayWool, 35, 7, 0.8f, true, false, false, BlockType.GrayWool),
        new Entry(BlockType.LightGrayWool, 35, 8, 0.8f, true, false, false, BlockType.LightGrayWool),
        new Entry(BlockType.CyanWool, 35, 9, 0.8f, true, false, false, BlockType.CyanWool),
        new Entry(BlockType.PurpleWool, 35, 10, 0.8f, true, false, false, BlockType.PurpleWool),
        new Entry(BlockType.BlueWool, 35, 11, 0.8f, true, false, false, BlockType.BlueWool),
        new Entry(BlockType.BrownWool, 35, 12, 0.8f, true, false, false, BlockType.BrownWool),
        new Entry(BlockType.GreenWool, 35, 13, 0.8f, true, false, false, BlockType.GreenWool),
        new Entry(BlockType.RedWool, 35, 14, 0.8f, true, false, false, BlockType.RedWool),
        new Entry(BlockType.BlackWool, 35, 15, 0.8f, true, false, false, BlockType.BlackWool),
        new Entry(BlockType.WheatCrop1, 59, 1, 0.0f, true, true, false, BlockType.Nothing),
        new Entry(BlockType.WheatCrop2, 59, 2, 0.0f, true, true, false, BlockType.Nothing),
        new Entry(BlockType.WheatCrop3, 59, 3, 0.0f, true, true, false, BlockType.Nothing),
        new Entry(BlockType.WheatCrop4, 59, 4, 0.0f, true, true, false, BlockType.Nothing),
        new Entry(BlockType.WheatCrop5, 59, 5, 0.0f, true, true, false, BlockType.Nothing),
        new Entry(BlockType.WheatCrop6, 59, 6, 0.0f, true, true, false, BlockType.Nothing),
        new Entry(BlockType.WheatCrop7, 59, 7, 0.0f, true, true, false, BlockType.Nothing),
    };
    public static bool TryGet(BlockType type, out Entry entry)
    {
        int id = (int)type;
        if (id >= 0 && id < Entries.Length && Entries[id].Type == type) { entry = Entries[id]; return true; }
        entry = default(Entry); return false;
    }
    public static bool TryGetVanilla(int sourceId, int metadata, out BlockType type)
    {
        if (sourceId == 8) sourceId = 9;
        if (sourceId == 10) sourceId = 11;
        if (sourceId == 17 || sourceId == 18 || sourceId == 6) metadata &= 3;
        else if (sourceId == 35) metadata &= 15;
        else if (sourceId == 43 || sourceId == 44) metadata &= 3;
        else if (sourceId == 60) metadata = metadata > 0 ? 7 : 0;
        else if (sourceId != 31 && sourceId != 59) metadata = 0;
        foreach (var entry in Entries)
            if (entry.SourceId == sourceId && entry.Metadata == metadata) { type=entry.Type; return true; }
        type=BlockType.Nothing; return false;
    }
}

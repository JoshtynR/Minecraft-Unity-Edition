public static class BlockSoundRules
{
    public static string Material(BlockType type)
    {
        if(type>=BlockType.OrangeWool&&type<=BlockType.BlackWool)return "cloth";
        if(type>=BlockType.WheatCrop1&&type<=BlockType.WheatCrop7)return "grass";
        switch(type)
        {
            case BlockType.Nothing: case BlockType.Air: case BlockType.Water: case BlockType.Lava: case BlockType.Fire: case BlockType.NetherPortal: return null;
            case BlockType.Grass: case BlockType.Leaves: case BlockType.SpruceLeaves: case BlockType.BirchLeaves:
            case BlockType.OakSapling: case BlockType.BirchSapling: case BlockType.SpruceSapling: case BlockType.TallGrass:
            case BlockType.DryTallGrass: case BlockType.Fern: case BlockType.DeadBush: case BlockType.Dandelion: case BlockType.Rose:
            case BlockType.BrownMushroom: case BlockType.RedMushroom: case BlockType.WheatCrop0: case BlockType.SugarCane:
            case BlockType.Sponge: case BlockType.TNT: return "grass";
            case BlockType.Dirt: case BlockType.Gravel: case BlockType.Clay: case BlockType.Farmland: case BlockType.MoistFarmland: return "gravel";
            case BlockType.Sand: case BlockType.SoulSand: return "sand";
            case BlockType.SnowLayer: case BlockType.SnowBlock: return "snow";
            case BlockType.WhiteWool: case BlockType.Bed: case BlockType.Cobweb: case BlockType.Cactus: case BlockType.Cake: return "cloth";
            case BlockType.Log: case BlockType.SpruceLog: case BlockType.BirchLog: case BlockType.WoodenPlanks:
            case BlockType.WoodenSlab: case BlockType.DoubleWoodenSlab: case BlockType.WoodenStairs: case BlockType.Bookshelf:
            case BlockType.Chest: case BlockType.LockedChest: case BlockType.CraftingTable: case BlockType.Fence:
            case BlockType.WoodenDoor: case BlockType.Trapdoor: case BlockType.Ladder: case BlockType.StandingSign:
            case BlockType.WallSign: case BlockType.NoteBlock: case BlockType.Jukebox: case BlockType.WoodenPressurePlate:
            case BlockType.Torch: case BlockType.RedstoneTorch: case BlockType.RedstoneTorchOff:
            case BlockType.Pumpkin: case BlockType.JackOLantern: return "wood";
            default: return "stone";
        }
    }
    public static string Step(BlockType type)
    {
        var material=Material(type);return material==null?null:type==BlockType.Ladder?"step/ladder":"step/"+material;
    }
    public static string Break(BlockType type)
    {
        var material=Material(type);return material==null?null:type==BlockType.Glass||type==BlockType.Ice||type==BlockType.GlowStone?"random/glass":"dig/"+material;
    }
    public static string Place(BlockType type)
    {
        var material=Material(type);return material==null?null:"dig/"+material;
    }
    public static string Use(BlockType type,byte state)
    {
        if(type==BlockType.WoodenDoor||type==BlockType.IronDoor||type==BlockType.Trapdoor)return (state&4)!=0?"random/door_open":"random/door_close";
        return type==BlockType.Lever||type==BlockType.StoneButton?"random/click":null;
    }
}

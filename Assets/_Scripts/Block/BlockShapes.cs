public static class BlockShapesList
{
    // Beta plant selection bounds, in sixteenths (not their rendered sheets).
    public static readonly BlockShape Flower = new BlockShape(4.8f, 0, 4.8f, 11.2f, 9.6f, 11.2f);
    public static readonly BlockShape Mushroom = new BlockShape(4.8f, 0, 4.8f, 11.2f, 6.4f, 11.2f);
    public static readonly BlockShape Bush = new BlockShape(1.6f, 0, 1.6f, 14.4f, 12.8f, 14.4f);
    public static readonly BlockShape Reed = new BlockShape(2, 0, 2, 14, 16, 14);
    public static readonly BlockShape Crop = new BlockShape(0, 0, 0, 16, 4, 16);
    public static readonly BlockShape FullBlock = new BlockShape(0, 0, 0, 16, 16, 16);
    public static readonly BlockShape Slab = new BlockShape(0, 0, 0, 16, 8, 16);
    public static readonly BlockShape Farmland = new BlockShape(0, 0, 0, 16, 15, 16);
    public static readonly BlockShape SnowLayer = new BlockShape(0, 0, 0, 16, 2, 16);
    public static readonly BlockShape Cactus = new BlockShape(1, 0, 1, 15, 16, 15);
    // Beta's physical cactus box is one pixel shorter than its rendered/selected box.
    public static readonly BlockShape CactusCollision = new BlockShape(1, 0, 1, 15, 15, 15);
    public static readonly BlockShape Cake = new BlockShape(1, 0, 1, 15, 8, 15);
    public static readonly BlockShape Bed = new BlockShape(0, 0, 0, 16, 9, 16);
    public static readonly BlockShape PressurePlate = new BlockShape(1, 0, 1, 15, 1, 15);
    public static readonly BlockShape Repeater = new BlockShape(0, 0, 0, 16, 2, 16);
    public static readonly BlockShape Trapdoor = new BlockShape(0, 0, 0, 16, 3, 16);
    public static readonly BlockShape WallButton = new BlockShape(5, 6, 14, 11, 10, 16);
    public static readonly BlockShape SoulSandCollision = new BlockShape(0, 0, 0, 16, 14, 16);
}

// Preserve serialized values 0/1. State-dependent and multipart models are
// separate from these default cuboids; no texture/data asset migration needed.
public enum BlockShapes { FullBlock = 0, Slab = 1, Farmland, SnowLayer, Cactus, Cake, Bed, PressurePlate, Repeater, Trapdoor, WallButton, Flower, Mushroom, Bush, Reed, Crop, StairsEast, StairsWest, StairsSouth, StairsNorth }

public static class BlockShapesExtensions
{
    public static BlockShapes ForBlock(BlockType type)
    {
        if (SpecialBlockGeometry.Handles(type)) return SpecialBlockGeometry.Shape(type, SpecialBlockGeometry.DefaultState(type));
        if (IsCrop(type)) return BlockShapes.Crop;
        switch (type)
        {
            case BlockType.WoodenStairs: case BlockType.CobblestoneStairs: return BlockShapes.StairsEast;
            case BlockType.Dandelion: case BlockType.Rose: return BlockShapes.Flower;
            case BlockType.BrownMushroom: case BlockType.RedMushroom: return BlockShapes.Mushroom;
            case BlockType.OakSapling: case BlockType.SpruceSapling: case BlockType.BirchSapling:
            case BlockType.TallGrass: case BlockType.DryTallGrass: case BlockType.Fern:
            case BlockType.DeadBush: return BlockShapes.Bush;
            case BlockType.SugarCane: return BlockShapes.Reed;
            case BlockType.WoodenSlab: case BlockType.StoneSlab: case BlockType.SandstoneSlab: case BlockType.CobblestoneSlab: return BlockShapes.Slab;
            case BlockType.Farmland: case BlockType.MoistFarmland: return BlockShapes.Farmland;
            case BlockType.SnowLayer: return BlockShapes.SnowLayer;
            case BlockType.Cactus: return BlockShapes.Cactus;
            case BlockType.Cake: return BlockShapes.Cake;
            case BlockType.Bed: return BlockShapes.Bed;
            case BlockType.StonePressurePlate: case BlockType.WoodenPressurePlate: return BlockShapes.PressurePlate;
            case BlockType.RedstoneRepeater: case BlockType.PoweredRepeater: return BlockShapes.Repeater;
            case BlockType.Trapdoor: return BlockShapes.Trapdoor;
            case BlockType.StoneButton: return BlockShapes.WallButton;
            default: return BlockShapes.FullBlock;
        }
    }
    public static BlockShape GetShape(this BlockShapes shape)
    {
        if (SpecialBlockGeometry.Encoded(shape)) return SpecialBlockGeometry.Decode(shape);
        switch (shape)
        {
            case BlockShapes.Flower: return BlockShapesList.Flower;
            case BlockShapes.Mushroom: return BlockShapesList.Mushroom;
            case BlockShapes.Bush: return BlockShapesList.Bush;
            case BlockShapes.Reed: return BlockShapesList.Reed;
            case BlockShapes.Crop: return BlockShapesList.Crop;
            case BlockShapes.Slab: return BlockShapesList.Slab;
            case BlockShapes.Farmland: return BlockShapesList.Farmland;
            case BlockShapes.SnowLayer: return BlockShapesList.SnowLayer;
            case BlockShapes.Cactus: return BlockShapesList.Cactus;
            case BlockShapes.Cake: return BlockShapesList.Cake;
            case BlockShapes.Bed: return BlockShapesList.Bed;
            case BlockShapes.PressurePlate: return BlockShapesList.PressurePlate;
            case BlockShapes.Repeater: return BlockShapesList.Repeater;
            case BlockShapes.Trapdoor: return BlockShapesList.Trapdoor;
            case BlockShapes.WallButton: return BlockShapesList.WallButton;
            default: return BlockShapesList.FullBlock;
        }
    }
    public static bool IsCrop(BlockType type)
    {
        return type == BlockType.WheatCrop0 || (type >= BlockType.WheatCrop1 && type <= BlockType.WheatCrop7);
    }
    public static bool IsPlant(BlockType type)
    {
        var shape = ForBlock(type);
        return shape == BlockShapes.Flower || shape == BlockShapes.Mushroom ||
            shape == BlockShapes.Bush || shape == BlockShapes.Reed || shape == BlockShapes.Crop;
    }
    public static BlockShape CollisionShape(BlockType type, BlockShape shape)
    {
        // Beta farmland collides at full height; soul sand's drawn cube is
        // full height but its collision surface is two pixels lower.
        if (type == BlockType.Farmland || type == BlockType.MoistFarmland) return BlockShapesList.FullBlock;
        if (type == BlockType.SoulSand) return BlockShapesList.SoulSandCollision;
        if (type == BlockType.Cactus) return BlockShapesList.CactusCollision;
        if (type == BlockType.Fence) return SpecialBlockGeometry.FenceCollision;
        return shape;
    }
}

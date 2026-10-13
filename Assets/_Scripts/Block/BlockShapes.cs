public static class BlockShapesList
{
    public static readonly BlockShape FullBlock = new BlockShape(0, 0, 0, 16, 16, 16);
    public static readonly BlockShape Slab = new BlockShape(0, 0, 0, 16, 8, 16);
    public static readonly BlockShape Farmland = new BlockShape(0, 0, 0, 16, 15, 16);
    public static readonly BlockShape SnowLayer = new BlockShape(0, 0, 0, 16, 2, 16);
    public static readonly BlockShape Cactus = new BlockShape(1, 0, 1, 15, 16, 15);
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
public enum BlockShapes { FullBlock = 0, Slab = 1, Farmland, SnowLayer, Cactus, Cake, Bed, PressurePlate, Repeater, Trapdoor, WallButton }

public static class BlockShapesExtensions
{
    public static BlockShapes ForBlock(BlockType type)
    {
        switch (type)
        {
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
        switch (shape)
        {
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
    public static BlockShape CollisionShape(BlockType type, BlockShape shape)
    {
        // Beta farmland collides at full height; soul sand's drawn cube is
        // full height but its collision surface is two pixels lower.
        if (type == BlockType.Farmland || type == BlockType.MoistFarmland) return BlockShapesList.FullBlock;
        if (type == BlockType.SoulSand) return BlockShapesList.SoulSandCollision;
        return shape;
    }
}

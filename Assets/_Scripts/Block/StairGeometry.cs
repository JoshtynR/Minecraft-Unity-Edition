using System;
using UnityEngine;

// Beta 1.7.3 metadata: 0 high +X, 1 high -X, 2 high +Z, 3 high -Z.
// No upside-down or corner stairs in this version.
public static class StairGeometry
{
    private static readonly BlockShape[] Upper = {
        new BlockShape(8,8,0,16,16,16), new BlockShape(0,8,0,8,16,16),
        new BlockShape(0,8,8,16,16,16), new BlockShape(0,8,0,16,16,8) };
    private static readonly BlockShape[] Low = {
        new BlockShape(0,0,0,8,8,16), new BlockShape(8,0,0,16,8,16),
        new BlockShape(0,0,0,16,8,8), new BlockShape(0,0,8,16,8,16) };
    private static readonly BlockShape[] High = {
        new BlockShape(8,0,0,16,16,16), new BlockShape(0,0,0,8,16,16),
        new BlockShape(0,0,8,16,16,16), new BlockShape(0,0,0,16,16,8) };
    public static bool IsStair(BlockType type) => type == BlockType.WoodenStairs || type == BlockType.CobblestoneStairs;
    public static byte Facing(BlockShapes shape)
    {
        return shape >= BlockShapes.StairsEast && shape <= BlockShapes.StairsNorth
            ? (byte)(shape - BlockShapes.StairsEast) : (byte)0;
    }
    public static BlockShapes Shape(byte facing) => (BlockShapes)((int)BlockShapes.StairsEast + (facing & 3));
    public static byte FacingForLook(Vector3 look)
    {
        int quadrant = (int)Math.Floor(Math.Atan2(look.x, look.z) * 2 / Math.PI + .5) & 3;
        return quadrant == 0 ? (byte)2 : quadrant == 1 ? (byte)0 : quadrant == 2 ? (byte)3 : (byte)1;
    }
    public static BlockShape CollisionPart(byte facing, int part) => part == 0 ? Low[facing & 3] : High[facing & 3];
    public static BlockShape RenderFace(byte facing, int part, Direction direction)
    {
        // Match Beta's two side-by-side cuboids. Remove the low internal
        // wall and retain only the exposed upper half of the high riser.
        Direction highDirection = (facing & 3) == 0 ? Direction.right : (facing & 3) == 1 ? Direction.left :
            (facing & 3) == 2 ? Direction.forwards : Direction.backwards;
        Direction lowDirection = (facing & 3) == 0 ? Direction.left : (facing & 3) == 1 ? Direction.right :
            (facing & 3) == 2 ? Direction.backwards : Direction.forwards;
        if (part == 0) return direction == highDirection ? null : Low[facing & 3];
        return direction == lowDirection ? Upper[facing & 3] : High[facing & 3];
    }

    public static bool CoversFace(byte facing, BlockShape other, Direction direction)
    {
        if (direction == Direction.down) return BlockShapesList.FullBlock.CoversBoundaryFace(other, direction);
        return Low[facing & 3].CoversBoundaryFace(other, direction) || High[facing & 3].CoversBoundaryFace(other, direction);
    }
    public static bool TryDoubleSlab(BlockType single, BlockType below, out BlockType result)
    {
        result = single;
        if (single != below) return false;
        switch (single)
        {
            case BlockType.StoneSlab: result=BlockType.DoubleStoneSlab; return true;
            case BlockType.SandstoneSlab: result=BlockType.DoubleSandstoneSlab; return true;
            case BlockType.WoodenSlab: result=BlockType.DoubleWoodenSlab; return true;
            case BlockType.CobblestoneSlab: result=BlockType.DoubleCobblestoneSlab; return true;
            default: return false;
        }
    }
}

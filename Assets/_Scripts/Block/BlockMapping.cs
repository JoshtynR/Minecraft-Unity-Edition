public static class BlockMapping
{
    public static Block MapTypeToBlock(BlockType type, Block block)
    {
        switch (type)
        {
            case BlockType.Water: return new WaterBlock(block);
            case BlockType.WoodenSlab:
            case BlockType.StoneSlab:
            case BlockType.SandstoneSlab:
            case BlockType.CobblestoneSlab: return new SlabBlock(block);
            default: return block.Reset(block);
        }
    }
}

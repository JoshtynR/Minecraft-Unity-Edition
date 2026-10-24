public static class BlockMapping
{
    public static Block CreateGenerated(BlockType type, UnityEngine.Vector3Int position, ChunkSection section)
    {
        switch(WheatCropRules.Canonical(type))
        {
            case BlockType.Water:return new WaterBlock(position,section);
            case BlockType.WoodenSlab:
            case BlockType.StoneSlab:
            case BlockType.SandstoneSlab:
            case BlockType.CobblestoneSlab:return new SlabBlock(type,position,section);
            default:return new Block(type,position,section);
        }
    }

    public static Block MapGeneratedTypeToBlock(Block block)
    {
        switch (block.type)
        {
            case BlockType.Water: return block is WaterBlock ? block : new WaterBlock(block);
            case BlockType.WoodenSlab:
            case BlockType.StoneSlab:
            case BlockType.SandstoneSlab:
            case BlockType.CobblestoneSlab: return block is SlabBlock ? block : new SlabBlock(block);
            default: return block;
        }
    }

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

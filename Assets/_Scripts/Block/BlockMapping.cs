public static class BlockMapping
{
    public static Block CreateGenerated(BlockType type, UnityEngine.Vector3Int position, ChunkSection section)
    {
        return new Block(type,position,section);
    }

    public static Block MapGeneratedTypeToBlock(Block block)
    {
        return block;
    }

    public static Block MapTypeToBlock(BlockType type, Block block)
    {
        // Mapping no longer swaps in per-type subclasses, so the block already
        // has the canonical type/shape/state. Avoid decoding and re-encoding
        // the same object on every placement and deserialized cell.
        return block;
    }
}

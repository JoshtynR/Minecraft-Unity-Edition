using UnityEngine;

public static class SlabBlock
{
    public static Block Create(BlockType type, Vector3Int position, ChunkSection section)
    {
        var block = new Block(type, position, section);
        block.blockShape = BlockShapes.Slab;
        return block;
    }
}

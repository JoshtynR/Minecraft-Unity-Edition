using System;
using UnityEngine;

[Serializable]
public class SlabBlock : Block
{
    public string test;
    public SlabBlock(BlockType type,Vector3Int position,ChunkSection section) : base(type,position,section)
    {
        blockShape=BlockShapes.Slab;
    }
    public SlabBlock(Block block) : base(block)
    {
        blockShape = BlockShapes.Slab;
    }
}

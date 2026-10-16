using System;
using UnityEngine;

[Serializable]
public class BlockSaveData
{
    public BlockType type;
    public int length;
    public byte waterLevel;
    public byte stairFacing; // Legacy field name; now carries all native 4-bit block state.
    public bool hasBlockState; // Old saves use type defaults, while stairs retain their old facing.
    
    public BlockSaveData(BlockType type, int length, byte waterLevel = 0, byte stairFacing = 0)
    {
        this.type = type;
        this.length = length;
        this.waterLevel = waterLevel;
        this.stairFacing = (byte)(stairFacing & 15);
        hasBlockState = true;
    }
    
    public BlockSaveData()
    {
    }
}

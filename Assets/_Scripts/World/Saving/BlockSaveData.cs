using System;
using UnityEngine;

[Serializable]
public class BlockSaveData
{
    public BlockType type;
    public int length;
    public byte waterLevel;
    public byte stairFacing; // Missing in older JSON saves: default facing 0.
    
    public BlockSaveData(BlockType type, int length, byte waterLevel = 0, byte stairFacing = 0)
    {
        this.type = type;
        this.length = length;
        this.waterLevel = waterLevel;
        this.stairFacing = (byte)(stairFacing & 3);
    }
    
    public BlockSaveData()
    {
    }
}

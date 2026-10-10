using System;
using UnityEngine;

[Serializable]
public class BlockSaveData
{
    public BlockType type;
    public int length;
    public byte waterLevel;
    
    public BlockSaveData(BlockType type, int length, byte waterLevel = 0)
    {
        this.type = type;
        this.length = length;
        this.waterLevel = waterLevel;
    }
    
    public BlockSaveData()
    {
    }
}

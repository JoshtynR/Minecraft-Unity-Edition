using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChunkSectionSaveData
{
    // [SerializeReference]
    public BlockSaveData[] blocks;
    public int yOffset;

    public ChunkSectionSaveData()
    {
        
    }
    
    public ChunkSectionSaveData(ChunkSection section)
    {
        var blocksL = new List<BlockSaveData>(); 
        // Here we iterate through all the blocks in the chunk section
        // and save them with run length encoding
        
        int size = section.dataRef.chunkSize;
        int height = section.dataRef.chunkHeight;
        for (int i = 0; i < section.blocks.Length;)
        {
            Block first = section.blocks[i % size, (i / size) % height, i / (size * height)];
            byte level = first.type == BlockType.Water ? first.waterLevel : (byte)0;
            byte facing = SpecialBlockGeometry.State(first.type, first.blockShape);
            int count = 1;
            while (i + count < section.blocks.Length)
            {
                int next = i + count;
                Block block = section.blocks[next % size, (next / size) % height, next / (size * height)];
                if (block.type != first.type || (block.type == BlockType.Water && block.waterLevel != level) ||
                    SpecialBlockGeometry.State(block.type, block.blockShape) != facing) break;
                count++;
            }
            blocksL.Add(new BlockSaveData(first.type, count, level, facing));
            i += count;
        }

        blocks = blocksL.ToArray();
        yOffset = section.yOffset;
    }
}

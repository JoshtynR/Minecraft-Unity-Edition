using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class ChunkSection
{
    public Block[,,] blocks;
    // Sky and block light are four bits each. A byte is the complete cell;
    // char doubled this array for every loaded section without storing more data.
    private byte[,,] lightMap;
    public int yOffset;
    public ChunkData dataRef;

    public ChunkSection(ChunkData dataRef, int yOffset, BlockType populate = BlockType.Nothing, bool initializeBlocks = true)
    {
        this.dataRef = dataRef;
        this.yOffset = yOffset;
        blocks = new Block[dataRef.chunkSize, dataRef.chunkHeight, dataRef.chunkSize];
        lightMap = new byte[dataRef.chunkSize, dataRef.chunkHeight, dataRef.chunkSize];
        if (initializeBlocks) Populate(populate);
    }

    // Terrain generation already owns the final block type for every cell. Fill
    // an uninitialized section once instead of constructing 4096 Nothing blocks
    // and immediately mutating/replacing every one of them.
    public void InitializeGenerated(BlockType[,,] source, bool reflectX, int sourceHeight)
    {
        for (int x = 0; x < dataRef.chunkSize; x++)
        for (int y = 0; y < dataRef.chunkHeight; y++)
        for (int z = 0; z < dataRef.chunkSize; z++)
        {
            int worldY=yOffset+y;
            int sourceX=reflectX?dataRef.chunkSize-1-x:x;
            var type=worldY<sourceHeight?source[sourceX,worldY,z]:BlockType.Air;
            blocks[x,y,z]=BlockMapping.CreateGenerated(type,new Vector3Int(x,y,z),this);
        }
    }

    public static ChunkSection Deserialize(ChunkSectionSaveData saveData, ChunkData dataRef)
    {
        var chunkSection = new ChunkSection(dataRef, saveData.yOffset, BlockType.Air, false);

        var pos = new Vector3Int(0, 0, 0);
        var posIndex = 0;
        for (var i = 0; i < saveData.blocks.Length; i++)
        {
            for (var j = 0; j < saveData.blocks[i].length; j++)
            {
                pos.x = posIndex % dataRef.chunkSize;
                pos.z = posIndex / (dataRef.chunkSize * dataRef.chunkHeight);
                pos.y = (posIndex / dataRef.chunkSize) % dataRef.chunkHeight;

                var block = new Block(saveData.blocks[i].type, pos, chunkSection);
                if (saveData.blocks[i].hasBlockState || StairGeometry.IsStair(block.type))
                    block.blockShape = SpecialBlockGeometry.Shape(block.type, WheatCropRules.IsLegacy(saveData.blocks[i].type) ? WheatCropRules.LegacyAge(saveData.blocks[i].type) : saveData.blocks[i].stairFacing);
                block.waterLevel = block.type == BlockType.Water || block.type == BlockType.Lava || block.type == BlockType.Fire ? (byte)(saveData.blocks[i].waterLevel & 15) : (byte)0;
                chunkSection.blocks[pos.x, pos.y, pos.z] = BlockMapping.MapTypeToBlock(block.type, block);
                chunkSection.blocks[pos.x, pos.y, pos.z].Loaded();
                posIndex++;
            }
        }

        // Valid saves cover the full section. Preserve the old Air fallback for
        // truncated legacy/corrupt RLE data without paying for it on normal loads.
        while(posIndex<chunkSection.blocks.Length)
        {
            pos.x=posIndex%dataRef.chunkSize;
            pos.z=posIndex/(dataRef.chunkSize*dataRef.chunkHeight);
            pos.y=(posIndex/dataRef.chunkSize)%dataRef.chunkHeight;
            chunkSection.blocks[pos.x,pos.y,pos.z]=new Block(BlockType.Air,pos,chunkSection);
            posIndex++;
        }

        return chunkSection;
    }

    // Populate the chunk with the given block type
    private void Populate(BlockType type)
    {
        for (int x = 0; x < dataRef.chunkSize; x++)
            for (int y = 0; y < dataRef.chunkHeight; y++)
                for (int z = 0; z < dataRef.chunkSize; z++)
                    blocks[x, y, z] = new Block(type, new Vector3Int(x, y, z), this);
    }

    /// <summary>Returns the block at the given position. X,Z are local; Y is global.</summary>
    public Block GetBlock(Vector3Int pos)
    {
        return blocks[pos.x, pos.y - yOffset, pos.z];
    }

    /// <summary>Set a block type at the given position. X,Z are local; Y is global.</summary>
    public void SetBlock(Vector3Int pos, BlockType type)
    {
        pos.y -= yOffset;
        blocks[pos.x, pos.y, pos.z].SetType(type);
    }

    /// <summary>Initializes a cell before this chunk joins the live world.</summary>
    public void SetGeneratedBlock(Vector3Int pos, BlockType type)
    {
        pos.y -= yOffset;
        var block = blocks[pos.x, pos.y, pos.z];
        block.InitializeGeneratedType(type);
        blocks[pos.x, pos.y, pos.z] = BlockMapping.MapGeneratedTypeToBlock(block);
    }

    #region Lighting (packed into a char: XXXX0000 = skylight, 0000XXXX = blocklight)

    // Get XXXX0000
    public int GetSkylight(Vector3Int pos)
    {
        return (lightMap[pos.x, pos.y, pos.z] >> 4) & 0xF;
    }

    // Set XXXX0000
    public void SetSunlight(Vector3Int pos, int value)
    {
        lightMap[pos.x, pos.y, pos.z] = (byte)((lightMap[pos.x, pos.y, pos.z] & 0xF) | ((value & 0xF) << 4));
    }

    // Get 0000XXXX
    public int GetBlockLight(Vector3Int pos)
    {
        return lightMap[pos.x, pos.y, pos.z] & 0xF;
    }

    // Set 0000XXXX
    public void SetBlockLight(Vector3Int pos, int value)
    {
        lightMap[pos.x, pos.y, pos.z] = (byte)((lightMap[pos.x, pos.y, pos.z] & 0xF0) | (value & 0xF));
    }

    /// <summary>Highest of block or skylight at pos.</summary>
    public int GetLight(Vector3Int pos)
    {
        return Math.Max(GetSkylight(pos), GetBlockLight(pos));
    }

    #endregion

    public Vector3Int GetGlobalBlockCoords(Vector3Int localPos)
    {
        return new Vector3Int
        (
            localPos.x + dataRef.worldPos.x,
            localPos.y + yOffset,
            localPos.z + dataRef.worldPos.z
        );
    }

    public static void ChooseFaceAxes(Vector3Int n, out Vector3Int b, out Vector3Int c)
    {
        if (n.x != 0) { b = new Vector3Int(0, 0, 1); c = new Vector3Int(0, 1, 0); }      // X faces: (Z,Y)
        else if (n.y != 0) { b = new Vector3Int(1, 0, 0); c = new Vector3Int(0, 0, 1); } // Y faces: (X,Z)
        else { b = new Vector3Int(1, 0, 0); c = new Vector3Int(0, 1, 0); }               // Z faces: (X,Y)
    }

}

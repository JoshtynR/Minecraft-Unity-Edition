using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class ChunkSection
{
    public Block[,,] blocks;
    private char[,,] lightMap;
    public int yOffset;
    public ChunkData dataRef;

    public ChunkSection(ChunkData dataRef, int yOffset, BlockType populate = BlockType.Nothing)
    {
        this.dataRef = dataRef;
        this.yOffset = yOffset;
        blocks = new Block[dataRef.chunkSize, dataRef.chunkHeight, dataRef.chunkSize];
        lightMap = new char[dataRef.chunkSize, dataRef.chunkHeight, dataRef.chunkSize];
        Populate(populate);
    }

    public static ChunkSection Deserialize(ChunkSectionSaveData saveData, ChunkData dataRef)
    {
        var chunkSection = new ChunkSection(dataRef, saveData.yOffset, BlockType.Air);

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

    #region Lighting (packed into a char: XXXX0000 = skylight, 0000XXXX = blocklight)

    // Get XXXX0000
    public int GetSkylight(Vector3Int pos)
    {
        return (lightMap[pos.x, pos.y, pos.z] >> 4) & 0xF;
    }

    // Set XXXX0000
    public void SetSunlight(Vector3Int pos, int value)
    {
        lightMap[pos.x, pos.y, pos.z] = (char)((lightMap[pos.x, pos.y, pos.z] & 0xF) | (value << 4));
    }

    // Get 0000XXXX
    public int GetBlockLight(Vector3Int pos)
    {
        return lightMap[pos.x, pos.y, pos.z] & 0xF;
    }

    // Set 0000XXXX
    public void SetBlockLight(Vector3Int pos, int value)
    {
        lightMap[pos.x, pos.y, pos.z] = (char)((lightMap[pos.x, pos.y, pos.z] & 0xF0) | value);
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class ChunkSection
{
    public sealed class PackedBlockStorage : IEnumerable<Block>
    {
        private readonly ChunkSection owner;
        private readonly int size;
        private readonly int height;
        private readonly ushort[] cells;

        internal PackedBlockStorage(ChunkSection owner, int size, int height)
        {
            this.owner = owner;
            this.size = size;
            this.height = height;
            cells = new ushort[size * height * size];
        }

        public int Length => cells.Length;
        public bool HasNonAir
        {
            get
            {
                for (int i = 0; i < cells.Length; i++)
                    if ((BlockType)(cells[i] & 0x0FFF) != BlockType.Air) return true;
                return false;
            }
        }
        internal int Index(int x, int y, int z) => x + y * size + z * size * height;
        internal ushort Read(ushort packedPosition)
        {
            int x = packedPosition & 15, y = (packedPosition >> 4) & 15, z = (packedPosition >> 8) & 15;
            return cells[Index(x, y, z)];
        }
        internal void Write(ushort packedPosition, ushort cell)
        {
            int x = packedPosition & 15, y = (packedPosition >> 4) & 15, z = (packedPosition >> 8) & 15;
            cells[Index(x, y, z)] = cell;
        }
        internal void Set(int x, int y, int z, BlockType type, byte state)
        {
            type = WheatCropRules.Canonical(type);
            cells[Index(x, y, z)] = (ushort)(((int)type & 0x0FFF) | ((state & 15) << 12));
        }
        internal void Fill(BlockType type)
        {
            byte state = WheatCropRules.IsCrop(type) ? WheatCropRules.LegacyAge(type) : SpecialBlockGeometry.DefaultState(type);
            type = WheatCropRules.Canonical(type);
            ushort cell = (ushort)(((int)type & 0x0FFF) | ((state & 15) << 12));
            for (int i = 0; i < cells.Length; i++) cells[i] = cell;
        }
        public Block this[int x, int y, int z]
        {
            get
            {
                ushort position = (ushort)((x & 15) | ((y & 15) << 4) | ((z & 15) << 8));
                return Block.Attached(position, owner);
            }
            set { cells[Index(x, y, z)] = value.PackedCell; }
        }
        public IEnumerator<Block> GetEnumerator()
        {
            for (int z = 0; z < size; z++)
            for (int y = 0; y < height; y++)
            for (int x = 0; x < size; x++) yield return this[x, y, z];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public PackedBlockStorage blocks;
    // Sky and block light are four bits each. A byte is the complete cell;
    // char doubled this array for every loaded section without storing more data.
    private byte[] lightMap;
    public int yOffset;
    public ChunkData dataRef;

    public ChunkSection(ChunkData dataRef, int yOffset, BlockType populate = BlockType.Nothing, bool initializeBlocks = true)
    {
        this.dataRef = dataRef;
        this.yOffset = yOffset;
        blocks = new PackedBlockStorage(this, dataRef.chunkSize, dataRef.chunkHeight);
        lightMap = new byte[dataRef.chunkSize * dataRef.chunkHeight * dataRef.chunkSize];
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
            byte state = WheatCropRules.IsCrop(type) ? WheatCropRules.LegacyAge(type) : SpecialBlockGeometry.DefaultState(type);
            blocks.Set(x, y, z, type, state);
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

                BlockType type = WheatCropRules.Canonical(saveData.blocks[i].type);
                byte state = (saveData.blocks[i].hasBlockState || StairGeometry.IsStair(type))
                    ? (WheatCropRules.IsLegacy(saveData.blocks[i].type) ? WheatCropRules.LegacyAge(saveData.blocks[i].type) : saveData.blocks[i].stairFacing)
                    : SpecialBlockGeometry.DefaultState(type);
                if (type == BlockType.Water || type == BlockType.Lava || type == BlockType.Fire)
                    state = (byte)(saveData.blocks[i].waterLevel & 15);
                chunkSection.blocks.Set(pos.x, pos.y, pos.z, type, state);
                var loaded = chunkSection.blocks[pos.x, pos.y, pos.z];
                loaded.Loaded();
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
            chunkSection.blocks.Set(pos.x, pos.y, pos.z, BlockType.Air, 0);
            posIndex++;
        }

        return chunkSection;
    }

    // Populate the chunk with the given block type
    private void Populate(BlockType type)
    {
        blocks.Fill(type);
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
        byte state = WheatCropRules.IsCrop(type) ? WheatCropRules.LegacyAge(type) : SpecialBlockGeometry.DefaultState(type);
        blocks.Set(pos.x, pos.y, pos.z, type, state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ushort ReadPackedCell(ushort packedPosition) => blocks.Read(packedPosition);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WritePackedCell(ushort packedPosition, ushort cell) => blocks.Write(packedPosition, cell);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int LightIndex(Vector3Int pos) => pos.x + pos.y * dataRef.chunkSize + pos.z * dataRef.chunkSize * dataRef.chunkHeight;

    #region Lighting (packed into a byte: XXXX0000 = skylight, 0000XXXX = blocklight)

    // Get XXXX0000
    public int GetSkylight(Vector3Int pos)
    {
        return (lightMap[LightIndex(pos)] >> 4) & 0xF;
    }

    // Set XXXX0000
    public void SetSunlight(Vector3Int pos, int value)
    {
        int index = LightIndex(pos);
        lightMap[index] = (byte)((lightMap[index] & 0xF) | ((value & 0xF) << 4));
    }

    // Get 0000XXXX
    public int GetBlockLight(Vector3Int pos)
    {
        return lightMap[LightIndex(pos)] & 0xF;
    }

    // Set 0000XXXX
    public void SetBlockLight(Vector3Int pos, int value)
    {
        int index = LightIndex(pos);
        lightMap[index] = (byte)((lightMap[index] & 0xF0) | (value & 0xF));
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

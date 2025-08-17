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
                chunkSection.blocks[pos.x, pos.y, pos.z] = block;
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

    // -----------------------------------------------------------------------------
    // Smooth lighting sampling helpers (for per-vertex sky/block floats 0..15)
    // -----------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool InBounds(Vector3Int p)
    {
        int sx = dataRef.chunkSize;
        int sy = dataRef.chunkHeight;
        return (uint)p.x < (uint)sx &&
               (uint)p.z < (uint)sx &&
               (uint)p.y < (uint)sy;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SkyLocal(Vector3Int p) => InBounds(p) ? GetSkylight(p) : 15; // OOB = air column
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int BlockLocal(Vector3Int p) => InBounds(p) ? GetBlockLight(p) : 0;  // OOB = no block light

    /// <summary>
    /// Pick in-plane axes (b,c) for a face normal n. X faces -> (Z,Y); Y -> (X,Z); Z -> (X,Y)
    /// </summary>
    public static void ChooseFaceAxes(Vector3Int n, out Vector3Int b, out Vector3Int c)
    {
        if (n.x != 0) { b = new Vector3Int(0, 0, 1); c = new Vector3Int(0, 1, 0); }      // X faces: (Z,Y)
        else if (n.y != 0) { b = new Vector3Int(1, 0, 0); c = new Vector3Int(0, 0, 1); } // Y faces: (X,Z)
        else { b = new Vector3Int(1, 0, 0); c = new Vector3Int(0, 1, 0); }               // Z faces: (X,Y)
    }

    /// <summary>
    /// Smoothly sample sky/block for ONE vertex at a face corner using LOCAL coords.
    /// faceLocal: local coords of the solid block whose face is visible.
    /// n: face normal (+/-X/Y/Z). b & c: in-plane axes from ChooseFaceAxes.
    /// sB/sC: corner signs (-1 or +1) along b and c for this vertex.
    /// Returns sky/block floats in 0..15 (NOT rounded) for smooth interpolation in shader.
    /// </summary>
    public void SampleVertexSkyBlockSmoothLocal

    (
    Vector3Int faceLocal,
    Vector3Int n, Vector3Int b, Vector3Int c,
    int sB, int sC,
    out float sky, out float block,
    float frontLayerWeight = 0.65f  // 0.6–0.7 feels good
    )

    {
        // FRONT = just outside the face (air side), BACK = the solid cell itself
        var front = faceLocal + n;
        var back = faceLocal;

        // In-plane corner offsets for this vertex (±b, ±c based on sB/sC)
        var oB = b * sB;
        var oC = c * sC;

        // 2×2 "tent" taps (weights 4,2,2,1 → sum=9). Same pattern on front/back.
        // FRONT positions (air side)
        var f00 = front;           // center
        var f10 = front + oB;      // along b
        var f01 = front + oC;      // along c
        var f11 = front + oB + oC; // diagonal

        // BACK positions (inside)
        var b00 = back;
        var b10 = back + oB;
        var b01 = back + oC;
        var b11 = back + oB + oC;

        float wf = frontLayerWeight / 9f;
        float wb = (1f - frontLayerWeight) / 9f;

        float sAcc = 0f, blAcc = 0f;

        // FRONT sky/block
        sAcc += SkyAt(f00) * 4f * wf;  blAcc += BlockAt(f00) * 4f * wf;
        sAcc += SkyAt(f10) * 2f * wf;  blAcc += BlockAt(f10) * 2f * wf;
        sAcc += SkyAt(f01) * 2f * wf;  blAcc += BlockAt(f01) * 2f * wf;
        sAcc += SkyAt(f11) * 1f * wf;  blAcc += BlockAt(f11) * 1f * wf;

        // BACK sky/block
        sAcc += SkyAt(b00) * 4f * wb;  blAcc += BlockAt(b00) * 4f * wb;
        sAcc += SkyAt(b10) * 2f * wb;  blAcc += BlockAt(b10) * 2f * wb;
        sAcc += SkyAt(b01) * 2f * wb;  blAcc += BlockAt(b01) * 2f * wb;
        sAcc += SkyAt(b11) * 1f * wb;  blAcc += BlockAt(b11) * 1f * wb;

        sky = Mathf.Clamp(sAcc, 0f, 15f);
        block = Mathf.Clamp(blAcc, 0f, 15f);
    }



    /// <summary>
    /// Convenience when the caller has GLOBAL Y: converts to local Y then samples.
    /// faceGlobal: x,z local; y global (to match your other APIs).
    /// </summary>
    public void SampleVertexSkyBlockSmoothGlobal(
        Vector3Int faceGlobal,
        Vector3Int n, Vector3Int b, Vector3Int c,
        int sB, int sC,
        out float sky, out float block,
        float frontLayerWeight = 0.6f
    )
    {
        var faceLocal = new Vector3Int(faceGlobal.x, faceGlobal.y - yOffset, faceGlobal.z);
        SampleVertexSkyBlockSmoothLocal(faceLocal, n, b, c, sB, sC, out sky, out block, frontLayerWeight);
    }
    
        // Convert this section's local (x,y,z) to WORLD block coords
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3Int ToGlobal(Vector3Int local) => new Vector3Int(local.x, local.y + yOffset, local.z);

    // Ask ChunkData for the block at a WORLD coord (it will route to the right chunk/section)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Block BlockAtGlobal(Vector3Int localInThisSection)
    {
        var g = ToGlobal(localInThisSection);
        return dataRef.GetBlock(g); // your ChunkData.GetBlock(...) already handles cross-chunk
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SkyAt(Vector3Int p)
    {
        if (InBounds(p)) return GetSkylight(p);
        var b = BlockAtGlobal(p);
        return b != null ? b.GetSkyLight() : 0;   // if truly outside world, treat as 0
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int BlockAt(Vector3Int p)
    {
        if (InBounds(p)) return GetBlockLight(p);
        var b = BlockAtGlobal(p);
        return b != null ? b.GetBlockLight() : 0;
}

}

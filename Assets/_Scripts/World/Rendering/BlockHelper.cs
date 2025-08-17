using System;
using Cysharp.Threading.Tasks;
using UnityEngine;


public static class BlockHelper
{
    public static readonly Direction[] directions =
    {
        Direction.forwards,
        Direction.backwards,
        Direction.left,
        Direction.right,
        Direction.up,
        Direction.down,
    };
    
    public static readonly Direction[] revDirections =
    {
        Direction.backwards,
        Direction.forwards,
        Direction.right,
        Direction.left,
        Direction.down,
        Direction.up,
    };
    
    public static readonly Block NOTHING = new Block(BlockType.Nothing, Vector3Int.zero, null);
    
    
    //NOTE: block vertices start from bottom left and go clockwise

    public static MeshData GetMeshData(ChunkData chunk, Vector3Int pos, MeshData meshData, BlockType blockType)
    {
        if(blockType == BlockType.Air || blockType == BlockType.Nothing)
        {
            return meshData;
        }

        BlockTypeData blockTypeData = BlockDataManager.blockTypeDataDictionary[(int)blockType];
        Block block = chunk.GetBlock(pos);

        foreach (var dir in directions)
        {
            Vector3Int neighbourPos = pos + dir.GetVector();

            Block neighbourBlock = chunk.GetBlock(neighbourPos);
            BlockType neighbourBlockType = neighbourBlock.type;
            if (true/*neighbourBlockType != BlockType.Nothing*/)
            {
                BlockTypeData neighbourBlockTypeData = BlockDataManager.blockTypeDataDictionary[(int)neighbourBlockType];

                if (blockTypeData.isTransparent)
                {
                    if (blockType == BlockType.Water)
                    {
                        if (neighbourBlockType != BlockType.Water && neighbourBlockTypeData.isTransparent)
                        {
                            meshData.transparentMesh = GetFaceDataIn(dir, pos, meshData.transparentMesh, block,blockTypeData, chunk);
                        }
                    }
                    else if (neighbourBlockTypeData.isTransparent)
                    {
                        meshData.transparentMesh = GetFaceDataIn(dir, pos, meshData.transparentMesh, block,blockTypeData, chunk);
                    }
                }
                else if(neighbourBlockTypeData.isTransparent || !neighbourBlock.blockShape.GetShape().isFullBlock() && !neighbourBlock.blockShape.GetShape().isSideFull(dir.GetOpposite()))
                {
                    meshData = GetFaceDataIn(dir, pos, meshData, block,blockTypeData, chunk);
                } else if (!block.blockShape.GetShape().isFullBlock() && !block.blockShape.GetShape().isSideFull(dir))
                {
                    meshData = GetFaceDataIn(dir, pos, meshData, block,blockTypeData, chunk);
                }
            }
        }
        return meshData;
    }

    public static MeshData GetFaceDataIn(Direction dir, Vector3Int pos, MeshData meshData, Block block, BlockTypeData blockTypeData, ChunkData chunk)
    {
        block.blockShape.GetShape().SetFaceVertices(dir,pos,meshData);
        if (chunk != null)
        {
            GetFaceLightingAndAO(dir, pos, meshData, chunk);
        }
        meshData.AddQuadTriangles();
        var uvs = FaceUVs(dir, block.type, blockTypeData);
        meshData.uv.AddRange(uvs);

        // Add tint from block data
        meshData.AddQuadColor(blockTypeData.albedoTint);


        return meshData;
    }
    
    public static Vector2Int TexturePosition(Direction dir, BlockTypeData blockTypeData)
    {
        return dir switch
        {
            Direction.up => blockTypeData.textureData.up,
            Direction.down => blockTypeData.textureData.down,
            _ => blockTypeData.textureData.side
        };
    }

    public static Vector2 TextureExtends(Direction dir, BlockTypeData blockTypeData)
    {
        return dir switch
        {
            Direction.up => blockTypeData.textureData.upExtends,
            Direction.down => blockTypeData.textureData.downExtends,
            _ => blockTypeData.textureData.sideExtends
        };
    }

        public static void GetFaceLightingAndAO(Direction direction, Vector3Int pos, MeshData meshData, ChunkData chunk)
    {
        // assumes SetFaceVertices already appended 4 verts (bottom-left clockwise)
        var section = chunk.GetBlock(pos).section;
        if (section == null)
        {
            // keep arrays aligned
            for (int i = 0; i < 4; i++)
            {
                meshData.skyLight.Add(0);
                meshData.blockLight.Add(0);
                meshData.sides.Add(Vector3.zero);
            }
            return;
        }

        // face normal and in-plane axes
        Vector3Int n = direction.GetVector();
        ChunkSection.ChooseFaceAxes(n, out var uAxis, out var vAxis);

        // base index of the 4 verts we just wrote
        int baseIdx = meshData.vertices.Count - 4;

        for (int i = 0; i < 4; i++)
        {
            Vector3 v = meshData.vertices[baseIdx + i];

            // derive corner signs from actual vertex position (relative to cell)
            float alongU = Vector3.Dot(v - (Vector3)pos, (Vector3)uAxis); // ~0 or ~1
            float alongV = Vector3.Dot(v - (Vector3)pos, (Vector3)vAxis); // ~0 or ~1
            int sU = (alongU >= 0.5f) ? +1 : -1;
            int sV = (alongV >= 0.5f) ? +1 : -1;

            // ---- smooth lighting (avg 4 samples just outside the face) ----
            // tweak the bias (last arg) to taste: ~0.6..0.7 feels MC-like
            section.SampleVertexSkyBlockSmoothGlobal(
                pos, n, uAxis, vAxis, sU, sV,
                out float sky, out float blk, 0.65f
            );

            meshData.skyLight.Add(sky);     // floats 0..15
            meshData.blockLight.Add(blk);   // floats 0..15

            // ---- MC-style AO flags (sideU, sideV, corner) at the corner ----
            // sample just one cell outside the face:
            var pCenter = pos + n;
            var pU      = pCenter + uAxis * sU;
            var pV      = pCenter + vAxis * sV;
            var pDiag   = pCenter + uAxis * sU + vAxis * sV;

            bool sideU  = Occludes(chunk.GetBlock(pU));
            bool sideV  = Occludes(chunk.GetBlock(pV));
            bool diag   = Occludes(chunk.GetBlock(pDiag));

            meshData.sides.Add(new Vector3(sideU ? 1f : 0f, sideV ? 1f : 0f, diag ? 1f : 0f));
        }

        // local helper
        bool Occludes(Block b) =>
            b != null && b.type != BlockType.Nothing && b.BlockData.opacity >= 15;
    }


    public static int GetLightIndex(Vector3Int pos, ChunkData chunk)
    {
        return pos.x + pos.y * chunk.chunkSize + pos.z * chunk.chunkSize * World.Instance.worldHeight;
    }

    public static Vector2[] FaceUVs(Direction dir, BlockType type, BlockTypeData blockTypeData = null)
    {
        Vector2[] UVs = new Vector2[4];
        blockTypeData ??= BlockDataManager.blockTypeDataDictionary[(int)type];
        var tilePos = TexturePosition(dir, blockTypeData);
        var tileExtends = TextureExtends(dir, blockTypeData);
        var tileSizeX = BlockDataManager.tileSizeX;
        var tileSizeY = BlockDataManager.tileSizeY;

        UVs[0] = new Vector2(tilePos.x * tileSizeX, tilePos.y * tileSizeY);
        UVs[1] = new Vector2(tilePos.x * tileSizeX, (tilePos.y + tileExtends.y) * tileSizeY);
        UVs[2] = new Vector2((tilePos.x + tileExtends.x) * tileSizeX, (tilePos.y + tileExtends.y) * tileSizeY);
        UVs[3] = new Vector2((tilePos.x + tileExtends.x) * tileSizeX, tilePos.y * tileSizeY);

        return UVs;
    }
}

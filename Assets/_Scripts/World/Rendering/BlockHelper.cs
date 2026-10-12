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
        if (block.type == BlockType.Water && chunk != null)
            SetWaterFaceHeight(pos, meshData, chunk);
        meshData.AddQuadTriangles();
        meshData.AddFaceUVs(blockTypeData.textureData.FaceIndex(dir));

        // Add tint from block data
        meshData.AddQuadColor(blockTypeData.albedoTint);


        return meshData;
    }

    private static void SetWaterFaceHeight(Vector3Int pos, MeshData meshData, ChunkData chunk)
    {
        for (int i = meshData.vertices.Count - 4; i < meshData.vertices.Count; i++)
        {
            var vertex = meshData.vertices[i];
            if (vertex.y < pos.y + 0.5f) continue;
            int cornerX = pos.x + (vertex.x > pos.x + 0.5f ? 1 : 0);
            int cornerZ = pos.z + (vertex.z > pos.z + 0.5f ? 1 : 0);
            float height = WaterSurface.CornerHeight(p =>
            {
                var sample = chunk.GetBlock(new Vector3Int(p.x, p.y, p.z));
                if (sample.type == BlockType.Water) return sample.waterLevel;
                if (sample.type == BlockType.Air) return WaterSimulation.Air;
                return WaterSimulation.Solid;
            }, new WaterPosition(cornerX, pos.y, cornerZ));
            vertex.y = pos.y + height + 0.0005f;
            meshData.vertices[i] = vertex;
        }
    }
    
    public static void GetFaceLightingAndAO(Direction direction, Vector3Int pos, MeshData meshData, ChunkData chunk)
    {
        Vector3Int normal = direction.GetVector();
        ChunkSection.ChooseFaceAxes(normal, out var axisA, out var axisB);
        var front = pos + normal;
        var faceBlock = chunk.GetBlock(pos);
        int emission = faceBlock.BlockData.lightEmission;
        int darkness = chunk.worldRef.betaSkylightSubtracted;
        float flat = SampleBrightness(chunk, front, darkness, emission);
        bool smooth = faceBlock.type != BlockType.Water && faceBlock.type != BlockType.Lava &&
            faceBlock.blockShape.GetShape().isFullBlock();
        int firstVertex = meshData.vertices.Count - 4;
        for (int i = 0; i < 4; i++)
        {
            var vertex = meshData.vertices[firstVertex + i];
            int signA = Vector3.Dot(vertex - (Vector3)pos, (Vector3)axisA) >= 0.5f ? 1 : -1;
            int signB = Vector3.Dot(vertex - (Vector3)pos, (Vector3)axisB) >= 0.5f ? 1 : -1;
            var a = front + axisA * signA;
            var b = front + axisB * signB;
            float corner = smooth ? Beta173Lighting.CornerBrightness(flat,
                SampleBrightness(chunk, a, darkness, emission),
                SampleBrightness(chunk, b, darkness, emission),
                SampleBrightness(chunk, a + axisB * signB, darkness, emission),
                PassesCornerSample(chunk.GetBlock(a)), PassesCornerSample(chunk.GetBlock(b))) : flat;
            var sample = chunk.GetBlock(front);
            meshData.skyLight.Add(front.y >= chunk.worldRef.worldHeight ? 15 : sample.GetSkyLight());
            meshData.blockLight.Add(sample.GetBlockLight());
            // UV2: smooth brightness, flat brightness, Beta sample marker.
            meshData.sides.Add(new Vector3(corner, flat, 1));
        }
    }

    private static readonly Vector3Int[] SlabLightNeighbors =
        { Vector3Int.up, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };

    private static bool PassesCornerSample(Block block)
    {
        // All currently implemented solid/liquid materials use Beta's false
        // canBlockGrass entry; air (including outside bounds) uses true.
        return block == null || block.type == BlockType.Air || block.type == BlockType.Nothing;
    }

    private static float SampleBrightness(ChunkData chunk, Vector3Int position, int darkness, int emission)
    {
        if (position.y >= chunk.worldRef.worldHeight)
            return Beta173Lighting.Brightness(15, 0, darkness, emission);
        if (position.y < 0) return Beta173Lighting.Brightness(0, 0, darkness, emission);
        var block = chunk.GetBlock(position);
        if (block.type == BlockType.WoodenSlab)
        {
            // Beta half slabs sample above and four sides, never below.
            int sky = 0, light = 0;
            foreach (var dir in SlabLightNeighbors)
            {
                var neighbor = chunk.GetBlock(position + dir);
                sky = Math.Max(sky, neighbor.GetSkyLight());
                light = Math.Max(light, neighbor.GetBlockLight());
            }
            return Beta173Lighting.Brightness(sky, light, darkness, emission);
        }
        return Beta173Lighting.Brightness(block.GetSkyLight(), block.GetBlockLight(), darkness, emission);
    }

    public static int GetLightIndex(Vector3Int pos, ChunkData chunk)
    {
        return pos.x + pos.y * chunk.chunkSize + pos.z * chunk.chunkSize * World.Instance.worldHeight;
    }

    public static Vector3[] FaceUVs(Direction dir, BlockType type, BlockTypeData blockTypeData = null)
    {
        if (blockTypeData == null)
            blockTypeData = BlockDataManager.blockTypeDataDictionary[(int)type];
        int layer = blockTypeData.textureData.FaceIndex(dir);
        return new[]
        {
            new Vector3(0, 0, layer),
            new Vector3(0, 1, layer),
            new Vector3(1, 1, layer),
            new Vector3(1, 0, layer)
        };
    }
}

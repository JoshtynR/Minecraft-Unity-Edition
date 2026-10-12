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
                            meshData.waterMesh = GetFaceDataIn(dir, pos, meshData.waterMesh, block,blockTypeData, chunk);
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
        if (block.type == BlockType.Water && chunk != null)
            SetWaterFaceHeight(pos, meshData, chunk);
        if (chunk != null)
            GetFaceLightingAndAO(dir, pos, meshData, chunk);
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
        var faceBlock = chunk.GetBlock(pos);
        var shape = faceBlock.blockShape.GetShape();
        bool fullSmooth = chunk.worldRef.fullSmoothLighting;
        var front = pos + normal;
        // A slab's inset face is inside its block cell, whose brightness already
        // borrows from above/four sides. External cube and liquid faces use the
        // adjacent cell as before.
        if (fullSmooth && faceBlock.type != BlockType.Water && faceBlock.type != BlockType.Lava &&
            IsInsetFace(normal, shape)) front = pos;
        if (!fullSmooth && direction == Direction.up &&
            (faceBlock.type == BlockType.Water || faceBlock.type == BlockType.Lava)) front = pos;
        int emission = faceBlock.BlockData.lightEmission;
        int darkness = chunk.worldRef.betaSkylightSubtracted;
        float flat = SampleBrightness(chunk, front, darkness, emission);
        bool smooth = fullSmooth || (faceBlock.type != BlockType.Water && faceBlock.type != BlockType.Lava);
        float c00 = smooth ? SampleCorner(chunk, front, axisA, axisB, -1, -1, darkness, emission, flat) : flat;
        float c01 = smooth ? SampleCorner(chunk, front, axisA, axisB, -1, 1, darkness, emission, flat) : flat;
        float c11 = smooth ? SampleCorner(chunk, front, axisA, axisB, 1, 1, darkness, emission, flat) : flat;
        float c10 = smooth ? SampleCorner(chunk, front, axisA, axisB, 1, -1, darkness, emission, flat) : flat;
        int firstVertex = meshData.vertices.Count - 4;
        var sample = chunk.GetBlock(front);
        float q0 = 0, q1 = 0, q2 = 0, q3 = 0;
        for (int i = 0; i < 4; i++)
        {
            var vertex = meshData.vertices[firstVertex + i];
            float alongA = Vector3.Dot(vertex - (Vector3)pos, (Vector3)axisA);
            float alongB = Vector3.Dot(vertex - (Vector3)pos, (Vector3)axisB);
            float a = fullSmooth ? FaceCoordinate(alongA) : (alongA >= ShapeMidpoint(axisA, shape) ? 1 : 0);
            float b = fullSmooth ? FaceCoordinate(alongB) : (alongB >= ShapeMidpoint(axisB, shape) ? 1 : 0);
            float corner = Beta173Lighting.InterpolateCorners(c00, c01, c11, c10, a, b);
            if (i == 0) q0 = corner;
            else if (i == 1) q1 = corner;
            else if (i == 2) q2 = corner;
            else q3 = corner;
            meshData.skyLight.Add(front.y >= chunk.worldRef.worldHeight ? 15 : sample.GetSkyLight());
            meshData.blockLight.Add(sample.GetBlockLight());
            // Marker 1 retains Beta vertex interpolation; 2 enables fragment
            // bilinear interpolation. Both retain flat brightness for AO-off.
            meshData.sides.Add(new Vector3(corner, flat, fullSmooth ? 2 : 1));
        }
        // Face UVs are always (0,0),(0,1),(1,1),(1,0). Store the light values in
        // that same order, independently of face orientation or triangle order.
        var corners = new Vector4(q0, q1, q2, q3);
        for (int i = 0; i < 4; i++) meshData.smoothLightCorners.Add(corners);
    }

    private static float ShapeMidpoint(Vector3Int axis, BlockShape shape)
    {
        if (axis.x != 0) return (shape.xMin + shape.xMax) / 32f;
        if (axis.y != 0) return (shape.yMin + shape.yMax) / 32f;
        return (shape.zMin + shape.zMax) / 32f;
    }

    private static bool IsInsetFace(Vector3Int normal, BlockShape shape)
    {
        return (normal.x < 0 && shape.xMin > 0) || (normal.x > 0 && shape.xMax < 16) ||
            (normal.y < 0 && shape.yMin > 0) || (normal.y > 0 && shape.yMax < 16) ||
            (normal.z < 0 && shape.zMin > 0) || (normal.z > 0 && shape.zMax < 16);
    }

    private static float FaceCoordinate(float coordinate)
    {
        // BlockShape/water vertices include a small z-fighting offset. Snap cell
        // boundaries so full-cube reference corner values stay exact.
        coordinate -= 0.0005f;
        if (coordinate < 0.001f) return 0;
        if (coordinate > 0.999f) return 1;
        return coordinate;
    }

    private static float SampleCorner(ChunkData chunk, Vector3Int front, Vector3Int axisA,
        Vector3Int axisB, int signA, int signB, int darkness, int emission, float flat)
    {
        var a = front + axisA * signA;
        var b = front + axisB * signB;
        return Beta173Lighting.CornerBrightness(flat,
            SampleBrightness(chunk, a, darkness, emission),
            SampleBrightness(chunk, b, darkness, emission),
            SampleBrightness(chunk, a + axisB * signB, darkness, emission),
            PassesCornerSample(chunk.GetBlock(a)), PassesCornerSample(chunk.GetBlock(b)));
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

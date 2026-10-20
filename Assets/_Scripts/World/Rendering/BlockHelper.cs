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
        if(blockType==BlockType.Chest&&ModernChestVisual.Enabled)
        {
            if(meshData.chests==null)meshData.chests=new System.Collections.Generic.List<Vector3Int>();
            meshData.chests.Add(pos);return meshData;
        }

        if (SpecialBlockGeometry.Handles(blockType))
        {
            GetFaceDataIn(Direction.forwards, pos, SpecialBlockGeometry.NonOccluding(blockType) ? meshData.transparentMesh : meshData, block, blockTypeData, chunk);
            return meshData;
        }

        if (StairGeometry.IsStair(blockType))
        {
            foreach (var direction in directions)
                GetFaceDataIn(direction, pos, blockTypeData.isTransparent ? meshData.transparentMesh : meshData, block, blockTypeData, chunk);
            return meshData;
        }
        if (BlockShapesExtensions.IsPlant(blockType))
        {
            GetFaceDataIn(Direction.forwards, pos, meshData.transparentMesh, block, blockTypeData, chunk);
            return meshData;
        }
        foreach (var dir in directions)
        {
            Vector3Int neighbourPos = pos + dir.GetVector();

            Block neighbourBlock = chunk.GetBlock(neighbourPos);
            BlockType neighbourBlockType = neighbourBlock.type;
            if (true/*neighbourBlockType != BlockType.Nothing*/)
            {
                BlockTypeData neighbourBlockTypeData = BlockDataManager.blockTypeDataDictionary[(int)neighbourBlockType];

                var shape = block.blockShape.GetShape();
                var neighborShape = neighbourBlock.blockShape.GetShape();
                bool visible = BlockShapesExtensions.IsPlant(neighbourBlockType) || SpecialBlockGeometry.NonOccluding(neighbourBlockType) || !shape.TouchesCellBoundary(dir) || neighbourBlockTypeData.isTransparent ||
                    !(StairGeometry.IsStair(neighbourBlockType) ? StairGeometry.CoversFace(StairGeometry.Facing(neighbourBlock.blockShape), shape, dir.GetOpposite()) : neighborShape.CoversBoundaryFace(shape, dir.GetOpposite()));
                if (blockType == BlockType.Water || blockType == BlockType.Lava)
                {
                    if (neighbourBlockType != blockType && neighbourBlockType != BlockType.Ice && (dir == Direction.up || visible))
                        GetFaceDataIn(dir, pos, blockType==BlockType.Water?meshData.waterMesh:meshData, block, blockTypeData, chunk);
                }
                else if (visible)
                    GetFaceDataIn(dir, pos, blockTypeData.isTransparent ? meshData.transparentMesh : meshData, block, blockTypeData, chunk);

            }
        }
        return meshData;
    }

    public static MeshData GetFaceDataIn(Direction dir, Vector3Int pos, MeshData meshData, Block block, BlockTypeData blockTypeData, ChunkData chunk)
    {
        Color tint = BiomeBlockColors.ForFace(block.type, dir, chunk, pos, blockTypeData.albedoTint);
        if (SpecialBlockGeometry.Handles(block.type))
        {
            if (dir != Direction.forwards) return meshData;
            int first = meshData.vertices.Count;
            SpecialBlockMesh.Append(block.type, SpecialBlockGeometry.State(block.type, block.blockShape), pos,
                chunk == null ? (Func<Vector3Int,BlockType>)null : p => chunk.GetBlock(p).type,
                d => blockTypeData.textureData.FaceIndex(SpecialBlockGeometry.TextureDirection(block.type, SpecialBlockGeometry.State(block.type, block.blockShape), d)), blockTypeData.albedoTint, meshData, part => blockTypeData.textureData.PartIndex(part));
            if (chunk != null)
            {
                float brightness = SampleBrightness(chunk, pos, chunk.worldRef.betaSkylightSubtracted, block.BlockData.lightEmission);
                if (SpecialBlockGeometry.IsTorch(block.type) && block.BlockData.lightEmission > 0) brightness = 1;
                for (int i=first;i<meshData.vertices.Count;i++)
                {
                    int face=first+((i-first)/4)*4;
                    var a=meshData.vertices[face];var b=meshData.vertices[face+1];var c=meshData.vertices[face+2];
                    float ax=b.x-a.x,ay=b.y-a.y,az=b.z-a.z,bx=c.x-a.x,by=c.y-a.y,bz=c.z-a.z;
                    float nx=ay*bz-az*by,ny=az*bx-ax*bz,nz=ax*by-ay*bx;
                    bool flat=SpecialBlockGeometry.FlatShading(block.type);
                    float shade=flat?1:Math.Abs(ny)>=Math.Abs(nx)&&Math.Abs(ny)>=Math.Abs(nz)?(ny<0?.5f:1):Math.Abs(nx)>Math.Abs(nz)?.6f:.8f;
                    var sample=pos;
                    if (!SpecialBlockGeometry.NonOccluding(block.type))
                        sample+=Math.Abs(ny)>=Math.Abs(nx)&&Math.Abs(ny)>=Math.Abs(nz)?new Vector3Int(0,ny<0?-1:1,0):Math.Abs(nx)>Math.Abs(nz)?new Vector3Int(nx<0?-1:1,0,0):new Vector3Int(0,0,nz<0?-1:1);
                    float light=sample.Equals(pos)?brightness:SampleBrightness(chunk,sample,chunk.worldRef.betaSkylightSubtracted,block.BlockData.lightEmission);
                    var sampleBlock=chunk.GetBlock(sample);
                    meshData.skyLight.Add(sampleBlock.GetSkyLight());meshData.blockLight.Add(sampleBlock.GetBlockLight());
                    meshData.sides.Add(new Vector3(light*shade,light*shade,3));
                    meshData.smoothLightCorners.Add(new Vector4(light*shade,light*shade,light*shade,light*shade));
                }
            }
            return meshData;
        }
        if (BlockShapesExtensions.IsPlant(block.type))
        {
            // Emit the entire model once. Inventory and world share this path.
            if (dir != Direction.forwards) return meshData;
            int first = meshData.vertices.Count;
            PlantMesh.Append(block.type, pos, chunk == null ? pos : chunk.GetGlobalBlockCoords(pos),
                chunk != null, blockTypeData.textureData.FaceIndex(Direction.backwards), tint, meshData);
            if (chunk != null)
            {
                float brightness = SampleBrightness(chunk, pos, chunk.worldRef.betaSkylightSubtracted, block.BlockData.lightEmission);
                for (int i = first; i < meshData.vertices.Count; i++)
                {
                    meshData.skyLight.Add(block.GetSkyLight());
                    meshData.blockLight.Add(block.GetBlockLight());
                    // Marker 3: flat plant brightness, no AO or cube face shading.
                    meshData.sides.Add(new Vector3(brightness, brightness, 3));
                    meshData.smoothLightCorners.Add(new Vector4(brightness, brightness, brightness, brightness));
                }
            }
            return meshData;
        }
        if (StairGeometry.IsStair(block.type))
        {
            for (int part = 0; part < 2; part++)
            {
                var face = StairGeometry.RenderFace(StairGeometry.Facing(block.blockShape), part, dir);
                if (face == null) continue;
                if (chunk != null && face.TouchesCellBoundary(dir))
                {
                    var neighbor = chunk.GetBlock(pos + dir.GetVector());
                    if (!BlockDataManager.blockTypeDataDictionary[(int)neighbor.type].isTransparent && !BlockShapesExtensions.IsPlant(neighbor.type) && !SpecialBlockGeometry.NonOccluding(neighbor.type) &&
                        (StairGeometry.IsStair(neighbor.type) ? StairGeometry.CoversFace(StairGeometry.Facing(neighbor.blockShape), face, dir.GetOpposite()) :
                            neighbor.blockShape.GetShape().CoversBoundaryFace(face, dir.GetOpposite()))) continue;
                }
                face.SetFaceVertices(dir, pos, meshData);
                if (chunk != null) GetFaceLightingAndAO(dir, pos, meshData, chunk, face);
                meshData.AddQuadTriangles();
                meshData.AddFaceUVs(blockTypeData.textureData.FaceIndex(block.type, dir), face.FaceUVTransform(dir));
                meshData.AddQuadColor(blockTypeData.albedoTint);
            }
            return meshData;
        }
        block.blockShape.GetShape().SetFaceVertices(dir,pos,meshData);
        if ((block.type == BlockType.Water || block.type == BlockType.Lava) && chunk != null)
            SetWaterFaceHeight(pos, meshData, chunk);
        if (chunk != null)
            GetFaceLightingAndAO(dir, pos, meshData, chunk);
        meshData.AddQuadTriangles();
        var shape = block.blockShape.GetShape();
        // Cactus uses the complete face artwork on its inset sides/top, as in
        // Beta's special cactus renderer. Generic partial cuboids crop by bounds.
        if ((block.type == BlockType.Water || block.type == BlockType.Lava) && chunk != null)
            AddFluidUVs(dir, pos, meshData, chunk, blockTypeData.textureData);
        else meshData.AddFaceUVs(blockTypeData.textureData.FaceIndex(block.type, dir), block.type == BlockType.Cactus ? new Vector4(1,1,0,0) : shape.FaceUVTransform(dir));

        // Grass dirt/base sides stay neutral; only the top and overlay use the map.
        meshData.AddQuadColor(tint);

        int overlay=block.type==BlockType.Grass&&dir!=Direction.up&&dir!=Direction.down?
            blockTypeData.textureData.PartIndex(BlockTexturePart.GrassSideOverlay):0;
        if(overlay!=0)
        {
            int first=meshData.vertices.Count-4;
            var n=dir.GetVector();
            for(int i=0;i<4;i++)
            {
                var v=meshData.vertices[first+i];
                meshData.AddVertex(new Vector3(v.x+n.x*.0001f,v.y+n.y*.0001f,v.z+n.z*.0001f));
            }
            if(chunk!=null)GetFaceLightingAndAO(dir,pos,meshData,chunk);
            meshData.AddQuadTriangles();meshData.AddFaceUVs(overlay);
            meshData.AddQuadColor(BiomeBlockColors.ForFace(block.type, dir, chunk, pos, blockTypeData.albedoTint, true));
        }

        return meshData;
    }

    private static void SetWaterFaceHeight(Vector3Int pos, MeshData meshData, ChunkData chunk)
    {
        var liquid=chunk.GetBlock(pos).type;
        for (int i = meshData.vertices.Count - 4; i < meshData.vertices.Count; i++)
        {
            var vertex = meshData.vertices[i];
            if (vertex.y < pos.y + 0.5f) continue;
            int cornerX = pos.x + (vertex.x > pos.x + 0.5f ? 1 : 0);
            int cornerZ = pos.z + (vertex.z > pos.z + 0.5f ? 1 : 0);
            float height = WaterSurface.CornerHeight(p =>
            {
                var sample = chunk.GetBlock(new Vector3Int(p.x, p.y, p.z));
                if (sample.type == liquid) return sample.waterLevel;
                if (!World.FluidHasSolidMaterial(sample.type)) return WaterSimulation.Air;
                return WaterSimulation.Solid;
            }, new WaterPosition(cornerX, pos.y, cornerZ));
            vertex.y = pos.y + height + 0.0005f;
            meshData.vertices[i] = vertex;
        }
    }

    private static void AddFluidUVs(Direction dir, Vector3Int pos, MeshData meshData, ChunkData chunk, TextureData textures)
    {
        var liquid = chunk.GetBlock(pos).type;
        double angle = WaterSurface.FlowAngle(p =>
        {
            var b = chunk.GetBlock(new Vector3Int(p.x, p.y, p.z));
            if (b.type == liquid) return b.waterLevel;
            return World.FluidHasSolidMaterial(b.type) ? WaterSimulation.Solid : WaterSimulation.Air;
        }, new WaterPosition(pos.x, pos.y, pos.z));
        bool flowing = dir != Direction.down && (dir != Direction.up || angle > -999);
        int layer = textures.PartIndex(flowing ? BlockTexturePart.FluidFlow : BlockTexturePart.FluidStill,
            textures.FaceIndex(flowing ? Direction.left : Direction.up));
        int first = meshData.vertices.Count - 4;
        for (int i = 0; i < 4; i++)
        {
            var vertex = meshData.vertices[first + i];
            float u, v;
            if (dir == Direction.up)
                WaterSurface.TopUV(angle, vertex.x > pos.x + .5f, vertex.z > pos.z + .5f, out u, out v);
            else if (dir == Direction.down) { u = i == 1 || i == 2 ? 1 : 0; v = i >= 2 ? 1 : 0; }
            else
            {
                u = i >= 2 ? .4996875f : 0;
                v = .5f + (vertex.y - pos.y - .0005f) * .5f;
            }
            meshData.uv.Add(new Vector3(u, v, layer));
        }
        meshData.AddQuadFaceCoordinates();
    }
    
    public static void GetFaceLightingAndAO(Direction direction, Vector3Int pos, MeshData meshData, ChunkData chunk, BlockShape faceShape = null)
    {
        Vector3Int normal = direction.GetVector();
        ChunkSection.ChooseFaceAxes(normal, out var axisA, out var axisB);
        var faceBlock = chunk.GetBlock(pos);
        var shape = faceShape ?? faceBlock.blockShape.GetShape();
        bool fullSmooth = chunk.worldRef.fullSmoothLighting;
        var front = pos + normal;
        // A slab/stair inset face is inside its block cell, whose brightness already
        // borrows from above/four sides. External cube and liquid faces use the
        // adjacent cell as before.
        if ((fullSmooth || StairGeometry.IsStair(faceBlock.type) || BlockShapesExtensions.ForBlock(faceBlock.type) == BlockShapes.Slab) &&
            faceBlock.type != BlockType.Water && faceBlock.type != BlockType.Lava &&
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
        if (block == null || block.type == BlockType.Air || block.type == BlockType.Nothing) return true;
        BetaBlockCatalog.Entry entry;
        return BetaBlockCatalog.TryGet(block.type, out entry) && entry.CornerPass;
    }

    private static float SampleBrightness(ChunkData chunk, Vector3Int position, int darkness, int emission)
    {
        if (position.y >= chunk.worldRef.worldHeight)
            return Beta173Lighting.Brightness(15, 0, darkness, emission);
        if (position.y < 0) return Beta173Lighting.Brightness(0, 0, darkness, emission);
        var block = chunk.GetBlock(position);
        if (BlockShapesExtensions.ForBlock(block.type) == BlockShapes.Slab || StairGeometry.IsStair(block.type))
        {
            // Beta half slabs and stairs sample above and four sides, never below.
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
        int layer = blockTypeData.textureData.FaceIndex(type, dir);
        return new[]
        {
            new Vector3(0, 0, layer),
            new Vector3(0, 1, layer),
            new Vector3(1, 1, layer),
            new Vector3(1, 0, layer)
        };
    }
}

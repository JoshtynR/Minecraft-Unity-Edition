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
            var target = blockType == BlockType.NetherPortal ? (meshData.portalMesh ?? meshData.transparentMesh ?? meshData) :
                SpecialBlockGeometry.NonOccluding(blockType) ? meshData.transparentMesh : meshData;
            GetFaceDataIn(Direction.forwards, pos, target, block, blockTypeData, chunk);
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
                // Fluids never occlude the solid block faces around them. Lava is
                // intentionally in the opaque draw pass, so relying on the material
                // flag here opened holes in terrain whenever the camera entered it.
                bool fluidNeighbor = neighbourBlockType == BlockType.Water || neighbourBlockType == BlockType.Lava;
                // Transparent blocks normally expose the face behind them, but
                // adjacent glass cubes share a seamless boundary. Do not build
                // the two invisible internal quads between matching glass cells.
                bool joinedGlass = blockType == BlockType.Glass && neighbourBlockType == BlockType.Glass;
                bool visible = !joinedGlass && (fluidNeighbor || BlockShapesExtensions.IsPlant(neighbourBlockType) || SpecialBlockGeometry.NonOccluding(neighbourBlockType) || !shape.TouchesCellBoundary(dir) || neighbourBlockTypeData.isTransparent ||
                    !(StairGeometry.IsStair(neighbourBlockType) ? StairGeometry.CoversFace(StairGeometry.Facing(neighbourBlock.blockShape), shape, dir.GetOpposite()) : neighborShape.CoversBoundaryFace(shape, dir.GetOpposite())));

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
                    var sampleBlock=chunk.GetBlock(sample);
                    meshData.skyLight.Add(sampleBlock.GetSkyLight());
                    meshData.blockLight.Add(Math.Max(sampleBlock.GetBlockLight(),(int)block.BlockData.lightEmission));
                    meshData.sides.Add(new Vector3(shade,shade,5));
                    meshData.smoothLightCorners.Add(new Vector4());
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
                chunk != null, WheatCropRules.IsCrop(block.type) ? blockTypeData.textureData.GrowthStageIndex(SpecialBlockGeometry.State(block.type, block.blockShape)) : blockTypeData.textureData.FaceIndex(Direction.backwards), tint, meshData);
            if (chunk != null)
            {
                for (int i = first; i < meshData.vertices.Count; i++)
                {
                    meshData.skyLight.Add(block.GetSkyLight());
                    meshData.blockLight.Add(Math.Max(block.GetBlockLight(),(int)block.BlockData.lightEmission));
                    // Marker 3: lightmap sampling without AO or cube face shading.
                    meshData.sides.Add(new Vector3(1,1,3));
                    meshData.smoothLightCorners.Add(new Vector4());
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
                meshData.SetLastQuadNormals();
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
        meshData.SetLastQuadNormals();
        var shape = block.blockShape.GetShape();
        if ((block.type == BlockType.Water || block.type == BlockType.Lava) && chunk != null)
            AddFluidUVs(dir, pos, meshData, chunk, blockTypeData.textureData);
        else meshData.AddFaceUVs(blockTypeData.textureData.FaceIndex(block.type, dir), shape.FaceUVTransform(dir));

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
            meshData.AddQuadTriangles();meshData.SetLastQuadNormals();meshData.AddFaceUVs(overlay);
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
        var block=chunk.GetBlock(pos);var shape=faceShape??block.blockShape.GetShape();
        var normal=direction.GetVector();ChunkSection.ChooseFaceAxes(normal,out var axisA,out var axisB);
        var front=IsInsetFace(normal,shape)?pos:pos+normal;
        bool liquid=block.type==BlockType.Water||block.type==BlockType.Lava;
        if(liquid&&direction==Direction.up)front=pos;
        int emission=block.BlockData.lightEmission;
        var flat=SampleLightLevels(chunk,front,emission);
        bool ao=!liquid&&emission==0;
        var c00=SampleLightCorner(chunk,front,axisA,axisB,-1,-1,emission,flat);
        var c01=SampleLightCorner(chunk,front,axisA,axisB,-1,1,emission,flat);
        var c11=SampleLightCorner(chunk,front,axisA,axisB,1,1,emission,flat);
        var c10=SampleLightCorner(chunk,front,axisA,axisB,1,-1,emission,flat);
        int first=meshData.vertices.Count-4;
        for(int i=0;i<4;i++)
        {
            var v=meshData.vertices[first+i]-(Vector3)pos;
            float a=FaceCoordinate(Vector3.Dot(v,(Vector3)axisA)),b=FaceCoordinate(Vector3.Dot(v,(Vector3)axisB));
            float sky=ao?VoxelLightingMath.Interpolate(c00.x,c01.x,c11.x,c10.x,a,b):flat.x;
            float light=ao?VoxelLightingMath.Interpolate(c00.y,c01.y,c11.y,c10.y,a,b):flat.y;
            float shade=ao?VoxelLightingMath.Interpolate(c00.z,c01.z,c11.z,c10.z,a,b):1;
            meshData.skyLight.Add(sky);meshData.blockLight.Add(light);
            meshData.sides.Add(new Vector3(shade,1,4));
            // AO-off uses the flat face levels stored here.
            meshData.smoothLightCorners.Add(new Vector4(flat.x,flat.y,0,0));
        }
    }

    private static Vector3 SampleLightCorner(ChunkData chunk,Vector3Int front,Vector3Int axisA,Vector3Int axisB,int signA,int signB,int emission,Vector3 flat)
    {
        var a=SampleLightLevels(chunk,front+axisA*signA,emission);
        var b=SampleLightLevels(chunk,front+axisB*signB,emission);
        var diagonal=a.z<1&&b.z<1?a:SampleLightLevels(chunk,front+axisA*signA+axisB*signB,emission);
        // Vanilla substitutes the face brightness when a packed neighbor light is zero.
        if(a.x==0&&a.y==0){a.x=flat.x;a.y=flat.y;}
        if(b.x==0&&b.y==0){b.x=flat.x;b.y=flat.y;}
        if(diagonal.x==0&&diagonal.y==0){diagonal.x=flat.x;diagonal.y=flat.y;}
        return new Vector3((flat.x+a.x+b.x+diagonal.x)*.25f,(flat.y+a.y+b.y+diagonal.y)*.25f,(flat.z+a.z+b.z+diagonal.z)*.25f);
    }

    private static Vector3 SampleLightLevels(ChunkData chunk,Vector3Int pos,int emission)
    {
        if(pos.y>=chunk.worldRef.worldHeight)return new Vector3(chunk.worldRef.dimension==WorldDimension.Nether?0:15,emission,1);
        if(pos.y<0)return new Vector3(0,emission,1);
        var block=chunk.GetBlock(pos);
        if(block==null||block.type==BlockType.Nothing)return new Vector3(0,emission,1);
        int sky=block.GetSkyLight(),light=Math.Max(block.GetBlockLight(),emission);
        var shape=BlockShapesExtensions.ForBlock(block.type);
        if(shape==BlockShapes.Slab||StairGeometry.IsStair(block.type))
            foreach(var dir in SlabLightNeighbors)
            {var neighbor=chunk.GetBlock(pos+dir);if(neighbor!=null){sky=Math.Max(sky,neighbor.GetSkyLight());light=Math.Max(light,neighbor.GetBlockLight());}}
        float ao=block.BlockData.opacity==15&&shape==BlockShapes.FullBlock?.2f:1;
        return new Vector3(sky,light,ao);
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

    private static readonly Vector3Int[] SlabLightNeighbors =
        { Vector3Int.up, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };

    public static int GetLightIndex(Vector3Int pos, ChunkData chunk)
    {
        return pos.x + pos.y * chunk.chunkSize + pos.z * chunk.chunkSize * chunk.worldRef.worldHeight;
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

using UnityEngine;

// Geometry from Beta 1.7.3 RenderBlocks.renderCrossedSquares/func_1245_b.
// The assigned Side image supplies all sheets; alpha is handled by the cutout material.
public static class PlantMesh
{
    public static Vector3 GrassOffset(Vector3Int world)
    {
        unchecked
        {
            // Java performs the first multiplication as int, then the hash as long.
            long hash = (long)(world.x * 3129871) ^ (long)world.z * 116129781L ^ (long)world.y;
            hash = hash * hash * 42317861L + hash * 11L;
            return new Vector3(((float)((hash >> 16) & 15) / 15f - .5f) * .5f,
                ((float)((hash >> 20) & 15) / 15f - 1f) * .2f,
                ((float)((hash >> 24) & 15) / 15f - .5f) * .5f);
        }
    }

    public static void Append(BlockType type, Vector3Int local, Vector3Int world, bool inWorld,
        int layer, Color tint, MeshData mesh)
    {
        Vector3 origin = (Vector3)local;
        if (BlockShapesExtensions.IsCrop(type))
        {
            origin.y -= .0625f;
            Sheet(origin, .25f, 0, .25f, 1, layer, tint, mesh);
            Sheet(origin, .75f, 1, .75f, 0, layer, tint, mesh);
            Sheet(origin, 0, .25f, 1, .25f, layer, tint, mesh);
            Sheet(origin, 1, .75f, 0, .75f, layer, tint, mesh);
        }
        else
        {
            if (inWorld && (type == BlockType.TallGrass || type == BlockType.DryTallGrass || type == BlockType.Fern))
                origin += GrassOffset(world);
            Sheet(origin, .05f, .05f, .95f, .95f, layer, tint, mesh);
            Sheet(origin, .05f, .95f, .95f, .05f, layer, tint, mesh);
        }
    }

    private static void Sheet(Vector3 origin, float ax, float az, float bx, float bz,
        int layer, Color tint, MeshData mesh)
    {
        // Separate reverse-wound quads retain mirrored back-face artwork and
        // work with the existing back-face culling material.
        Quad(origin, ax, az, bx, bz, layer, tint, mesh);
        Quad(origin, bx, bz, ax, az, layer, tint, mesh);
    }
    private static void Quad(Vector3 origin, float ax, float az, float bx, float bz,
        int layer, Color tint, MeshData mesh)
    {
        mesh.AddVertex(origin + new Vector3(ax, 0, az));
        mesh.AddVertex(origin + new Vector3(ax, 1, az));
        mesh.AddVertex(origin + new Vector3(bx, 1, bz));
        mesh.AddVertex(origin + new Vector3(bx, 0, bz));
        mesh.AddQuadTriangles();
        mesh.AddFaceUVs(layer);
        mesh.AddQuadColor(tint);
    }
}

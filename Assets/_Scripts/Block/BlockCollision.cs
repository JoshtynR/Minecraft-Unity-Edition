using UnityEngine;

// Swept axis-aligned player box against code-defined block bounds. No native
// colliders or per-frame shape/list allocations; data's collision toggle is kept.
public static class BlockCollision
{
    private const float Epsilon = 0.00001f;
    // Movement clips bodies flush against a block. Hazard queries must include
    // that flush boundary or a player/item stopped by a cactus never "touches" it.
    private const float ContactPadding = 0.001f;
    private static float Axis(Vector3 v, int axis) { return axis == 0 ? v.x : axis == 1 ? v.y : v.z; }

    public static float Clip(World world, Vector3 feet, float halfWidth, float height, float movement, int axis)
    {
        if (world == null || movement == 0) return movement;
        Vector3 min = new Vector3(feet.x - halfWidth, feet.y, feet.z - halfWidth);
        Vector3 max = new Vector3(feet.x + halfWidth, feet.y + height, feet.z + halfWidth);
        Vector3 sweepMin = min, sweepMax = max;
        if (axis == 0) { sweepMin.x += Mathf.Min(0,movement); sweepMax.x += Mathf.Max(0,movement); }
        else if (axis == 1) { sweepMin.y += Mathf.Min(0,movement); sweepMax.y += Mathf.Max(0,movement); }
        else { sweepMin.z += Mathf.Min(0,movement); sweepMax.z += Mathf.Max(0,movement); }
        for (int x = Mathf.FloorToInt(sweepMin.x); x <= Mathf.FloorToInt(sweepMax.x); x++)
        // Fences extend half a block above their own voxel.
        for (int y = Mathf.Max(0,Mathf.FloorToInt(sweepMin.y)-1); y <= Mathf.Min(world.worldHeight - 1,Mathf.FloorToInt(sweepMax.y)); y++)
        for (int z = Mathf.FloorToInt(sweepMin.z); z <= Mathf.FloorToInt(sweepMax.z); z++)
        {
            var block = world.GetBlock(new Vector3Int(x,y,z));
            if (block == null || block.type == BlockType.Nothing || block.type == BlockType.Air || block.type == BlockType.Water || block.type == BlockType.Lava || BlockShapesExtensions.IsPlant(block.type) || SpecialBlockGeometry.NoCollision(block.type) || !block.BlockData.generateCollider) continue;
            int parts = SpecialBlockGeometry.CollisionCount(block.type);
            for (int part = 0; part < parts; part++)
            {
                var shape = SpecialBlockGeometry.CollisionPart(block.type,block.blockShape,part);
                Vector3 bmin = new Vector3(x + shape.xMin/16f,y + shape.yMin/16f,z + shape.zMin/16f);
                Vector3 bmax = new Vector3(x + shape.xMax/16f,y + shape.yMax/16f,z + shape.zMax/16f);
                bool overlaps = true;
                for (int a=0; a<3; a++)
                    if (a != axis && (Axis(max,a) <= Axis(bmin,a) + Epsilon || Axis(min,a) >= Axis(bmax,a) - Epsilon)) overlaps=false;
                if (!overlaps) continue;
                if (movement > 0 && Axis(max,axis) <= Axis(bmin,axis) + Epsilon)
                    movement = Mathf.Min(movement, Mathf.Max(0,Axis(bmin,axis)-Axis(max,axis)));
                else if (movement < 0 && Axis(min,axis) >= Axis(bmax,axis) - Epsilon)
                    movement = Mathf.Max(movement, Mathf.Min(0,Axis(bmax,axis)-Axis(min,axis)));
            }
        }
        return movement;
    }

    public static bool OverlapsBody(Vector3 feet, float halfWidth, float height, Vector3Int cell, BlockShape shape)
    {
        return feet.x + halfWidth > cell.x + shape.xMin/16f + Epsilon && feet.x - halfWidth < cell.x + shape.xMax/16f - Epsilon &&
            feet.y + height > cell.y + shape.yMin/16f + Epsilon && feet.y < cell.y + shape.yMax/16f - Epsilon &&
            feet.z + halfWidth > cell.z + shape.zMin/16f + Epsilon && feet.z - halfWidth < cell.z + shape.zMax/16f - Epsilon;
    }

    // Contact queries share the exact code-defined collision boxes used by
    // movement. This matters for hazards such as cactus, whose model and
    // collision box are inset from the voxel boundary.
    public static bool TouchesBlock(World world, Vector3 feet, float halfWidth, float height, BlockType type)
    {
        if (world == null) return false;
        int x0 = Mathf.FloorToInt(feet.x - halfWidth - ContactPadding);
        int x1 = Mathf.FloorToInt(feet.x + halfWidth + ContactPadding);
        int y0 = Mathf.Max(0, Mathf.FloorToInt(feet.y - ContactPadding));
        int y1 = Mathf.Min(world.worldHeight - 1, Mathf.FloorToInt(feet.y + height + ContactPadding));
        int z0 = Mathf.FloorToInt(feet.z - halfWidth - ContactPadding);
        int z1 = Mathf.FloorToInt(feet.z + halfWidth + ContactPadding);
        for (int x = x0; x <= x1; x++)
        for (int y = y0; y <= y1; y++)
        for (int z = z0; z <= z1; z++)
        {
            var block = world.GetBlock(new Vector3Int(x,y,z));
            if (block == null || block.type != type) continue;
            int parts = SpecialBlockGeometry.CollisionCount(type);
            for (int part = 0; part < parts; part++)
                if (TouchesBody(feet,halfWidth,height,new Vector3Int(x,y,z),SpecialBlockGeometry.CollisionPart(type,block.blockShape,part)))
                    return true;
        }
        return false;
    }

    private static bool TouchesBody(Vector3 feet, float halfWidth, float height, Vector3Int cell, BlockShape shape)
    {
        return feet.x + halfWidth + ContactPadding >= cell.x + shape.xMin/16f &&
            feet.x - halfWidth - ContactPadding <= cell.x + shape.xMax/16f &&
            feet.y + height + ContactPadding >= cell.y + shape.yMin/16f &&
            feet.y - ContactPadding <= cell.y + shape.yMax/16f &&
            feet.z + halfWidth + ContactPadding >= cell.z + shape.zMin/16f &&
            feet.z - halfWidth - ContactPadding <= cell.z + shape.zMax/16f;
    }

    public static bool Supported(World world, Vector3 feet, float halfWidth, float height)
    { return Clip(world,feet,halfWidth,height,-0.02f,1) > -0.02f + Epsilon; }

    public static Vector3 Resolve(World world, Vector3 feet, float halfWidth, float height, Vector3 requested, bool allowStep)
    {
        float y = Clip(world,feet,halfWidth,height,requested.y,1);
        Vector3 pos = feet + new Vector3(0,y,0);
        float x = Clip(world,pos,halfWidth,height,requested.x,0); pos.x += x;
        float z = Clip(world,pos,halfWidth,height,requested.z,2);
        var result = new Vector3(x,y,z);
        if (allowStep && requested.y <= 0 && (x != requested.x || z != requested.z))
        {
            float up = Clip(world,feet,halfWidth,height,0.5f,1);
            pos = feet + new Vector3(0,up,0);
            float sx = Clip(world,pos,halfWidth,height,requested.x,0); pos.x += sx;
            float sz = Clip(world,pos,halfWidth,height,requested.z,2); pos.z += sz;
            float down = Clip(world,pos,halfWidth,height,-up,1);
            if (sx*sx + sz*sz > x*x + z*z + Epsilon) result = new Vector3(sx,up+down,sz);
        }
        return result;
    }
}

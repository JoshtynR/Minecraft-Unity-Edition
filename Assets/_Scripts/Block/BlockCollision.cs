using UnityEngine;

// Swept axis-aligned player box against code-defined block bounds. No native
// colliders or per-frame shape/list allocations; data's collision toggle is kept.
public static class BlockCollision
{
    private const float Epsilon = 0.00001f;
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
        for (int y = Mathf.Max(0,Mathf.FloorToInt(sweepMin.y)); y <= Mathf.Min(world.worldHeight - 1,Mathf.FloorToInt(sweepMax.y)); y++)
        for (int z = Mathf.FloorToInt(sweepMin.z); z <= Mathf.FloorToInt(sweepMax.z); z++)
        {
            var block = world.GetBlock(new Vector3Int(x,y,z));
            if (block == null || block.type == BlockType.Nothing || block.type == BlockType.Air || !block.BlockData.generateCollider) continue;
            var shape = BlockShapesExtensions.CollisionShape(block.type, block.blockShape.GetShape());
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
        return movement;
    }

    public static bool OverlapsBody(Vector3 feet, float halfWidth, float height, Vector3Int cell, BlockShape shape)
    {
        return feet.x + halfWidth > cell.x + shape.xMin/16f + Epsilon && feet.x - halfWidth < cell.x + shape.xMax/16f - Epsilon &&
            feet.y + height > cell.y + shape.yMin/16f + Epsilon && feet.y < cell.y + shape.yMax/16f - Epsilon &&
            feet.z + halfWidth > cell.z + shape.zMin/16f + Epsilon && feet.z - halfWidth < cell.z + shape.zMax/16f - Epsilon;
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

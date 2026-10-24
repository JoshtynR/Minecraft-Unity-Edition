using System;
using UnityEngine;

/// <summary>Shared initial-spawn and death-respawn selection.</summary>
public static class WorldSpawnRules
{
    public const int SearchRadius = 15;
    private const int FallbackFloorY = 65;

    public static Vector3 Resolve(World world)
    {
        if (world == null) return new Vector3(.5f, FallbackFloorY + 1, .5f);

        Vector3 spawn;
        if (TryFindNatural(world, out spawn)) return spawn;

        // A valid loaded Overworld should virtually always have a natural
        // candidate. Still guarantee respawn for ocean-only, obstructed, or
        // unusual custom terrain instead of leaving the player dead forever.
        int floorY = Math.Max(1, Math.Min(FallbackFloorY, world.worldHeight - 3));
        var floor = new Vector3Int(0, floorY, 0);
        WorldDataHelper.SetBlock(world, floor, BlockType.Stone);
        WorldDataHelper.SetBlock(world, floor + Vector3Int.up, BlockType.Air);
        WorldDataHelper.SetBlock(world, floor + Vector3Int.up + Vector3Int.up, BlockType.Air);
        var chunkPosition = WorldDataHelper.GetChunkPosition(world, floor);
        var renderer = WorldDataHelper.GetChunk(world, chunkPosition);
        if (renderer != null) world.AddChunkToUpdate(renderer, true);
        return new Vector3(.5f, floorY + 1, .5f);
    }

    public static bool TryFindNatural(World world, out Vector3 spawn)
    {
        spawn = default(Vector3);
        if (world == null || world.worldHeight < 4) return false;

        // Nearest square ring first, clipped to a true 15-block circle around
        // X/Z zero. Y is determined entirely by the generated terrain column.
        int radiusSquared = SearchRadius * SearchRadius;
        for (int ring = 0; ring <= SearchRadius; ring++)
        for (int x = -ring; x <= ring; x++)
        for (int z = -ring; z <= ring; z++)
        {
            if (Math.Max(Math.Abs(x), Math.Abs(z)) != ring || x * x + z * z > radiusSquared) continue;
            for (int floorY = world.worldHeight - 3; floorY >= 0; floorY--)
            {
                var floor = world.GetBlock(new Vector3Int(x, floorY, z));
                if (floor == null || !SpecialBlockGeometry.NormalCube(floor.type)) continue;
                var feet = world.GetBlock(new Vector3Int(x, floorY + 1, z));
                var head = world.GetBlock(new Vector3Int(x, floorY + 2, z));
                if (feet == null || head == null || feet.type != BlockType.Air || head.type != BlockType.Air) continue;
                spawn = new Vector3(x + .5f, floorY + 1, z + .5f);
                return true;
            }
        }
        return false;
    }

    public static bool TryResolveBed(World world, Vector3Int bed, out Vector3 spawn)
    {
        spawn=default(Vector3);
        if(world==null||world.dimension!=WorldDimension.Overworld)return false;
        var block=world.GetBlock(bed);
        if(block==null||block.type!=BlockType.Bed)return false;
        byte state=SpecialBlockGeometry.State(block.type,block.blockShape);
        var partner=bed+SpecialBlockGeometry.PartnerOffset(BlockType.Bed,state);
        var partnerBlock=world.GetBlock(partner);
        if(partnerBlock==null||partnerBlock.type!=BlockType.Bed)return false;
        // Match classic behavior: prefer the eight blocks surrounding either
        // bed half, then the two cells directly above it.
        Vector3Int[] horizontal={Vector3Int.left,Vector3Int.right,Vector3Int.back,Vector3Int.forward,
            new Vector3Int(-1,0,-1),new Vector3Int(-1,0,1),new Vector3Int(1,0,-1),new Vector3Int(1,0,1)};
        foreach(var origin in new[]{bed,partner})
            foreach(var offset in horizontal)
                if(TryBedCandidate(world,origin+offset,out spawn))return true;
        if(TryBedCandidate(world,bed+Vector3Int.up,out spawn))return true;
        return TryBedCandidate(world,partner+Vector3Int.up,out spawn);
    }

    private static bool TryBedCandidate(World world,Vector3Int feet,out Vector3 spawn)
    {
        spawn=default(Vector3);
        if(feet.y<1||feet.y>=world.worldHeight-1)return false;
        var floor=world.GetBlock(feet+Vector3Int.down);
        var lower=world.GetBlock(feet);var upper=world.GetBlock(feet+Vector3Int.up);
        if(floor==null||lower==null||upper==null||!SpecialBlockGeometry.NormalCube(floor.type)||
            lower.type!=BlockType.Air||upper.type!=BlockType.Air)return false;
        spawn=new Vector3(feet.x+.5f,feet.y,feet.z+.5f);return true;
    }
}

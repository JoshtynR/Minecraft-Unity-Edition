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
}

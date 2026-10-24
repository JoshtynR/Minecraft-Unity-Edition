using System;
using System.Collections.Generic;
using UnityEngine;

// One source of truth for both player placement and neighbor-change survival.
// "Nothing" means that the chunk is not loaded: placement rejects it, while a
// runtime check waits rather than destroying a block at a streaming boundary.
public static class BlockSurvivalRules
{
    public enum LeafConnectionState { Disconnected, Connected, Unknown }
    private static readonly Vector3Int[] Horizontal = { Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };
    public static bool IsLeaves(BlockType type) => type == BlockType.Leaves ||
        type == BlockType.SpruceLeaves || type == BlockType.BirchLeaves;

    public static bool IsLog(BlockType type) => type == BlockType.Log ||
        type == BlockType.SpruceLog || type == BlockType.BirchLog;

    public static LeafConnectionState LeafConnection(Vector3Int origin, Func<Vector3Int, BlockType> get, int maximumDistance = 4)
    {
        var open = new Queue<Vector3Int>();
        var visited = new HashSet<Vector3Int>();
        bool unknown = false;
        open.Enqueue(origin); visited.Add(origin);
        while (open.Count > 0)
        {
            var position = open.Dequeue();
            int distance = Mathf.Abs(position.x - origin.x) + Mathf.Abs(position.y - origin.y) + Mathf.Abs(position.z - origin.z);
            if (distance >= maximumDistance) continue;
            foreach (var direction in BlockHelper.directions)
            {
                var neighbor = position + direction.GetVector();
                var type = get(neighbor);
                if (type == BlockType.Nothing) { unknown = true; continue; }
                if (IsLog(type)) return LeafConnectionState.Connected;
                if (IsLeaves(type) && visited.Add(neighbor)) open.Enqueue(neighbor);
            }
        }
        return unknown ? LeafConnectionState.Unknown : LeafConnectionState.Disconnected;
    }

    public static bool CanPlace(BlockType type, byte state, Vector3Int position,
        Func<Vector3Int, BlockType> get)
    {
        var existing = get(position);
        if (existing == BlockType.Nothing || !CanReplace(existing, type)) return false;

        if (SpecialBlockGeometry.IsDoor(type) || type == BlockType.Bed)
        {
            var partner = position + SpecialBlockGeometry.PartnerOffset(type, state);
            if (get(partner) != BlockType.Air) return false;
            if (!SupportedBelow(position, get)) return false;
            if (type == BlockType.Bed && !SupportedBelow(partner, get)) return false;
            return true;
        }

        return CanSurvive(type, state, position, get, false);
    }

    public static bool CanReplace(BlockType existing, BlockType placed)
    {
        if (existing == BlockType.Air || existing == BlockType.Fire || existing == BlockType.SnowLayer) return true;
        return (existing == BlockType.WoodenSlab && placed == BlockType.DoubleWoodenSlab) ||
            (existing == BlockType.StoneSlab && placed == BlockType.DoubleStoneSlab) ||
            (existing == BlockType.SandstoneSlab && placed == BlockType.DoubleSandstoneSlab) ||
            (existing == BlockType.CobblestoneSlab && placed == BlockType.DoubleCobblestoneSlab);
    }

    public static bool CanSurvive(BlockType type, byte state, Vector3Int position,
        Func<Vector3Int, BlockType> get, bool unknownSurvives = true)
    {
        if (type == BlockType.Nothing || type == BlockType.Air || type == BlockType.Water || type == BlockType.Lava || IsLeaves(type)) return true;
        if (TouchesUnknown(type, state, position, get)) return unknownSurvives;

        switch (type)
        {
            case BlockType.OakSapling: case BlockType.SpruceSapling: case BlockType.BirchSapling:
            case BlockType.TallGrass: case BlockType.Fern:
            case BlockType.Dandelion: case BlockType.Rose:
                return PlantSoil(get(position + Vector3Int.down));
            case BlockType.DryTallGrass: case BlockType.DeadBush:
                return get(position + Vector3Int.down) == BlockType.Sand;
            case BlockType.BrownMushroom: case BlockType.RedMushroom:
                return SpecialBlockGeometry.NormalCube(get(position + Vector3Int.down));
            case BlockType.Cactus:
                return CactusSurvives(position, get);
            case BlockType.SugarCane:
                return ReedSurvives(position, get);
            case BlockType.SnowLayer:
            case BlockType.StonePressurePlate: case BlockType.WoodenPressurePlate:
            case BlockType.Cake: case BlockType.StandingSign:
            case BlockType.Rail: case BlockType.PoweredRail: case BlockType.DetectorRail:
            case BlockType.RedstoneWire: case BlockType.RedstoneRepeater: case BlockType.PoweredRepeater:
                return SupportedBelow(position, get);
            case BlockType.WheatCrop0: case BlockType.WheatCrop1: case BlockType.WheatCrop2: case BlockType.WheatCrop3:
            case BlockType.WheatCrop4: case BlockType.WheatCrop5: case BlockType.WheatCrop6: case BlockType.WheatCrop7:
                return WheatCropRules.IsSoil(get(position + Vector3Int.down));
            case BlockType.WoodenDoor: case BlockType.IronDoor:
            case BlockType.Bed:
                return PairSurvives(type, state, position, get);
            case BlockType.Fire:
                return FireSurvives(position, get);
            default:
                return SpecialBlockGeometry.CanAttach(type, state, position, get);
        }
    }

    private static bool PlantSoil(BlockType type) => type == BlockType.Grass || type == BlockType.Dirt || WheatCropRules.IsSoil(type);
    private static bool SupportedBelow(Vector3Int position, Func<Vector3Int, BlockType> get) => SpecialBlockGeometry.NormalCube(get(position + Vector3Int.down));

    private static bool CactusSurvives(Vector3Int position, Func<Vector3Int, BlockType> get)
    {
        var below = get(position + Vector3Int.down);
        if (below != BlockType.Cactus && below != BlockType.Sand) return false;
        return !World.FluidHasSolidMaterial(get(position + Vector3Int.left)) &&
            !World.FluidHasSolidMaterial(get(position + Vector3Int.right)) &&
            !World.FluidHasSolidMaterial(get(position + Vector3Int.forward)) &&
            !World.FluidHasSolidMaterial(get(position + Vector3Int.back));
    }

    private static bool ReedSurvives(Vector3Int position, Func<Vector3Int, BlockType> get)
    {
        var belowPosition = position + Vector3Int.down;
        var below = get(belowPosition);
        if (below == BlockType.SugarCane) return true;
        if (below != BlockType.Grass && below != BlockType.Dirt && below != BlockType.Sand) return false;
        return get(belowPosition + Vector3Int.left) == BlockType.Water || get(belowPosition + Vector3Int.right) == BlockType.Water ||
            get(belowPosition + Vector3Int.forward) == BlockType.Water || get(belowPosition + Vector3Int.back) == BlockType.Water;
    }

    private static bool PairSurvives(BlockType type, byte state, Vector3Int position, Func<Vector3Int, BlockType> get)
    {
        var partner = position + SpecialBlockGeometry.PartnerOffset(type, state);
        if (get(partner) != type) return false;
        if ((state & 8) != 0) return true;
        if (!SupportedBelow(position, get)) return false;
        return type != BlockType.Bed || SupportedBelow(partner, get);
    }

    private static bool FireSurvives(Vector3Int position, Func<Vector3Int, BlockType> get)
    {
        if (SpecialBlockGeometry.NormalCube(get(position + Vector3Int.down))) return true;
        foreach (var direction in BlockHelper.directions)
            if (BetaFireSimulation.Flammability(get(position + direction.GetVector())) > 0) return true;
        return false;
    }

    private static bool TouchesUnknown(BlockType type, byte state, Vector3Int position, Func<Vector3Int, BlockType> get)
    {
        if (type == BlockType.Cactus)
        {
            if (get(position + Vector3Int.down) == BlockType.Nothing) return true;
            foreach (var direction in Horizontal)
                if (get(position + direction) == BlockType.Nothing) return true;
        }
        if (type == BlockType.Fire)
            foreach (var direction in BlockHelper.directions)
                if (get(position + direction.GetVector()) == BlockType.Nothing) return true;
        if (type == BlockType.SugarCane)
        {
            var below = position + Vector3Int.down;
            if (get(below) == BlockType.Nothing) return true;
            foreach (var direction in Horizontal)
                if (get(below + direction) == BlockType.Nothing) return true;
        }
        if (SpecialBlockGeometry.IsDoor(type) || type == BlockType.Bed)
        {
            if (get(position + SpecialBlockGeometry.PartnerOffset(type, state)) == BlockType.Nothing) return true;
            if ((state & 8) == 0 && get(position + Vector3Int.down) == BlockType.Nothing) return true;
            if (type == BlockType.Bed && (state & 8) == 0 &&
                get(position + SpecialBlockGeometry.PartnerOffset(type, state) + Vector3Int.down) == BlockType.Nothing) return true;
        }
        if (SpecialBlockGeometry.IsTorch(type) || type == BlockType.Lever || type == BlockType.StoneButton ||
            type == BlockType.Ladder || type == BlockType.WallSign || type == BlockType.Trapdoor)
            return get(position + AttachmentSupport(type, state)) == BlockType.Nothing;
        return NeedsFloor(type) && get(position + Vector3Int.down) == BlockType.Nothing;
    }

    private static Vector3Int AttachmentSupport(BlockType type, byte state)
    {
        int side = state & (type == BlockType.Trapdoor ? 3 : 7);
        if (type == BlockType.Ladder || type == BlockType.WallSign)
            return side == 2 ? Vector3Int.forward : side == 3 ? Vector3Int.back : side == 4 ? Vector3Int.right : Vector3Int.left;
        if (type == BlockType.Trapdoor)
            return side == 0 ? Vector3Int.forward : side == 1 ? Vector3Int.back : side == 2 ? Vector3Int.right : Vector3Int.left;
        return side == 1 ? Vector3Int.left : side == 2 ? Vector3Int.right : side == 3 ? Vector3Int.back : side == 4 ? Vector3Int.forward : Vector3Int.down;
    }

    private static bool NeedsFloor(BlockType type)
    {
        return BlockShapesExtensions.IsPlant(type) || type == BlockType.SnowLayer || type == BlockType.Cake ||
            type == BlockType.StandingSign || type == BlockType.StonePressurePlate || type == BlockType.WoodenPressurePlate ||
            SpecialBlockGeometry.IsRail(type) || type == BlockType.RedstoneWire ||
            type == BlockType.RedstoneRepeater || type == BlockType.PoweredRepeater;
    }
}

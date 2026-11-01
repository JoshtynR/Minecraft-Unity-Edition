using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public partial class World
{
    private const int LeafLogDistance = 4;
    [Tooltip("Independent apple chance for naturally decaying or hand-broken oak leaves. Shears preserve the leaves instead.")]
    [Range(0f, 100f)] public float leafAppleDropPercent = 2f;

    public void DropLeafApple(BlockType type, Vector3Int position, ItemType tool = ItemType.Nothing, int damage = 0)
    {
        if (!DropAuthority || type != BlockType.Leaves) return;
        if (BlockSurvivalRules.DropsApple(type, leafAppleDropPercent, UnityEngine.Random.value,
            tool == ItemType.Shears && damage < ItemDurability.Maximum(tool)))
            SpawnItemDrop(new InventoryStack(ItemType.Apple, 1), (Vector3)position + Vector3.one * .5f);
    }

    // Beta samples 80 positions per 32768-block column each 20 Hz tick.
    // Geometric waiting time matches that marginal rate without scanning columns.
    private static float LeafDecayDelay()
    {
        return BlockSurvivalRules.DecayDelay(UnityEngine.Random.value);
    }
    [NonSerialized] private HashSet<Block> runtimeQueuedBlockUpdates;
    [NonSerialized] private Dictionary<Vector3Int, float> runtimeLeafDecay;
    [NonSerialized] private List<Vector3Int> runtimeLeafDecayBuffer;

    private HashSet<Block> queuedBlockUpdates => System.Threading.LazyInitializer.EnsureInitialized(
        ref runtimeQueuedBlockUpdates, () => new HashSet<Block>());
    private Dictionary<Vector3Int, float> leafDecay => System.Threading.LazyInitializer.EnsureInitialized(
        ref runtimeLeafDecay, () => new Dictionary<Vector3Int, float>());
    private List<Vector3Int> leafDecayBuffer => System.Threading.LazyInitializer.EnsureInitialized(
        ref runtimeLeafDecayBuffer, () => new List<Vector3Int>());

    public bool CanPlaceSurvivalBlock(Vector3Int position, BlockType type, byte state)
    {
        if (position.y < 0 || position.y >= worldHeight) return false;
        if(type==BlockType.Chest&&!CanPlaceChest(position))return false;
        return BlockSurvivalRules.CanPlace(type, state, position, p => GetBlock(p).type);
    }

    internal void QueueSurvivalAround(Vector3Int position)
    {
        if (!IsWorldCreated || !DropAuthority) return;
        QueueSurvivalAt(position);
        foreach (var direction in BlockHelper.directions)
            QueueSurvivalAt(position + direction.GetVector());
    }

    internal void QueueLeavesAroundRemovedLog(Vector3Int position)
    {
        QueueLeafDecayAround(position, LeafLogDistance);
    }

    internal void QueueLeavesAroundRemovedLeaf(Vector3Int position)
    {
        QueueLeafDecayAround(position, 1);
    }

    private void QueueLeafDecayAround(Vector3Int position, int radius)
    {
        if (!IsWorldCreated || !DropAuthority) return;
        for (int x = -radius; x <= radius; x++)
        for (int y = -radius; y <= radius; y++)
        for (int z = -radius; z <= radius; z++)
        {
            var leafPosition = position + new Vector3Int(x, y, z);
            var leaf = GetBlock(leafPosition);
            if (!BlockSurvivalRules.IsLeaves(leaf.type)) continue;
            byte state = SpecialBlockGeometry.State(leaf.type, leaf.blockShape);
            if ((state & 4) != 0) continue;
            SetLeafDecayState(leaf, (byte)(state | 8));
            if (!leafDecay.ContainsKey(leafPosition)) leafDecay[leafPosition] = Time.time + LeafDecayDelay();
        }
    }

    private void SetLeafDecayState(Block leaf, byte state)
    {
        leaf.blockShape = SpecialBlockGeometry.Shape(leaf.type, state);
        leaf.section.dataRef.modifiedAfterSave = true;
    }

    internal void QueueLeafDecayOnChunkActivation(Block leaf)
    {
        if (!DropAuthority || !BlockSurvivalRules.IsLeaves(leaf.type)) return;
        if ((SpecialBlockGeometry.State(leaf.type, leaf.blockShape) & 12) != 8) return;
        var position = leaf.globalWorldPosition;
        if (!leafDecay.ContainsKey(position)) leafDecay[position] = Time.time + LeafDecayDelay();
    }

    private void QueueSurvivalAt(Vector3Int position)
    {
        if (position.y < 0 || position.y >= worldHeight) return;
        var block = GetBlock(position);
        if (block == null || block.type == BlockType.Nothing || !queuedBlockUpdates.Add(block)) return;
        blockToUpdate.Enqueue(block);
    }

    internal void ResetBlockSurvival()
    {
        blockToUpdate.Clear();
        queuedBlockUpdates.Clear();
        leafDecay.Clear();
        leafDecayBuffer.Clear();
    }

    internal void ProcessBlockUpdate(Block block)
    {
        if (block == null) return;
        queuedBlockUpdates.Remove(block);
        if (block.section == null || block.section.dataRef == null || block.section.dataRef.worldRef != this) return;

        Vector3Int position;
        try { position = block.globalWorldPosition; }
        catch { return; }
        if (GetBlock(position) != block) return;

        if (DropAuthority)
        {
            byte state = SpecialBlockGeometry.State(block.type, block.blockShape);
            if (IsFallingBlock(block.type) && TryStartFallingBlock(position,block.type,state))
                return;
            if (BlockSurvivalRules.IsLeaves(block.type))
            {
                // Only log/leaf removal starts decay; generic neighbor updates do not.
            }
            else if (!BlockSurvivalRules.CanSurvive(block.type, state, position, p => GetBlock(p).type))
            {
                BreakUnsupportedBlock(position, block.type, state);
                return;
            }
        }

        block.OnBlockUpdate();
    }

    internal void UpdateLeafDecay()
    {
        if (!DropAuthority || leafDecay.Count == 0 || disabled || !IsWorldCreated ||
            System.Threading.Volatile.Read(ref betaPopulationJobs) != 0 || isSaving) return;
        leafDecayBuffer.Clear();
        foreach (var entry in leafDecay)
        {
            if (entry.Value <= Time.time) leafDecayBuffer.Add(entry.Key);
            if (leafDecayBuffer.Count >= 8) break;
        }

        foreach (var position in leafDecayBuffer)
        {
            leafDecay.Remove(position);
            var block = GetBlock(position);
            if (block.type == BlockType.Nothing)
            { leafDecay[position] = Time.time + 1f; continue; }
            if (block == null || !BlockSurvivalRules.IsLeaves(block.type) ||
                (SpecialBlockGeometry.State(block.type, block.blockShape) & 4) != 0) continue;
            // Beta waits for the surrounding five-block chunk halo before testing.
            bool loaded = true;
            for (int x = -5; x <= 5; x += 10)
            for (int z = -5; z <= 5; z += 10)
                if (GetBlock(position + new Vector3Int(x, 0, z)).type == BlockType.Nothing) loaded = false;
            if (!loaded) { leafDecay[position] = Time.time + 1f; continue; }
            var connection = BlockSurvivalRules.LeafConnection(position,
                p => p.y < 0 || p.y >= worldHeight ? BlockType.Air : GetBlock(p).type, LeafLogDistance);
            if (connection == BlockSurvivalRules.LeafConnectionState.Unknown)
            {
                leafDecay[position] = Time.time + 1f;
                continue;
            }
            if (connection == BlockSurvivalRules.LeafConnectionState.Disconnected)
                BreakUnsupportedBlock(position, block.type, SpecialBlockGeometry.State(block.type, block.blockShape));
            else SetLeafDecayState(block, (byte)(SpecialBlockGeometry.State(block.type, block.blockShape) & ~8));
        }
    }

    private void BreakUnsupportedBlock(Vector3Int position, BlockType expected, byte state)
    {
        var current = GetBlock(position);
        if (current == null || current.type != expected || expected == BlockType.Air || expected == BlockType.Nothing) return;

        InventoryStack drop = new InventoryStack();
        bool crop = WheatCropRules.IsCrop(expected);
        bool orphanedUpperPair = (SpecialBlockGeometry.IsDoor(expected) || expected == BlockType.Bed) && (state & 8) != 0;
        if (!crop && !orphanedUpperPair) drop = UnsupportedBlockDrop(expected);

        SetBlock(position, BlockType.Air);
        if (NetworkServer.active && WorldServer.instance != null)
            WorldServer.instance.BroadcastBlock(this, position, BlockType.Air, 0);
        if (!drop.Empty) SpawnItemDrop(drop, (Vector3)position + Vector3.one * .5f);
        DropLeafApple(expected, position);
    }

    private InventoryStack UnsupportedBlockDrop(BlockType type)
    {
        if (BlockSurvivalRules.IsLeaves(type) || type == BlockType.TallGrass || type == BlockType.Fern)
            return MiningItemDrop(type, ItemType.Nothing);
        switch (type)
        {
            case BlockType.Fire: case BlockType.SnowLayer: case BlockType.Cake:
            case BlockType.DeadBush: case BlockType.DryTallGrass:
                return new InventoryStack();
            case BlockType.RedstoneWire:
                return new InventoryStack(ItemType.RedstoneDust, 1);
            case BlockType.RedstoneTorchOff:
                return new InventoryStack(ItemType.RedstoneTorch, 1);
            case BlockType.PoweredRepeater:
                return new InventoryStack(ItemType.RedstoneRepeater, 1);
            case BlockType.WallSign:
                return new InventoryStack(ItemType.StandingSign, 1);
            default:
                var item = ItemCatalog.FromBlock(type);
                return item == ItemType.Nothing ? new InventoryStack() : new InventoryStack(item, 1);
        }
    }
}

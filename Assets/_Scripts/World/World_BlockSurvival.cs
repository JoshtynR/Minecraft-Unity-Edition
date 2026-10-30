using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public partial class World
{
    private const int LeafLogDistance = 4;
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
        if (!IsWorldCreated || !DropAuthority) return;
        for (int x = -LeafLogDistance; x <= LeafLogDistance; x++)
        for (int y = -LeafLogDistance; y <= LeafLogDistance; y++)
        for (int z = -LeafLogDistance; z <= LeafLogDistance; z++)
        {
            var leafPosition = position + new Vector3Int(x, y, z);
            if (BlockSurvivalRules.IsLeaves(GetBlock(leafPosition).type)) QueueSurvivalAt(leafPosition);
        }
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
                if ((state & 4) == 0 && !leafDecay.ContainsKey(position))
                    leafDecay[position] = Time.time + UnityEngine.Random.Range(.5f, 1.5f);
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
        if (!DropAuthority || leafDecay.Count == 0 || disabled || !IsWorldCreated || GenerationBusy || isSaving) return;
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
            if (block == null || !BlockSurvivalRules.IsLeaves(block.type) ||
                (SpecialBlockGeometry.State(block.type, block.blockShape) & 4) != 0) continue;
            var connection = BlockSurvivalRules.LeafConnection(position, p => GetBlock(p).type, LeafLogDistance);
            if (connection == BlockSurvivalRules.LeafConnectionState.Unknown)
            {
                leafDecay[position] = Time.time + 1f;
                continue;
            }
            if (connection == BlockSurvivalRules.LeafConnectionState.Disconnected)
                BreakUnsupportedBlock(position, block.type, SpecialBlockGeometry.State(block.type, block.blockShape));
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

using System.Collections.Generic;
using UnityEngine;

public partial class World
{
    // AO, fluid corner heights and connected models all sample a one-cell halo.
    // Iterate origins instead of nine cells: interior changes visit one chunk,
    // edge changes two, and corner changes four (including negative coordinates).
    internal void CollectRenderUpdatesAround(Vector3Int position, ICollection<ChunkRenderer> output, ChunkData exclude = null)
    {
        if (worldData.chunkDict == null) return;
        var min = WorldDataHelper.GetChunkPosition(this, position + new Vector3Int(-1, 0, -1));
        var max = WorldDataHelper.GetChunkPosition(this, position + new Vector3Int(1, 0, 1));
        for (int x = min.x; x <= max.x; x += chunkSize)
        for (int z = min.z; z <= max.z; z += chunkSize)
            if (worldData.chunkDict.TryGetValue(new Vector3Int(x, 0, z), out var renderer) &&
                renderer != null && renderer.ChunkData != null && !ReferenceEquals(renderer.ChunkData, exclude))
            {
                renderer.ChunkData.MarkRenderSectionsDirty(position.y);
                output.Add(renderer);
            }
    }

    [System.NonSerialized] private HashSet<ChunkRenderer> runtimeChangedBlockRenderers;
    private HashSet<ChunkRenderer> changedBlockRenderers => System.Threading.LazyInitializer.EnsureInitialized(
        ref runtimeChangedBlockRenderers, () => new HashSet<ChunkRenderer>());
    private void InvalidateBlockRendering(Vector3Int position)
    {
        changedBlockRenderers.Clear();
        CollectRenderUpdatesAround(position, changedBlockRenderers);
        foreach (var renderer in changedBlockRenderers) AddChunkToUpdate(renderer);
        changedBlockRenderers.Clear();
    }
}

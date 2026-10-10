using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class World
{
    private readonly ConcurrentDictionary<Vector3Int, byte> treeMeshChanges = new ConcurrentDictionary<Vector3Int, byte>();
    public void NotifyTreeChunkChanged(Vector3Int origin) { treeMeshChanges.TryAdd(origin, 0); }
    private void ResetTreeMeshChanges() { treeMeshChanges.Clear(); }

    // Awaited by generation on the main thread. New chunks receive their normal
    // first mesh; existing neighbors need a refresh for cross-border branches.
    private async Task RefreshTreeMeshes()
    {
        var chunks = new List<ChunkRenderer>();
        foreach (var origin in treeMeshChanges.Keys)
            if (treeMeshChanges.TryRemove(origin, out _) && worldData.chunkDict.TryGetValue(origin, out var chunk))
                chunks.Add(chunk);
        if (chunks.Count == 0) return;
        var meshes = await UpdateMeshDataAsync(chunks, false);
        foreach (var chunk in chunks)
            if (chunk != null && worldData.chunkDict.TryGetValue(chunk.ChunkData.worldPos, out var current) && current == chunk)
                chunk.UpdateChunk(meshes[chunk]);
    }
}

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class World
{
    [System.NonSerialized] private ConcurrentDictionary<Vector3Int, byte> runtimeTreeMeshChanges;
    private ConcurrentDictionary<Vector3Int, byte> treeMeshChanges => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeTreeMeshChanges, () => new ConcurrentDictionary<Vector3Int, byte>());
    public void NotifyTreeChunkChanged(Vector3Int origin) { treeMeshChanges.TryAdd(origin, 0); }
    private void ResetTreeMeshChanges() { treeMeshChanges.Clear(); }

    // Awaited by generation on the main thread. New chunks receive their normal
    // first mesh; existing neighbors need a refresh for cross-border branches.
    private async Task RefreshTreeMeshes()
    {
        int session=workLifetime.Version;
        var chunks = new List<ChunkRenderer>();
        foreach (var origin in treeMeshChanges.Keys)
            if (treeMeshChanges.TryRemove(origin, out _) && worldData.chunkDict.TryGetValue(origin, out var chunk))
                chunks.Add(chunk);
        if (chunks.Count == 0) return;
        var meshes = await UpdateMeshDataAsync(chunks, false);
        try
        {
            if (!IsWorkSessionCurrent(session)) return;
            for(int i=0;i<chunks.Count;i++)
            {
                if (!IsWorkSessionCurrent(session)) return;
                TryApplyChunkMesh(chunks[i],meshes[chunks[i]],session);
                await YieldBetweenMeshUploadsAsync(i+1,chunks.Count);
            }
        }
        finally { ChunkMeshPipeline.Release(meshes.Values); }
    }
}

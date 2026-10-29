using System;
using System.Threading.Tasks;
using UnityEngine;

public partial class World
{
    internal bool TryApplyChunkMesh(ChunkRenderer renderer, MeshData meshData, int session)
    {
        try
        {
            if (!IsWorkSessionCurrent(session) || !IsLiveChunk(renderer) || meshData == null) return false;
            if (meshData.sourceChunk != null && !ReferenceEquals(meshData.sourceChunk, renderer.ChunkData)) return false;
            renderer.UpdateChunk(meshData);
            return true;
        }
        finally { meshData?.ReleaseAfterUpload(); }
    }

    // Every upload path uses this policy. Runtime batches already fit the budget;
    // initial generation, trees and dimension rebuilds may span multiple frames.
    private async Task YieldBetweenMeshUploadsAsync(int uploaded, int total)
    {
        if (Application.isPlaying && uploaded < total && uploaded % Math.Max(1, (int)chunksPerFrame) == 0)
            await Task.Yield();
    }
}

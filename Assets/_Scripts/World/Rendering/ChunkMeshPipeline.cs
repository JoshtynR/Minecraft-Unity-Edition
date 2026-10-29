using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// A failed/cancelled batch must not retain successful siblings' staging buffers.
public static class ChunkMeshPipeline
{
    public static MeshData[] Build(IList<ChunkData> chunks, ParallelOptions options, CancellationToken token)
    {
        var results = new MeshData[chunks.Count];
        try
        {
            Parallel.For(0, chunks.Count, options, index =>
            {
                token.ThrowIfCancellationRequested();
                results[index] = chunks[index].GetMeshData();
            });
            return results;
        }
        catch
        {
            Release(results);
            throw;
        }
    }
    public static void Release(IEnumerable<MeshData> meshes)
    {
        foreach (var mesh in meshes) mesh?.ReleaseAfterUpload();
    }
}

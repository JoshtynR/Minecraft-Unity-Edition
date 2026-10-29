using System.Collections.Generic;
using UnityEngine;

public class WorldRenderer : MonoBehaviour
{
    private const int MaximumPooledChunkObjects=32;
    public GameObject chunkPrefab;
    [System.NonSerialized] private Queue<ChunkRenderer> runtimeChunkPool;
    public Queue<ChunkRenderer> chunkPool => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeChunkPool, () => new Queue<ChunkRenderer>());

    public ChunkRenderer AcquireChunk(WorldData worldData, Vector3Int pos)
    {
        if (!worldData.chunkDataDict.TryGetValue(pos, out var data)) return null;
        ChunkRenderer newChunk;
        if(chunkPool.Count > 0)
        {
            newChunk = chunkPool.Dequeue();
            newChunk.transform.position = pos;
        }
        else
        {
            var chunkObj = Instantiate(chunkPrefab, pos, Quaternion.identity, transform);
            newChunk = chunkObj.GetComponent<ChunkRenderer>();
            
        }

        newChunk.Initialize(data);
        
        return newChunk;
    }
    
    public void RemoveChunk(ChunkRenderer chunk)
    {
        if(chunk==null)return;
        chunk.ReleaseForPool();
        chunk.gameObject.SetActive(false);
        if(chunkPool.Count<MaximumPooledChunkObjects)chunkPool.Enqueue(chunk);
        else Destroy(chunk.gameObject);
    }
}

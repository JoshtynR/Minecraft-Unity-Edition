using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class WorldSaveData
{
    public string worldName;
    public WorldDimension dimension;
    
    public Vector3Int seedOffset;
    public long betaWorldSeed;
    public bool hasWorldTime;
    public double worldTimeTicks;
    public FluidTickSave fluidTicks;
    public DroppedItemState[] droppedItems;
    public MobSaveData mobs;
    // Read-only compatibility with releases that stored every species as cows.
    public MobSaveData cows;
    public MobSaveData SavedMobs => mobs ?? cows;
    
    [NonSerialized]
    public ChunkSaveData[] chunks;
    
    public Dictionary<Vector3Int, ChunkSaveData> chunksDic = new ();
    
    public ChunkSaveData RequestChunkData(Vector3Int chunkPos)
    {
        if (chunksDic.ContainsKey(chunkPos))
        {
            return chunksDic[chunkPos];
        }
        else
        {
            return null;
        }
    }
}

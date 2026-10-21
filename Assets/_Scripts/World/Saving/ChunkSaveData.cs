using System;
using UnityEngine;

[Serializable]
public class ChunkSaveData
{
    public Vector3Int position;
    // Retained for compatibility with existing serialized worlds.
    public int betaTreePopulationVersion;
    public bool betaTreePopulationDone;
    public int betaPopulationVersion;
    public bool betaPopulationDone;
    public BetaPopulationRecord[] betaPopulationRecords;
    
    public ChunkSectionSaveData[] sections;

    public ChunkSaveData()
    {
        
    }
    
    public ChunkSaveData(Vector3Int position, ChunkSectionSaveData[] sections)
    {
        this.position = position;
        this.sections = sections;
    }

    public static ChunkSaveData Serialize(ChunkData chunk) 
    {
        var chunkSaveData = new ChunkSaveData(chunk.worldPos,new ChunkSectionSaveData[chunk.sections.Length]);
        for (int i = 0; i < chunkSaveData.sections.Length; i++)
        {
            chunkSaveData.sections[i] = new ChunkSectionSaveData(chunk.sections[i]);
        }
        
        chunkSaveData.betaPopulationVersion = 1;
        chunkSaveData.betaPopulationDone = chunk.betaPopulationDone;
        lock (chunk.betaPopulationRecords)
            chunkSaveData.betaPopulationRecords = chunk.betaPopulationRecords.ToArray();
        chunkSaveData.betaTreePopulationVersion = 1;
        chunkSaveData.betaTreePopulationDone = chunk.betaPopulationDone;
        // Creating a network/save snapshot is read-only. Only the actual save
        // transaction may clear dirty state; network transfers are not disk saves.
        
        return chunkSaveData;
    }
}

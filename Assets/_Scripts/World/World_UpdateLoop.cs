using System;
using System.Collections.Generic;
using UnityEngine;

public partial class World
{
    [NonSerialized] private Queue<Block> runtimeBlockToUpdate;
    public Queue<Block> blockToUpdate
    {get=>System.Threading.LazyInitializer.EnsureInitialized(ref runtimeBlockToUpdate, () => new Queue<Block>());set=>runtimeBlockToUpdate=value;}
    private Camera backgroundCamera;

    private void Update()
    {
        AdvanceAtmosphereClock(Time.deltaTime);
        UpdateWater();
        UpdateBlocks();
        UpdateLeafDecay();
        UpdateFallingBlocks();
        UpdateDroppedItems();
        UpdateFurnaces();
        UpdateMobs();
        UpdatePortals();
        StreamDimensionPlayers();
        if (Application.isPlaying) UpdateChunks();
        if (Instance != this) return;
        UpdateLighting();
        if (backgroundCamera == null) backgroundCamera = Camera.main;
        if (backgroundCamera != null)
            backgroundCamera.backgroundColor = dimension == WorldDimension.Nether ? new Color(.2f,.03f,.03f,1) :
                Color.Lerp(new Color(.015f,.02f,.045f),new Color(.50f,.70f,1f),SkyClock.Daylight(CurrentWorldTicks));
    }

    public void UpdateBlocks()
    {
        if (disabled || !IsWorldCreated || GenerationBusy || isSaving) return;
        // Neighbor cascades (notably leaves) must never monopolize a frame.
        int budget = Math.Min(blockToUpdate.Count, 128);
        while (budget-- > 0) ProcessBlockUpdate(blockToUpdate.Dequeue());
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using UnityEngine;

[Serializable]
public class Block
{
    public Vector3Int position;
    public Vector3Int localChunkPosition => position + new Vector3Int(0, section.yOffset, 0);
    public Vector3Int globalWorldPosition => section.dataRef.GetGlobalBlockCoords(localChunkPosition);

    /// <summary>
    /// Note: never set this directly, use <see cref="SetType(BlockType)"/> instead
    /// </summary>
    public BlockType type;
    // 0: source, 1..7: horizontal flow, 8..15: falling water.
    public byte waterLevel;
    public BlockTypeData BlockData => BlockDataManager.blockTypeDataDictionary[(int)type];
    
    [NonSerialized] public ChunkSection section;
    public ChunkData chunkData => section.dataRef;
    
    public BlockShapes blockShape;

    public Block(BlockType type, Vector3Int position, ChunkSection section)
    {
        this.type = type;
        this.position = position;
        this.section = section;
        blockShape = BlockShapes.FullBlock;
    }

    public Block(Block block)
    {
        this.type = block.type;
        this.waterLevel = block.waterLevel;
        this.position = block.position;
        this.section = block.section;
        blockShape = BlockShapes.FullBlock;
    }

    public Block Reset(Block block)
    {
        this.type = block.type;
        this.waterLevel = block.waterLevel;
        this.position = block.position;
        this.section = block.section;
        blockShape = BlockShapes.FullBlock;
        return this;
    }

    public void Loaded()
    {
        if (BlockData.lightEmission > 0)
        {
            SetBlockLight(BlockData.lightEmission);
            chunkData.blockLightUpdateQueue.Enqueue(new BlockLightNode(this, BlockData.lightEmission));
        }
    }

    public int GetBlockLight()
    {
        if (type == BlockType.Nothing) return 0;
        
        return section.GetBlockLight(position);
    }
    
    public int GetSkyLight()
    {
        if (type == BlockType.Nothing) return 0;
        
        return section.GetSkylight(position);
    }
    
    public void SetBlockLight(int value)
    {
        section.SetBlockLight(position, value);
    }
    
    public void SetSkyLight(int value)
    {
        if (type == BlockType.Nothing || value == GetSkyLight()) return;

        section.SetSunlight(position, value);
    }
    
    // This will return the highest value of either the block or the sunlight
    public int GetLight()
    {
        if (type == BlockType.Nothing) return 0;
        
        return Mathf.Max(GetSkyLight(), GetBlockLight());
    }
    
    public void SetType(BlockType type)
    {
        if (chunkData.isGenerated && chunkData.worldRef.IsWorldCreated)
        {
            lock (Lighting.SyncRoot) SetTypeCore(type);
        }
        else SetTypeCore(type);
    }

    private void SetTypeCore(BlockType type)
    {
        var x = position.x;
        var y = position.y;
        var z = position.z;
        
        var oldOpacity = BlockData.opacity;
        var oldEmission = BlockData.lightEmission;
        var brokeBlock = this.type != BlockType.Nothing && type == BlockType.Air;
        var oldType = this.type;

        this.type = type;
        waterLevel = 0;

        if (brokeBlock)
        {
            BrokeBlock(oldType);
        }
        else
        {
            PlacedBlock();
        }

        section.blocks[x, y, z] = BlockMapping.MapTypeToBlock(type, this);
        // A previously lit data-only halo can receive trees before it gets its
        // first renderer. Re-seed it after those population changes.
        if ((oldOpacity != BlockData.opacity || oldEmission != BlockData.lightEmission) && (!chunkData.isGenerated || !chunkData.worldRef.IsWorldCreated))
            chunkData.lightingInitialized = false;
        if ((oldOpacity != BlockData.opacity || oldEmission != BlockData.lightEmission) && chunkData.isGenerated && chunkData.worldRef.IsWorldCreated)
        {
            if (oldOpacity != BlockData.opacity) Lighting.RecastSunLight(chunkData, localChunkPosition, true);
            int previousLight = GetBlockLight();
            if (previousLight > 0)
                chunkData.blockLightRemoveQueue.Enqueue(new BlockLightNode(this, (byte)previousLight));
            SetBlockLight(BlockData.lightEmission);
            if (BlockData.lightEmission > 0)
                chunkData.blockLightUpdateQueue.Enqueue(new BlockLightNode(this, BlockData.lightEmission));
            foreach (var neighbor in GetNeighbors())
                if (neighbor.type != BlockType.Nothing && neighbor.GetBlockLight() > 0)
                    chunkData.blockLightUpdateQueue.Enqueue(new BlockLightNode(neighbor, (byte)neighbor.GetBlockLight()));
        }
        if (chunkData.isGenerated && chunkData.worldRef.IsWorldCreated)
            chunkData.worldRef.NotifyWaterBlockChanged(globalWorldPosition);
    }

    public void PlacedBlock()
    {
        if (BlockData.opacity == 15)
        {
            if (GetSkyLight() > 0)
            {
                if (GetSkyLight() == 15)
                {
                    SetSkyLight(0);
                    chunkData.skyRemoveList.Insert(chunkData.skyRemoveList.Count, this);
                }
                else
                {
                    chunkData.skyLightRemoveQueue.Enqueue(new BlockLightNode(this, (byte)GetSkyLight()));
                    SetSkyLight(0);
                }
            }
                
                
            if (GetBlockLight() > 0)
            {
                chunkData.blockLightRemoveQueue.Enqueue(new BlockLightNode(this, (byte)GetBlockLight()));
                SetBlockLight(0);
            }
        }

        if (BlockData.lightEmission > 0)
        {
            SetBlockLight(BlockData.lightEmission);
            chunkData.blockLightUpdateQueue.Enqueue(new BlockLightNode(this, BlockData.lightEmission));
        }
    }

    public void BrokeBlock(BlockType oldType)
    {
        if (BlockDataManager.blockTypeDataDictionary[(int)oldType].opacity == 15)
        {
            // Block
            if(GetNeighbors().Any(n => n.GetBlockLight() > 0))
            {
                chunkData.blockLightRemoveQueue.Enqueue(new BlockLightNode(this, (byte)GetBlockLight()));
                SetBlockLight(0);
            }


            if (localChunkPosition.y == World.Instance.worldHeight - 1 || chunkData.GetBlock(localChunkPosition + Vector3Int.up).GetSkyLight() == 15)
            {
                SetSkyLight(15);
                chunkData.skyExtendList.Insert(chunkData.skyExtendList.Count, this);
            }
            else
            {
                chunkData.skyLightRemoveQueue.Enqueue(new BlockLightNode(this, (byte)GetSkyLight()));
                SetSkyLight(0);
            }
                
        }
    }
    
    public Block[] GetNeighbors()
    {
        return new Block[] {
            chunkData.GetBlock(localChunkPosition + Vector3Int.left),
            chunkData.GetBlock(localChunkPosition + Vector3Int.right),
            chunkData.GetBlock(localChunkPosition + Vector3Int.forward),
            chunkData.GetBlock(localChunkPosition + Vector3Int.back),
            chunkData.GetBlock(localChunkPosition + Vector3Int.up),
            chunkData.GetBlock(localChunkPosition + Vector3Int.down),
        };
    }

    public virtual void OnBlockPlaced()
    {
        if(!section.dataRef.worldRef.IsWorldCreated) return;
        
        // Debug.Log($"Block {type.ToString()} placed");
        World.Instance.ExecuteAfterFrames(1, () => World.Instance.blockToUpdate.Enqueue(this));
        UpdateNeighbours();
    }
    
    public virtual void OnBlockDestroyed()
    {
        if(section?.dataRef.worldRef.IsWorldCreated != true) return;
        // Debug.Log($"Block {type.ToString()} destroyed");
        UpdateNeighbours();
    }
    
    public void UpdateNeighbours()
    {
        if(section?.dataRef.worldRef.IsWorldCreated != true) return;
        
        foreach (var direction in BlockHelper.directions)
        {
            var pos = position + direction.GetVector();
            pos.y += section.yOffset;
            var neighbour = section.dataRef.GetBlock(pos);
            if (neighbour != null)
            {
                World.Instance.ExecuteAfterFrames(1, () => World.Instance.blockToUpdate.Enqueue(neighbour));
            }
        }
    }

    public virtual void OnBlockUpdate()
    {
        if(section?.dataRef.worldRef.IsWorldCreated != true) return;
        
        // Debug.Log($"Block {type.ToString()} updated");
    }
}

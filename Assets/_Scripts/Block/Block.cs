using System;
using UnityEngine;

[Serializable]
public struct Block : IEquatable<Block>
{
    // A Block is a short-lived handle, not the cell itself. Resident type/state
    // data lives in ChunkSection.PackedBlockStorage (two bytes per cell).
    [NonSerialized] private ushort packedPosition;
    [NonSerialized] private ushort detachedCell;
    [NonSerialized] private bool attached;
    [NonSerialized] private bool initialized;
    public Vector3Int position
    {
        get => new Vector3Int(packedPosition & 15,(packedPosition >> 4) & 15,(packedPosition >> 8) & 15);
        private set => packedPosition=(ushort)((value.x & 15)|((value.y & 15)<<4)|((value.z & 15)<<8));
    }
    public Vector3Int localChunkPosition => position + new Vector3Int(0, section == null ? 0 : section.yOffset, 0);
    public Vector3Int globalWorldPosition => section == null ? position : section.dataRef.GetGlobalBlockCoords(localChunkPosition);

    /// <summary>
    /// Note: never set this directly, use <see cref="SetType(BlockType)"/> instead
    /// </summary>
    public BlockType type
    {
        get => (BlockType)(ReadCell() & 0x0FFF);
        set
        {
            initialized = true;
            WriteCell(Pack(WheatCropRules.Canonical(value), State));
        }
    }
    // Legacy name: shared water/lava metadata. 0 source, 1..7 flow, 8..15 falling.
    public byte waterLevel
    {
        get => State;
        set { initialized = true; WriteCell(Pack(type, value)); }
    }
    public BlockTypeData BlockData => BlockDataManager.blockTypeDataDictionary[(int)type];
    
    [NonSerialized] public ChunkSection section;
    public ChunkData chunkData => section.dataRef;
    
    public BlockShapes blockShape
    {
        get => SpecialBlockGeometry.Shape(type, State);
        set { initialized = true; WriteCell(Pack(type, SpecialBlockGeometry.State(type, value))); }
    }

    internal ushort PackedCell => ReadCell();
    internal byte State => (byte)((ReadCell() >> 12) & 15);

    private static ushort Pack(BlockType type, byte state)
    {
        return (ushort)(((int)WheatCropRules.Canonical(type) & 0x0FFF) | ((state & 15) << 12));
    }

    private ushort ReadCell()
    {
        return attached && section != null ? section.ReadPackedCell(packedPosition) : detachedCell;
    }

    private void WriteCell(ushort cell)
    {
        if (attached && section != null) section.WritePackedCell(packedPosition, cell);
        else detachedCell = cell;
    }

    internal static Block Attached(ushort packedPosition, ChunkSection section)
    {
        return new Block { packedPosition = packedPosition, section = section, attached = true, initialized = true };
    }

    public Block(BlockType type, Vector3Int position, ChunkSection section)
    {
        packedPosition = 0;
        detachedCell = 0;
        attached = false;
        initialized = true;
        this.section = section;
        this.position = position;
        byte state = WheatCropRules.IsCrop(type) ? WheatCropRules.LegacyAge(type) : SpecialBlockGeometry.DefaultState(type);
        detachedCell = Pack(type, state);
    }

    public Block(Block block)
    {
        packedPosition = block.packedPosition;
        detachedCell = block.ReadCell();
        attached = false;
        initialized = block.initialized;
        section = block.section;
    }

    public Block Reset(Block block)
    {
        packedPosition = block.packedPosition;
        detachedCell = block.ReadCell();
        attached = false;
        initialized = block.initialized;
        section = block.section;
        return this;
    }

    public bool Equals(Block other)
    {
        if (!initialized || !other.initialized) return initialized == other.initialized;
        if (attached || other.attached)
            return attached == other.attached && ReferenceEquals(section, other.section) && packedPosition == other.packedPosition;
        return packedPosition == other.packedPosition && ReadCell() == other.ReadCell() && ReferenceEquals(section, other.section);
    }

    public override bool Equals(object obj) => obj is Block && Equals((Block)obj);
    public override int GetHashCode()
    {
        unchecked { return ((section == null ? 0 : section.GetHashCode()) * 397) ^ packedPosition ^ (attached ? 0x40000000 : ReadCell()); }
    }
    public static bool operator ==(Block left, Block right) => left.Equals(right);
    public static bool operator !=(Block left, Block right) => !left.Equals(right);
    // Preserve the old reference-style null checks while callers migrate: a
    // default Block handle represents no raycast/query result.
    public static bool operator ==(Block left, object right) => right == null ? !left.initialized : left.Equals(right);
    public static bool operator !=(Block left, object right) => !(left == right);
    public static bool operator ==(object left, Block right) => left == null ? !right.initialized : right.Equals(left);
    public static bool operator !=(object left, Block right) => !(left == right);

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

    // Raw terrain and population for a chunk that has never gone live do not
    // need placement callbacks or incremental lighting. The complete lighting
    // pass runs after population, so doing that work for every one of the
    // chunk's 32,768 cells only creates queues and garbage that are discarded.
    internal void InitializeGeneratedType(BlockType type)
    {
        byte cropAge = WheatCropRules.LegacyAge(type);
        initialized = true;
        WriteCell(Pack(type, WheatCropRules.IsCrop(type) ? cropAge : SpecialBlockGeometry.DefaultState(type)));
    }

    private void SetTypeCore(BlockType type)
    {
        byte cropAge = WheatCropRules.LegacyAge(type);
        type = WheatCropRules.Canonical(type);
        var x = position.x;
        var y = position.y;
        var z = position.z;
        
        var oldOpacity = BlockData.opacity;
        var oldEmission = BlockData.lightEmission;
        var brokeBlock = this.type != BlockType.Nothing && type == BlockType.Air;
        var oldType = this.type;

        WriteCell(Pack(type, WheatCropRules.IsCrop(type) ? cropAge : SpecialBlockGeometry.DefaultState(type)));

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
            for (int i = 0; i < 6; i++)
            {
                var neighbor = GetNeighbor(i);
                if (neighbor.type != BlockType.Nothing && neighbor.GetBlockLight() > 0)
                    chunkData.blockLightUpdateQueue.Enqueue(new BlockLightNode(neighbor, (byte)neighbor.GetBlockLight()));
            }
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
            bool hasLitNeighbor = false;
            for (int i = 0; i < 6 && !hasLitNeighbor; i++)
            {
                var neighbor = GetNeighbor(i);
                hasLitNeighbor = neighbor.type != BlockType.Nothing && neighbor.GetBlockLight() > 0;
            }
            if(hasLitNeighbor)
            {
                chunkData.blockLightRemoveQueue.Enqueue(new BlockLightNode(this, (byte)GetBlockLight()));
                SetBlockLight(0);
            }


            if (chunkData.worldRef.dimension != WorldDimension.Nether && (localChunkPosition.y == chunkData.worldRef.worldHeight - 1 || chunkData.GetBlock(localChunkPosition + Vector3Int.up).GetSkyLight() == 15))
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
    
    public Block GetNeighbor(int index)
    {
        Vector3Int offset;
        switch (index)
        {
            case 0: offset = Vector3Int.left; break;
            case 1: offset = Vector3Int.right; break;
            case 2: offset = Vector3Int.forward; break;
            case 3: offset = Vector3Int.back; break;
            case 4: offset = Vector3Int.up; break;
            default: offset = Vector3Int.down; break;
        }
        return chunkData.GetBlock(localChunkPosition + offset);
    }

    public void OnBlockPlaced()
    {
        if(!section.dataRef.worldRef.IsWorldCreated) return;
        
        // Debug.Log($"Block {type.ToString()} placed");
        var self = this;
        var world = chunkData.worldRef;
        world.ExecuteAfterFrames(1, () => world.blockToUpdate.Enqueue(self));
        UpdateNeighbours();
    }
    
    public void OnBlockDestroyed()
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
                var world = chunkData.worldRef;
                world.ExecuteAfterFrames(1, () => world.blockToUpdate.Enqueue(neighbour));
            }
        }
    }

    public void OnBlockUpdate()
    {
        if(section?.dataRef.worldRef.IsWorldCreated != true) return;
        // Water used to allocate a derived Block object for every water cell
        // solely for this callback. Type dispatch keeps the behavior without a
        // second object class or replacement allocation on every fluid change.
        if(type==BlockType.Water)
            chunkData.worldRef.NotifyWaterBlockChanged(globalWorldPosition);
    }
}

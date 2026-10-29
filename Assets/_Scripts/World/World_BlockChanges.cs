using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Live block changes: validate paired state, write cells, run effects, then invalidate rendering.
public partial class World
{
    public void SetBlocks(IEnumerable<Vector3Int> blockPoss, BlockType blockType)
    {
        if (blockPoss == null) return;
        var positions = blockPoss as IList<Vector3Int> ?? blockPoss.ToList();
        if (positions.Count == 0) return;
        var chunkPos = WorldDataHelper.GetChunkPosition(this, positions[0]);
        var chunk = WorldDataHelper.GetChunk(this, chunkPos);
        SetBlocks(chunk, positions, blockType);
    }

    public void SetBlocks(ChunkRenderer chunk, IEnumerable<Vector3Int> blockPoss, BlockType blockType, byte stairFacing = 0)
    {
        if(disabled||chunk==null||chunk.ChunkData==null||blockPoss==null)return;
        if(WheatCropRules.IsLegacy(blockType)){stairFacing=WheatCropRules.LegacyAge(blockType);blockType=WheatCropRules.Crop;}
        var affectedChunks = new HashSet<ChunkRenderer>();

        foreach (var pos in blockPoss)
        {
            if(pos.y<0||pos.y>=worldHeight)continue;
            var previous = GetBlock(pos);
            BlockType previousType = previous != null ? previous.type : BlockType.Nothing;
            bool removingCrop=previous!=null&&WheatCropRules.IsCrop(previous.type)&&!WheatCropRules.IsCrop(blockType);
            byte cropAge=removingCrop?SpecialBlockGeometry.State(previous.type,previous.blockShape):(byte)0;
            bool removingChest=previous!=null&&previous.type==BlockType.Chest&&blockType!=BlockType.Chest;
            bool removingFurnace=previous!=null&&IsFurnace(previous.type)&&!IsFurnace(blockType);
            if(!CanReplaceChest(pos,blockType))continue;
            if(previousType==BlockType.Nothing)continue;
            PrepareDoorHinge(pos, previousType, blockType, ref stairFacing, affectedChunks);
            if (!TryWritePairedBlock(pos, previousType, blockType, stairFacing, affectedChunks)) continue;
            RemovePairedBlock(pos, previous, blockType, affectedChunks);
            WorldDataHelper.SetBlock(this, pos, blockType);
            var placedBlock = GetBlock(pos);
            placedBlock.blockShape = SpecialBlockGeometry.Shape(blockType, stairFacing);
            ApplyBlockChangeEffects(pos, previousType, blockType, cropAge, removingCrop, removingChest, removingFurnace);
            RefreshRailsAround(pos, affectedChunks);
            CollectRenderUpdatesAround(pos, affectedChunks);
        }
        affectedChunks.Add(chunk);

        // Mutations request rendering; only the shared scheduler starts jobs.
        foreach (var affected in affectedChunks) AddChunkToUpdate(affected);
    }

    public void SetBlock(ChunkRenderer chunk, Vector3Int blockPos, BlockType blockType, byte stairFacing = 0)
    {
        SetBlocks(chunk, new[] {blockPos}, blockType, stairFacing);
    }


    private void PrepareDoorHinge(Vector3Int pos, BlockType previousType, BlockType blockType, ref byte stairFacing, HashSet<ChunkRenderer> affectedChunks)
    {
        if(SpecialBlockGeometry.IsDoor(blockType)&&previousType!=blockType&&(stairFacing&8)==0&&
            BlockSurvivalRules.CanReplace(previousType,blockType)&&pos.y+1<worldHeight&&BlockSurvivalRules.Replaceable(GetBlock(pos+Vector3Int.up).type))
        {
            // Placing the right-hand door must also move the existing
            // left door's hinge to its outside edge, not into the seam.
            int direction=(stairFacing&4)!=0?(stairFacing+1)&3:stairFacing&3;
            var side=direction==0?Vector3Int.forward:direction==1?Vector3Int.left:direction==2?Vector3Int.back:Vector3Int.right;
            var neighborPos=pos+side;var neighbor=GetBlock(neighborPos);
            byte neighborState=SpecialBlockGeometry.State(neighbor.type,neighbor.blockShape);
            int plane=(neighborState&4)==0?(neighborState-1)&3:neighborState&3;
            if(neighbor.type==blockType&&(neighborState&8)==0&&plane==((direction-1)&3))
            {
                byte hinged=(byte)(((direction-1)&3)|4);
                neighbor.blockShape=SpecialBlockGeometry.Shape(blockType,hinged);
                var upper=GetBlock(neighborPos+Vector3Int.up);
                if(upper.type==blockType)upper.blockShape=SpecialBlockGeometry.Shape(blockType,(byte)(hinged|8));
                var neighborChunk=WorldDataHelper.GetChunk(this,WorldDataHelper.GetChunkPosition(this,neighborPos));
                if(neighborChunk!=null){neighborChunk.ChunkData.modifiedAfterSave=true;CollectRenderUpdatesAround(neighborPos, affectedChunks);}
                stairFacing=(byte)direction;
            }
        }
    }

    private bool TryWritePairedBlock(Vector3Int pos, BlockType previousType, BlockType blockType, byte stairFacing, HashSet<ChunkRenderer> affectedChunks)
    {
        // Validate both halves before writing either. Direct calls and remote
        // clients must not overwrite a neighbor when placing a door/bed.
        if ((SpecialBlockGeometry.IsDoor(blockType) || blockType == BlockType.Bed) && (stairFacing & 8) == 0)
        {
            var partnerPos = pos + SpecialBlockGeometry.PartnerOffset(blockType, stairFacing);
            var partner = GetBlock(partnerPos);
            bool updating = previousType == blockType && partner != null && partner.type == blockType;
            if (partnerPos.y < 0 || partnerPos.y >= worldHeight || partner == null ||
                (!updating && (!BlockSurvivalRules.Replaceable(partner.type) || !BlockSurvivalRules.CanReplace(previousType,blockType)))) return false;
            WorldDataHelper.SetBlock(this, partnerPos, blockType);
            var partnerBlock = GetBlock(partnerPos);
            partnerBlock.blockShape = SpecialBlockGeometry.Shape(blockType, (byte)(stairFacing|8));
            QueueSurvivalAround(partnerPos);
            var partnerChunk = WorldDataHelper.GetChunk(this, WorldDataHelper.GetChunkPosition(this,partnerPos));
            if (partnerChunk != null) { partnerChunk.ChunkData.modifiedAfterSave = true; CollectRenderUpdatesAround(partnerPos, affectedChunks); }
        }
        return true;
    }

    private void RemovePairedBlock(Vector3Int pos, Block previous, BlockType blockType, HashSet<ChunkRenderer> affectedChunks)
    {
        if ((SpecialBlockGeometry.IsDoor(previous.type) || previous.type == BlockType.Bed) && previous.type != blockType)
        {
            byte oldState = SpecialBlockGeometry.State(previous.type,previous.blockShape);
            var partnerPos = pos + SpecialBlockGeometry.PartnerOffset(previous.type,oldState);
            var partner = GetBlock(partnerPos);
            if (partner != null && partner.type == previous.type && (SpecialBlockGeometry.State(partner.type,partner.blockShape)^oldState)==8)
            {
                WorldDataHelper.SetBlock(this,partnerPos,BlockType.Air);
                QueueSurvivalAround(partnerPos);
                var partnerChunk = WorldDataHelper.GetChunk(this,WorldDataHelper.GetChunkPosition(this,partnerPos));
                if (partnerChunk != null) { partnerChunk.ChunkData.modifiedAfterSave = true; CollectRenderUpdatesAround(partnerPos, affectedChunks); }
            }
        }
    }

    private void ApplyBlockChangeEffects(Vector3Int pos, BlockType previousType, BlockType blockType, byte cropAge, bool removingCrop, bool removingChest, bool removingFurnace)
    {
        QueueSurvivalAround(pos);
        if (BlockSurvivalRules.IsLog(previousType) && !BlockSurvivalRules.IsLog(blockType)) QueueLeavesAroundRemovedLog(pos);
        QueuePortalValidation(pos);
        if(removingChest)chests.Removed(pos);
        if(removingFurnace)RemoveFurnace(pos);
        if(removingCrop)DropWheat(pos,cropAge);
        if(IsFurnace(blockType))TrackFurnace(pos);
    }

    private void RefreshRailsAround(Vector3Int pos, HashSet<ChunkRenderer> affectedChunks)
    {
        // Track is stateful, unlike purely visual fence/dust joins. Update
        // its saved selection bounds and neighboring slopes before meshing.
        for (int rx=-1;rx<=1;rx++) for(int ry=-1;ry<=1;ry++) for(int rz=-1;rz<=1;rz++)
        {
            var railPos=pos+new Vector3Int(rx,ry,rz);
            var rail=GetBlock(railPos);
            if(rail==null||!SpecialBlockGeometry.IsRail(rail.type))continue;
            byte state=SpecialBlockGeometry.RailState(rail.type,SpecialBlockGeometry.State(rail.type,rail.blockShape),railPos,p=>GetBlock(p).type);
            rail.blockShape=SpecialBlockGeometry.Shape(rail.type,state);
            var railChunk=WorldDataHelper.GetChunk(this,WorldDataHelper.GetChunkPosition(this,railPos));
            if(railChunk!=null){railChunk.ChunkData.modifiedAfterSave=true;CollectRenderUpdatesAround(railPos, affectedChunks);}
        }
    }
}

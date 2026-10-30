using UnityEngine;

public partial class World
{
    public static bool BucketCanReplace(BlockType type) => FluidCanReplace(type)||type==BlockType.Water||type==BlockType.Lava;

    public bool UseBucket(Vector3Int position,ref InventoryStack bucket)
    {
        if(!DropAuthority||!IsWorldCreated||disabled||isSaving||bucket.count!=1||!TryGetWaterBlock(position,out var block))return false;
        BlockType placed;ItemType result;
        if(bucket.type==ItemType.Bucket)
        {
            if(block.waterLevel!=0||block.type!=BlockType.Water&&block.type!=BlockType.Lava)return false;
            placed=BlockType.Air;result=block.type==BlockType.Water?ItemType.WaterBucket:ItemType.LavaBucket;
        }
        else
        {
            if(bucket.type!=ItemType.WaterBucket&&bucket.type!=ItemType.LavaBucket||!BucketCanReplace(block.type))return false;
            placed=bucket.type==ItemType.WaterBucket?BlockType.Water:BlockType.Lava;result=ItemType.Bucket;
            if(placed==BlockType.Water&&dimension==WorldDimension.Nether)
            {
                // Water evaporates rather than leaving a source in the Nether.
                EmitFluidEffect(position,0);bucket=new InventoryStack(result,1);return true;
            }
        }
        if(!ApplyFluidState(position,placed,0))return false;
        // Use the existing fluid batch transport, dirty meshes and tick scheduler.
        waterChanges[position]=placed==BlockType.Air?byte.MaxValue:placed==BlockType.Lava?(byte)32:(byte)0;
        NotifyWaterBlockChanged(position);
        bucket=new InventoryStack(result,1);return true;
    }
}

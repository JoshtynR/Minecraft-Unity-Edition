using Mirror;
using UnityEngine;

public partial class World
{
    private int FarmLight(Block block) => block == null ? 0 : System.Math.Max(block.GetBlockLight(), block.GetSkyLight() - skyLightReduction);

    private void SetFarmBlock(Vector3Int position, BlockType type, byte state = 0)
    {
        SetBlock(position, type, state);
        if (NetworkServer.active && WorldServer.instance != null) WorldServer.instance.BroadcastBlock(this,position, type, state);
    }

    public void DropWheat(Vector3Int position, int age)
    {
        if (!DropAuthority) return;
        if (age == 7) SpawnItemDrop(new InventoryStack(ItemType.Wheat, 1), (Vector3)position + Vector3.one * .5f);
        int seeds = WheatCropRules.SeedDrops(age, bound => fluidRandom != null ? fluidRandom.NextInt(bound) : Random.Range(0, bound));
        if (seeds > 0) SpawnItemDrop(new InventoryStack(ItemType.Seeds, seeds), (Vector3)position + Vector3.one * .5f);
    }

    private bool CropSurvives(Vector3Int position, Block crop)
    {
        var below = GetBlock(position + Vector3Int.down);
        if (below == null || below.type == BlockType.Nothing) return true; // Wait for the neighbor data.
        return WheatCropRules.IsSoil(below.type) && (FarmLight(crop) >= 8 || crop.GetSkyLight() == 15);
    }

    private void ValidateFarmNeighbors(Vector3Int changed)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            var p = changed + new Vector3Int(0, dy, 0);
            if (!TryGetWaterBlock(p, out var block)) continue;
            if (WheatCropRules.IsCrop(block.type) && !CropSurvives(p, block)) SetFarmBlock(p, BlockType.Air);
            else if (WheatCropRules.IsSoil(block.type))
            {
                var above = GetBlock(p + Vector3Int.up);
                if (above != null && above.type != BlockType.Nothing && FluidHasSolidMaterial(above.type)) SetFarmBlock(p, BlockType.Dirt);
            }
        }
    }

    private void TickFarming(WaterPosition position, Block block)
    {
        var p = BlockPos(position);
        if (WheatCropRules.IsCrop(block.type))
        {
            if (!CropSurvives(p, block)) { SetFarmBlock(p, BlockType.Air); return; }
            int age = SpecialBlockGeometry.State(block.type, block.blockShape) & 7;
            if (age == 7 || FarmLight(GetBlock(p + Vector3Int.up)) < 9) return;
            int bound = WheatCropRules.GrowthBound((x,z) => GetBlock(p + new Vector3Int(x,0,z)).type, (x,z) =>
            {
                var soil = GetBlock(p + new Vector3Int(x,-1,z));
                return !WheatCropRules.IsSoil(soil.type) ? -1 : SpecialBlockGeometry.State(soil.type,soil.blockShape) & 7;
            });
            if (fluidRandom.NextInt(bound) == 0) SetFarmBlock(p, WheatCropRules.Crop, (byte)(age + 1));
            return;
        }
        if (!WheatCropRules.IsSoil(block.type)) return;
        var top = GetBlock(p + Vector3Int.up);
        if (top.type != BlockType.Nothing && FluidHasSolidMaterial(top.type)) { SetFarmBlock(p, BlockType.Dirt); return; }
        if (fluidRandom.NextInt(5) != 0) return;
        bool water = false, unknown = false;
        for (int x = -4; x <= 4; x++) for (int z = -4; z <= 4; z++) for (int y = 0; y <= 1; y++)
        {
            var type = GetBlock(p + new Vector3Int(x,y,z)).type;
            water |= type == BlockType.Water; unknown |= type == BlockType.Nothing;
        }
        int moisture = SpecialBlockGeometry.State(block.type,block.blockShape) & 7;
        if (water)
        {
            if (moisture != 7 || block.type != BlockType.MoistFarmland) SetFarmBlock(p, BlockType.MoistFarmland, 7);
        }
        else if (!unknown)
        {
            if (moisture > 0) SetFarmBlock(p, moisture == 1 ? BlockType.Farmland : BlockType.MoistFarmland, (byte)(moisture - 1));
            else if (!WheatCropRules.IsCrop(top.type)) SetFarmBlock(p, BlockType.Dirt);
        }
    }

    private void TickGrass(WaterPosition position)
    {
        var p=BlockPos(position);
        var above=GetBlock(p+Vector3Int.up);
        if(above==null||above.type==BlockType.Nothing)return;
        int lightAbove=FarmLight(above);
        if(GrassSpreadRules.ShouldDecay(lightAbove,above.BlockData.opacity))
        {
            // Beta only converts covered grass on one of four selected ticks.
            if(fluidRandom.NextInt(4)==0)SetFarmBlock(p,BlockType.Dirt);
            return;
        }
        if(!GrassSpreadRules.CanSpreadFrom(lightAbove))return;

        // One nearby dirt candidate per selected grass tick: X/Z +/-1 and
        // Y -3..+1, exactly matching BlockGrass.updateTick in Beta 1.7.3.
        var candidate=p+new Vector3Int(fluidRandom.NextInt(3)-1,fluidRandom.NextInt(5)-3,fluidRandom.NextInt(3)-1);
        var dirt=GetBlock(candidate);var candidateAbove=GetBlock(candidate+Vector3Int.up);
        if(dirt==null||candidateAbove==null||dirt.type==BlockType.Nothing||candidateAbove.type==BlockType.Nothing)return;
        if(GrassSpreadRules.CanBecomeGrass(dirt.type,FarmLight(candidateAbove),candidateAbove.BlockData.opacity))
            SetFarmBlock(candidate,BlockType.Grass);
    }

    public bool UseFarmItem(Vector3Int target, Vector3Int adjacent, ref InventoryStack held)
    {
        var block = GetBlock(target); if (block == null || held.Empty) return false;
        var offset=adjacent-target;
        if(System.Math.Abs(offset.x)+System.Math.Abs(offset.y)+System.Math.Abs(offset.z)!=1)return false;
        if(held.type==ItemType.FlintAndSteel)
        {
            if(!TryIgnitePortal(adjacent))
            {
                if(GetBlock(adjacent).type!=BlockType.Air||!SpecialBlockGeometry.NormalCube(GetBlock(adjacent+Vector3Int.down).type))return false;
                SetFarmBlock(adjacent,BlockType.Fire);
            }
            ItemDurability.Wear(ref held,1);GameAudio.Broadcast("fire/ignite",(Vector3)adjacent,.7f,1,this);return true;
        }
        if(held.type==ItemType.WaterBucket||held.type==ItemType.LavaBucket)
        {
            var type=GetBlock(adjacent).type;if(type!=BlockType.Air&&type!=BlockType.Fire)return false;
            if(held.type==ItemType.WaterBucket&&dimension==WorldDimension.Nether)
                GameAudio.Broadcast("random/fizz",(Vector3)adjacent,.5f,1,this);
            else SetFarmBlock(adjacent,held.type==ItemType.WaterBucket?BlockType.Water:BlockType.Lava);
            held=new InventoryStack(ItemType.Bucket,1);return true;
        }
        var above = GetBlock(target + Vector3Int.up);
        bool seeds = held.type == ItemType.Seeds || WheatCropRules.IsCrop((BlockType)(int)held.type);
        if (seeds && WheatCropRules.IsSoil(block.type) && adjacent == target + Vector3Int.up && above.type == BlockType.Air)
        {
            SetFarmBlock(target + Vector3Int.up, WheatCropRules.Crop);
            held.count--; held.Normalize(); return true;
        }
        if (FarmingItems.IsHoe(held.type) && (block.type == BlockType.Grass || block.type == BlockType.Dirt) && adjacent != target + Vector3Int.down && above.type == BlockType.Air)
        {
            SetFarmBlock(target, BlockType.Farmland);
            ItemDurability.Wear(ref held, 1); return true;
        }
        if (held.type == ItemType.BoneMeal && WheatCropRules.IsCrop(block.type) && SpecialBlockGeometry.State(block.type,block.blockShape) < 7)
        {
            SetFarmBlock(target, WheatCropRules.Crop, 7);
            held.count--; held.Normalize(); return true;
        }
        return false;
    }
}

public static class FarmingItems
{
    public static bool IsHoe(ItemType type) => type == ItemType.WoodenHoe || type == ItemType.StoneHoe || type == ItemType.IronHoe || type == ItemType.DiamondHoe || type == ItemType.GoldenHoe;
    public static bool CanUse(ItemType type) => type == ItemType.FlintAndSteel || type == ItemType.WaterBucket || type == ItemType.LavaBucket || IsHoe(type) || type == ItemType.Seeds || type == ItemType.BoneMeal || WheatCropRules.IsCrop((BlockType)(int)type);
}

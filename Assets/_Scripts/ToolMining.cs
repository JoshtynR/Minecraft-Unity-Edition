// Beta 1.7.3 ItemTool subclasses, InventoryPlayer and Block.blockStrength.
// Efficiency lists deliberately predate modern material-wide tool bonuses.
public static class ToolMining
{
    public static int Tier(ItemType tool)
    {
        switch(tool)
        {
            case ItemType.DiamondPickaxe:return 3;
            case ItemType.IronPickaxe:return 2;
            case ItemType.StonePickaxe:return 1;
            default:return 0;
        }
    }
    private static float Efficiency(ItemType tool)
    {
        switch(tool)
        {
            case ItemType.WoodenPickaxe:case ItemType.WoodenAxe:case ItemType.WoodenShovel:return 2;
            case ItemType.StonePickaxe:case ItemType.StoneAxe:case ItemType.StoneShovel:return 4;
            case ItemType.IronPickaxe:case ItemType.IronAxe:case ItemType.IronShovel:return 6;
            case ItemType.DiamondPickaxe:case ItemType.DiamondAxe:case ItemType.DiamondShovel:return 8;
            case ItemType.GoldenPickaxe:case ItemType.GoldenAxe:case ItemType.GoldenShovel:return 12;
            default:return 1;
        }
    }
    public static bool Harvestable(BlockType block,ItemType tool)
    {
        if(!HandMining.IsTarget(block)||HandMining.Hardness(block)<0)return false;
        if(HandMining.Harvestable(block))return true;
        var kind=ItemDurability.Kind(tool);
        if(block==BlockType.Cobweb)return kind==ItemToolKind.Sword||kind==ItemToolKind.Shears;
        if(block==BlockType.SnowLayer||block==BlockType.SnowBlock)return kind==ItemToolKind.Shovel;
        if(kind!=ItemToolKind.Pickaxe)return false;
        switch(block)
        {
            case BlockType.Obsidian:return Tier(tool)==3;
            case BlockType.DiamondOre:case BlockType.DiamondBlock:case BlockType.GoldOre:case BlockType.GoldBlock:
            case BlockType.RedstoneOre:case BlockType.LitRedstoneOre:return Tier(tool)>=2;
            case BlockType.IronOre:case BlockType.IronBlock:case BlockType.LapisOre:case BlockType.LapisBlock:return Tier(tool)>=1;
            default:return true; // Remaining non-hand materials are rock or iron.
        }
    }
    public static float Strength(BlockType block,ItemType tool)
    {
        var kind=ItemDurability.Kind(tool);
        if(kind==ItemToolKind.Sword)return block==BlockType.Cobweb?15:1.5f;
        if(kind==ItemToolKind.Shears)return block==BlockType.Cobweb||ItemDurability.IsLeaves(block)?15:ItemDurability.IsWool(block)?5:1;
        switch(kind)
        {
            case ItemToolKind.Pickaxe:
                switch(block)
                {
                    case BlockType.Cobblestone:case BlockType.DoubleStoneSlab:case BlockType.StoneSlab:
                    case BlockType.WoodenSlab:case BlockType.SandstoneSlab:case BlockType.CobblestoneSlab:
                    case BlockType.DoubleSandstoneSlab:case BlockType.DoubleWoodenSlab:case BlockType.DoubleCobblestoneSlab:
                    case BlockType.Stone:case BlockType.Sandstone:case BlockType.MossyCobblestone:
                    case BlockType.IronOre:case BlockType.IronBlock:case BlockType.CoalOre:case BlockType.GoldBlock:
                    case BlockType.GoldOre:case BlockType.DiamondOre:case BlockType.DiamondBlock:case BlockType.Ice:
                    case BlockType.Netherrack:case BlockType.LapisOre:case BlockType.LapisBlock:return Efficiency(tool);
                }
                break;
            case ItemToolKind.Axe:
                if(block==BlockType.WoodenPlanks||block==BlockType.Bookshelf||block==BlockType.Log||block==BlockType.SpruceLog||block==BlockType.BirchLog||block==BlockType.Chest)return Efficiency(tool);
                break;
            case ItemToolKind.Shovel:
                if(block==BlockType.Grass||block==BlockType.Dirt||block==BlockType.Sand||block==BlockType.Gravel||block==BlockType.SnowLayer||block==BlockType.SnowBlock||block==BlockType.Clay||block==BlockType.Farmland||block==BlockType.MoistFarmland)return Efficiency(tool);
                break;
        }
        return 1;
    }
    public static float DamagePerTick(BlockType block,ItemType tool,bool grounded,bool underwater)
    {
        float hardness=HandMining.Hardness(block);
        if(hardness<0)return 0;
        if(hardness==0)return 1;
        if(!Harvestable(block,tool))return 1/hardness/100;
        float strength=Strength(block,tool);
        if(underwater)strength/=5;
        if(!grounded)strength/=5;
        return strength/hardness/30;
    }
    // Rolls are supplied by the authoritative world, making the loot rules testable.
    public static InventoryStack Drop(BlockType block,ItemType tool,int gravelRoll,int saplingRoll,int quantityRoll,int toolDamage=0)
    {
        if(!Harvestable(block,tool))return new InventoryStack();
        if(ItemDurability.IsLeaves(block))
        {
            // Beta applies wear before the leaves special harvest callback.
            if(tool==ItemType.Shears&&toolDamage<ItemDurability.Maximum(tool))return new InventoryStack(ItemCatalog.FromBlock(block),1);
            return saplingRoll==0?new InventoryStack(block==BlockType.SpruceLeaves?ItemType.SpruceSapling:block==BlockType.BirchLeaves?ItemType.BirchSapling:ItemType.OakSapling,1):new InventoryStack();
        }
        switch(block)
        {
            case BlockType.Stone:return new InventoryStack(ItemType.Cobblestone,1);
            case BlockType.Gravel:return new InventoryStack(gravelRoll==0?ItemType.Flint:ItemType.Gravel,1);
            case BlockType.CoalOre:return new InventoryStack(ItemType.Coal,1);
            case BlockType.DiamondOre:return new InventoryStack(ItemType.Diamond,1);
            case BlockType.LapisOre:return new InventoryStack(ItemType.LapisLazuli,4+quantityRoll%5);
            case BlockType.RedstoneOre:case BlockType.LitRedstoneOre:return new InventoryStack(ItemType.RedstoneDust,4+quantityRoll%2);
            case BlockType.GlowStone:return new InventoryStack(ItemType.GlowstoneDust,2+quantityRoll%3);
            case BlockType.Cobweb:return new InventoryStack(ItemType.String,1);
            case BlockType.SnowLayer:return new InventoryStack(ItemType.Snowball,1);
            case BlockType.SnowBlock:return new InventoryStack(ItemType.Snowball,4);
            case BlockType.Clay:return new InventoryStack(ItemType.ClayBall,4);
            case BlockType.IronDoor:return new InventoryStack(ItemType.IronDoor,1);
            case BlockType.LitFurnace:return new InventoryStack(ItemType.Furnace,1);
            case BlockType.DoubleStoneSlab:return new InventoryStack(ItemType.StoneSlab,2);
            case BlockType.DoubleSandstoneSlab:return new InventoryStack(ItemType.SandstoneSlab,2);
            case BlockType.DoubleWoodenSlab:return new InventoryStack(ItemType.WoodenSlab,2);
            case BlockType.DoubleCobblestoneSlab:return new InventoryStack(ItemType.CobblestoneSlab,2);
            case BlockType.CobblestoneStairs:return new InventoryStack(ItemType.Cobblestone,1);
            case BlockType.MonsterSpawner:case BlockType.Glass:case BlockType.Ice:return new InventoryStack();
        }
        // Every remaining tool-required rock/iron block drops itself.
        if(!HandMining.Harvestable(block))return new InventoryStack(ItemCatalog.FromBlock(block),1);
        var drop=HandMining.ExistingDrop(block,gravelRoll);
        return drop==BlockType.Nothing?new InventoryStack():new InventoryStack(ItemCatalog.FromBlock(drop),1);
    }
}

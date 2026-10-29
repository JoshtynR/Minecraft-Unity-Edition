using System;

// TMI 2011-07-01 walks vanilla item IDs, then distinct metadata variants.
// Local enum/save IDs are deliberately unrelated to this presentation order.
public static class CreativeItemOrder
{
    private static void Identity(ItemType item,out int id,out int metadata)
    {
        BetaItemCatalog.Entry entry;
        if(BetaItemCatalog.TryGet(item,out entry))
        {id=entry.SourceId;metadata=entry.Metadata;return;}
        BetaBlockCatalog.Entry block;
        if(BetaBlockCatalog.TryGet((BlockType)(int)item,out block))
        {
            id=block.SourceId;metadata=block.Metadata;
            // Our fluids unify moving/still states. TMI exposes moving 8/10
            // and excludes the duplicate still-water/still-lava items 9/11.
            if(item==ItemType.Water)id=8;
            if(item==ItemType.Lava)id=10;
            return;
        }
        // Keep project extensions after the original Beta catalog.
        id=10000+(int)item;metadata=0;
    }

    public static bool Included(ItemType item)
    {
        if(item==ItemType.Nothing||item==ItemType.Air||WheatCropRules.IsLegacy((BlockType)(int)item))return false;
        Identity(item,out int id,out int metadata);
        // Original TMIConfig.excludeIds. Shared block/item IDs (bed, door,
        // reeds, sign, repeater) resolve to their real item ID above.
        switch(id)
        {
            case 9:case 11:case 63:case 64:case 68:case 71:case 74:
            case 75:case 59:case 83:case 55:case 26:case 93:case 94:return false;
        }
        // Moisture is a world state, not a separate named/icon item variant.
        return item!=ItemType.MoistFarmland;
    }

    public static int Compare(ItemType a,ItemType b)
    {
        Identity(a,out int idA,out int metaA);Identity(b,out int idB,out int metaB);
        int order=idA.CompareTo(idB);
        if(order==0)order=metaA.CompareTo(metaB);
        return order!=0?order:((int)a).CompareTo((int)b);
    }
}

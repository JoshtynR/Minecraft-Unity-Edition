using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemDefinition
{
    public ItemType id;
    public string displayName;
    [Tooltip("Individual item image. Leave empty on block items to reuse the block mesh.")]
    public Texture2D icon;
    [Range(1,64)] public int maxStack = 64;
    [Tooltip("Zero for items without durability. Damage used belongs to each stack.")]
    public int maxDamage;
    public BlockType placedBlock = BlockType.Nothing;
}

public class ItemDataSO : ScriptableObject
{
    public List<ItemDefinition> items = new List<ItemDefinition>();
}

// Definitions are shared; inventory stacks contain only IDs, counts and damage.
public static class ItemCatalog
{
    private static Dictionary<ItemType,ItemDefinition> definitions;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() { definitions=null; }
    public static IEnumerable<ItemDefinition> All { get { EnsureLoaded(); return definitions.Values; } }
    public static void Reload(ItemDataSO data)
    {
        definitions = new Dictionary<ItemType,ItemDefinition>();
        foreach(ItemType id in Enum.GetValues(typeof(ItemType)))
        {
            if(id==ItemType.Nothing||id==ItemType.Air)continue;
            definitions.Add(id,Default(id));
        }
        if(data==null||data.items==null)return;
        var seen=new HashSet<ItemType>();
        foreach(var item in data.items)
        {
            if(item==null||!definitions.ContainsKey(item.id)||!seen.Add(item.id))continue;
            // Snapshot and validate asset values without modifying the asset.
            var entry=new ItemDefinition{id=item.id,displayName=item.displayName,icon=item.icon,
                maxStack=Math.Max(1,Math.Min(64,item.maxStack)),maxDamage=Math.Max(0,item.maxDamage),
                placedBlock=Enum.IsDefined(typeof(BlockType),item.placedBlock)?item.placedBlock:BlockType.Nothing};
            if(entry.maxDamage>0)entry.maxStack=1;
            definitions[item.id]=entry;
        }
    }
    private static void EnsureLoaded()
    {
        if(definitions==null)Reload(Resources.Load<ItemDataSO>("Item Data"));
    }
    public static ItemDefinition Get(ItemType id)
    {
        EnsureLoaded();ItemDefinition result;
        return definitions.TryGetValue(id,out result)?result:null;
    }
    public static bool Valid(ItemType id) => Get(id)!=null;
    public static int StackLimit(ItemType id) { var item=Get(id);return item==null?0:item.maxStack; }
    public static ItemType FromBlock(BlockType block)
    {
        var id=(ItemType)(int)block;
        return block!=BlockType.Nothing&&block!=BlockType.Air&&Enum.IsDefined(typeof(BlockType),block)&&
            Enum.IsDefined(typeof(ItemType),id)?id:ItemType.Nothing;
    }
    public static bool TryPlacedBlock(ItemType id,out BlockType block)
    {
        var item=Get(id);block=item==null?BlockType.Nothing:item.placedBlock;
        return block!=BlockType.Nothing&&block!=BlockType.Air;
    }
    public static ItemDefinition Default(ItemType id)
    {
        var block=(BlockType)(int)id;
        bool blockItem=Enum.IsDefined(typeof(BlockType),block)&&block!=BlockType.Nothing&&block!=BlockType.Air;
        int limit=id==ItemType.Bed||id==ItemType.Cake||id==ItemType.WoodenDoor||id==ItemType.IronDoor?1:
            id==ItemType.StandingSign||id==ItemType.WallSign?1:64;
        BetaItemCatalog.Entry beta;
        int damage=0;
        if(BetaItemCatalog.TryGet(id,out beta)){limit=beta.StackLimit;damage=beta.MaxDamage;}
        return new ItemDefinition{id=id,maxDamage=damage,displayName=System.Text.RegularExpressions.Regex.Replace(id.ToString(),"([a-z0-9])([A-Z])","$1 $2"),
            placedBlock=blockItem?block:BlockType.Nothing,maxStack=limit};
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class PlayerInventory
{
 static void CheckBetaItems()
 {
  var lines=File.ReadAllLines(Environment.GetCommandLineArgs()[1]).Skip(1);
  var types=new HashSet<ItemType>();var nativeIds=new HashSet<int>();var variants=new HashSet<string>();
  foreach(var line in lines)
  {
   var v=line.Split('\t');var type=(ItemType)Enum.Parse(typeof(ItemType),v[4]);
   int source=int.Parse(v[2]),metadata=int.Parse(v[3]),limit=int.Parse(v[5]),damage=int.Parse(v[6]);
   BetaItemCatalog.Entry reference;
   Check(types.Add(type)&&variants.Add(source+":"+metadata),"vanilla item variants each have one distinct item definition");
   Check(BetaItemCatalog.TryGet(type,out reference)&&reference.SourceId==source&&reference.Metadata==metadata&&reference.StackLimit==limit&&reference.MaxDamage==damage,"runtime source mapping matches checked Beta fixture");
   var definition=ItemCatalog.Get(type);
   Check(definition!=null&&definition.maxStack==limit&&definition.maxDamage==damage,"all Beta items have source stack limits and durability defaults");
   if((int)type>=1000){BlockType block;Check(!ItemCatalog.TryPlacedBlock(type,out block),"catalogue entries do not introduce unimplemented item actions");}
   nativeIds.Add(source);
  }
  Check(types.Count==122&&nativeIds.Count==106,"all 106 native items plus coal and dye variants covered");
  Check(Enumerable.Range(256,104).All(nativeIds.Contains)&&nativeIds.Contains(2256)&&nativeIds.Contains(2257),"native source IDs include all registered Beta items and both records");
  Check(BetaItemCatalog.Entries.Length==122&&ItemCatalog.All.Count()==251,"catalogue includes Beta counterparts plus five survival foods");
  Check(variants.Count(x=>x.StartsWith("351:"))==16&&variants.Contains("263:1"),"all sixteen dyes and charcoal are separate assignable images");
  Check(ItemCatalog.Get(ItemType.Bow).maxDamage==0&&ItemCatalog.Get(ItemType.Cookie).maxStack==8&&ItemCatalog.Get(ItemType.Map).maxStack==64,"Beta-specific bow, cookie and map defaults retained");
  var inv=new UnityEngine.GameObject("NewItemsSave").AddComponent<PlayerInventory>();
  Check(inv.AddItem(ItemType.DiamondPickaxe,1,damage:100)&&inv.AddItem(ItemType.BoneMeal,64)&&inv.AddItem(ItemType.MusicDiscCat),"new item IDs work through actual inventory grants");
  var saved=inv.SavedInventory();inv.LoadInventory(saved);
  Check(inv.slots[0].type==ItemType.DiamondPickaxe&&inv.slots[0].damage==100&&inv.slots[1].type==ItemType.BoneMeal&&inv.slots[2].type==ItemType.MusicDiscCat,"new item IDs and durability survive save snapshots and load");
 }
}

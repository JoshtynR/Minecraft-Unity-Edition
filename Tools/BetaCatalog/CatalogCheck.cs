using System;
public static class CatalogCheck
{
 static int checks;static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 public static void Main()
 {
  var original=new[]{BlockType.Nothing,BlockType.Air,BlockType.Dirt,BlockType.Grass,BlockType.Stone,BlockType.Log,BlockType.Leaves,BlockType.Sand,BlockType.Water,BlockType.Glass,BlockType.GlowStone,BlockType.WoodenSlab,BlockType.Bedrock,BlockType.Gravel,BlockType.Sandstone,BlockType.Lava,BlockType.Ice,BlockType.SpruceLog,BlockType.SpruceLeaves,BlockType.BirchLog,BlockType.BirchLeaves};
  for(int i=0;i<original.Length;i++)Check((int)original[i]==i,"original saved ID unchanged");
  Check(Enum.GetValues(typeof(BlockType)).Length==133&&BetaBlockCatalog.Entries.Length==133,"complete catalog size");
  foreach(BlockType type in Enum.GetValues(typeof(BlockType)))
  {
   BetaBlockCatalog.Entry entry;Check(BetaBlockCatalog.TryGet(type,out entry)&&entry.Type==type,"enum has matching descriptor");
   Check(HandMining.Hardness(type)==entry.Hardness,"all catalog blocks have hand mining stats");
   Check(!float.IsInfinity(HandMining.DamagePerTick(type,true,false)),"zero-hardness blocks have finite progress");
  }
  for(int id=0;id<=96;id++){BlockType type;Check(BetaBlockCatalog.TryGetVanilla(id,0,out type),"every registered Beta block ID represented");}
  BlockType resolved;Check(BetaBlockCatalog.TryGetVanilla(8,0,out resolved)&&resolved==BlockType.Water,"flowing water shares working water engine");Check(BetaBlockCatalog.TryGetVanilla(10,0,out resolved)&&resolved==BlockType.Lava,"lava source alias");
  Check(BetaBlockCatalog.TryGetVanilla(17,6,out resolved)&&resolved==BlockType.BirchLog,"tree species masks state metadata");
  Check(BetaBlockCatalog.TryGetVanilla(44,2,out resolved)&&resolved==BlockType.WoodenSlab,"legacy wooden slab mapping");
  Check(BetaBlockCatalog.TryGetVanilla(35,15,out resolved)&&resolved==BlockType.BlackWool,"all wool colors represented");
  Check(BetaBlockCatalog.TryGetVanilla(59,7,out resolved)&&resolved==BlockType.WheatCrop7,"crop stages represented");
  Check(BetaBlockCatalog.TryGetVanilla(60,3,out resolved)&&resolved==BlockType.MoistFarmland,"moist farmland texture state");
  Check(!BetaBlockCatalog.TryGetVanilla(97,0,out resolved)&&!BetaBlockCatalog.TryGet((BlockType)999,out var unknown),"unknown later blocks rejected");
  Check(HandMining.ExistingDrop(BlockType.MoistFarmland,1)==BlockType.Dirt,"farmland drops dirt");
  Check(HandMining.ExistingDrop(BlockType.SpruceSapling,1)==BlockType.SpruceSapling,"sapling drop retains species");
  Check(BetaBlockCatalog.Entries[(int)BlockType.StoneSlab].HalfSlab&&BetaBlockCatalog.Entries[(int)BlockType.SandstoneSlab].HalfSlab,"single slab shape descriptors");
  Console.WriteLine(checks+" production catalog/legacy ID/vanilla variant/mining checks passed.");
 }
}

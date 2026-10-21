using System;
using System.Linq;
using UnityEngine;
using Mirror;

// Storage/raycast doubles isolate the production farming and inventory commands.
public partial class World
{
 public bool TryIgnitePortal(Vector3Int p)=>false;
 private JavaRandom fluidRandom=new JavaRandom(173);
 private static Vector3Int BlockPos(WaterPosition p)=>new Vector3Int(p.x,p.y,p.z);
 private bool TryGetWaterBlock(Vector3Int p,out Block block){block=GetBlock(p);return block.type!=BlockType.Nothing;}
 public static bool FluidHasSolidMaterial(BlockType type)=>type==BlockType.Stone||type==BlockType.Dirt||type==BlockType.Grass||WheatCropRules.IsSoil(type);
 public void FarmTick(Vector3Int p){TickFarming(new WaterPosition(p.x,p.y,p.z),GetBlock(p));}
 public void FarmChanged(Vector3Int p){ValidateFarmNeighbors(p);}
}
public partial class Player
{
 private bool InMiningReach(Vector3Int p)=>(transform.position-(Vector3)p).magnitude<reach;
 private bool MiningLineOfSight(Vector3Int p,Vector3 center)=>true;
 public void FarmAction(Block block,Vector3Int adjacent){TryUseFarming(block,adjacent);}
 public void ReplayFarm(int request,Vector3Int target,Vector3Int adjacent,InventoryStack[] snapshot,int revision)
 {ProcessFarmItem(request,target,adjacent,0,snapshot,false,revision);}
}
public partial class PlayerInventory
{
 static void CheckFarming()
 {
  Func<int,int,BlockType> empty=(x,z)=>BlockType.Air;
  Check(WheatCropRules.GrowthBound(empty,(x,z)=>x==0&&z==0?0:-1)==50,"isolated dry crop growth");
  Check(WheatCropRules.GrowthBound(empty,(x,z)=>x==0&&z==0?7:-1)==25,"isolated wet crop growth");
  Check(WheatCropRules.GrowthBound(empty,(x,z)=>0)==25&&WheatCropRules.GrowthBound(empty,(x,z)=>7)==10,"surrounding farmland boosts growth");
  Check(WheatCropRules.GrowthBound((x,z)=>WheatCropRules.Crop,(x,z)=>7)==20,"crowded crops halve growth rate");
  for(int age=0;age<8;age++)
  {
   int rolls=0;Check(WheatCropRules.SeedDrops(age,n=>{Check(n==15,"seed roll bound");rolls++;return age;})==3&&rolls==3,"inclusive age threshold on three seed attempts");
   Check(WheatCropRules.SeedDrops(age,n=>age+1)==0,"seed rolls above age fail");
   Check(ToolMining.Drop(WheatCropRules.LegacyTextureType(age),ItemType.Nothing,0,0,0).Empty,"crop item drops use state-aware harvest");
  }
  NetworkClient.active=false;NetworkServer.active=false;WorldServer.instance=null;
  var w=World.Instance=new World();var p=new Vector3Int(0,0,2);var up=p+Vector3Int.up;
  Check(w.MiningItemDrop(BlockType.TallGrass,ItemType.Shears).type==ItemType.TallGrass&&w.MiningItemDrop(BlockType.Fern,ItemType.Shears).type==ItemType.Fern,"sheared grass preserves plant instead of rolling seeds");
  w.SetBlock(p,BlockType.Grass,0);var held=new InventoryStack(ItemType.WoodenHoe,1);
  Check(!w.UseFarmItem(p,p+Vector3Int.down,ref held)&&held.damage==0,"cannot hoe bottom face");
  Check(w.UseFarmItem(p,up,ref held)&&held.damage==1&&w.GetBlock(p).type==BlockType.Farmland,"hoeing creates farmland and wears tool once");
  held=new InventoryStack(ItemType.Seeds,3);
  Check(!w.UseFarmItem(p,p+new Vector3Int(1,0,0),ref held)&&held.count==3,"seeds require top face");
  Check(w.UseFarmItem(p,up,ref held)&&held.count==2&&w.GetBlock(up).type==WheatCropRules.Crop&&SpecialBlockGeometry.State(w.GetBlock(up).type,w.GetBlock(up).blockShape)==0,"seeds plant stage zero");
  Check(!w.UseFarmItem(p,up,ref held)&&held.count==2,"occupied planting cannot consume seeds");
  held=new InventoryStack(ItemType.BoneMeal,2);
  Check(w.UseFarmItem(up,up+Vector3Int.up,ref held)&&held.count==1&&SpecialBlockGeometry.State(w.GetBlock(up).type,w.GetBlock(up).blockShape)==7,"Beta bone meal fully matures wheat");
  Check(!w.UseFarmItem(up,up+Vector3Int.up,ref held)&&held.count==1,"mature wheat does not consume bone meal");
  w.SetBlock(up,BlockType.Air,0);Check(w.droppedItems.Items.Where(d=>d.stack.type==ItemType.Wheat).Sum(d=>d.stack.count)==1,"mature wheat yields one wheat");
  w.ClearDrops();w.SetBlock(up,WheatCropRules.Crop,3);w.SetBlock(up,BlockType.Air,0);
  Check(!w.droppedItems.Items.Any(d=>d.stack.type==ItemType.Wheat),"immature wheat never drops wheat");
  w.blocks.Clear();w.SetBlock(p,BlockType.Farmland,0);w.SetBlock(p+new Vector3Int(4,1,4),BlockType.Water,0);
  for(int i=0;i<100;i++)w.FarmTick(p);
  Check(w.GetBlock(p).type==BlockType.MoistFarmland&&SpecialBlockGeometry.State(w.GetBlock(p).type,w.GetBlock(p).blockShape)==7,"water hydrates diagonally four blocks away at soil height plus one");
  w.SetBlock(p+new Vector3Int(4,1,4),BlockType.Air,0);
  for(int i=0;i<200;i++)w.FarmTick(p);
  Check(w.GetBlock(p).type==BlockType.Dirt,"unplanted farmland dries through all moisture states into dirt");
  w.SetBlock(p,BlockType.Farmland,0);w.SetBlock(up,WheatCropRules.Crop,0);
  for(int i=0;i<200;i++)w.FarmTick(p);
  Check(w.GetBlock(p).type==BlockType.Farmland,"crop preserves dry farmland");
  w.GetBlock(up).sky=0;w.blocks[up+Vector3Int.up]=new Block(BlockType.Air,up+Vector3Int.up,null){sky=0};
  w.FarmTick(up);Check(w.GetBlock(up).type==BlockType.Air,"unsupported light level breaks crops");
  w.SetBlock(up,WheatCropRules.Crop,0);w.GetBlock(up+Vector3Int.up).sky=8;
  for(int i=0;i<200;i++)w.FarmTick(up);
  Check(SpecialBlockGeometry.State(w.GetBlock(up).type,w.GetBlock(up).blockShape)==0,"growth requires light nine above crop");
  w.GetBlock(up+Vector3Int.up).sky=15;
  for(int i=0;i<2000;i++)w.FarmTick(up);
  Check(SpecialBlockGeometry.State(w.GetBlock(up).type,w.GetBlock(up).blockShape)==7,"random ticks grow all eight states on the same block type");
  w.ClearDrops();w.SetBlock(p,BlockType.Dirt,0);w.FarmChanged(p);
  Check(w.GetBlock(up).type==BlockType.Air&&w.droppedItems.Items.Count(d=>d.stack.type==ItemType.Wheat)==1,"removing farmland breaks and harvests mature crop once");
  w.FarmChanged(p);Check(w.droppedItems.Items.Count(d=>d.stack.type==ItemType.Wheat)==1,"repeated neighbor updates cannot duplicate harvest");
  w.blocks.Clear();w.SetBlock(p,BlockType.Farmland,0);w.SetBlock(up,BlockType.Stone,0);w.FarmChanged(up);
  Check(w.GetBlock(p).type==BlockType.Dirt,"solid cover destroys farmland");
  w.SetBlock(up,BlockType.Air,0);w.SetBlock(p,BlockType.Farmland,0);
  var menu=BuildMenu();menu.AddItem(ItemType.Seeds,2);menu.player.FarmAction(w.GetBlock(p),up);
  Check(menu.slots[0].count==1&&!menu.DropRequestPending&&w.GetBlock(up).type==WheatCropRules.Crop,"plant action atomically updates inventory and world");
  w.SetBlock(up,BlockType.Air,0);menu.player.ReplayFarm(1,p,up,menu.DropInventorySnapshot(),0);
  Check(w.GetBlock(up).type==BlockType.Air&&menu.slots[0].count==1,"replayed planting request does not plant twice");
  menu.player.transform.position=new Vector3(100,0,0);menu.player.FarmAction(w.GetBlock(p),up);
  Check(menu.slots[0].count==1&&w.GetBlock(up).type==BlockType.Air,"distant planting is rejected");
  menu.OnDestroy();World.Instance=new World();
 }
}

using System;
using System.Linq;
using System.IO;
using UnityEngine;
using Mirror;

public partial class World
{
 public bool furnaceChunksLoaded=true;
 public void StepFurnaces(float dt){Time.deltaTime=dt;UpdateFurnaces();}
 public void BreakFurnaceForTest(Vector3Int position){RemoveFurnace(position);}
 public void SetBlock(Vector3Int position,BlockType type,byte facing)
 {var old=GetBlock(position);bool harvest=WheatCropRules.IsCrop(old.type)&&!WheatCropRules.IsCrop(type);int age=SpecialBlockGeometry.State(old.type,old.blockShape);blocks[position]=new Block(type,position,null){blockShape=facing};if(harvest)DropWheat(position,age);}
}
public static class WorldDataHelper
{
 public static Vector3Int GetChunkPosition(World world,Vector3Int p)=>new Vector3Int((int)Math.Floor(p.x/16.0)*16,0,(int)Math.Floor(p.z/16.0)*16);
 public static ChunkRenderer GetChunk(World world,Vector3Int p)=>world.furnaceChunksLoaded?new ChunkRenderer():null;
}
public partial class WorldServer
{
 public int furnaceBroadcasts;public byte lastFurnaceFacing;
 public void RpcSetBlock(Vector3Int position,BlockType type,byte facing){furnaceBroadcasts++;lastFurnaceFacing=facing;}
}
public partial class PlayerInventory
{
 static void CheckFurnaces()
 {
  foreach(var pair in new[]{new[]{ItemType.IronOre,ItemType.IronIngot},new[]{ItemType.GoldOre,ItemType.GoldIngot},new[]{ItemType.DiamondOre,ItemType.Diamond},new[]{ItemType.Sand,ItemType.Glass},new[]{ItemType.RawPorkchop,ItemType.CookedPorkchop},new[]{ItemType.RawFish,ItemType.CookedFish},new[]{ItemType.Cobblestone,ItemType.Stone},new[]{ItemType.ClayBall,ItemType.Brick},new[]{ItemType.Cactus,ItemType.CactusGreen},new[]{ItemType.Log,ItemType.Charcoal},new[]{ItemType.SpruceLog,ItemType.Charcoal},new[]{ItemType.BirchLog,ItemType.Charcoal}})
  {
   var state=new BetaSmelting.State();state.items[0]=new InventoryStack(pair[0],1);state.items[1]=new InventoryStack(ItemType.Coal,1);
   for(int i=0;i<199;i++)BetaSmelting.Tick(state);
   Check(state.items[2].Empty&&state.cookTicks==199,"no early smelting output");BetaSmelting.Tick(state);
   Check(state.items[2].type==pair[1]&&state.items[2].count==1&&state.items[0].Empty&&state.items[1].Empty,"every Beta recipe finishes after 200 ticks");
  }
  Check(BetaSmelting.FuelTicks(ItemType.Coal)==1600&&BetaSmelting.FuelTicks(ItemType.Charcoal)==1600&&BetaSmelting.FuelTicks(ItemType.LavaBucket)==20000,"Beta coal charcoal lava fuel durations");
  Check(BetaSmelting.FuelTicks(ItemType.Stick)==100&&BetaSmelting.FuelTicks(ItemType.OakSapling)==100&&BetaSmelting.FuelTicks(ItemType.WoodenPlanks)==300,"small and wood fuels");
  Check(BetaSmelting.FuelTicks(ItemType.WoodenPickaxe)==0&&BetaSmelting.FuelTicks(ItemType.WoodenSlab)==0&&BetaSmelting.Result(ItemType.CoalOre)==ItemType.Nothing,"later-version fuels and recipes not added to Beta");
  var coal=new BetaSmelting.State();coal.items[0]=new InventoryStack(ItemType.Sand,16);coal.items[1]=new InventoryStack(ItemType.Coal,1);
  for(int i=0;i<1601;i++)BetaSmelting.Tick(coal);
  Check(coal.items[2].count==8&&coal.items[0].count==8&&!coal.Burning&&coal.cookTicks==0,"one coal smelts exactly eight items");
  var blocked=new BetaSmelting.State();blocked.items[0]=new InventoryStack(ItemType.Sand,2);blocked.items[1]=new InventoryStack(ItemType.Coal,2);blocked.items[2]=new InventoryStack(ItemType.Glass,64);
  BetaSmelting.Tick(blocked);Check(!blocked.Burning&&blocked.items[1].count==2,"full output never starts burning new fuel");
  blocked.items[2]=new InventoryStack(ItemType.Diamond,1);BetaSmelting.Tick(blocked);Check(!blocked.Burning,"incompatible output blocks smelting");
  blocked.items[2]=new InventoryStack();BetaSmelting.Tick(blocked);blocked.items[0]=new InventoryStack();BetaSmelting.Tick(blocked);
  Check(blocked.Burning&&blocked.cookTicks==0&&blocked.items[1].count==1,"fuel keeps burning while cooking resets without input");
  var lava=new BetaSmelting.State();lava.items[0]=new InventoryStack(ItemType.Sand,1);lava.items[1]=new InventoryStack(ItemType.LavaBucket,1);BetaSmelting.Tick(lava);
  Check(lava.burnTicks==20000&&lava.items[1].Empty,"Beta lava fuel consumes its bucket");
  var inventory=new InventoryStack[36];var cursor=new InventoryStack(ItemType.Sand,4);var furnace=new BetaSmelting.State();furnace.items[2]=new InventoryStack(ItemType.Glass,3);
  BetaSmelting.Click(furnace,inventory,ref cursor,2,false,false);Check(furnace.items[2].count==3&&cursor.count==4,"output rejects insertion and swaps");
  cursor=new InventoryStack();BetaSmelting.Click(furnace,inventory,ref cursor,2,true,false);Check(cursor.count==2&&furnace.items[2].count==1,"right click takes half output");
  inventory[0]=new InventoryStack(ItemType.IronOre,8);inventory[1]=new InventoryStack(ItemType.Coal,4);
  BetaSmelting.Click(furnace,inventory,ref cursor,3,false,true);BetaSmelting.Click(furnace,inventory,ref cursor,4,false,true);
  Check(furnace.items[0].count==8&&furnace.items[1].count==4,"shift click routes recipes and fuel to their slots");
  var random=new System.Random(173);
  for(int trial=0;trial<30;trial++)
  {
   var s=new BetaSmelting.State();var bag=new InventoryStack[36];var held=new InventoryStack();
   for(int i=0;i<36;i++)bag[i]=new InventoryStack(i%2==0?ItemType.Sand:ItemType.Coal,random.Next(1,65));
   int total=bag.Sum(x=>x.count);
   for(int i=0;i<200;i++)
   {
    Check(BetaSmelting.Click(s,bag,ref held,random.Next(39),random.Next(2)==0,random.Next(3)==0),"valid furnace click");
    Check(s.items.Sum(x=>x.count)+bag.Sum(x=>x.count)+held.count==total,"random furnace transfers conserve items");
   }
  }
  string name="furnace-check-"+Guid.NewGuid().ToString("N"),directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),".minecraftUnity/saves",name);
  var store=new FurnaceStorage();var p=new Vector3Int(3,64,5);
  try
  {
   var bag=new InventoryStack[36];bag[0]=new InventoryStack(ItemType.IronOre,5);bag[1]=new InventoryStack(ItemType.Coal,1);
   var opened=store.Process(name,4,new FurnaceRequest{action=0,position=p,inventory=bag},true);
   Check(opened.accepted&&opened.opened,"furnace opens with authoritative session");
   Check(!store.Process(name,5,new FurnaceRequest{action=0,position=p,inventory=bag},true).accepted,"exclusive furnace session prevents duplicate transfers");
   Check(!store.CanRemove(name,p),"open furnace protected from removal");
   store.Process(name,4,new FurnaceRequest{action=1,position=p,slot=3,shift=true,sequence=1},true);
   store.Process(name,4,new FurnaceRequest{action=1,position=p,slot=4,shift=true,sequence=2},true);
   Check(!store.Process(name,4,new FurnaceRequest{action=1,position=p,slot=4,shift=true,sequence=2},true).accepted,"replayed clicks cannot consume inventory twice");
   var entry=store.Entries.Single();for(int i=0;i<75;i++)BetaSmelting.Tick(entry.state);
   store.SaveWorld(name);var reloaded=new FurnaceStorage();reloaded.Load(name);var copy=reloaded.Entries.Single();
   Check(copy.state.cookTicks==75&&copy.state.burnTicks==1526&&copy.state.items[0].count==5,"slots and partially used fuel/cooking survive save reload");
   var closed=store.Close(4);Check(closed.accepted&&!closed.opened&&store.CanRemove(name,p),"close releases furnace lock");
   Check(store.Remove(p).Sum(x=>x.count)==5&&!store.Entries.Any(),"broken furnace yields contents exactly once");
   Check(store.Remove(p).Length==0,"repeated removal cannot duplicate contents");
  }
  finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
  World.Instance=new World();NetworkClient.active=false;NetworkServer.active=false;Time.unscaledTime=0;
  var menu=BuildMenu();var position=new Vector3Int(0,0,2);
  World.Instance.blocks[position]=new Block(BlockType.Furnace,position,null){blockShape=(byte)4};
  var assets=new PlayerHudAssets{furnace=new Texture2D{width=256,height=256}};Resources.hud=assets;
  menu.AddItem(ItemType.IronOre,2);menu.AddItem(ItemType.Coal,1);menu.RequestFurnace(position);
  Check(menu.furnaceOpen&&menu.isOpen&&menu.furnacePanel.GetComponent<UnityEngine.UI.RawImage>().texture==assets.furnace,"right click opens texture-backed furnace UI");
  Check(menu.inventorySlotsRect.parent==menu.furnacePanel&&menu.furnaceRects[2].GetComponent<InventorySlot>().slotIndex==302,"player inventory and three furnace slots are clickable");
  Check(menu.furnaceRects[0].anchoredPosition.x==-24&&menu.furnaceRects[0].anchoredPosition.y==58&&menu.furnaceRects[2].anchoredPosition.x==36,"slots use native sheet coordinates");
  menu.ClickInventorySlot(0,false,true);menu.ClickInventorySlot(1,false,true);
  Check(menu.furnaceSlots[0].count==2&&menu.furnaceSlots[1].count==1&&menu.slots[0].count==0,"UI shift transfers pass through furnace authority");
  World.Instance.StepFurnaces(.05f);menu.UpdateFurnacePanel();
  Check(World.Instance.GetBlock(position).type==BlockType.LitFurnace,"burning furnace switches block type");
  World.Instance.isSaving=true;int paused=World.Instance.furnaces.Entries.Single().state.burnTicks;World.Instance.StepFurnaces(1);World.Instance.isSaving=false;
  Check(World.Instance.furnaces.Entries.Single().state.burnTicks==paused,"world snapshot pauses furnace ticking");
  World.Instance.furnaceChunksLoaded=false;World.Instance.StepFurnaces(1);World.Instance.furnaceChunksLoaded=true;
  Check(World.Instance.furnaces.Entries.Single().state.burnTicks==paused,"streamed-out furnace pauses without repeated lit-state broadcasts");
  menu.CloseInventory();Check(!menu.furnaceOpen&&!menu.isOpen&&menu.inventorySlotsRect.parent==menu.inventoryRect,"closing restores survival inventory");
  World.Instance.StepFurnaces(9.95f);menu.RequestFurnace(position);
  Check(menu.furnaceSlots[2].type==ItemType.IronIngot&&menu.furnaceSlots[2].count==1,"closed furnace continues cooking and output reopens");
  menu.ClickInventorySlot(302,false,true);Check(Total(menu,ItemType.IronIngot)==1&&menu.furnaceSlots[2].count==0,"output shift-pickup adds exactly one ingot");
  Time.unscaledTime+=.3f;menu.UpdateFurnacePanel();
  var arrow=menu.furnaceArrow.GetComponent<UnityEngine.UI.RawImage>();
  Check(arrow.uvRect.y==1-31/256f&&menu.furnaceFlame.GetComponent<UnityEngine.UI.RawImage>().uvRect.y==1-14/256f,"progress sprites sample the native arrow and flame regions");
  menu.transform.position=new Vector3(30,0,30);Time.unscaledTime+=1;menu.UpdateFurnacePanel();Check(!menu.furnaceOpen,"walking away closes offline furnace session");
  menu.transform.position=Vector3.zero;NetworkClient.active=true;menu.RequestFurnace(position);
  Check(menu.furnacePending,"network open waits for authoritative reply");menu.CloseInventory();
  menu.ReceiveFurnaceReply(new FurnaceReply{accepted=true,opened=true,position=position,items=new InventoryStack[3],inventory=new InventoryStack[36]});
  Check(NetworkClient.lastFurnace.action==2&&menu.furnacePending,"close requested during network open is sent after acceptance");
  menu.ReceiveFurnaceReply(new FurnaceReply{accepted=true,position=position,items=new InventoryStack[3],inventory=new InventoryStack[36]});
  Check(!menu.isOpen&&!menu.furnaceOpen,"authoritative close resolves pending menu");NetworkClient.active=false;
  var server=new WorldServer();WorldServer.instance=server;NetworkServer.active=true;
  var connection=new NetworkConnectionToClient{connectionId=71,identity=menu.gameObject};NetworkServer.connections[71]=connection;
  server.ReceiveFurnaceRequest(connection,new FurnaceRequest{action=0,position=position,inventory=new InventoryStack[36],itemWearRevision=menu.player.ItemWearRevision});
  Check(connection.lastFurnace.accepted&&connection.lastFurnace.opened,"server authorizes furnace open");
  server.ReceiveChestRequest(connection,new ChestRequest{action=0,position=position,inventory=new InventoryStack[36]});Check(!connection.last.accepted,"one player cannot open chest and furnace sessions simultaneously");
  menu.player.health=0;Time.unscaledTime+=1;server.PollChests();Check(!connection.lastFurnace.opened&&!World.Instance.furnaces.HasOwner(71),"death closes authoritative furnace session");
  menu.player.health=20;NetworkServer.connections.Clear();NetworkServer.active=false;WorldServer.instance=null;
  var loaded=World.Instance.furnaces.Entries.Single().state;loaded.items[0]=new InventoryStack(ItemType.GoldOre,3);loaded.items[1]=new InventoryStack(ItemType.Coal,2);
  int dropCount=World.Instance.droppedItems.Items.Count();World.Instance.BreakFurnaceForTest(position);
  Check(World.Instance.droppedItems.Items.Count()==dropCount+2,"breaking a furnace emits remaining contents through item drops");
  Resources.hud=null;
 }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

// Unity storage and the existing fluid transport/scheduler are test doubles;
// production bucket logic and the production voxel raycaster are compiled intact.
namespace UnityEngine
{
 public class MonoBehaviour {public Transform transform=new Transform();}
 public class Transform{public Vector3 position,forward=new Vector3(0,0,1);}
 public static class Mathf
 {public static int FloorToInt(float v)=>(int)Math.Floor(v);public static float Clamp(float v,float min,float max)=>Math.Max(min,Math.Min(max,v));}
}
namespace Mirror
{
 public class CommandAttribute:Attribute{}
 public static class NetworkClient{public static bool ready;}
}
public static class WorldLoadingScreen{public static bool IsLoading;}
public enum WorldDimension{Overworld,Nether}
public struct InventoryStack
{
 public ItemType type;public int count,damage;
 public InventoryStack(ItemType id,int amount){type=id;count=amount;damage=0;}
}
public class BlockShape {public float xMin,yMin,zMin,xMax=16,yMax=16,zMax=16;}
public class Shape{public BlockShape GetShape()=>new BlockShape();}
public class Block
{
 public BlockType type;public byte waterLevel;public Vector3Int globalWorldPosition;public Shape blockShape=new Shape();
}
public static class HandMining{public static bool IsTarget(BlockType t)=>t!=BlockType.Air&&t!=BlockType.Nothing&&t!=BlockType.Water&&t!=BlockType.Lava;}
public static class ChestTransactions
{
 public static bool Valid(InventoryStack[] items,InventoryStack cursor)
 {if(items==null||items.Length!=36)return false;foreach(var item in items)if(item.count<0||item.count>64)return false;return true;}
}
public partial class World
{
 public bool DropAuthority=true,IsWorldCreated=true,disabled,isSaving;public WorldDimension dimension;
 public Dictionary<Vector3Int,Block> cells=new Dictionary<Vector3Int,Block>();
 public Dictionary<Vector3Int,byte> waterChanges=new Dictionary<Vector3Int,byte>();public int notifications,edits,effects;
 public Block GetBlock(Vector3Int p)=>cells.TryGetValue(p,out var block)?block:new Block{type=BlockType.Air,globalWorldPosition=p};
 public void Set(Vector3Int p,BlockType t,int level=0){cells[p]=new Block{type=t,waterLevel=(byte)level,globalWorldPosition=p};}
 private bool TryGetWaterBlock(Vector3Int p,out Block block){block=GetBlock(p);return p.y>=0&&p.y<128&&Math.Abs(p.x)<32&&Math.Abs(p.z)<32&&block.type!=BlockType.Nothing;}
 private static bool FluidCanReplace(BlockType t)=>t==BlockType.Air||t==BlockType.SnowLayer||t==BlockType.Rose;
 private bool ApplyFluidState(Vector3Int p,BlockType t,int level)
 {var old=GetBlock(p);if(old.type==t&&old.waterLevel==level)return false;Set(p,t,level);edits++;return true;}
 public void NotifyWaterBlockChanged(Vector3Int p){notifications++;}
 private void EmitFluidEffect(Vector3Int p,int kind){effects++;}
}
public class PlayerInventory
{
 public bool DropRequestPending;public int selectedSlotIndex;public InventoryStack[] slots=new InventoryStack[36];
 public InventoryStack HeldItem()=>slots[selectedSlotIndex];public InventoryStack[] DropInventorySnapshot()=>(InventoryStack[])slots.Clone();
 public void ApplyDropInventory(InventoryStack[] items){slots=(InventoryStack[])items.Clone();}
}
public partial class Player:MonoBehaviour
{
 public PlayerInventory inventory=new PlayerInventory();public World CurrentWorld=new World();public bool networkStarted,isServer=true;
 public Transform cam=new Transform();public float eyeHeight=.5f,reach=4.5f;public int health=20;
 private bool dimensionTransfer,awaitingNetworkDrop,serverMining;
 private int dropRequestId,knownItemWearRevision,lastServerDropRequest;private InventoryStack[] lastServerDropReply;private bool lastServerDropAccepted;
 public bool accepted;public bool AcceptsItemRevision(int revision)=>revision==knownItemWearRevision;
 private void CompleteDropAction(int request,InventoryStack[] snapshot,bool ok,bool network){accepted=ok;inventory.DropRequestPending=false;awaitingNetworkDrop=false;}
 public void Use(){TryUseBucket();}
 public void Replay(int request,Vector3 aim,int slot,InventoryStack[] snapshot,int revision=0){ProcessBucketUse(request,aim,slot,snapshot,false,revision);}
}
public static class Check
{
 static int count;static void Expect(bool value,string message){count++;if(!value)throw new Exception(message);}
 static Vector3Int P(int z,int y=0)=>new Vector3Int(0,y,z);
 public static void Main()
 {
  foreach(var fluid in new[]{BlockType.Water,BlockType.Lava})
  {
   var filled=fluid==BlockType.Water?ItemType.WaterBucket:ItemType.LavaBucket;
   for(int level=0;level<16;level++)
   {
    var world=new World();world.Set(P(1),fluid,level);var bucket=new InventoryStack(ItemType.Bucket,1);
    bool ok=world.UseBucket(P(1),ref bucket);
    Expect(ok==(level==0),"only source metadata zero is collectible");
    Expect(bucket.type==(level==0?filled:ItemType.Bucket)&&bucket.count==1,"bucket exchange matches successful collection only");
    Expect(world.GetBlock(P(1)).type==(level==0?BlockType.Air:fluid),"flowing fluid is not removed");
    if(ok)Expect(world.waterChanges[P(1)]==255&&world.notifications==1,"collection sends air update and wakes flow");
   }
   var placedWorld=new World();var full=new InventoryStack(filled,1);
   Expect(placedWorld.UseBucket(P(1),ref full)&&full.type==ItemType.Bucket&&placedWorld.GetBlock(P(1)).type==fluid&&placedWorld.GetBlock(P(1)).waterLevel==0,"filled bucket places a source and returns empty bucket");
   Expect(placedWorld.waterChanges[P(1)]==(fluid==BlockType.Water?0:32)&&placedWorld.notifications==1,"placement uses correct fluid wire code and schedules flow");
   foreach(var invalid in new[]{BlockType.Stone,BlockType.Chest,BlockType.Furnace,BlockType.Nothing})
   {placedWorld.Set(P(2),invalid);full=new InventoryStack(filled,1);Expect(!placedWorld.UseBucket(P(2),ref full)&&full.type==filled&&placedWorld.GetBlock(P(2)).type==invalid,"bucket cannot overwrite solids, containers or unloaded blocks");}
  }
  var nether=new World{dimension=WorldDimension.Nether};var water=new InventoryStack(ItemType.WaterBucket,1);
  Expect(nether.UseBucket(P(1),ref water)&&water.type==ItemType.Bucket&&nether.GetBlock(P(1)).type==BlockType.Air&&nether.effects==1,"Nether water evaporates and returns bucket");
  var clientWorld=new World{DropAuthority=false};var lava=new InventoryStack(ItemType.LavaBucket,1);
  Expect(!clientWorld.UseBucket(P(1),ref lava)&&lava.type==ItemType.LavaBucket,"remote world cannot mutate fluids locally");
  var player=new Player{networkStarted=true,isServer=true};NetworkClient.ready=false;
  player.CurrentWorld.Set(P(2),BlockType.Water);player.inventory.slots[0]=new InventoryStack(ItemType.Bucket,1);player.Use();
  Expect(player.accepted&&player.inventory.slots[0].type==ItemType.WaterBucket&&!player.inventory.DropRequestPending,"solo host collects without network readiness or inventory lock");
  int edits=player.CurrentWorld.edits;
  var old=new InventoryStack[36];old[0]=new InventoryStack(ItemType.Bucket,1);player.Replay(1,new Vector3(0,0,1),0,old);
  Expect(player.CurrentWorld.edits==edits&&player.inventory.slots[0].type==ItemType.WaterBucket,"duplicate request cannot repeat world edit or bucket exchange");
  player.CurrentWorld.Set(P(3),BlockType.Stone);player.Use();
  Expect(player.accepted&&player.CurrentWorld.GetBlock(P(2)).type==BlockType.Water&&player.inventory.slots[0].type==ItemType.Bucket,"filled bucket ray ignores liquids and places against solid face");
  player.CurrentWorld.Set(P(1),BlockType.Stone);player.Use();Expect(!player.accepted&&player.inventory.slots[0].type==ItemType.Bucket,"solid occluder blocks collection behind it");
  var sourceWorld=new World();sourceWorld.Set(P(1),BlockType.Water,3);sourceWorld.Set(P(2),BlockType.Lava,0);
  var hit=BlockTargeting.Raycast(sourceWorld,new Vector3(.5f,.5f,0),new Vector3(0,0,1),4,out var adjacent,out var point,true);
  Expect(hit!=null&&hit.globalWorldPosition==P(2),"empty bucket ray ignores flowing fluid and targets source");
  hit=BlockTargeting.Raycast(sourceWorld,new Vector3(.5f,.5f,0),new Vector3(0,0,1),4,out adjacent,out point);
  Expect(hit==null,"default mining ray remains fluid-transparent");
  var remote=new Player{networkStarted=true,isServer=false};remote.inventory.slots[0]=new InventoryStack(ItemType.Bucket,1);remote.Use();
  Expect(!remote.inventory.DropRequestPending&&remote.CurrentWorld.edits==0,"unready remote client leaves world and inventory unlocked");
  var snapshot=remote.inventory.DropInventorySnapshot();remote.Replay(1,new Vector3(float.NaN,0,0),0,snapshot);Expect(!remote.accepted,"invalid aim cannot mutate world");
  Console.WriteLine(count+" production bucket source/placement/raycast/host/replay checks passed (Unity storage/transport mocked).");
 }
}

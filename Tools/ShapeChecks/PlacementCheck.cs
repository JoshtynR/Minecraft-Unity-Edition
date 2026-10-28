using System;
using UnityEngine;
namespace UnityEngine
{
 public class Transform {public Vector3 position,forward;}
 public class ScriptableObject {}
 public enum RuntimeInitializeLoadType {SubsystemRegistration}
 public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type){}}
 public class Texture2D {}
 public class TooltipAttribute:Attribute {public TooltipAttribute(string value){}}
 public class RangeAttribute:Attribute {public RangeAttribute(int min,int max){}}
 public static class Resources {public static T Load<T>(string name)where T:class=>null;}
}
namespace Mirror {public static class NetworkClient {public static WorldServer.SetBlockMessage last;public static void Send(WorldServer.SetBlockMessage message){last=message;}}}
public class WorldServer
{
 public struct SetBlockMessage {public Vector3Int position;public BlockType blockType;public byte stairFacing;public SetBlockMessage(Vector3Int p,BlockType type,byte facing=0){position=p;blockType=type;stairFacing=facing;}}
}
public partial class World
{
 public void SetBlock(Vector3Int pos,BlockType type,byte facing=0){blocks[pos]=new Block(type){globalWorldPosition=pos,blockShape=SpecialBlockGeometry.Shape(type,facing)};}
}
public static class BlockDataManager {public static BlockData[] blockTypeDataDictionary=new BlockData[133];}
public class Inventory {public bool DropRequestPending;public Vector3Int tablePosition;public void RequestFurnace(Vector3Int position){}public void OpenCraftingTable(Vector3Int position){tablePosition=position;}public class Slot {public ItemType type;public int count=64;}public Slot[] slots={new Slot()};public int selectedSlotIndex;public void RemoveHeldItem(){slots[0].count--;}public void RequestChest(Vector3Int position){}}
public partial class Player
{
 public class Objects {public Transform body=new Transform();}
 public Objects objects=new Objects();public Transform transform=new Transform(),cam=new Transform();public World world=new World();public Inventory inventory=new Inventory();public bool networkStarted,isCrouching;public float reach=5,entityWidth=.3f,entityHeight=1.8f;public Vector3Int aimed,placement;
 private object TargetedMob()=>null;
 private bool TryUseNetherBed(Block block)=>false;
 private bool TryUseBed(Block block)=>false;
 private bool TryUseFarming(Block block,Vector3Int adjacent)=>false;
 public bool UseForCheck()=>TryUseBlock();
 public Block TargetedBlock(float reach,out Vector3Int adjacent){adjacent=placement;return world.GetBlock(aimed);}
}
public static class PlacementCheck
{
 static void Check(bool b,string label){if(!b)throw new Exception(label);}
 public static void Run()
 {
  for(int i=0;i<133;i++)BlockDataManager.blockTypeDataDictionary[i]=new BlockData();
  for(byte facing=0;facing<4;facing++)
  {
   var p=new Player{aimed=new Vector3Int(0,0,0),placement=new Vector3Int(0,1,0)};p.transform.position=new Vector3(10,10,10);
   p.world.SetBlock(p.aimed,BlockType.Stone);p.inventory.slots[0].type=ItemType.WoodenStairs;
   p.cam.forward=facing==0?new Vector3(1,0,0):facing==1?new Vector3(-1,0,0):facing==2?new Vector3(0,0,1):new Vector3(0,0,-1);
   p.objects.body.forward=p.cam.forward*-1; // Movement-facing body must not steer placement.
   Check(p.PlaceBlock()&&StairGeometry.Facing(p.world.GetBlock(p.placement).blockShape)==facing&&p.inventory.slots[0].count==63,"offline placement applies direction and consumes one item");
   p.world.SetBlock(p.placement,BlockType.Air);p.networkStarted=true;p.inventory.slots[0].type=ItemType.CobblestoneStairs;
   Check(p.PlaceBlock()&&Mirror.NetworkClient.last.stairFacing==facing,"network placement carries direction");
  }
  var chestStates=new byte[]{4,5,2,3};
  for(int direction=0;direction<4;direction++)
  {
   var p=new Player{aimed=new Vector3Int(0,0,0),placement=new Vector3Int(0,1,0)};p.transform.position=new Vector3(10,10,10);
   p.world.SetBlock(p.aimed,BlockType.Stone);p.inventory.slots[0].type=ItemType.Chest;
   p.cam.forward=direction==0?new Vector3(1,0,0):direction==1?new Vector3(-1,0,0):direction==2?new Vector3(0,0,1):new Vector3(0,0,-1);
   p.objects.body.forward=p.cam.forward*-1;
   Check(p.PlaceBlock()&&SpecialBlockGeometry.State(BlockType.Chest,p.world.GetBlock(p.placement).blockShape)==chestStates[direction],"chest front faces the viewing player rather than movement direction");
  }
  foreach(var slab in new[]{BlockType.StoneSlab,BlockType.SandstoneSlab,BlockType.WoodenSlab,BlockType.CobblestoneSlab})
  {
   var p=new Player{aimed=new Vector3Int(),placement=new Vector3Int(0,1,0)};p.transform.position=new Vector3(10,10,10);p.world.SetBlock(p.aimed,slab);p.inventory.slots[0].type=ItemCatalog.FromBlock(slab);
   BlockType doubled;StairGeometry.TryDoubleSlab(slab,slab,out doubled);
   Check(p.PlaceBlock()&&p.world.GetBlock(p.aimed).type==doubled&&p.world.GetBlock(p.placement).type==BlockType.Air&&p.inventory.slots[0].count==63,"actual placement combines matching lower slab without leaving an upper block");
   p.world.SetBlock(p.aimed,slab);p.transform.position=new Vector3(.5f,.5f,.5f);Check(!p.PlaceBlock()&&p.world.GetBlock(p.aimed).type==slab,"slab merge cannot fill the player's body");
  }
  var normals=new[]{new Vector3Int(1,0,0),new Vector3Int(-1,0,0),new Vector3Int(0,0,1),new Vector3Int(0,0,-1),new Vector3Int(0,1,0)};
  for(byte s=1;s<=5;s++)
  {
   var p=new Player{aimed=new Vector3Int(4,1,4),placement=new Vector3Int(4+normals[s-1].x,1+normals[s-1].y,4+normals[s-1].z)};
   p.transform.position=new Vector3(20,20,20);p.world.SetBlock(p.aimed,BlockType.Stone);p.inventory.slots[0].type=ItemType.Torch;
   Check(p.PlaceBlock()&&SpecialBlockGeometry.State(BlockType.Torch,p.world.GetBlock(p.placement).blockShape)==s,"production torch placement records clicked attachment face");
  }
  var user=new Player{networkStarted=true,aimed=new Vector3Int(0,2,0)};
  user.world.SetBlock(user.aimed,BlockType.WoodenDoor,10);
  Check(user.UseForCheck()&&Mirror.NetworkClient.last.position.y==1&&Mirror.NetworkClient.last.stairFacing==6&&user.inventory.slots[0].count==64,"upper door use addresses lower half and consumes no item");
  user.isCrouching=true;Check(!user.UseForCheck(),"crouch bypasses block use for placement");
  var material=new Player();material.inventory.slots[0].type=ItemType.Coal;
  Check(!material.PlaceBlock()&&material.inventory.slots[0].count==64,"standalone item cannot be interpreted as a world block");
  material.inventory.slots[0].type=ItemType.Stone;material.inventory.slots[0].count=0;
  Check(!material.PlaceBlock(),"empty stack cannot place a block");
  material.inventory.DropRequestPending=true;material.inventory.slots[0].count=64;
  Check(!material.PlaceBlock()&&material.inventory.slots[0].count==64,"placement cannot consume a slot while a drop transaction is in flight");
  var rules=new World();var at=new Vector3Int(8,4,8);
  Check(!rules.CanPlaceSurvivalBlock(at,BlockType.Dandelion,0),"flowers cannot be placed in air");
  rules.SetBlock(at+Vector3Int.down,BlockType.Grass);
  Check(rules.CanPlaceSurvivalBlock(at,BlockType.Dandelion,0)&&rules.CanPlaceSurvivalBlock(at,BlockType.TallGrass,0),"flowers and tall grass accept grass support");
  rules.SetBlock(at+Vector3Int.down,BlockType.Sand);
  Check(rules.CanPlaceSurvivalBlock(at,BlockType.DeadBush,0)&&rules.CanPlaceSurvivalBlock(at,BlockType.Cactus,0),"desert plants accept sand support");
  rules.SetBlock(at+Vector3Int.right,BlockType.Stone);
  Check(!rules.CanPlaceSurvivalBlock(at,BlockType.Cactus,0),"cactus rejects a solid horizontal neighbor");
  rules.blocks.Remove(at+Vector3Int.right);rules.SetBlock(at+Vector3Int.down,BlockType.Dirt);rules.SetBlock(at+Vector3Int.down+Vector3Int.left,BlockType.Water);
  Check(rules.CanPlaceSurvivalBlock(at,BlockType.SugarCane,0),"sugar cane requires water beside valid soil");
  rules.blocks.Remove(at+Vector3Int.down+Vector3Int.left);
  Check(!rules.CanPlaceSurvivalBlock(at,BlockType.SugarCane,0),"sugar cane rejects dry soil");
  rules.SetBlock(at+Vector3Int.down,BlockType.Stone);
  Check(rules.CanPlaceSurvivalBlock(at,BlockType.SnowLayer,0)&&rules.CanPlaceSurvivalBlock(at,BlockType.RedstoneWire,0),"snow and redstone require solid floor support");
  Check(SpecialBlockGeometry.State(BlockType.Leaves,SpecialBlockGeometry.Shape(BlockType.Leaves,4))==4,"player-placed leaf no-decay metadata round-trips");
  rules.blocks.Clear();for(int x=0;x<4;x++)rules.SetBlock(at+new Vector3Int(x,0,0),BlockType.Leaves);rules.SetBlock(at+new Vector3Int(4,0,0),BlockType.Log);
  Check(BlockSurvivalRules.LeafConnection(at,p=>rules.GetBlock(p).type)==BlockSurvivalRules.LeafConnectionState.Connected,"leaves find a log through four connected leaf steps");
  rules.blocks.Remove(at+new Vector3Int(4,0,0));
  Check(BlockSurvivalRules.LeafConnection(at,p=>rules.GetBlock(p).type)==BlockSurvivalRules.LeafConnectionState.Disconnected,"orphan leaves decay when no log is reachable");
  Check(BlockSurvivalRules.LeafConnection(at,p=>p.x==7?BlockType.Nothing:rules.GetBlock(p).type)==BlockSurvivalRules.LeafConnectionState.Unknown,"unloaded leaf boundary defers decay");
  var crafter=new Player{aimed=new Vector3Int(3,2,4)};crafter.world.SetBlock(crafter.aimed,BlockType.CraftingTable);
  Check(crafter.UseForCheck()&&crafter.inventory.tablePosition.Equals(crafter.aimed),"right-click dispatches crafting tables to the new inventory interface");
  Console.WriteLine("39 production placement and block-survival checks passed (Unity/network mocked).");
 }
}

public static class GameAudio {public static void Play(string family,Vector3 p,float volume=1,float pitch=1){}public static void BroadcastBlock(BlockType t,Vector3Int p,bool broken,World world=null){}}

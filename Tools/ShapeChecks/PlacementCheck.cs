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
   p.objects.body.forward=facing==0?new Vector3(1,0,0):facing==1?new Vector3(-1,0,0):facing==2?new Vector3(0,0,1):new Vector3(0,0,-1);
   Check(p.PlaceBlock()&&StairGeometry.Facing(p.world.GetBlock(p.placement).blockShape)==facing&&p.inventory.slots[0].count==63,"offline placement applies direction and consumes one item");
   p.networkStarted=true;p.inventory.slots[0].type=ItemType.CobblestoneStairs;Check(p.PlaceBlock()&&Mirror.NetworkClient.last.stairFacing==facing,"network placement carries direction");
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
  var crafter=new Player{aimed=new Vector3Int(3,2,4)};crafter.world.SetBlock(crafter.aimed,BlockType.CraftingTable);
  Check(crafter.UseForCheck()&&crafter.inventory.tablePosition.Equals(crafter.aimed),"right-click dispatches crafting tables to the new inventory interface");
  Console.WriteLine("27 production stair/slab/torch/item/interaction placement checks passed (Unity/network mocked).");
 }
}

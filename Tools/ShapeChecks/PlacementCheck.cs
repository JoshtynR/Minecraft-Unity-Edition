using System;
using UnityEngine;
namespace UnityEngine
{
 public class Transform {public Vector3 position,forward;}
}
namespace Mirror {public static class NetworkClient {public static WorldServer.SetBlockMessage last;public static void Send(WorldServer.SetBlockMessage message){last=message;}}}
public class WorldServer
{
 public struct SetBlockMessage {public Vector3Int position;public BlockType blockType;public byte stairFacing;public SetBlockMessage(Vector3Int p,BlockType type,byte facing=0){position=p;blockType=type;stairFacing=facing;}}
}
public partial class World
{
 public void SetBlock(Vector3Int pos,BlockType type,byte facing=0){blocks[pos]=new Block(type){blockShape=StairGeometry.IsStair(type)?StairGeometry.Shape(facing):BlockShapesExtensions.ForBlock(type)};}
}
public static class BlockDataManager {public static BlockData[] blockTypeDataDictionary=new BlockData[133];}
public class Inventory {public class Slot {public BlockType type;public int count=64;}public Slot[] slots={new Slot()};public int selectedSlotIndex;public void RemoveHeldItem(){slots[0].count--;}}
public partial class Player
{
 public class Objects {public Transform body=new Transform();}
 public Objects objects=new Objects();public Transform transform=new Transform();public World world=new World();public Inventory inventory=new Inventory();public bool networkStarted;public float reach=5,entityWidth=.3f,entityHeight=1.8f;public Vector3Int aimed,placement;
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
   p.world.SetBlock(p.aimed,BlockType.Stone);p.inventory.slots[0].type=BlockType.WoodenStairs;
   p.objects.body.forward=facing==0?new Vector3(1,0,0):facing==1?new Vector3(-1,0,0):facing==2?new Vector3(0,0,1):new Vector3(0,0,-1);
   Check(p.PlaceBlock()&&StairGeometry.Facing(p.world.GetBlock(p.placement).blockShape)==facing&&p.inventory.slots[0].count==63,"offline placement applies direction and consumes one item");
   p.networkStarted=true;p.inventory.slots[0].type=BlockType.CobblestoneStairs;Check(p.PlaceBlock()&&Mirror.NetworkClient.last.stairFacing==facing,"network placement carries direction");
  }
  foreach(var slab in new[]{BlockType.StoneSlab,BlockType.SandstoneSlab,BlockType.WoodenSlab,BlockType.CobblestoneSlab})
  {
   var p=new Player{aimed=new Vector3Int(),placement=new Vector3Int(0,1,0)};p.transform.position=new Vector3(10,10,10);p.world.SetBlock(p.aimed,slab);p.inventory.slots[0].type=slab;
   BlockType doubled;StairGeometry.TryDoubleSlab(slab,slab,out doubled);
   Check(p.PlaceBlock()&&p.world.GetBlock(p.aimed).type==doubled&&p.world.GetBlock(p.placement).type==BlockType.Air&&p.inventory.slots[0].count==63,"actual placement combines matching lower slab without leaving an upper block");
   p.world.SetBlock(p.aimed,slab);p.transform.position=new Vector3(.5f,.5f,.5f);Check(!p.PlaceBlock()&&p.world.GetBlock(p.aimed).type==slab,"slab merge cannot fill the player's body");
  }
  Console.WriteLine("16 production stair/slab placement checks passed (Unity/network mocked).");
 }
}

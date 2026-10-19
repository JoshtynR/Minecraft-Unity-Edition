using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
 public struct Vector3Int {public static Vector3Int down=>new Vector3Int(0,-1,0);public static Vector3Int FloorToInt(Vector3 v)=>new Vector3Int((int)Math.Floor(v.x),(int)Math.Floor(v.y),(int)Math.Floor(v.z));public static implicit operator Vector3(Vector3Int v)=>new Vector3(v.x,v.y,v.z);public static Vector3Int operator +(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;} }
 public struct Vector3
 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);
  public float sqrMagnitude=>x*x+y*y+z*z;
  public static Vector3 zero=>new Vector3();
  public static Vector3 up=>new Vector3(0,1,0);
  public static Vector3 operator *(Vector3 a,float f)=>new Vector3(a.x*f,a.y*f,a.z*f);
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
 }
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;} }
 public struct Vector4 {public float x,y,z,w;public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;} }
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public static class Mathf {public static float Abs(float f)=>Math.Abs(f);public static float Clamp(float f,float a,float b)=>Math.Max(a,Math.Min(b,f));public static int FloorToInt(float f)=>(int)Math.Floor(f);public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);}
}
public enum Direction {down,up,backwards,forwards,left,right}
public class ChunkData {}
public class BlockData {public bool generateCollider=true;}
public class Block {public byte waterLevel;public Vector3Int globalWorldPosition;public BlockType type;public BlockData BlockData=new BlockData();public BlockShapes blockShape;public Block(BlockType t){type=t;blockShape=BlockShapesExtensions.ForBlock(t);} }
public partial class World {public int worldHeight=12;public Dictionary<Vector3Int,Block> blocks=new Dictionary<Vector3Int,Block>();public Block GetBlock(Vector3Int p){Block b;return blocks.TryGetValue(p,out b)?b:new Block(BlockType.Air);} }
public class TestTransform {public Vector3 position;public Vector3 forward=new Vector3(0,0,1),right=new Vector3(1,0,0);}
namespace UnityEngine {public static class Time {public static float deltaTime=0.02f,fixedDeltaTime=0.02f;}}
public partial class Controller
{
 public bool jumpRequest;public float jumpForce=10.15f;
 protected Vector3 extraForce;
 public void Move(){transform.position+=velocity;}
 public void MoveForces(){}
 public World world;public TestTransform transform=new TestTransform(),direction=new TestTransform();public bool isGrounded,isFlying,isCrouching,isSprinting;public float verticalMomentum,gravity=-32,walkSpeed=4.317f,crouchSpeed=1.31f,sprintSpeed=5.612f,flySpeed=10.92f,flySprintSpeed=21.6f,entityWidth=0.3f,entityHeight=1.8f,horizontal,vertical;public Vector3 velocity;
}
public static partial class ShapeCheck
{
 static int checks;static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 static bool Near(float a,float b)=>Math.Abs(a-b)<0.00001f;
 static void Put(World w,int x,int y,int z,BlockType type){w.blocks[new Vector3Int(x,y,z)]=new Block(type);}
 static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
 public static void Main()
 {
  CheckStairs();
  CheckFluidController();
  CheckSpecial();
  CheckSpecialTextures();
  CheckJumpInput();
  PlacementCheck.Run();
  var plants = new[]{BlockType.Dandelion,BlockType.Rose,BlockType.BrownMushroom,BlockType.RedMushroom,
   BlockType.OakSapling,BlockType.SpruceSapling,BlockType.BirchSapling,BlockType.TallGrass,BlockType.DryTallGrass,
   BlockType.Fern,BlockType.DeadBush,BlockType.SugarCane,BlockType.WheatCrop0,BlockType.WheatCrop7};
  foreach(var plant in plants)
  {
   var plantWorld=new World();Put(plantWorld,0,0,0,plant);
   Check(Near(BlockCollision.Clip(plantWorld,V(-1,0,0.5f),0.3f,1.8f,2,0),2),"plants have no body collision even with collider flag enabled");
   Vector3Int next;Vector3 point;
   var pickedPlant=BlockTargeting.Raycast(plantWorld,V(0.5f,0.1f,-1),V(0,0,1),3,out next,out point);
   Check(pickedPlant!=null&&pickedPlant.type==plant,"thin plant selection remains targetable");
  }
  Check(Near(BlockShapesList.Flower.xMin/16f,.3f)&&Near(BlockShapesList.Flower.yMax/16f,.6f),"Beta flower bounds");
  Check(Near(BlockShapesList.Mushroom.yMax/16f,.4f)&&Near(BlockShapesList.Bush.xMin/16f,.1f)&&Near(BlockShapesList.Bush.yMax/16f,.8f),"Beta mushroom/sapling/bush bounds");
  Check(Near(BlockShapesList.Reed.xMin/16f,.125f)&&Near(BlockShapesList.Crop.yMax/16f,.25f),"Beta reed/crop bounds");
  foreach(BlockType type in Enum.GetValues(typeof(BlockType)))
  {
   var s=BlockShapesExtensions.ForBlock(type).GetShape();Check(s!=null&&s.xMax>s.xMin&&s.yMax>s.yMin&&s.zMax>s.zMin,"all catalog types have valid geometry");
   foreach(Direction dir in Enum.GetValues(typeof(Direction)))
   {var m=new MeshData(false);s.SetFaceVertices(dir,new Vector3Int(),m);m.AddQuadTriangles();m.AddFaceUVs(3,s.FaceUVTransform(dir));Check(m.vertices.Count==4&&m.triangles.Count==6&&m.faceCoordinates.Count==4,"face geometry/channel alignment");}
  }
  Check(BlockShapesExtensions.ForBlock(BlockType.DoubleWoodenSlab)==BlockShapes.FullBlock&&BlockShapesExtensions.ForBlock(BlockType.WoodenSlab)==BlockShapes.Slab,"old slabs and double slabs retain identity");
  Check(BlockShapesExtensions.ForBlock(BlockType.Farmland).GetShape().yMax==15&&BlockShapesExtensions.ForBlock(BlockType.MoistFarmland).GetShape().yMax==15,"farmland visual height");
  Check(BlockShapesList.Bed.yMax==9&&BlockShapesList.Cake.yMax==8&&BlockShapesList.Trapdoor.yMax==3&&BlockShapesList.PressurePlate.yMax==1,"Beta default cuboid dimensions");
  Check(!BlockShapesList.Cactus.isSideFull(Direction.up)&&!BlockShapesList.Cactus.isSideFull(Direction.down),"inset cactus never hides a full neighbor face");
  Check(!BlockShapesList.Cake.TouchesCellBoundary(Direction.up)&&BlockShapesList.Slab.CoversBoundaryFace(BlockShapesList.Slab,Direction.right),"inset face and equal slab boundary coverage");
  Check(!BlockShapesList.Slab.CoversBoundaryFace(BlockShapesList.FullBlock,Direction.right),"half slab cannot hide full cube side");
  var uv=BlockShapesList.Slab.FaceUVTransform(Direction.right);Check(uv.x==1&&uv.y==0.5f,"slab side crops half image rather than stretches it");
  var w=new World();Put(w,0,0,0,BlockType.StoneSlab);
  var d=BlockCollision.Resolve(w,V(0.5f,2,0.5f),0.3f,1.8f,V(0,-3,0),false);Check(Near(d.y,-1.5f)&&BlockCollision.Supported(w,V(0.5f,0.5f,0.5f),0.3f,1.8f),"land on half slab without tunneling or floor rounding");
  Put(w,0,0,0,BlockType.SoulSand);d=BlockCollision.Resolve(w,V(0.5f,2,0.5f),0.3f,1.8f,V(0,-3,0),false);Check(Near(d.y,-1.125f)&&BlockShapesExtensions.ForBlock(BlockType.SoulSand).GetShape().yMax==16,"soul sand full visual cube and lower collider");
  Put(w,0,0,0,BlockType.Farmland);d=BlockCollision.Resolve(w,V(0.5f,2,0.5f),0.3f,1.8f,V(0,-3,0),false);Check(Near(d.y,-1),"Beta farmland collides at full height");
  w=new World();Put(w,1,1,0,BlockType.Cactus);d=BlockCollision.Resolve(w,V(0.5f,1,0.5f),0.3f,1.8f,V(1,0,0),false);Check(Near(d.x,0.2625f),"cactus collision uses inset side");
  w.GetBlock(new Vector3Int(1,1,0)).BlockData.generateCollider=false;d=BlockCollision.Resolve(w,V(0.5f,1,0.5f),0.3f,1.8f,V(1,0,0),false);Check(d.x==1,"respects user's collision flag");
  w=new World();Put(w,0,0,0,BlockType.Stone);Put(w,1,0,0,BlockType.Stone);Put(w,1,1,0,BlockType.StoneSlab);
  d=BlockCollision.Resolve(w,V(0.5f,1,0.5f),0.3f,1.8f,V(0.5f,0,0),true);Check(Near(d.x,0.5f)&&Near(d.y,0.5f),"step onto half slab");
  Put(w,1,1,0,BlockType.Stone);d=BlockCollision.Resolve(w,V(0.5f,1,0.5f),0.3f,1.8f,V(0.5f,0,0),true);Check(Near(d.x,0.2f)&&Near(d.y,0),"cannot step over full block");
  Put(w,1,1,0,BlockType.StoneSlab);Put(w,0,3,0,BlockType.Stone);d=BlockCollision.Resolve(w,V(0.5f,1,0.5f),0.3f,1.8f,V(0.5f,0,0),true);Check(Near(d.x,0.2f),"low ceiling prevents step");
  w=new World();Put(w,0,0,0,BlockType.WoodenPressurePlate);Vector3Int adjacent;Vector3 hit;
  var picked=BlockTargeting.Raycast(w,V(0.5f,0.095f,0.5f),V(0,-1,0),1,out adjacent,out hit);Check(picked!=null&&picked.type==BlockType.WoodenPressurePlate&&adjacent.y==1,"thin pressure plate picked from same voxel without coarse steps");
  Check(hit.y<=0.0625f&&hit.y>0,"reported mining point lies inside plate");
  w=new World();Put(w,0,0,0,BlockType.Cactus);Put(w,0,0,1,BlockType.Stone);
  picked=BlockTargeting.Raycast(w,V(0.02f,0.5f,-1),V(0,0,1),4,out adjacent,out hit);Check(picked!=null&&picked.type==BlockType.Stone,"ray passes through empty margin around inset cactus");
  picked=BlockTargeting.Raycast(w,V(0.5f,0.5f,-1),V(0,0,1),4,out adjacent,out hit);Check(picked!=null&&picked.type==BlockType.Cactus&&adjacent.z==-1,"ray picks inset face with correct placement neighbor");
  Check(!BlockCollision.OverlapsBody(V(0.5f,0.5f,0.5f),0.3f,1.8f,new Vector3Int(),BlockShapesList.Slab),"can place half slab directly beneath feet without false full-cell overlap");
  Check(BlockCollision.OverlapsBody(V(0.5f,0.5f,0.5f),0.3f,1.8f,new Vector3Int(),BlockShapesList.FullBlock),"full cube placement overlapping body remains blocked");
  var controller=new Controller{world=new World(),isGrounded=true};Put(controller.world,0,0,0,BlockType.StoneSlab);controller.transform.position=V(0.5f,0.5f,0.5f);controller.CalculateVelocity();Check(controller.transform.position.y==0.5f&&controller.isGrounded,"production controller preserves slab standing height");
  controller.world.blocks.Clear();controller.CalculateVelocity();controller.CalculateVelocity();Check(!controller.isGrounded&&controller.velocity.y<0,"controller falls when support removed");
  Console.WriteLine(checks+" production cuboid/UV/occlusion/swept collision/step checks passed (world storage mocked).");
 }
}

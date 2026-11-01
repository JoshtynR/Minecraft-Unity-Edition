using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
 public class MonoBehaviour {public GameObject gameObject=new GameObject();public Transform transform=>gameObject.transform;public static void Destroy(object o){} }
 public class Transform {public Vector3 position,localPosition,localScale,eulerAngles,forward=new Vector3(0,0,1);public Quaternion rotation;public void SetParent(Transform p,bool b){}public void SetPositionAndRotation(Vector3 p,Quaternion q){position=p;rotation=q;}}
 public class GameObject {public string name;public Transform transform=new Transform();public GameObject(string name=""){}public void SetActive(bool b){}public T AddComponent<T>()where T:new()=>new T();public T GetComponent<T>()where T:new()=>new T();public static GameObject CreatePrimitive(PrimitiveType t)=>new GameObject();}
 public enum PrimitiveType {Cube}
 public class Collider {public bool enabled;}
 public class MeshRenderer {public Material sharedMaterial;}
 public class MeshFilter {public Mesh sharedMesh;}
 public class Mesh {public string name;public void SetVertices(List<Vector3> v){}public void SetUVs(int channel,List<Vector2> v){}public void SetTriangles(List<int> t,int sub){}public void RecalculateNormals(){}public void RecalculateBounds(){}}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public class Texture2D {public static Texture2D whiteTexture=new Texture2D();}
 public class Shader {public static Shader Find(string s)=>new Shader();}
 public class Material {public Material(Material m){}public Material(Shader s){}public Texture2D mainTexture;public Color color;public bool HasProperty(string s)=>true;public void SetColor(string s,Color c){}public void SetFloat(string s,float f){}}
 public struct Color {public Color(float r,float g,float b){}}
 public struct Quaternion {public static Quaternion Euler(float x,float y,float z)=>new Quaternion();public static Quaternion Slerp(Quaternion a,Quaternion b,float t)=>a;}
 public static class Resources {public static bool tntMaterial;public static T Load<T>(string s)where T:class=>tntMaterial&&s=="Materials/DroppedBlockMaterial"?new Material((Shader)null) as T:null;}
 public static class Time {public static float deltaTime=.05f,unscaledDeltaTime=.05f,fixedDeltaTime=.02f,unscaledTime;}
 public enum KeyCode {LeftShift}
 public static class Input {public static bool GetKeyDown(KeyCode k)=>false;}
 public static class Mathf
 {
  public const float Deg2Rad=(float)(Math.PI/180),Rad2Deg=(float)(180/Math.PI);
  public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static int Max(int a,int b)=>Math.Max(a,b);
  public static float Clamp(float v,float a,float b)=>Max(a,Min(b,v));public static float Clamp01(float f)=>Clamp(f,0,1);public static int FloorToInt(float f)=>(int)Math.Floor(f);
  public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static float Sin(float f)=>(float)Math.Sin(f);public static float Cos(float f)=>(float)Math.Cos(f);public static float Exp(float f)=>(float)Math.Exp(f);public static float Sign(float f)=>f<0?-1:1;public static float Abs(float f)=>Math.Abs(f);public static float Atan2(float a,float b)=>(float)Math.Atan2(a,b);
 }
 public struct Ray {public Vector3 origin,direction;public Ray(Vector3 p,Vector3 d){origin=p;direction=d;}}
 public struct Bounds
 {
  Vector3 c,s;public Bounds(Vector3 c,Vector3 s){this.c=c;this.s=s;}
  public bool IntersectRay(Ray r,out float distance)
  {
   float lo=0,hi=float.MaxValue;
   for(int i=0;i<3;i++){float p=i==0?r.origin.x:i==1?r.origin.y:r.origin.z,d=i==0?r.direction.x:i==1?r.direction.y:r.direction.z,mid=i==0?c.x:i==1?c.y:c.z,size=(i==0?s.x:i==1?s.y:s.z)/2;
    if(Math.Abs(d)<.00001f){if(p<mid-size||p>mid+size){distance=0;return false;}}else{float a=(mid-size-p)/d,b=(mid+size-p)/d;lo=Math.Max(lo,Math.Min(a,b));hi=Math.Min(hi,Math.Max(a,b));}}
   distance=lo;return hi>=lo;
  }
 }
}
namespace Mirror
{
 public interface NetworkMessage{}public class CommandAttribute:Attribute{}public class TargetRpcAttribute:Attribute{}
 public static class NetworkClient {public static bool active,ready;public static void RegisterHandler<T>(Action<T> action){}}
 public static class NetworkServer {public static bool active;public static Dictionary<int,int> connections=new Dictionary<int,int>();public static void SendToAll<T>(T message){}}
}
public enum WorldDimension {Overworld,Nether}
public class PlayerHudAssets {public Texture2D boatSkin,minecartSkin;}
public struct InventoryStack {public ItemType type;public int count,damage;public bool Empty=>count<=0;public InventoryStack(ItemType t,int n){type=t;count=n;damage=0;}}
public class Inventory {public InventoryStack[] slots=new InventoryStack[36];public bool DropRequestPending,isOpen;public int selectedSlotIndex;public InventoryStack HeldItem()=>slots[selectedSlotIndex];public InventoryStack[] DropInventorySnapshot()=>(InventoryStack[])slots.Clone();public void ApplyDropInventory(InventoryStack[] s){slots=(InventoryStack[])s.Clone();}}
public static class ChestTransactions {public static bool Valid(InventoryStack[] s,InventoryStack c)=>s!=null&&s.Length==36;}
public class FallState {public void Reset(){}}
public partial class Player
{
 public World CurrentWorld=>world;
 public World world;public Transform transform=new Transform(),cam=new Transform();public uint netId=1;public int health=20;public bool DimensionTransferInProgress,networkStarted,isServer=true,isLocalPlayer=true,dimensionTransfer,escapeMenuOpen,awaitingNetworkDrop,serverMining,isFlying,isSprinting,jumpRequest;
 public Inventory inventory=new Inventory();public float eyeHeight=1.6f,reach=5,horizontal,vertical,verticalMomentum;public Vector3 velocity;public FallState falling=new FallState();public int knownItemWearRevision,dropRequestId,lastServerDropRequest;public InventoryStack[] lastServerDropReply;public bool lastServerDropAccepted;
 public bool AcceptsItemRevision(int i)=>i==knownItemWearRevision;public void CancelMining(){}public void ResetWalkMotion(){}
 public void CompleteDropAction(int r,InventoryStack[] s,bool accepted,bool n){inventory.DropRequestPending=false;if(accepted)inventory.ApplyDropInventory(s);}
 public void Action(int req,int id,bool attack,Vector3 look){ProcessVehicleAction(req,id,attack,0,inventory.DropInventorySnapshot(),knownItemWearRevision,look,false);}
 public bool Click()=>TryUseVehicle(false);
}
public class BlockData {public bool generateCollider=true;}
public class Block {public BlockType type;public byte blockShape,waterLevel;public Vector3Int globalWorldPosition;public BlockData BlockData=new BlockData();}
public struct BlockShape {public int xMin,yMin,zMin,xMax,yMax,zMax;}
public static class BlockShapesExtensions {public static bool IsPlant(BlockType t)=>false;}
public static class SpecialBlockGeometry
{
 public static bool NormalCube(BlockType t)=>t==BlockType.Stone||t==BlockType.TNT;
 public static bool IsRail(BlockType t)=>t==BlockType.Rail||t==BlockType.PoweredRail||t==BlockType.DetectorRail;
 public static bool NoCollision(BlockType t)=>IsRail(t)||t==BlockType.RedstoneWire;
 public static byte State(BlockType t,byte s)=>s;
 public static int CollisionCount(BlockType t)=>1;
 public static BlockShape CollisionPart(BlockType t,byte state,int part)=>new BlockShape{xMax=16,yMax=16,zMax=16};
}
public static class BetaFluidPhysics {public static float EyeSurface(int y,int state)=>y+1-(state>=8?1:state+1)/9f;}
public static class HandMining {public static bool IsTarget(BlockType t)=>t!=BlockType.Air&&t!=BlockType.Nothing&&t!=BlockType.Water&&t!=BlockType.Lava;}
public static class ShapeExtension {public static BlockShape GetShape(this byte state)=>new BlockShape{xMax=16,yMax=16,zMax=16};}
public class ChunkRenderer {public object ChunkData=new object();public Transform transform=new Transform();public int immediateRefreshes;public void RefreshBlockSectionNow(int y){immediateRefreshes++;}}
public class WorldData {public Dictionary<Vector3Int,ChunkRenderer> chunkDict=new Dictionary<Vector3Int,ChunkRenderer>();}
public static class WorldDataHelper {public static Vector3Int GetChunkPosition(World w,Vector3Int p)=>new Vector3Int((int)Math.Floor(p.x/16f)*16,0,(int)Math.Floor(p.z/16f)*16);}
public partial class World
{
 public WorldData worldData=new WorldData();public Dictionary<Vector3Int,Block> blocks=new Dictionary<Vector3Int,Block>();public int worldHeight=128,betaPopulationJobs;public bool DropAuthority=true,isSaving,disabled,IsWorldCreated=true;public WorldDimension dimension;public ChunkRenderer worldRenderer=new ChunkRenderer();public List<InventoryStack> drops=new List<InventoryStack>();
 public Block GetBlock(Vector3Int p){if(blocks.TryGetValue(p,out var b))return b;return new Block{type=worldData.chunkDict.ContainsKey(WorldDataHelper.GetChunkPosition(this,p))?BlockType.Air:BlockType.Nothing,globalWorldPosition=p};}
 public void Put(int x,int y,int z,BlockType t,byte s=0){var p=new Vector3Int(x,y,z);blocks[p]=new Block{type=t,blockShape=s,globalWorldPosition=p};}
 public static World FindDimension(WorldDimension d)=>null;public static void Destroy(object o){}
 public bool SpawnItemDrop(InventoryStack stack,Vector3 position){drops.Add(stack);return true;}
 public void Tick(){Time.unscaledTime+=.05f;UpdateVehicles();}
 public void Sim(VehicleState v){TickVehicle(v);}
}
public static class VehicleCheck
{
 static int checks;static void Check(bool b,string message){checks++;if(!b)throw new Exception(message);}
 static World Ready(){var w=new World();for(int x=-32;x<=32;x+=16)for(int z=-32;z<=32;z+=16)w.worldData.chunkDict[new Vector3Int(x,0,z)]=new ChunkRenderer();return w;}
 public static void Main()
 {
  foreach(var kind in new[]{VehicleKind.Boat,VehicleKind.Minecart})
  {
   var model=VehicleModelGeometry.Build(kind);int count=kind==VehicleKind.Boat?120:124;
   Check(model.vertices.Count==count&&model.uv.Count==count,"classic model faces and UV channels");
   Check(model.triangles.Count==count/4*6,"classic model triangle count");
   float minY=100,maxY=-100,minZ=100,maxZ=-100;
   foreach(var v in model.vertices){minY=Math.Min(minY,v.y);maxY=Math.Max(maxY,v.y);minZ=Math.Min(minZ,v.z);maxZ=Math.Max(maxZ,v.z);}
   Check(Math.Abs(minY)<.00001f&&Math.Abs(maxY-.625f)<.00001f,"classic hull base and rim heights");
   Check(Math.Abs(maxZ-minZ-(kind==VehicleKind.Boat?1.5f:1.25f))<.00001f,"classic hull length follows forward axis");
   foreach(var uv in model.uv)Check(uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1,"skin UV inside 64x32 atlas");
   // First floor box's +X face starts at (u+d+w+d, v+d),
   // inset by the reference 0.1 texel and with image V inverted.
   float u=kind==VehicleKind.Boat?32:24,top=kind==VehicleKind.Boat?12:12;
   Check(Math.Abs(model.uv[0].x-(u-.1f)/64)<.00001f&&Math.Abs(model.uv[0].y-(1-(top+.1f)/32))<.00001f,"floor atlas offsets and V conversion");
  }
  for(int shape=0;shape<10;shape++)
  {
   VehiclePhysics.RailEnds(shape,out var a,out var b);
   Check((VehiclePhysics.RailPoint(shape,0)-a).magnitude<.0001f,"rail start");Check((VehiclePhysics.RailPoint(shape,1)-b).magnitude<.0001f,"rail end");
   for(int i=0;i<=20;i++){float t=i/20f;Check(Math.Abs(VehiclePhysics.RailParameter(shape,VehiclePhysics.RailPoint(shape,t))-t)<.003f,"rail projection round trip");}
  }
  var boat=new VehicleState{kind=VehicleKind.Boat};VehiclePhysics.BoatVelocity(boat,0,.05f);Check(boat.velocity.y<0,"dry boat falls");
  boat.velocity=Vector3.zero;VehiclePhysics.BoatVelocity(boat,1,.05f);Check(boat.velocity.y>0,"submerged boat rises");
  boat.throttle=1;for(int i=0;i<1000;i++)VehiclePhysics.BoatVelocity(boat,.4f,.05f);Check(boat.velocity.magnitude<7.1f,"boat speed bounded");
  var w=Ready();for(int x=-5;x<24;x++)for(int z=-5;z<6;z++)w.Put(x,0,z,BlockType.Stone);
  for(int x=0;x<20;x++)w.Put(x,1,0,BlockType.Rail,1);
  Check(w.SpawnVehicle(VehicleKind.Minecart,new Vector3(.5f,1.0625f,.5f),0),"place cart on rail");var cart=w.Vehicle(1);cart.velocity=new Vector3(5,0,0);
  for(int i=0;i<30;i++)w.Sim(cart);Check(cart.position.x>3&&Math.Abs(cart.position.z-.5f)<.01f,"cart traverses consecutive rails");
  // Continuous circuit: straight -> SE curve -> straight south.
  w=Ready();w.Put(0,1,0,BlockType.Rail,0);w.Put(0,1,1,BlockType.Rail,9);w.Put(1,1,1,BlockType.Rail,1);w.Put(2,1,1,BlockType.Rail,1);
  cart=new VehicleState{kind=VehicleKind.Minecart,position=new Vector3(.5f,1.0625f,.6f),velocity=new Vector3(0,0,5),occupied=true};
  for(int i=0;i<10;i++)w.Sim(cart);Check(cart.position.x>1&&Math.Abs(cart.position.z-1.5f)<.15f,"cart follows corner into adjacent rail");
  w=Ready();w.Put(0,0,0,BlockType.Stone);w.Put(1,0,0,BlockType.Stone);w.Put(1,1,0,BlockType.Stone);w.Put(0,1,0,BlockType.Rail,2);w.Put(1,2,0,BlockType.Rail,1);w.Put(2,2,0,BlockType.Rail,1);
  cart=new VehicleState{kind=VehicleKind.Minecart,position=new Vector3(.3f,1.3625f,.5f),velocity=new Vector3(6,0,0),occupied=true};
  for(int i=0;i<8;i++)w.Sim(cart);Check(cart.position.x>1&&cart.position.y>2,"cart climbs slope without hitting track support");
  w.Put(2,2,0,BlockType.Stone);cart.position=new Vector3(1.4f,2.0625f,.5f);cart.velocity=new Vector3(8,0,0);for(int i=0;i<5;i++)w.Sim(cart);Check(cart.position.x<=1.511f,"cart cannot pass solid wall");
  w=Ready();w.worldData.chunkDict.Remove(new Vector3Int(16,0,0));cart=new VehicleState{kind=VehicleKind.Minecart,position=new Vector3(15.4f,4,.5f),velocity=new Vector3(8,0,0)};var before=cart.position;w.Sim(cart);Check((cart.position-before).magnitude<.001f,"unloaded swept boundary suspends simulation");
  w=Ready();for(int x=-2;x<3;x++)for(int z=-2;z<3;z++)w.Put(x,1,z,BlockType.Water);
  boat=new VehicleState{kind=VehicleKind.Boat,position=new Vector3(.5f,1.5f,.5f)};for(int i=0;i<200;i++)w.Sim(boat);Check(boat.position.y>1.3f&&boat.position.y<1.8f,"boat settles at water surface");
  Check(w.SpawnVehicle(VehicleKind.Boat,new Vector3(.5f,1.5f,.5f),0),"spawn boat");var player=new Player{world=w};player.transform.position=new Vector3(.5f,1.5f,-1);
  Check(w.UseVehicle(player,1,false),"mount boat");Check(!w.UseVehicle(new Player{world=w},1,false),"one rider only");
  var saved=w.CaptureVehicles();Check(!saved[0].occupied&&saved[0].driver==null,"save excludes live rider");w.RestoreVehicles(saved);Check(w.Vehicle(1)!=null&&!w.Vehicle(1).occupied,"restore vehicle unmounted");
  Check(w.UseVehicle(player,1,true)&&w.drops.Count==1,"break empty vehicle returns item");Check(!w.UseVehicle(player,1,true)&&w.drops.Count==1,"cannot duplicate broken vehicle");
  w=Ready();w.Put(0,1,0,BlockType.Rail,1);player=new Player{world=w};player.transform.position=new Vector3(.5f,1,-1);player.inventory.slots[0]=new InventoryStack(ItemType.Minecart,1);
  var aim=new Vector3(0,-1.3f,1.5f).normalized;
  player.Action(1,0,false,aim);Check(player.inventory.slots[0].Empty&&w.Vehicle(1)!=null,"player item spawns cart and consumes original slot");player.Action(1,0,false,aim);Check(w.Vehicle(2)==null,"request replay cannot duplicate spawn");
  Check(!w.SpawnVehicle(VehicleKind.Boat,new Vector3(float.NaN,1,0),0),"invalid coordinates rejected");
  w=Ready();for(int x=-4;x<=4;x++)for(int z=-4;z<=4;z++)w.Put(x,0,z,BlockType.Stone);
  Check(w.SpawnVehicle(VehicleKind.Minecart,new Vector3(.5f,1.0625f,.5f),0),"ground cart spawn");player=new Player{world=w};
  Check(w.UseVehicle(player,1,false),"ground cart mount");Check(w.LeaveVehicle(player)&&!w.Vehicle(1).occupied&&Math.Abs(player.transform.position.y-1)<.001f,"dismount settles onto safe adjacent floor");
  w.UseVehicle(player,1,false);w.SetVehicleInput(player,1,1,1,0);var owned=w.Vehicle(1);Check(owned.throttle==1,"rider controls accepted");
  w.SetVehicleInput(new Player{world=w},1,-1,-1,0);Check(owned.throttle==1,"other player cannot steer");
  w.SetVehicleInput(player,1,float.NaN,0,0);Check(owned.throttle==1,"invalid control rejected");
  Time.unscaledTime+=1;w.Sim(owned);Check(owned.throttle==0,"stale input expires");
  w.DropAuthority=false;Check(!w.SpawnVehicle(VehicleKind.Boat,new Vector3(3,2,3),0)&&!w.UseVehicle(player,1,true),"remote world cannot spawn or break authoritatively");
  w.DropAuthority=true;player.health=0;w.Tick();Check(!w.Vehicle(1).occupied,"dead rider released");
  var restore=w.CaptureVehicles();w.RestoreVehicles(new[]{restore[0],restore[0],new VehicleState{id=2,position=new Vector3(float.NaN,0,0)}});Check(w.CaptureVehicles().Length==1,"restore rejects duplicates and nonfinite state");
  w.RestoreVehicles(null);Check(w.CaptureVehicles().Length==0,"old save without vehicles loads empty");
  w=Ready();w.Put(0,1,0,BlockType.Rail,1);player=new Player{world=w,networkStarted=true,isServer=true};player.transform.position=new Vector3(.5f,1,-1);player.cam.position=player.transform.position+Vector3.up*player.eyeHeight;player.cam.forward=aim;player.inventory.slots[0]=new InventoryStack(ItemType.Minecart,1);
  Mirror.NetworkClient.active=true;Mirror.NetworkClient.ready=false;
  Check(player.Click()&&w.Vehicle(1)!=null&&!player.inventory.DropRequestPending,"solo host placement bypasses network readiness");
  w=Ready();w.Put(0,1,0,BlockType.Rail,1);player=new Player{world=w,networkStarted=true,isServer=false};player.inventory.slots[0]=new InventoryStack(ItemType.Minecart,1);
  Check(player.Click()&&w.Vehicle(1)==null&&player.inventory.slots[0].count==1,"unready remote cannot consume or spawn");Mirror.NetworkClient.active=false;
  w=Ready();w.Put(0,1,0,BlockType.Water);player=new Player{world=w};player.transform.position=new Vector3(.5f,2,.5f);player.inventory.slots[0]=new InventoryStack(ItemType.Boat,1);player.Action(1,0,false,Vector3.down);
  Check(w.Vehicle(1)!=null&&w.Vehicle(1).kind==VehicleKind.Boat&&player.inventory.slots[0].Empty,"source-water ray places boat and consumes item");
  w=Ready();w.SpawnVehicle(VehicleKind.Boat,new Vector3(.5f,1.5f,3.5f),0);
  Check(w.TargetVehicle(new Vector3(.5f,1.8f,0),new Vector3(0,0,1),5)==1,"vehicle target reachable");w.Put(0,1,2,BlockType.Stone);
  Check(w.TargetVehicle(new Vector3(.5f,1.8f,0),new Vector3(0,0,1),5)==0,"solid wall blocks riding/attack ray");
  Console.WriteLine(checks+" vehicle rail/buoyancy/collision/chunk/rider/inventory checks passed (Unity/network mocked).");
 }
}

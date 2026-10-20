using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
 public class AudioClip:Object {}
 public class Font:Object {}
 public class RangeAttribute:Attribute {public RangeAttribute(int a,int b){}}
 public enum RuntimeInitializeLoadType {SubsystemRegistration}
 public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){}}
 public class Object {public bool destroyed;public static void Destroy(Object o){if(o!=null)o.destroyed=true;} }
 public class Tooltip:Attribute {public Tooltip(string s){}}
 public struct Vector3Int
 {
  public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}
  public static Vector3Int zero=>new Vector3Int();public static Vector3Int down=>new Vector3Int(0,-1,0);
  public static Vector3Int operator +(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);
  public static implicit operator Vector3(Vector3Int v)=>new Vector3(v.x,v.y,v.z);
  public static bool operator ==(Vector3Int a,Vector3Int b)=>a.x==b.x&&a.y==b.y&&a.z==b.z;public static bool operator !=(Vector3Int a,Vector3Int b)=>!(a==b);
  public override bool Equals(object o)=>o is Vector3Int&&this==(Vector3Int)o;public override int GetHashCode()=>x*31+y*7+z;
 }
 public struct Vector3
 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 up=>new Vector3(0,1,0);public static Vector3 one=>new Vector3(1,1,1);
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
  public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;
 }
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Vector4 {public float x,y,z,w;public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}}
 public struct Color {}
 public class Component:Object {public GameObject gameObject;}
 public class Transform:Component {public Vector3 position,forward=new Vector3(0,0,1);public T GetComponentInParent<T>() where T:class{return null;}}
 public class GameObject:Object
 {
  public Transform transform=new Transform();public bool active;Dictionary<Type,Component> components=new Dictionary<Type,Component>();
  public GameObject(string n,params Type[] types){foreach(var t in types){var c=(Component)Activator.CreateInstance(t);c.gameObject=this;components[t]=c;}}public void SetActive(bool value){active=value;}public T GetComponent<T>() where T:Component{return (T)components[typeof(T)];}
 }
 public class Mesh:Object {public string name;public List<Vector3> vertices;public static Mesh last;public Mesh(){last=this;}public void Clear(){}public void SetVertices(List<Vector3> v){vertices=v;}public void SetUVs<T>(int channel,List<T> uv){}public void SetTriangles(List<int> t,int submesh){}public void RecalculateBounds(){} }
 public class MeshFilter:Component {public Mesh sharedMesh;}
 public class MeshRenderer:Component {public Material sharedMaterial;public Rendering.ShadowCastingMode shadowCastingMode;public bool receiveShadows;}
 public class Texture2D:Object {public int width=320,height=32;}
 public class Material:Object {public string name;public Texture2D mainTexture;public static Material last;public Vector4 frame;public Material(Material other){last=this;}public Material(){}public void SetVector(string s,Vector4 v){frame=v;}}
 public class ScriptableObject:Object {}
 public static class Resources {public static PlayerHudAssets assets;public static T Load<T>(string n) where T:class=>assets as T;}
 public static class Debug {public static void LogWarning(string s){} }
 public static class Time {public static float time,deltaTime=0.05f;}
 public static class Input {public static bool held;public static bool GetMouseButton(int i)=>held;}
 public struct RaycastHit {public Transform transform;}
 public static class Physics {public static bool Raycast(Vector3 p,Vector3 f,out RaycastHit hit,float r,int mask){hit=new RaycastHit();return false;}}
 public static class LayerMask {public static int GetMask(string s)=>0;}
 public static class Random {public static int Range(int a,int b)=>1;}
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static int CeilToInt(float v)=>(int)Math.Ceiling(v);public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));}
 namespace Rendering {public enum ShadowCastingMode {Off}}
}
namespace Mirror {public class Command:Attribute {}public class TargetRpc:Attribute {}public class NetworkConnection {}public static class NetworkTime {public static double time;}}
namespace TMPro {public class TMP_FontAsset:UnityEngine.Object {}}
public enum Direction {down,up,backwards,forwards,left,right}
public static class BlockHelper {public static Direction[] directions=(Direction[])Enum.GetValues(typeof(Direction));}
public class ChunkData {}
public class ShapeSlot {public BlockShapes orientation;public static implicit operator BlockShapes(ShapeSlot s)=>s.orientation;public BlockShape shape=new BlockShape(0,0,0,16,16,16);public BlockShape GetShape()=>shape;}
public class Block {public BlockType type;public Vector3Int globalWorldPosition;public ShapeSlot blockShape=new ShapeSlot();}
public struct MiningDropProbe {public BlockType type;public bool Empty=>type==BlockType.Nothing;}
public class World
{
 public List<InventoryStack> drops=new List<InventoryStack>();
 public InventoryStack MiningItemDrop(BlockType type,ItemType tool,int damage)=>ToolMining.Drop(type,tool,1,1,0,damage);
 public bool SpawnItemDrop(InventoryStack drop,Vector3 position){drops.Add(drop);return true;}
 public bool IsWorldCreated=true;public Dictionary<Vector3Int,Block> blocks=new Dictionary<Vector3Int,Block>();public int edits;
 public bool chestProtected;public bool CanReplaceChest(Vector3Int position,BlockType next)=>!chestProtected;
 public Block GetBlock(Vector3 p)=>GetBlock(new Vector3Int((int)Math.Floor(p.x),(int)Math.Floor(p.y),(int)Math.Floor(p.z)));
 public Block GetBlock(Vector3Int p){Block b;return blocks.TryGetValue(p,out b)?b:new Block{type=BlockType.Air,globalWorldPosition=p};}
 public void SetBlock(Vector3Int p,BlockType type){edits++;blocks[p].type=type;}
}
public class WorldServer {public static WorldServer instance;public void RpcSetBlock(Vector3Int p,BlockType t,byte facing){} }
public class Inventory
{
 public void ApplyCompletedMiningWear(int i,InventoryStack expected,int amount){ApplyItemWear(i,expected,amount);}
 public bool isOpen,DropRequestPending;public int selectedSlotIndex;
 public class Slot {public ItemType type;public int count,damage;}
 public Slot[] slots=new Slot[36];public Inventory(){for(int i=0;i<36;i++)slots[i]=new Slot();}
 public InventoryStack HeldItem()=>new InventoryStack(slots[selectedSlotIndex].type,slots[selectedSlotIndex].count,slots[selectedSlotIndex].damage);
 public Slot GetSlot(int i)=>slots[i];
 public InventoryStack[] DropInventorySnapshot(){var snapshot=new InventoryStack[36];for(int i=0;i<36;i++)snapshot[i]=new InventoryStack(slots[i].type,slots[i].count,slots[i].damage);return snapshot;}
 public void ApplyDropInventory(InventoryStack[] snapshot){for(int i=0;i<36;i++){slots[i].type=snapshot[i].type;slots[i].count=snapshot[i].count;slots[i].damage=snapshot[i].damage;}}
 public bool ApplyItemWear(int i,InventoryStack expected,int amount)
 {
  var current=new InventoryStack(slots[i].type,slots[i].count,slots[i].damage);
  if(!current.Matches(expected)||current.Empty)return false;
  bool broken=ItemDurability.Wear(ref current,amount);slots[i].type=current.type;slots[i].count=current.count;slots[i].damage=current.damage;return broken;
 }
 public List<BlockType> drops=new List<BlockType>();public void AddItem(BlockType t){drops.Add(t);}
}
public partial class Player
{
 public int knownItemWearRevision,serverItemWearRevision;public bool AcceptsItemRevision(int revision)=>revision==serverItemWearRevision;
 public World world=new World();public Transform transform=new Transform(),cam=new Transform();public int health=20;public float reach=4.5f,eyeHeight=1.578f;public Inventory inventory=new Inventory();public bool escapeMenuOpen,networkStarted,isServer=true,isLocalPlayer=true,isGrounded=true,serverGrounded=true;public Mirror.NetworkConnection connectionToClient=new Mirror.NetworkConnection();public float paidExhaustion;
 private HungerSimulation hungerSimulation=new HungerSimulation();private void LoadHungerState(){}private void StoreHungerState(){paidExhaustion=hungerSimulation.Exhaustion;}
 public Vector3Int aimed;public Block TargetedBlock(float reach,out Vector3Int pos){pos=aimed;targetHitPoint=(Vector3)aimed+Vector3.one*0.5f;return world.GetBlock(aimed);}
 public void Begin(Vector3Int p,BlockType t){BeginServerMining(p,t,(Vector3)p+Vector3.one*0.5f);}public void Step(){Mirror.NetworkTime.time+=0.05;Time.time+=0.05f;AdvanceMining(0.05f);}public void Cancel(){CmdCancelMining();}public void Visual(){UpdateMining();}public void Cleanup(){DisposeMining();}
}
public static class MiningCheck
{
 static int checks;static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static Player Setup(BlockType type){var p=new Player();p.aimed=new Vector3Int(0,1,2);p.world.blocks[p.aimed]=new Block{type=type,globalWorldPosition=p.aimed};return p;}
 static void CheckTools()
 {
  var effective=new HashSet<string>();
  foreach(var line in System.IO.File.ReadAllLines("Tools/MiningChecks/beta-tool-efficiency.tsv"))effective.Add(line);
  var tools=new[]{ItemType.WoodenPickaxe,ItemType.StonePickaxe,ItemType.IronPickaxe,ItemType.DiamondPickaxe,ItemType.GoldenPickaxe,
   ItemType.WoodenAxe,ItemType.StoneAxe,ItemType.IronAxe,ItemType.DiamondAxe,ItemType.GoldenAxe,
   ItemType.WoodenShovel,ItemType.StoneShovel,ItemType.IronShovel,ItemType.DiamondShovel,ItemType.GoldenShovel};
  float[] speeds={2,4,6,8,12};
  for(int i=0;i<tools.Length;i++)foreach(var entry in BetaBlockCatalog.Entries)
  {
   float expected=effective.Contains(ItemDurability.Kind(tools[i])+"\t"+entry.SourceId)?speeds[i%5]:1;
   Check(ToolMining.Strength(entry.Type,tools[i])==expected,"source efficiency list "+tools[i]+" / "+entry.Type);
  }
  foreach(BlockType block in Enum.GetValues(typeof(BlockType)))
  {
   Check(ToolMining.DamagePerTick(block,ItemType.Nothing,true,false)==HandMining.DamagePerTick(block,true,false),"empty-hand behavior preserved");
   Check(ToolMining.Strength(block,ItemType.Dirt)==1&&ToolMining.Strength(block,ItemType.DiamondHoe)==1,"held blocks and hoes grant no mining bonus");
  }
  for(int i=0;i<5;i++)
  {
   var pick=tools[i];int tier=new[]{0,1,2,3,0}[i];
   foreach(var block in new[]{BlockType.Stone,BlockType.CoalOre,BlockType.Netherrack,BlockType.WoodenSlab,BlockType.GlowStone})Check(ToolMining.Harvestable(block,pick),"any pick harvests low-tier rock");
   foreach(var block in new[]{BlockType.IronOre,BlockType.IronBlock,BlockType.LapisOre,BlockType.LapisBlock})Check(ToolMining.Harvestable(block,pick)==(tier>=1),"stone tier requirement");
   foreach(var block in new[]{BlockType.GoldOre,BlockType.GoldBlock,BlockType.DiamondOre,BlockType.DiamondBlock,BlockType.RedstoneOre,BlockType.LitRedstoneOre})Check(ToolMining.Harvestable(block,pick)==(tier>=2),"iron tier requirement");
   Check(ToolMining.Harvestable(BlockType.Obsidian,pick)==(tier==3),"only diamond harvests obsidian");
  }
  Check(ToolMining.DamagePerTick(BlockType.Stone,ItemType.DiamondPickaxe,true,false)==8f/1.5f/30,"diamond stone damage");
  Check(ToolMining.DamagePerTick(BlockType.Stone,ItemType.GoldenPickaxe,true,false)>ToolMining.DamagePerTick(BlockType.Stone,ItemType.DiamondPickaxe,true,false),"gold faster than diamond on stone");
  Check(ToolMining.Strength(BlockType.Obsidian,ItemType.DiamondPickaxe)==1&&ToolMining.Strength(BlockType.RedstoneOre,ItemType.DiamondPickaxe)==1,"Beta obsidian and redstone not on efficiency list");
  Check(Math.Abs(ToolMining.DamagePerTick(BlockType.Stone,ItemType.DiamondPickaxe,false,true)-8f/25/1.5f/30)<.000001,"water and air stack penalties on valid tool");
  Check(ToolMining.DamagePerTick(BlockType.DiamondOre,ItemType.GoldenPickaxe,false,true)==1f/3/100,"insufficient tier has no speed or environment multiplier");
  Check(ToolMining.Strength(BlockType.Cobweb,ItemType.IronSword)==15&&ToolMining.Harvestable(BlockType.Cobweb,ItemType.Shears)&&!ToolMining.Harvestable(BlockType.Cobweb,ItemType.DiamondPickaxe),"web sword/shears harvest");
  Check(ToolMining.Strength(BlockType.BlackWool,ItemType.Shears)==5&&ToolMining.Strength(BlockType.SpruceLeaves,ItemType.Shears)==15,"shears wool/leaves speed");
  Check(ToolMining.Drop(BlockType.Stone,ItemType.DiamondPickaxe,1,1,0).type==ItemType.Cobblestone,"stone drops cobblestone");
  Check(ToolMining.Drop(BlockType.DiamondOre,ItemType.GoldenPickaxe,1,1,0).Empty,"fast low-tier tool gives no diamond");
  Check(ToolMining.Drop(BlockType.IronOre,ItemType.StonePickaxe,1,1,0).type==ItemType.IronOre,"iron ore drops unsmelted");
  Check(ToolMining.Drop(BlockType.GoldOre,ItemType.IronPickaxe,1,1,0).type==ItemType.GoldOre,"gold ore drops unsmelted");
  Check(ToolMining.Drop(BlockType.DiamondOre,ItemType.IronPickaxe,1,1,0).type==ItemType.Diamond&&ToolMining.Drop(BlockType.CoalOre,ItemType.WoodenPickaxe,1,1,0).type==ItemType.Coal,"ore products");
  Check(ToolMining.Drop(BlockType.Obsidian,ItemType.DiamondPickaxe,1,1,0).type==ItemType.Obsidian,"diamond harvests obsidian");
  for(int q=0;q<30;q++)
  {
   var lapis=ToolMining.Drop(BlockType.LapisOre,ItemType.StonePickaxe,1,1,q);Check(lapis.type==ItemType.LapisLazuli&&lapis.count==4+q%5,"lapis count 4-8");
   var red=ToolMining.Drop(BlockType.LitRedstoneOre,ItemType.IronPickaxe,1,1,q);Check(red.type==ItemType.RedstoneDust&&red.count==4+q%2,"redstone count 4-5");
   var glow=ToolMining.Drop(BlockType.GlowStone,ItemType.WoodenPickaxe,1,1,q);Check(glow.type==ItemType.GlowstoneDust&&glow.count==2+q%3,"glowstone count 2-4");
  }
  foreach(var leaf in new[]{BlockType.Leaves,BlockType.SpruceLeaves,BlockType.BirchLeaves})Check(ToolMining.Drop(leaf,ItemType.Shears,1,1,0).type==ItemCatalog.FromBlock(leaf),"shears keep leaf species");
  Check(ToolMining.Drop(BlockType.Leaves,ItemType.Shears,1,1,0,238).Empty,"shears that break on this block lose the special leaf harvest");
  Check(ToolMining.Drop(BlockType.SnowBlock,ItemType.DiamondShovel,1,1,0).count==4&&ToolMining.Drop(BlockType.SnowLayer,ItemType.DiamondPickaxe,1,1,0).Empty,"snow shovel-only drops");
  Check(ToolMining.Drop(BlockType.DoubleWoodenSlab,ItemType.WoodenPickaxe,1,1,0).count==2,"double slabs give two singles");
  foreach(var block in new[]{BlockType.Glass,BlockType.Ice,BlockType.MonsterSpawner,BlockType.Bedrock})Check(ToolMining.Drop(block,ItemType.DiamondPickaxe,1,1,0).Empty,"no invented drops");
  foreach(var tool in new[]{ItemType.DiamondPickaxe,ItemType.GoldenPickaxe,ItemType.WoodenPickaxe})
  {
   var p=Setup(BlockType.Stone);p.inventory.slots[0].type=tool;p.inventory.slots[0].count=1;p.Begin(p.aimed,BlockType.Stone);
   float progress=0;int ticks=0;while(progress<1){progress+=ToolMining.DamagePerTick(BlockType.Stone,tool,true,false);ticks++;}
   for(int i=0;i<ticks-1;i++)p.Step();Check(p.world.edits==0,"tool cannot finish early");p.Step();
   Check(p.world.edits==1&&p.world.drops.Count==1&&p.world.drops[0].type==ItemType.Cobblestone&&p.inventory.slots[0].damage==1,"tool completion spawns correct drop and charges wear once");
  }
  var denied=Setup(BlockType.DiamondOre);denied.inventory.slots[0].type=ItemType.GoldenPickaxe;denied.inventory.slots[0].count=1;denied.Begin(denied.aimed,BlockType.DiamondOre);for(int i=0;i<301;i++)denied.Step();
  Check(denied.world.edits==1&&denied.world.drops.Count==0&&denied.inventory.slots[0].damage==1,"insufficient tier can destroy ore slowly but loses the drop and wears tool");
 }
 public static void Main()
 {
  foreach(BlockType t in Enum.GetValues(typeof(BlockType)))
  {float h=HandMining.Hardness(t);Check(h>=0||HandMining.DamagePerTick(t,true,false)==0,"unbreakable/fluid progress zero");if(h>=0){float normal=HandMining.DamagePerTick(t,true,false);float penalty=HandMining.DamagePerTick(t,false,true);Check(Math.Abs(penalty-(h==0?1:HandMining.Harvestable(t)?normal/25:normal))<0.00001f,"Beta material-sensitive penalties");}}
  Check(HandMining.Hardness(BlockType.Stone)==1.5f&&HandMining.Hardness(BlockType.WoodenSlab)==2&&HandMining.Hardness(BlockType.GlowStone)==0.3f,"Beta hardness cases");
  Check(!HandMining.Harvestable(BlockType.WoodenSlab)&&!HandMining.Harvestable(BlockType.GlowStone),"Beta rock slab and glowstone require tool");
  Check(HandMining.ExistingDrop(BlockType.Grass,1)==BlockType.Dirt&&HandMining.ExistingDrop(BlockType.Stone,1)==BlockType.Nothing,"hand harvest/drop restrictions");
  Check(HandMining.ExistingDrop(BlockType.Gravel,0)==BlockType.Nothing&&HandMining.ExistingDrop(BlockType.Gravel,1)==BlockType.Gravel,"flint roll does not invent gravel");
  foreach(var t in new[]{BlockType.Dirt,BlockType.Log,BlockType.Stone,BlockType.Sandstone,BlockType.Glass,BlockType.WoodenSlab,BlockType.BirchLog})
  {var p=Setup(t);p.Begin(p.aimed,t);float progress=0;int ticks=0;while(progress<1){progress+=HandMining.DamagePerTick(t,true,false);ticks++;}for(int i=0;i<ticks-1;i++)p.Step();Check(p.world.edits==0,"cannot remove before accumulated hand progress");p.Step();Check(p.world.edits==1&&p.world.GetBlock(p.aimed).type==BlockType.Air,"authoritative completion once");p.Step();Check(p.world.edits==1,"completion not duplicated");Check(p.world.drops.Count==(HandMining.ExistingDrop(t,1)==BlockType.Nothing?0:1)&&p.inventory.drops.Count==0,"only permitted hand drops spawn in the world instead of auto-inserting into inventory");}
  var cancel=Setup(BlockType.Log);cancel.Begin(cancel.aimed,BlockType.Log);cancel.Step();cancel.Cancel();for(int i=0;i<100;i++)cancel.Step();Check(cancel.world.edits==0,"release cancels server mining");
  var far=Setup(BlockType.Dirt);far.transform.position=new Vector3(100,0,0);far.Begin(far.aimed,BlockType.Dirt);for(int i=0;i<40;i++)far.Step();Check(far.world.edits==0,"out of reach rejected");
  var occluded=Setup(BlockType.Dirt);occluded.world.blocks[new Vector3Int(0,1,1)]=new Block{type=BlockType.Stone,globalWorldPosition=new Vector3Int(0,1,1)};occluded.Begin(occluded.aimed,BlockType.Dirt);for(int i=0;i<40;i++)occluded.Step();Check(occluded.world.edits==0,"occluded mining rejected");
  var changed=Setup(BlockType.Dirt);changed.Begin(changed.aimed,BlockType.Dirt);changed.world.blocks[changed.aimed].type=BlockType.Stone;for(int i=0;i<200;i++)changed.Step();Check(changed.world.edits==0,"changed target cancels");
  var ice=Setup(BlockType.Ice);ice.world.blocks[ice.aimed+Vector3Int.down]=new Block{type=BlockType.Stone};ice.Begin(ice.aimed,BlockType.Ice);for(int i=0;i<20;i++)ice.Step();Check(ice.world.GetBlock(ice.aimed).type==BlockType.Water,"supported ice becomes water");
  var cooldown=Setup(BlockType.Dirt);cooldown.Begin(cooldown.aimed,BlockType.Dirt);for(int i=0;i<20;i++)cooldown.Step();cooldown.world.blocks[cooldown.aimed].type=BlockType.Dirt;cooldown.Begin(cooldown.aimed,BlockType.Dirt);for(int i=0;i<30;i++)cooldown.Step();Check(cooldown.world.edits==1,"post-break cooldown rejects early restart");cooldown.Begin(cooldown.aimed,BlockType.Dirt);for(int i=0;i<20;i++)cooldown.Step();Check(cooldown.world.edits==2,"restart after cooldown can break again");
  var bedrock=Setup(BlockType.Bedrock);bedrock.Begin(bedrock.aimed,BlockType.Bedrock);for(int i=0;i<500;i++)bedrock.Step();Check(bedrock.world.edits==0,"bedrock never removed");
  var chest=Setup(BlockType.Chest);chest.world.chestProtected=true;chest.Begin(chest.aimed,BlockType.Chest);for(int i=0;i<200;i++)chest.Step();Check(chest.world.edits==0,"filled or open chest rejects mining before destruction");
  chest.world.chestProtected=false;chest.Begin(chest.aimed,BlockType.Chest);chest.Step();chest.world.chestProtected=true;for(int i=0;i<200;i++)chest.Step();Check(chest.world.edits==0,"chest opened or filled after mining began cancels destruction");
  Resources.assets=new PlayerHudAssets{blockBreak=new Texture2D(),blockBreakMaterial=new Material()};var visual=Setup(BlockType.Log);Input.held=true;visual.Visual();Check(Material.last.frame.x==0.1f&&Mesh.last.vertices.Count==24,"ten-frame strip and six-face overlay");var ownedMesh=Mesh.last;var ownedMaterial=Material.last;Input.held=false;visual.Visual();visual.Cleanup();Check(ownedMesh.destroyed&&ownedMaterial.destroyed,"overlay destroys owned mesh/material");
  foreach(var stair in new[]{BlockType.WoodenStairs,BlockType.CobblestoneStairs})
  for(byte facing=0;facing<4;facing++)
  {
   var target=new Block{type=stair};target.blockShape.orientation=StairGeometry.Shape(facing);
   var cracks=new BlockBreakOverlay();cracks.Show(target,3);
   Check(Mesh.last.vertices.Count==44,"stair cracks follow eleven exterior quads");
   float maxX=0,maxZ=0;
   foreach(var v in Mesh.last.vertices)if(v.y>.9f){maxX=Math.Max(maxX,v.x);maxZ=Math.Max(maxZ,v.z);}
   Check(facing!=1||maxX<.501f,"west stair overlay upper half orientation");
   Check(facing!=3||maxZ<.501f,"north stair overlay upper half orientation");
   cracks.Dispose();
  }
  foreach(var tool in new[]{ItemType.WoodenPickaxe,ItemType.IronAxe,ItemType.GoldenShovel,ItemType.StoneSword})
  {
   var p=Setup(BlockType.Dirt);p.inventory.slots[0].type=tool;p.inventory.slots[0].count=1;
   p.Begin(p.aimed,BlockType.Dirt);for(int i=0;i<30;i++)p.Step();
   Check(p.inventory.slots[0].damage==ItemDurability.MiningWear(tool,BlockType.Dirt),"completed mining charges the held item once");
   p=Setup(BlockType.Dirt);p.inventory.slots[0].type=tool;p.inventory.slots[0].count=1;p.inventory.slots[1].type=tool;p.inventory.slots[1].count=1;p.networkStarted=true;
   p.Begin(p.aimed,BlockType.Dirt);for(int i=0;i<30;i++)p.Step();
   Check(p.inventory.slots[0].damage==ItemDurability.MiningWear(tool,BlockType.Dirt)&&p.inventory.slots[1].damage==0,"host completion callback cannot double-charge another matching tool");
  }
  var swapped=Setup(BlockType.Dirt);swapped.inventory.slots[0].type=ItemType.WoodenPickaxe;swapped.inventory.slots[0].count=1;
  swapped.Begin(swapped.aimed,BlockType.Dirt);swapped.Step();swapped.inventory.slots[0].type=ItemType.IronPickaxe;
  for(int i=0;i<30;i++)swapped.Step();Check(swapped.world.edits==0&&swapped.inventory.slots[0].damage==0,"changing the mining item cancels stale server work without wearing its replacement");
  var worn=Setup(BlockType.Dirt);worn.inventory.slots[0].type=ItemType.WoodenPickaxe;worn.inventory.slots[0].count=1;worn.inventory.slots[0].damage=59;
  worn.Begin(worn.aimed,BlockType.Dirt);for(int i=0;i<30;i++)worn.Step();Check(worn.inventory.slots[0].count==0&&worn.world.edits==1,"last tool use completes the block before removing the broken item");
  var interrupted=Setup(BlockType.Dirt);interrupted.inventory.slots[0].type=ItemType.WoodenPickaxe;interrupted.inventory.slots[0].count=1;interrupted.Begin(interrupted.aimed,BlockType.Dirt);interrupted.Step();interrupted.Cancel();for(int i=0;i<30;i++)interrupted.Step();
  Check(interrupted.inventory.slots[0].damage==0,"canceled mining never charges durability");
  CheckTools();
  Check(HandMining.CrackStage(0)==0&&HandMining.CrackStage(0.99f)==9&&HandMining.CrackStage(1)==9,"ten bounded crack stages");
  Console.WriteLine(checks+" production tool and hand mining/progress/drop/reach/overlay checks passed (Unity/Mirror mocked).");
 }
}

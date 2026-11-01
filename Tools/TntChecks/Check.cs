using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
 public static class Application {public static bool isBatchMode=true;}
 public struct Vector4 {public Vector4(float x,float y,float z,float w){}}
 public class MaterialPropertyBlock {public void SetFloat(string name,float f){}public void SetVector(string name,Vector4 v){}}
}
public static class LightExtensions {public static int GetSkyLight(this Block b)=>15;public static int GetBlockLight(this Block b)=>0;}
public static class RendererExtensions {public static void SetPropertyBlock(this MeshRenderer r,MaterialPropertyBlock p){}}
public static class ItemVisualMesh {public static Mesh Build(BlockType t)=>new Mesh();}
public class EntityVisualMotion {public Vector3 Step(Vector3 p,float dt,float period)=>p;}
public static class ItemDurability {public static void Wear(ref InventoryStack held,int amount){held.damage+=amount;}}
public static class GameAudio {public static int hisses;public static void Broadcast(string family,Vector3 p,float volume,float pitch,World w){if(family=="random/fuse")hisses++;}}
public partial class World
{
 private JavaRandom mobRandom;private long betaWorldSeed=173;
 public int blasts,particles;public float lastStrength;
 private void RefreshMobPlayers(){}
 private void ExplodeMob(Vector3 p,float strength,bool mobSource=true){blasts++;lastStrength=strength;EmitExplosionEffects(p,strength);}
 private void SetFarmBlock(Vector3Int p,BlockType t){Put(p.x,p.y,p.z,t);}
 private bool TryIgnitePortal(Vector3Int p)=>false;
 public void PlayExplosionParticles(Vector3 p,float s,Vector3Int[] cells=null){particles++;}
 public void PlayTntSmoke(Vector3 p){}
 public void TntTick()=>TickTnt();
}
public static class TntCheck
{
 static int checks;static void Assert(bool b,string name){checks++;if(!b)throw new Exception(name);}
 static World Ready()
 {var w=new World();for(int x=-16;x<=16;x+=16)for(int z=-16;z<=16;z+=16)w.worldData.chunkDict[new Vector3Int(x,0,z)]=new ChunkRenderer();for(int x=-5;x<6;x++)for(int z=-5;z<6;z++)w.Put(x,0,z,BlockType.Stone);return w;}
 public static void Main()
 {
  Assert(Math.Abs(TntRules.Swell(80)-1)<.00001f&&Math.Abs(TntRules.Swell(9)-1)<.00001f,"no swelling before final ten ticks");
  Assert(Math.Abs(TntRules.Swell(0)-1.19683f)<.00001f&&TntRules.Swell(-1)==1.3f,"quartic swelling reaches reference limits");
  Assert(Math.Abs(TntRules.Flash(80,0)-.152f)<.00001f&&TntRules.Flash(75,0)==0&&Math.Abs(TntRules.Flash(0,0)-.792f)<.00001f,"alternating source flash grows with fuse age");
  var initial=TntRules.InitialVelocity(.5f);Assert(initial.x<0&&initial.x>-.044f&&initial.z<-.397f&&initial.y==4,"original radians-to-degrees quirk preserved");
  Assert(ResistanceAdapter.BedBlastResistance(BlockType.Stone)==6&&ResistanceAdapter.BedBlastResistance(BlockType.WoodenPlanks)==3,"explicit resistance conversion, not hardness times three");
  Assert(ResistanceAdapter.BedBlastResistance(BlockType.Rail)==.7f&&ResistanceAdapter.BedBlastResistance(BlockType.Obsidian)==1200,"rails resist blasts despite having no body collision");
  Assert(ResistanceAdapter.BedBlastResistance(BlockType.Chest)==2.5f&&ResistanceAdapter.BedBlastResistance(BlockType.Bed)==.2f,"hardness-derived resistance for containers and beds");
  var w=Ready();w.Put(0,1,0,BlockType.TNT);Assert(w.PrimeTnt(new Vector3Int(0,1,0)),"TNT primes");Assert(w.GetBlock(new Vector3Int(0,1,0)).type==BlockType.Air&&GameAudio.hisses==1,"block consumed once and fuse hiss emitted");
  Assert(!w.PrimeTnt(new Vector3Int(0,1,0))&&GameAudio.hisses==1,"replay cannot duplicate primed TNT");var state=w.CaptureTnt();Assert(state.Length==1&&state[0].fuse==80,"four second fuse");
  for(int i=0;i<80;i++)w.TntTick();Assert(w.blasts==0&&w.CaptureTnt()[0].fuse==0,"post-decrement keeps fuse zero through update eighty");w.TntTick();Assert(w.blasts==1&&w.lastStrength==4&&w.particles==1&&w.CaptureTnt().Length==0,"update eighty-one detonates once with shared blast particles");w.TntTick();Assert(w.blasts==1,"expired entity removed before blast");
  w=Ready();w.Put(0,1,0,BlockType.TNT);int beforeHiss=GameAudio.hisses;w.PrimeTnt(new Vector3Int(0,1,0),true);state=w.CaptureTnt();Assert(state[0].fuse>=10&&state[0].fuse<30,"chain fuse is 10 to 29 ticks");Assert(GameAudio.hisses==beforeHiss,"blast chains do not add an ignition hiss");
  var restored=Ready();restored.RestoreTnt(state);state[0].fuse=80;Assert(restored.CaptureTnt()[0].fuse!=80,"restore owns a copy");var copy=restored.CaptureTnt();copy[0].fuse=0;Assert(restored.CaptureTnt()[0].fuse>0,"save snapshot owns a copy");
  restored.RestoreTnt(new[]{new PrimedTntState{id=1,fuse=5,position=new Vector3(.5f,1,.5f)},new PrimedTntState{id=1,fuse=5},new PrimedTntState{id=2,fuse=81},new PrimedTntState{id=3,position=new Vector3(float.NaN,0,0)}});Assert(restored.CaptureTnt().Length==1,"invalid and duplicate saves rejected");
  restored.worldData.chunkDict.Clear();restored.TntTick();Assert(restored.CaptureTnt()[0].fuse==5,"unloaded chunk suspends fuse and physics");restored.RestoreTnt(null);Assert(restored.CaptureTnt().Length==0,"old saves load without TNT");
  w=Ready();w.DropAuthority=false;w.Put(0,1,0,BlockType.TNT);Assert(!w.PrimeTnt(new Vector3Int(0,1,0)),"remote clients cannot prime authoritatively");w.DropAuthority=true;w.isSaving=true;Assert(!w.PrimeTnt(new Vector3Int(0,1,0)),"saving rejects ignition without removing block");
  w=Ready();w.Put(0,1,0,BlockType.TNT);var held=new InventoryStack(ItemType.FlintAndSteel,1);var cell=new Vector3Int(0,1,0);
  Assert(!w.UseFarmItem(cell,cell,ref held)&&held.damage==0,"invalid hit face does not prime or wear tool");
  Assert(w.UseFarmItem(cell,cell+Vector3Int.up,ref held)&&held.damage==1&&w.CaptureTnt().Length==1,"actual flint-and-steel branch primes TNT and wears once");
  Assert(!w.UseFarmItem(cell,cell+Vector3Int.up,ref held)&&held.damage==1,"duplicate TNT ignition cannot wear tool again");
  var high=Ready();high.Put(0,127,0,BlockType.TNT);Assert(high.PrimeTnt(new Vector3Int(0,127,0)),"TNT can be primed at the build limit");high.TntTick();Assert(high.CaptureTnt()[0].fuse==79,"top-layer hop does not suspend fuse");
  w=Ready();var many=new List<PrimedTntState>();for(int i=1;i<=140;i++)many.Add(new PrimedTntState{id=i,fuse=0,position=new Vector3(.5f,1,.5f)});w.RestoreTnt(many.ToArray());Assert(w.CaptureTnt().Length==128,"bounded primed entity store");w.TntTick();Assert(w.blasts==4&&w.CaptureTnt().Length==124,"chain burst work budget");
  Console.WriteLine(checks+" primed TNT fuse/physics/authority/save/chain checks passed (Unity/world blast adapter mocked).");
 }
}

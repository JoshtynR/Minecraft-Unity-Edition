using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
namespace UnityEngine {
 public class MonoBehaviour {}
 public class HideInInspectorAttribute:Attribute {}
 public class HeaderAttribute:Attribute { public HeaderAttribute(string s){} }
 public class TooltipAttribute:Attribute { public TooltipAttribute(string s){} }
 public struct Vector3Int { public int x,y,z; public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;} }
 public struct Vector3 {}
 public static class Application { public static string dataPath="/tmp"; }
 public static class Debug { public static void Log(string s){} }
}
public class Block {public ChunkData chunkData; public BlockType type; public Block(BlockType t){type=t;} }
public class World {
 public long betaWorldSeed;public int worldHeight=240;public int meshNotifications;
 public Dictionary<UnityEngine.Vector3Int,ChunkData> chunks=new Dictionary<UnityEngine.Vector3Int,ChunkData>();
 public void NotifyTreeChunkChanged(UnityEngine.Vector3Int p){meshNotifications++;}
 public Block GetBlock(UnityEngine.Vector3Int p){
  if(p.y<0||p.y>=worldHeight)return new Block(BlockType.Air);
  var origin=new UnityEngine.Vector3Int((int)Math.Floor(p.x/16.0)*16,0,(int)Math.Floor(p.z/16.0)*16);ChunkData c;
  if(!chunks.TryGetValue(origin,out c))return new Block(BlockType.Nothing);
  return new Block(c.blocks[p.x-origin.x,p.y,p.z-origin.z]){chunkData=c};
 }
}
public class TreeData {}
public class ChunkData {
 public bool betaTreePopulationDone,betaPopulationDone,modifiedAfterSave;public List<BetaPopulationRecord> betaPopulationRecords=new List<BetaPopulationRecord>();public int chunkSize=16,chunkHeight=16;public World worldRef;public UnityEngine.Vector3Int worldPos;public TreeData treeData;
 public BlockType[,,] blocks=new BlockType[16,240,16];
 public void SetBlock(UnityEngine.Vector3Int p,BlockType b){blocks[p.x,p.y,p.z]=b;}
 public Block GetBlock(UnityEngine.Vector3Int p){return new Block(blocks[p.x,p.y,p.z]);}
}
public static class WorldDataHelper { public static void SetBlock(World w,UnityEngine.Vector3Int p,BlockType b){var block=w.GetBlock(p);if(block.chunkData!=null){var c=block.chunkData;c.blocks[p.x-c.worldPos.x,p.y,p.z-c.worldPos.z]=b;}} }
public class IntegrationCheck {
 public static int Main(string[] args) {
  int checks=0;
  foreach(string line in File.ReadAllLines(args[0])) {
   if(line.StartsWith("#"))continue;string[] f=line.Split('\t');if(f[3]!="caves")continue;
   long seed=long.Parse(f[0]);int cx=int.Parse(f[1]),cz=int.Parse(f[2]);
   var w=new World{betaWorldSeed=seed};var data=new ChunkData{worldRef=w,worldPos=new UnityEngine.Vector3Int((-cx-1)*16,0,cz*16)};
   var generator=new TerrainGenerator{biomeGenerator=new BiomeGenerator(),dumpBetaParity=false};generator.GenerateChunkData(data,new UnityEngine.Vector3Int());
   var canonical=new BlockType[16,128,16];
   for(int x=0;x<16;x++)for(int z=0;z<16;z++) {
    for(int y=0;y<128;y++)canonical[x,y,z]=data.blocks[15-x,y,z];
    for(int y=128;y<240;y++)if(data.blocks[x,y,z]!=BlockType.Air)throw new Exception("Air ceiling mismatch");
   }
   string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(BetaParityCheck.BlockBytes(canonical))).Replace("-","").ToLowerInvariant();
   if(hash!=f[4])throw new Exception("Integration mismatch "+line);checks++;
  }
  var populationWorld=new World{betaWorldSeed=17};var owners=new List<ChunkData>();
  for(int x=-1;x<=2;x++)for(int z=-1;z<=2;z++){
   var c=new ChunkData{worldRef=populationWorld,worldPos=new UnityEngine.Vector3Int(-16-x*16,0,z*16)};
   for(int xx=0;xx<16;xx++)for(int zz=0;zz<16;zz++)for(int y=0;y<240;y++)c.blocks[xx,y,zz]=y<63?BlockType.Stone:y==63?BlockType.Grass:BlockType.Air;
   owners.Add(c);
  }
  for(int i=0;i<15;i++)populationWorld.chunks[owners[i].worldPos]=owners[i];
  var owner=owners[5];
  var full=new TerrainGenerator{enableBetaTrees=false};
  full.GenerateFeatures(owner,new UnityEngine.Vector3Int());
  if(owner.betaPopulationDone)throw new Exception("population ran without complete neighborhood");checks++;
  populationWorld.chunks[owners[15].worldPos]=owners[15];full.GenerateFeatures(owner,new UnityEngine.Vector3Int());
  if(!owner.betaPopulationDone||!owner.betaTreePopulationDone)throw new Exception("complete population did not run/resume");checks++;
  int ore=0;foreach(var c in owners)foreach(var b in c.blocks)if(b==BlockType.CoalOre||b==BlockType.IronOre||b==BlockType.DiamondOre)ore++;
  if(ore==0||populationWorld.meshNotifications==0)throw new Exception("population omitted ore/dirty neighbors");checks++;
  int before=populationWorld.meshNotifications;full.GenerateFeatures(owner,new UnityEngine.Vector3Int());
  if(before!=populationWorld.meshNotifications)throw new Exception("complete population ran twice");checks++;
  Console.WriteLine(checks+" production TerrainGenerator boundary checks passed (Unity storage mocked).");return 0;
 }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
namespace UnityEngine {
 public class MonoBehaviour {}
 public class HideInInspectorAttribute:Attribute {}
 public class HeaderAttribute:Attribute { public HeaderAttribute(string s){} }
 public class TooltipAttribute:Attribute { public TooltipAttribute(string s){} }
 public struct Vector3Int {
 public static Vector3Int left=>new Vector3Int(-1,0,0);public static Vector3Int right=>new Vector3Int(1,0,0);public static Vector3Int up=>new Vector3Int(0,1,0);public static Vector3Int down=>new Vector3Int(0,-1,0);
 public static Vector3Int operator+(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);
 public int x,y,z; public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;} }
 public struct Vector3 {}
 public static class Application { public static string dataPath="/tmp"; }
 public static class Debug { public static void Log(string s){} }
}
public class Block {public ChunkData chunkData; public BlockType type; public Block(BlockType t){type=t;} }
public class World {public WorldDimension dimension;
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
public class ChunkData {
 public bool betaPopulationDone,modifiedAfterSave;public List<BetaPopulationRecord> betaPopulationRecords=new List<BetaPopulationRecord>();public int chunkSize=16,chunkHeight=16;public World worldRef;public UnityEngine.Vector3Int worldPos;
 public BlockType[,,] blocks=new BlockType[16,240,16];
 public void SetBlock(UnityEngine.Vector3Int p,BlockType b){blocks[p.x,p.y,p.z]=b;}
 public void InitializeGeneratedBlocks(BlockType[,,] source,bool reflectX,int sourceHeight){for(int x=0;x<16;x++)for(int z=0;z<16;z++)for(int y=0;y<240;y++)blocks[x,y,z]=y<sourceHeight?source[reflectX?15-x:x,y,z]:BlockType.Air;}
 public Block GetBlock(UnityEngine.Vector3Int p){return new Block(blocks[p.x,p.y,p.z]);}
}
public static class WorldDataHelper { public static void SetBlock(World w,UnityEngine.Vector3Int p,BlockType b){var block=w.GetBlock(p);if(block.chunkData!=null){var c=block.chunkData;c.blocks[p.x-c.worldPos.x,p.y,p.z-c.worldPos.z]=b;}} }
public class IntegrationCheck {
 public static int Main(string[] args) {
  int checks=0;
  var sharedGenerator=new TerrainGenerator();
  var terrainCases=new List<string[]>();
  foreach(string line in File.ReadAllLines(args[0])) {
   if(line.StartsWith("#"))continue;string[] f=line.Split('\t');if(f[3]!="caves")continue;
   long seed=long.Parse(f[0]);int cx=int.Parse(f[1]),cz=int.Parse(f[2]);
   var w=new World{betaWorldSeed=seed};var data=new ChunkData{worldRef=w,worldPos=new UnityEngine.Vector3Int((-cx-1)*16,0,cz*16)};
   var generator=new TerrainGenerator{dumpBetaParity=false};generator.GenerateChunkData(data);
   var canonical=new BlockType[16,128,16];
   for(int x=0;x<16;x++)for(int z=0;z<16;z++) {
    for(int y=0;y<128;y++)canonical[x,y,z]=data.blocks[15-x,y,z];
    for(int y=128;y<240;y++)if(data.blocks[x,y,z]!=BlockType.Air)throw new Exception("Air ceiling mismatch");
   }
   string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(BetaParityCheck.BlockBytes(canonical))).Replace("-","").ToLowerInvariant();
   if(hash!=f[4])throw new Exception("Integration mismatch "+line);checks++;
   terrainCases.Add(f);
  }
  // Exercise cold parallel startup, cache reuse, and switching seeds on one
  // production component. Each chunk must still match its own reference seed.
  Parallel.ForEach(terrainCases, f => {
   var world=new World{betaWorldSeed=long.Parse(f[0])};
   int cx=int.Parse(f[1]),cz=int.Parse(f[2]);
   var chunk=new ChunkData{worldRef=world,worldPos=new UnityEngine.Vector3Int((-cx-1)*16,0,cz*16)};
   sharedGenerator.GenerateChunkData(chunk);
   var canonical=new BlockType[16,128,16];
   for(int x=0;x<16;x++)for(int z=0;z<16;z++)for(int y=0;y<128;y++)canonical[x,y,z]=chunk.blocks[15-x,y,z];
   string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(BetaParityCheck.BlockBytes(canonical))).Replace("-","").ToLowerInvariant();
   if(hash!=f[4])throw new Exception("Parallel seed mismatch "+string.Join("/",f));
  });
  checks+=terrainCases.Count;
  foreach(var invalid in new[]{new ChunkData{chunkSize=8,worldRef=new World()},new ChunkData{worldRef=new World{worldHeight=127}}}) {
   bool rejected=false;try{sharedGenerator.GenerateChunkData(invalid);}catch(InvalidOperationException){rejected=true;}
   if(!rejected)throw new Exception("Invalid Beta world geometry accepted");checks++;
  }
  var populationWorld=new World{betaWorldSeed=17};var owners=new List<ChunkData>();
  for(int x=-1;x<=2;x++)for(int z=-1;z<=2;z++){
   var c=new ChunkData{worldRef=populationWorld,worldPos=new UnityEngine.Vector3Int(-16-x*16,0,z*16)};
   for(int xx=0;xx<16;xx++)for(int zz=0;zz<16;zz++)for(int y=0;y<240;y++)c.blocks[xx,y,zz]=y<63?BlockType.Stone:y==63?BlockType.Grass:BlockType.Air;
   owners.Add(c);
  }
  for(int i=0;i<15;i++)populationWorld.chunks[owners[i].worldPos]=owners[i];
  var owner=owners[5];
  var full=new TerrainGenerator();
  full.PopulateChunk(owner);
  if(owner.betaPopulationDone)throw new Exception("population ran without complete neighborhood");checks++;
  populationWorld.chunks[owners[15].worldPos]=owners[15];full.PopulateChunk(owner);
  if(!owner.betaPopulationDone)throw new Exception("complete population did not run/resume");checks++;
  int ore=0;foreach(var c in owners)foreach(var b in c.blocks)if(b==BlockType.CoalOre||b==BlockType.IronOre||b==BlockType.DiamondOre)ore++;
  if(ore==0||populationWorld.meshNotifications==0)throw new Exception("population omitted ore/dirty neighbors");checks++;
  int before=populationWorld.meshNotifications;full.PopulateChunk(owner);
  if(before!=populationWorld.meshNotifications)throw new Exception("complete population ran twice");checks++;
  if(args.Length>1)foreach(string line in File.ReadAllLines(args[1]))
  {
   var row=line.Split('\t');if(row[3]!="caves")continue;
   var world=new World{betaWorldSeed=long.Parse(row[0]),dimension=WorldDimension.Nether};
   int cx=int.Parse(row[1]),cz=int.Parse(row[2]);
   var chunk=new ChunkData{worldRef=world,worldPos=new UnityEngine.Vector3Int((-cx-1)*16,0,cz*16)};
   sharedGenerator.GenerateChunkData(chunk);
   var bytes=new byte[32768];
   for(int x=0;x<16;x++)for(int z=0;z<16;z++)for(int y=0;y<128;y++)
   {
    var type=chunk.blocks[15-x,y,z];BetaBlockCatalog.Entry entry;
    bytes[(x*16+z)*128+y]=type==BlockType.Air?(byte)0:BetaBlockCatalog.TryGet(type,out entry)?(byte)entry.SourceId:(byte)255;
   }
   string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
   if(hash!=row[4])throw new Exception("Nether adapter mismatch "+line);
   for(int x=0;x<16;x++)for(int z=0;z<16;z++)for(int y=128;y<240;y++)if(chunk.blocks[x,y,z]!=BlockType.Air)throw new Exception("Nether ceiling above height");
   checks++;
  }
  Console.WriteLine(checks+" production TerrainGenerator boundary checks passed (Unity storage mocked).");return 0;
 }
}

public enum WorldDimension {Nether=-1,Overworld=0}

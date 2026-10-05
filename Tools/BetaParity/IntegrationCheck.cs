using System;
using System.IO;
using System.Security.Cryptography;
namespace UnityEngine {
 public class MonoBehaviour {}
 public class HeaderAttribute:Attribute { public HeaderAttribute(string s){} }
 public class TooltipAttribute:Attribute { public TooltipAttribute(string s){} }
 public struct Vector3Int { public int x,y,z; public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;} }
 public struct Vector3 {}
 public static class Application { public static string dataPath="/tmp"; }
 public static class Debug { public static void Log(string s){} }
}
public class Block { public BlockType type; public Block(BlockType t){type=t;} }
public class World {public long betaWorldSeed;public int worldHeight=240;public Block GetBlock(UnityEngine.Vector3Int p){return new Block(BlockType.Air);} }
public class TreeData {}
public class ChunkData {
 public int chunkSize=16,chunkHeight=16;public World worldRef;public UnityEngine.Vector3Int worldPos;public TreeData treeData;
 public BlockType[,,] blocks=new BlockType[16,240,16];
 public void SetBlock(UnityEngine.Vector3Int p,BlockType b){blocks[p.x,p.y,p.z]=b;}
 public Block GetBlock(UnityEngine.Vector3Int p){return new Block(blocks[p.x,p.y,p.z]);}
}
public static class WorldDataHelper { public static void SetBlock(World w,UnityEngine.Vector3Int p,BlockType b){} }
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
  Console.WriteLine(checks+" production TerrainGenerator boundary checks passed (Unity storage mocked).");return 0;
 }
}

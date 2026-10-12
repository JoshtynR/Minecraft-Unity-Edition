using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
namespace UnityEngine
{
 public struct Vector3Int
 {
  public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}
  public static Vector3Int up=>new Vector3Int(0,1,0);public static Vector3Int down=>new Vector3Int(0,-1,0);public static Vector3Int left=>new Vector3Int(-1,0,0);public static Vector3Int right=>new Vector3Int(1,0,0);public static Vector3Int forward=>new Vector3Int(0,0,1);public static Vector3Int back=>new Vector3Int(0,0,-1);
  public static Vector3Int operator +(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3Int operator *(Vector3Int a,int b)=>new Vector3Int(a.x*b,a.y*b,a.z*b);
  public static explicit operator Vector3(Vector3Int a)=>new Vector3(a.x,a.y,a.z);
 }
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Color {}
 public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));}
}
public enum Direction {down,up,backwards,forwards,left,right}
public static class DirectionExt {public static Vector3Int GetVector(this Direction d){return new[]{Vector3Int.down,Vector3Int.up,Vector3Int.back,Vector3Int.forward,Vector3Int.left,Vector3Int.right}[(int)d];}}
public class ShapeSlot {public BlockShape shape=new BlockShape(0,0,0,16,16,16);public BlockShape GetShape()=>shape;}
public class BlockTypeData {public byte opacity,lightEmission;}
public class Block
{
 public BlockType type;public BlockTypeData BlockData=new BlockTypeData();public int sky,light;public ChunkData chunkData;public Vector3Int localChunkPosition;public Vector3Int globalWorldPosition=>localChunkPosition;public ShapeSlot blockShape=new ShapeSlot();
 public int GetSkyLight()=>sky;public int GetBlockLight()=>light;public void SetSkyLight(int v){sky=v;}public void SetBlockLight(int v){light=v;}
 public Block[] GetNeighbors(){return new[]{Direction.up,Direction.down,Direction.left,Direction.right,Direction.forwards,Direction.backwards}.Select(d=>chunkData.GetBlock(localChunkPosition+d.GetVector())).ToArray();}
}
public class BlockLightNode {public Block block;public byte lightLevel;public BlockLightNode(Block b,byte l){block=b;lightLevel=l;}}
public class ChunkRenderer {}
public class World {public int worldHeight=128,betaSkylightSubtracted,updates;public void AddChunkToUpdate(ChunkRenderer c,bool first){updates++;}}
public class ChunkData
{
 public int chunkSize=1;public World worldRef=new World();public ChunkRenderer renderer;
 public Dictionary<Vector3Int,Block> blocks=new Dictionary<Vector3Int,Block>();
 public Queue<BlockLightNode> skyLightRemoveQueue=new Queue<BlockLightNode>(),skyLightUpdateQueue=new Queue<BlockLightNode>(),blockLightRemoveQueue=new Queue<BlockLightNode>(),blockLightUpdateQueue=new Queue<BlockLightNode>();
 public List<Block> skyExtendList=new List<Block>(),skyRemoveList=new List<Block>();public List<ChunkRenderer> chunkToUpdateAfterLighting=new List<ChunkRenderer>();
 public Block GetBlock(Vector3Int p){Block b;return blocks.TryGetValue(p,out b)?b:new Block{type=BlockType.Nothing,localChunkPosition=p,chunkData=this};}
 public bool IsOnEdge(Vector3Int p)=>false;public List<ChunkData> GetNeighbourChunk(Vector3Int p)=>new List<ChunkData>();
}
public static class LightingCheck
{
 static int checks,aoCases;static readonly CultureInfo C=CultureInfo.InvariantCulture;
 static void Check(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
 static float F(string s)=>float.Parse(s,C);
 static Block Add(ChunkData c,Vector3Int pos,BlockType type=BlockType.Air,int opacity=0,int emission=0){var b=new Block{chunkData=c,localChunkPosition=pos,type=type,BlockData=new BlockTypeData{opacity=(byte)opacity,lightEmission=(byte)emission}};c.blocks[pos]=b;return b;}
 public static void Main(string[] args)
 {
  ChunkData chunk=null;Vector3Int center=new Vector3Int(-1,65,17);
  foreach(var line in File.ReadLines(args[0]))
  {
   if(line.StartsWith("#"))continue;var fields=line.Split('\t');
   if(fields[0]=="B")Check(Math.Abs(Beta173Lighting.Brightness(int.Parse(fields[1]),0)-F(fields[2]))<0.000001f,"brightness table");
   if(fields[0]=="W")
   {
    chunk=new ChunkData();chunk.worldRef.betaSkylightSubtracted=int.Parse(fields[2]);int emission=int.Parse(fields[3]);int i=0;
    for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)
    {var p=fields[4+i++].Split(',');var b=Add(chunk,center+new Vector3Int(x,y,z),p[2]=="1"?BlockType.Air:BlockType.Stone);b.sky=int.Parse(p[0]);b.light=int.Parse(p[1]);}
    chunk.GetBlock(center).type=BlockType.Stone;chunk.GetBlock(center).BlockData.lightEmission=(byte)emission;
   }
   if(fields[0]=="A")
   {
    int face=int.Parse(fields[2]);var dir=(Direction)face;var mesh=new MeshData(false);
    chunk.GetBlock(center).blockShape.GetShape().SetFaceVertices(dir,center,mesh);
    BlockHelper.GetFaceLightingAndAO(dir,center,mesh,chunk);
    float shade=face==0?0.5f:face==1?1f:face<4?0.8f:0.6f;
    var values=mesh.sides.Select(v=>v.x*shade).OrderBy(v=>v).ToArray();
    for(int i=0;i<4;i++)Check(Math.Abs(values[i]-F(fields[3+i]))<0.000002f,"Java RenderBlocks corner set case "+fields[1]+" face "+face);
    Check(mesh.skyLight.Count==4&&mesh.blockLight.Count==4&&mesh.sides.All(v=>v.z==1),"production vertex channel alignment");aoCases++;
   }
  }
  // Actual propagation adapter: a mixed-opacity corridor crossing chunk ownership.
  var corridor=new ChunkData();corridor.worldRef.worldHeight=8;
  for(int x=0;x<6;x++)Add(corridor,new Vector3Int(x,2,0));
  var source=corridor.GetBlock(new Vector3Int(0,2,0));source.BlockData.lightEmission=15;source.light=15;
  corridor.GetBlock(new Vector3Int(2,2,0)).BlockData.opacity=3;
  corridor.GetBlock(new Vector3Int(4,2,0)).BlockData.opacity=1;
  var neighborChunk=new ChunkData{worldRef=corridor.worldRef,renderer=new ChunkRenderer(),blocks=corridor.blocks};
  corridor.GetBlock(new Vector3Int(3,2,0)).chunkData=neighborChunk;
  corridor.blockLightUpdateQueue.Enqueue(new BlockLightNode(source,15));Lighting.CalculateLight(corridor);
  var expected=new[]{15,14,11,10,9,8};for(int x=0;x<6;x++)Check(corridor.GetBlock(new Vector3Int(x,2,0)).light==expected[x],"destination opacity/cross-chunk propagation");
  Check(corridor.worldRef.updates>0,"cross-chunk mesh notification");
  source.light=0;source.BlockData.lightEmission=0;corridor.blockLightRemoveQueue.Enqueue(new BlockLightNode(source,15));Lighting.CalculateLight(corridor);
  Check(corridor.blocks.Values.All(b=>b.light==0),"source removal without queued light resurrection");
  var column=new ChunkData();column.worldRef.worldHeight=8;
  for(int y=0;y<8;y++)Add(column,new Vector3Int(0,y,0));
  column.GetBlock(new Vector3Int(0,5,0)).BlockData.opacity=1;column.GetBlock(new Vector3Int(0,3,0)).BlockData.opacity=3;column.GetBlock(new Vector3Int(0,1,0)).BlockData.opacity=3;column.GetBlock(new Vector3Int(0,0,0)).BlockData.opacity=15;
  Lighting.RecastSunLight(column,new Vector3Int(0,7,0));expected=new[]{0,8,11,11,14,14,15,15};for(int y=0;y<8;y++)Check(column.GetBlock(new Vector3Int(0,y,0)).sky==expected[y],"initial Beta column attenuation");
  Lighting.RecastSunLight(column,new Vector3Int(0,7,0),true);expected=new[]{0,6,9,10,13,14,15,15};for(int y=0;y<8;y++)Check(column.GetBlock(new Vector3Int(0,y,0)).sky==expected[y],"changed Beta column attenuation");
  Check(Beta173Lighting.Brightness(15,0,11)==Beta173Lighting.Brightness(4,0),"night sky subtraction");
  Check(Beta173Lighting.Brightness(0,15,11)==1f,"block light remains bright at night");
  Check(Beta173Lighting.Brightness(0,0)==0.05f,"Beta ambient floor");
  Console.WriteLine(aoCases+" Java six-face AO cases; "+checks+" brightness/adapter/transport checks passed (Unity storage mocked).");
 }
}

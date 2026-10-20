using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
namespace UnityEngine
{
 public class Texture2D {}
 public struct Vector3Int
 {
  public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}
  public static Vector3Int up=>new Vector3Int(0,1,0);public static Vector3Int down=>new Vector3Int(0,-1,0);public static Vector3Int left=>new Vector3Int(-1,0,0);public static Vector3Int right=>new Vector3Int(1,0,0);public static Vector3Int forward=>new Vector3Int(0,0,1);public static Vector3Int back=>new Vector3Int(0,0,-1);
  public static Vector3Int operator +(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3Int operator *(Vector3Int a,int b)=>new Vector3Int(a.x*b,a.y*b,a.z*b);
  public static explicit operator Vector3(Vector3Int a)=>new Vector3(a.x,a.y,a.z);
 }
 public struct Vector3 {public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;}
 public struct Vector4 { public float x,y,z,w; public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;} }
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public struct Color32 {public byte r,g,b,a;public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));}
}
public enum Direction {down,up,backwards,forwards,left,right}
public static class DirectionExt {public static Direction GetOpposite(this Direction d)=>new[]{Direction.up,Direction.down,Direction.forwards,Direction.backwards,Direction.right,Direction.left}[(int)d];public static Vector3Int GetVector(this Direction d){return new[]{Vector3Int.down,Vector3Int.up,Vector3Int.back,Vector3Int.forward,Vector3Int.left,Vector3Int.right}[(int)d];}}
public class ShapeSlot {public BlockShapes orientation;public static implicit operator BlockShapes(ShapeSlot s)=>s.orientation;public BlockShape shape=new BlockShape(0,0,0,16,16,16);public BlockShape GetShape()=>shape;}
public class TextureData
{
 public Dictionary<BlockTexturePart,int> parts=new Dictionary<BlockTexturePart,int>();
 public int FaceIndex(Direction d)=>PartIndex(BlockTexturePart.Surface);
 public int FaceIndex(BlockType t,Direction d)=>t==BlockType.Water||t==BlockType.Lava?PartIndex(d==Direction.up||d==Direction.down?BlockTexturePart.FluidStill:BlockTexturePart.FluidFlow,FaceIndex(d)):FaceIndex(d);
 public int PartIndex(BlockTexturePart p,int fallback=0){int value;return parts.TryGetValue(p,out value)&&value!=0?value:fallback;}
}
public static class BlockDataManager {public static BlockTypeData[] blockTypeDataDictionary=new BlockTypeData[133];}
public class BlockTypeData {public bool isTransparent;public byte opacity,lightEmission;public TextureData textureData=new TextureData();public Color albedoTint;}
public class Block
{
 public byte waterLevel;public BlockType type;public BlockTypeData BlockData=new BlockTypeData();public int sky,light;public ChunkData chunkData;public Vector3Int localChunkPosition;public Vector3Int globalWorldPosition=>chunkData.worldPos+localChunkPosition;public ShapeSlot blockShape=new ShapeSlot();
 public int GetSkyLight()=>sky;public int GetBlockLight()=>light;public void SetSkyLight(int v){sky=v;}public void SetBlockLight(int v){light=v;}
 public Block[] GetNeighbors(){return new[]{Direction.up,Direction.down,Direction.left,Direction.right,Direction.forwards,Direction.backwards}.Select(d=>chunkData.GetBlock(localChunkPosition+d.GetVector())).ToArray();}
}
public class BlockLightNode {public Block block;public byte lightLevel;public BlockLightNode(Block b,byte l){block=b;lightLevel=l;}}
public class ChunkRenderer {}
public partial class World {public long betaWorldSeed;public int worldHeight=128,betaSkylightSubtracted,updates;public bool fullSmoothLighting=true;public void AddChunkToUpdate(ChunkRenderer c,bool first){updates++;}}
public static class ModernChestVisual {public static bool Enabled;}
public class ChunkData
{
 internal volatile BiomeBlockColors.ChunkColors biomeColors;
 public Vector3Int worldPos;public Vector3Int GetGlobalBlockCoords(Vector3Int p)=>worldPos+p;
 public bool lightingInitialized;
 public bool HasPendingLighting=>skyLightRemoveQueue.Count+skyLightUpdateQueue.Count+blockLightRemoveQueue.Count+blockLightUpdateQueue.Count+skyRemoveList.Count+skyExtendList.Count+chunkToUpdateAfterLighting.Count>0;
 public void UpdateLight(){Lighting.CalculateSkyLightRemove(this);Lighting.CalculateSkyLightExtend(this);Lighting.CalculateLight(this);}
 public int chunkSize=1;public World worldRef=new World();public ChunkRenderer renderer;
 public Dictionary<Vector3Int,Block> blocks=new Dictionary<Vector3Int,Block>();
 public Queue<BlockLightNode> skyLightRemoveQueue=new Queue<BlockLightNode>(),skyLightUpdateQueue=new Queue<BlockLightNode>(),blockLightRemoveQueue=new Queue<BlockLightNode>(),blockLightUpdateQueue=new Queue<BlockLightNode>();
 public List<Block> skyExtendList=new List<Block>(),skyRemoveList=new List<Block>();public List<ChunkRenderer> chunkToUpdateAfterLighting=new List<ChunkRenderer>();
 public Block GetBlock(Vector3Int p){Block b;return blocks.TryGetValue(worldPos+p,out b)?b:new Block{type=BlockType.Nothing,localChunkPosition=p,chunkData=this};}
 public bool IsOnEdge(Vector3Int p)=>false;public List<ChunkData> GetNeighbourChunk(Vector3Int p)=>new List<ChunkData>();
}
public static partial class LightingCheck
{
 static int checks,aoCases;static readonly CultureInfo C=CultureInfo.InvariantCulture;
 static void Check(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
 static float F(string s)=>float.Parse(s,C);
 static Block Add(ChunkData c,Vector3Int pos,BlockType type=BlockType.Air,int opacity=0,int emission=0){var b=new Block{chunkData=c,localChunkPosition=pos,type=type,BlockData=new BlockTypeData{opacity=(byte)opacity,lightEmission=(byte)emission}};c.blocks[c.worldPos+pos]=b;return b;}
 public static void Main(string[] args)
 {
  ChunkData chunk=null;MeshData lastFaceMesh=null;int orderedVertices=0;Vector3Int center=new Vector3Int(-1,65,17);
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
    Check(mesh.skyLight.Count==4&&mesh.blockLight.Count==4&&mesh.sides.All(v=>v.z==2),"production vertex channel alignment");aoCases++;lastFaceMesh=mesh;
   }
   if(fields[0]=="O")
   {
    int face=int.Parse(fields[2]),x=int.Parse(fields[3]),y=int.Parse(fields[4]),z=int.Parse(fields[5]);
    int vertex=-1;
    for(int i=0;i<4;i++)
    {var v=lastFaceMesh.vertices[i];if((int)Math.Round(v.x-center.x-0.0005f)==x&&(int)Math.Round(v.y-center.y-0.0005f)==y&&(int)Math.Round(v.z-center.z-0.0005f)==z)vertex=i;}
    Check(vertex>=0,"reference position exists in Unity quad");
    float shade=face==0?0.5f:face==1?1f:face<4?0.8f:0.6f;
    Check(Math.Abs(lastFaceMesh.sides[vertex].x*shade-F(fields[6]))<0.000002f,"positioned Java vertex color");
    var q=lastFaceMesh.smoothLightCorners[vertex];
    float[] cornerValues={q.x,q.y,q.z,q.w};Check(Math.Abs(cornerValues[vertex]-lastFaceMesh.sides[vertex].x)<0.000001f,"UV winding maps to same light corner");
    orderedVertices++;
   }
  }
  // Production face routing: only water enters the blended draw; its neighbors
  // still emit the terrain seen through it. Leaves/glass keep cutout geometry.
  for(int i=0;i<133;i++)BlockDataManager.blockTypeDataDictionary[i]=new BlockTypeData{isTransparent=true};
  BlockDataManager.blockTypeDataDictionary[(int)BlockType.Stone].isTransparent=false;
  foreach(var liquid in new[]{BlockType.Water,BlockType.Lava})
  {
   var liquidChunk=new ChunkData();var liquidPos=new Vector3Int(0,65,0);Add(liquidChunk,liquidPos,liquid);
   var upload=BlockHelper.GetMeshData(liquidChunk,liquidPos,new MeshData(true),liquid);upload.PrepareForRendering();
   Check(upload.vertices.Count==24&&upload.faceCoordinates.Count==24&&upload.uv.Count==24&&upload.lightingUVs.Count==24,
    "all six "+liquid+" faces retain matching UV0/UV5/lighting vertex counts after upload preparation");
  }
  var renderChunk=new ChunkData();var renderPos=new Vector3Int(0,65,0);Add(renderChunk,renderPos,BlockType.Water);
  var renderMesh=BlockHelper.GetMeshData(renderChunk,renderPos,new MeshData(true),BlockType.Water);
  Check(renderMesh.waterMesh.vertices.Count==24&&renderMesh.transparentMesh.vertices.Count==0&&renderMesh.vertices.Count==0,"water has dedicated blended submesh");
  Add(renderChunk,renderPos+Vector3Int.right,BlockType.Water);
  renderMesh=BlockHelper.GetMeshData(renderChunk,renderPos,new MeshData(true),BlockType.Water);
  Check(renderMesh.waterMesh.vertices.Count==20,"adjacent water suppresses internal faces");
  var stonePos=renderPos+Vector3Int.down;Add(renderChunk,stonePos,BlockType.Stone);
  renderMesh=BlockHelper.GetMeshData(renderChunk,stonePos,new MeshData(true),BlockType.Stone);
  Check(renderMesh.vertices.Count==24&&renderMesh.waterMesh.vertices.Count==0,"terrain beneath water remains rendered");
  foreach(var cutout in new[]{BlockType.Leaves,BlockType.Glass})
  {
   Add(renderChunk,renderPos,cutout);
   renderMesh=BlockHelper.GetMeshData(renderChunk,renderPos,new MeshData(true),cutout);
   Check(renderMesh.transparentMesh.vertices.Count>0&&renderMesh.waterMesh.vertices.Count==0,"cutout blocks stay out of blended water pass");
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
  // Both rendered data and the renderer-less halo must be seeded before meshes.
  var visible=new ChunkData{renderer=new ChunkRenderer()};var halo=new ChunkData{worldPos=new Vector3Int(1,0,0),blocks=visible.blocks};
  visible.worldRef.worldHeight=halo.worldRef.worldHeight=8;
  foreach(var data in new[]{visible,halo})
   for(int y=0;y<8;y++)Add(data,new Vector3Int(0,y,0),y==0?BlockType.Stone:BlockType.Air,y==0?15:0);
  visible.GetBlock(new Vector3Int(0,6,0)).BlockData.opacity=15;
  Lighting.InitializeLoadedChunks(new[]{visible,halo});
  Check(visible.GetBlock(new Vector3Int(0,5,0)).sky==14,"halo propagates sideways below visible roof before first mesh");
  Check(visible.lightingInitialized&&halo.lightingInitialized,"renderer-less lighting halo initialized");
  Check(halo.GetBlock(new Vector3Int(0,7,0)).sky==15,"halo sky available before first geometry");
  halo.GetBlock(new Vector3Int(0,7,0)).sky=3;
  Lighting.InitializeLoadedChunks(new[]{visible,halo});
  Check(halo.GetBlock(new Vector3Int(0,7,0)).sky==3,"unchanged halo not repeatedly reseeded");
  halo.lightingInitialized=false;
  Lighting.InitializeLoadedChunks(new[]{visible,halo});
  Check(halo.GetBlock(new Vector3Int(0,7,0)).sky==15,"population invalidation reseeds halo");
  var cancelled=new System.Threading.CancellationToken(true);bool cancelledThrow=false;
  try{Lighting.InitializeLoadedChunks(new[]{new ChunkData()},cancelled);}catch(OperationCanceledException){cancelledThrow=true;}
  Check(cancelledThrow,"initial lighting respects cancellation");
  Check(Beta173Lighting.Brightness(15,0,11)==Beta173Lighting.Brightness(4,0),"night sky subtraction");
  Check(Beta173Lighting.Brightness(0,15,11)==1f,"block light remains bright at night");
  Check(Beta173Lighting.Brightness(0,0)==0.05f,"Beta ambient floor");
  // Partial side faces interpolate at actual heights, rather than snapping every
  // top vertex to a full-height corner. Use an independently known vertical ramp.
  var smoothChunk=new ChunkData();var position=new Vector3Int(0,65,0);
  for(int x=-2;x<=2;x++)for(int y=-2;y<=2;y++)for(int z=-2;z<=2;z++)
  {var block=Add(smoothChunk,position+new Vector3Int(x,y,z));block.light=y<0?2:y==0?6:14;}
  var partial=smoothChunk.GetBlock(position);partial.type=BlockType.WoodenSlab;
  partial.blockShape.shape=new BlockShape(0,0,0,16,8,16);
  var slabMesh=new MeshData(false);BlockHelper.GetFaceDataIn(Direction.right,position,slabMesh,partial,partial.BlockData,smoothChunk);
  float low=(Beta173Lighting.Brightness(0,6)+Beta173Lighting.Brightness(0,2))*0.5f;
  float high=(Beta173Lighting.Brightness(0,6)+Beta173Lighting.Brightness(0,14))*0.5f;
  for(int i=0;i<4;i++)
  {float height=slabMesh.vertices[i].y-position.y-0.0005f;float expectedLight=low+(high-low)*height;Check(Math.Abs(slabMesh.sides[i].x-expectedLight)<0.00001f,"slab height interpolation");}
  var topMesh=new MeshData(false);BlockHelper.GetFaceDataIn(Direction.up,position,topMesh,partial,partial.BlockData,smoothChunk);
  Check(Math.Abs(topMesh.sides[0].y-Beta173Lighting.Brightness(0,14))<0.00001f,"inset slab top borrows above/sides brightness");
  partial.type=BlockType.Water;partial.blockShape.shape=new BlockShape(0,0,0,16,16,16);
  var waterMesh=new MeshData(false);BlockHelper.GetFaceDataIn(Direction.right,position,waterMesh,partial,partial.BlockData,smoothChunk);
  for(int i=0;i<4;i++)
  {float height=waterMesh.vertices[i].y-position.y-0.0005f;Check(Math.Abs(waterMesh.sides[i].x-(low+(high-low)*height))<0.00001f,"water samples after final surface height");}
  Check(waterMesh.vertices[1].y-position.y<0.9f,"fluid vertex actually deformed");
  smoothChunk.worldRef.fullSmoothLighting=false;
  var flatWater=new MeshData(false);BlockHelper.GetFaceDataIn(Direction.right,position,flatWater,partial,partial.BlockData,smoothChunk);
  Check(flatWater.sides.All(v=>v.z==1&&Math.Abs(v.x-v.y)<0.000001f),"previous flat liquid mode retained");
  var betaWaterTop=new MeshData(false);BlockHelper.GetFaceDataIn(Direction.up,position,betaWaterTop,partial,partial.BlockData,smoothChunk);
  Check(betaWaterTop.sides.All(v=>Math.Abs(v.x-Beta173Lighting.Brightness(0,6))<0.000001f),"Beta water top samples its own cell");
  partial.type=BlockType.WoodenSlab;partial.blockShape.shape=new BlockShape(0,0,0,16,8,16);
  var betaSlab=new MeshData(false);BlockHelper.GetFaceDataIn(Direction.right,position,betaSlab,partial,partial.BlockData,smoothChunk);
  for(int i=0;i<4;i++)
   Check(betaSlab.sides[i].z==1&&Math.Abs(betaSlab.sides[i].x-(betaSlab.vertices[i].y-position.y>0.25f?high:low))<0.00001f,"Beta slab uses shape top and bottom corner samples");
  // Bilinear face interpolation uses all four UV-wound corners, not a triangle's
  // three values. The same UV returns the same brightness across the diagonal.
  Check(Beta173Lighting.InterpolateCorners(0,1,0,0,0.5f,0.5f)==0.25f,"quad center uses all four corners");
  Check(Beta173Lighting.InterpolateCorners(0,1,0,0,0,1)==1,"UV corner winding");
  Check(Beta173Lighting.InterpolateCorners(0,1,0,0,-1,2)==1,"partial coordinate clamping");
  CheckPlants();
  CheckBiomeColors(args[1]);
  CheckStairMeshes();
  CheckSpecialMeshes();
  Console.WriteLine(aoCases+" Java six-face AO cases, "+orderedVertices+" positioned vertices; "+checks+" brightness/adapter/transport checks passed (Unity storage mocked).");
 }
}

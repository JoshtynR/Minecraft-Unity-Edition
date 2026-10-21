using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
namespace UnityEngine
{
 public class Object { public bool destroyed; public static void Destroy(Object o){if(o!=null)o.destroyed=true;} }
 public class MonoBehaviour:Object {public GameObject gameObject=new GameObject();public Transform transform=>gameObject.transform; public MeshFilter filter=new MeshFilter(); public T GetComponent<T>() where T:new(){if(typeof(T)==typeof(MeshFilter))return (T)(object)filter;return new T();} }
 public class GameObject:Object {public int layer;public Transform transform=new Transform();public GameObject(string name=""){}public void SetActive(bool active){}public T AddComponent<T>() where T:new()=>new T();}
 public class Transform {public Vector3 localPosition;public void SetParent(Transform parent,bool stays){}}
 public class RequireComponent:Attribute { public RequireComponent(Type a,Type b){} }
 public class HideInInspector:Attribute { }
 public struct Vector4 { public float x,y,z,w; public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;} }
 public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
 public struct Vector3 { public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z); }
 public struct Vector3Int { public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}public static implicit operator Vector3(Vector3Int p)=>new Vector3(p.x,p.y,p.z);public static Vector3Int operator +(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z); }
 public struct Color {public Color(float r,float g,float b,float a){} public static Color Lerp(Color a,Color b,float t)=>a; }
 public static class Application { public static bool isPlaying=true; }
 public class Camera:Object { public static Camera main=new Camera();public Color backgroundColor; }
 public class MeshFilter
 {
  public Mesh sharedMesh;
  public Mesh mesh { get { if(!owned){sharedMesh=new Mesh();owned=true;}return sharedMesh;} }
  private bool owned;
 }
 public class MeshRenderer { }
 public static class Debug { public static void LogException(Exception e){throw e;} }
 public class Mesh:Object
 {
  public int vertexCount=4;public static int created;public int uploads;public int subMeshCount;public Rendering.IndexFormat indexFormat;
  public List<Vector3> vertices;public List<Vector2> faceUVs;public List<Vector2> lightUVs;public List<Vector4> cornerUVs;public Dictionary<int,List<int>> indices=new Dictionary<int,List<int>>();
  public Mesh(){created++;}public void Clear(){}public void MarkDynamic(){}public void SetVertices(List<Vector3> list){vertices=new List<Vector3>(list);vertexCount=list.Count;uploads++;}
  public void SetUVs<T>(int channel,List<T> data){if(channel==4&&data.Count!=vertexCount)throw new Exception("UV5 count does not match mesh vertices");if(channel==4)faceUVs=new List<Vector2>((IEnumerable<Vector2>)(object)data);if(channel==1)lightUVs=new List<Vector2>((IEnumerable<Vector2>)(object)data);if(channel==3)cornerUVs=new List<Vector4>((IEnumerable<Vector4>)(object)data);}public void SetColors(List<Color> data){}
  public void SetTriangles(List<int> data,int submesh){indices[submesh]=new List<int>(data);}public void RecalculateNormals(){}public void RecalculateBounds(){}
 }
 namespace Rendering { public enum IndexFormat{UInt32} }
}
public partial class ChunkData { public bool modifiedByPlayer;public Vector3Int worldPos;public World worldRef;public Block GetBlock(Vector3Int p)=>new Block(); }
public enum BlockType {Chest}
public static class SpecialBlockGeometry {public static byte State(BlockType t,int shape)=>0;}
public class ModernChestVisual:MonoBehaviour {public void Initialize(World w,Vector3Int p,byte state){}}
public class Block {public BlockType type;public int blockShape; public void OnBlockUpdate(){} }
public partial class World
{
 public bool GenerationBusy;
 public WorldDimension dimension;private void UpdatePortals(){}private void StreamDimensionPlayers(){}
 public static World Instance;public bool disabled,IsWorldCreated=true;public byte chunksPerFrame=1;
 public Color nightColor,dayColor;public float skyLightMultiplier;
 public Data worldData=new Data();public class Data { public Dictionary<Vector3Int,ChunkRenderer> chunkDict=new Dictionary<Vector3Int,ChunkRenderer>(); }
 public int jobs;public TaskCompletionSource<Dictionary<ChunkRenderer,MeshData>> pending;
 private void UpdateWater(){}
 private void UpdateFurnaces(){}
 private void UpdateCows(){}
 private void UpdateDroppedItems(){}
 private void UpdateBetaLightingSettings(){}
 private Task<Dictionary<ChunkRenderer,MeshData>> UpdateMeshDataAsync(List<ChunkRenderer> chunks,bool markModifiedByPlayer)
 {
  if(markModifiedByPlayer)throw new Exception("lighting updates marked player edits");jobs++;
  pending=new TaskCompletionSource<Dictionary<ChunkRenderer,MeshData>>();return pending.Task;
 }
 public void ResetForTest(){ResetChunkUpdates();}
}
public static class LifecycleCheck
{
 static int checks;
 static void Check(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
 static MeshData Quad(ChunkData owner)
 {
  var data=new MeshData(true){sourceChunk=owner};
  foreach(var part in new[]{data,data.transparentMesh,data.waterMesh})
  {
   for(int i=0;i<4;i++){part.AddVertex(new Vector3(i,0,0));part.skyLight.Add(15);part.blockLight.Add(3);part.sides.Add(new Vector3());}
   for(int i=0;i<4;i++)part.smoothLightCorners.Add(new Vector4(0.1f,0.2f,0.3f,0.4f));
   part.AddFaceUVs(7);part.AddQuadColor(new Color());part.AddQuadTriangles();
  }
  return data;
 }
 static void Invoke(object o,string method){o.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);}
 public static void Main()
 {
  var data=new ChunkData();var renderer=new ChunkRenderer();renderer.Initialize(data);var mesh=renderer.filter.sharedMesh;
  renderer.Initialize(new ChunkData());Check(renderer.filter.sharedMesh==mesh&&Mesh.created==1,"pool reuse retains owned mesh");
  var staging=Quad(renderer.ChunkData);renderer.RenderMesh(staging);renderer.RenderMesh(staging);
  Check(mesh.vertices.Count==12&&staging.vertices.Count==12,"repeat uploads don't duplicate transparent vertices");
  Check(mesh.indices[1][0]==4&&mesh.indices[1][2]==6,"transparent indices offset exactly once");
  Check(staging.lightingUVs.Count==12&&staging.lightingUVs[4].x==15,"lighting channel merged");
  Check(staging.uv.Count==12&&staging.uv[4].z==7,"face texture layers retained");
  Check(staging.smoothLightCorners.Count==12&&mesh.cornerUVs.Count==12&&mesh.cornerUVs[7].z==0.3f,"smooth corner channel survives transparent merge and repeated upload");
  Check(mesh.subMeshCount==3&&mesh.indices[2][0]==8&&mesh.indices[2][2]==10,"water indices offset after opaque and cutout geometry exactly once");
  Check(mesh.lightUVs.Count==12&&mesh.cornerUVs[11].w==0.4f,"water retains brightness and corner channels");
  renderer.RenderMesh(new MeshData(true){sourceChunk=renderer.ChunkData});
  Check(mesh.indices[2].Count==0,"dry rebuild clears previous water triangles");
  Check(staging.faceCoordinates.Count==12&&staging.faceCoordinates[2].x==1,"normalized face coordinates merge independently of cropped images");
  int uploads=mesh.uploads;renderer.Initialize(new ChunkData());renderer.RenderMesh(staging);
  Check(mesh.uploads==uploads,"stale chunk data rejected after pool reuse");
  Check(typeof(ChunkRenderer).GetField("MeshData")==null,"renderer doesn't retain staging lists");
  Invoke(renderer,"OnDestroy");Check(mesh.destroyed,"native mesh owner cleanup");
  var world=new World();World.Instance=world;renderer=new ChunkRenderer();renderer.Initialize(new ChunkData());
  world.worldData.chunkDict[renderer.ChunkData.worldPos]=renderer;
  for(int i=0;i<100;i++)world.AddChunkToUpdate(renderer);
  Check(world.chunksToUpdate.Count==1,"queue deduplication");world.UpdateChunks();world.UpdateChunks();
  Check(world.jobs==1,"only one active batch");
  var first=world.pending;world.AddChunkToUpdate(renderer);first.SetResult(new Dictionary<ChunkRenderer,MeshData>{{renderer,Quad(renderer.ChunkData)}});
  world.UpdateChunks();Check(world.jobs==2,"changes arriving during build get another batch");
  world.ResetForTest();var mesh2=renderer.filter.sharedMesh;uploads=mesh2.uploads;
  world.pending.SetResult(new Dictionary<ChunkRenderer,MeshData>{{renderer,Quad(renderer.ChunkData)}});
  Check(mesh2.uploads==uploads&&world.chunksToUpdate.Count==0,"reset rejects late results and clears queue");
  world.disabled=true;world.AddChunkToUpdate(renderer);world.UpdateChunks();Check(world.jobs==2,"disabled world doesn't dispatch");
  var filter = new MeshFilter();var player=new Player();player.meshFilters=new[]{filter};
  player.SetMeshLight(15,3);var owned=filter.sharedMesh;int created=Mesh.created;
  for(int i=0;i<100;i++)player.SetMeshLight(i%16,4);
  Check(Mesh.created==created&&filter.sharedMesh==owned,"player lighting reuses single owned copy");
  Check(owned.lightUVs.Count==4&&owned.lightUVs[0].y==4,"player lighting updates UV1");
  player.DestroyForTest();Check(owned.destroyed,"player owned mesh destruction");
  var dirty=new ChunkData();Check(!dirty.HasPendingLighting,"clean mesh skips lighting work");
  foreach(var queue in new[]{dirty.skyRemoveList,dirty.skyExtendList,dirty.skyLightUpdateQueue,dirty.skyLightRemoveQueue,dirty.blockLightUpdateQueue,dirty.blockLightRemoveQueue,dirty.chunkToUpdateAfterLighting})
  {queue.Add(1);Check(dirty.HasPendingLighting,"each pending lighting channel requests processing");queue.Clear();}
  var budgetWorld=new World();var a=new ChunkRenderer();a.Initialize(new ChunkData());var b=new ChunkRenderer();b.Initialize(new ChunkData{worldPos=new Vector3Int(1,0,0)});
  budgetWorld.worldData.chunkDict[a.ChunkData.worldPos]=a;budgetWorld.worldData.chunkDict[b.ChunkData.worldPos]=b;budgetWorld.AddChunkToUpdate(a);budgetWorld.AddChunkToUpdate(b);budgetWorld.UpdateChunks();
  Check(budgetWorld.chunksToUpdate.Count==1,"chunksPerFrame bounds batch size");budgetWorld.ResetForTest();
  Console.WriteLine(checks+" production mesh ownership/upload/dispatcher checks passed (Unity APIs mocked).");
 }
}

public enum WorldDimension { Nether=-1,Overworld=0 }

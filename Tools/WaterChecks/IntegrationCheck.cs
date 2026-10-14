// Minimal Unity/Mirror storage doubles. Production Block, ChunkSection, saving,
// water simulation and World_Water code are compiled unchanged by run.py.
using System;
using System.Collections.Generic;
using UnityEngine;
namespace JetBrains.Annotations { public class CanBeNullAttribute : Attribute {} }
namespace UnityEngine
{
    public struct Vector3Int : IEquatable<Vector3Int>
    {
        public int x,y,z;
        public Vector3Int(int x,int y,int z) { this.x=x; this.y=y; this.z=z; }
        public static Vector3Int left = new Vector3Int(-1,0,0), right = new Vector3Int(1,0,0), up = new Vector3Int(0,1,0), down = new Vector3Int(0,-1,0), forward = new Vector3Int(0,0,1), back = new Vector3Int(0,0,-1);
        public static Vector3Int operator +(Vector3Int a,Vector3Int b) { return new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z); }
        public static Vector3Int operator -(Vector3Int a,Vector3Int b) { return new Vector3Int(a.x-b.x,a.y-b.y,a.z-b.z); }
        public static Vector3Int operator *(Vector3Int a,int b) { return new Vector3Int(a.x*b,a.y*b,a.z*b); }
        public bool Equals(Vector3Int b) { return x==b.x && y==b.y && z==b.z; }
        public override bool Equals(object b) { return b is Vector3Int && Equals((Vector3Int)b); }
        public override int GetHashCode() { unchecked { return (x*397^y)*397^z; } }
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x; this.y=y; this.z=z; }
    }
 public struct Vector4 { public float x,y,z,w; public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;} }
    public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
    public struct Color {}
    public static class Mathf { public static int Max(int a,int b) { return Math.Max(a,b); } public static float Min(float a,float b) { return Math.Min(a,b); } public static float Clamp(float v,float a,float b) { return Math.Min(b,Math.Max(a,v)); } }
    public static class Debug { public static void LogException(Exception e) { throw e; } }
    public static class Application { public static bool isPlaying=true; }
    public static class Time { public static float deltaTime=0.05f; }
}
namespace Mirror
{
    public static class NetworkServer { public static bool active=true; }
    public class ServerAttribute : Attribute {}
    public class ClientRpcAttribute : Attribute {}
    public class NetworkConnectionToClient {}
    public interface NetworkMessage {}
}
public class SlabBlock : Block { public SlabBlock(Block b):base(b) {} }
public class BlockTypeData { public byte opacity,lightEmission; }
public static class BlockDataManager
{
    public static Dictionary<int,BlockTypeData> blockTypeDataDictionary = new Dictionary<int,BlockTypeData>();
    static BlockDataManager() { foreach(BlockType t in Enum.GetValues(typeof(BlockType))) blockTypeDataDictionary[(int)t]=new BlockTypeData(); }
}
public struct BlockLightNode { public Block block;public int lightLevel;public BlockLightNode(Block b,int light) {block=b;lightLevel=light;} }
public enum Direction { up, down, left, right, forwards, backwards }
public static class DirectionExtensions
{
    public static Vector3Int GetVector(this Direction d) { return new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right,Vector3Int.forward,Vector3Int.back}[(int)d]; }
}
public static class BlockHelper { public static Direction[] directions=(Direction[])Enum.GetValues(typeof(Direction)); }
public class ChunkRenderer
{
    public ChunkData ChunkData;
    public void UpdateChunk(MeshData data) { ChunkData.worldRef.meshRequests++; }
}
public class WorldData
{
    public Dictionary<Vector3Int,ChunkData> chunkDataDict = new Dictionary<Vector3Int,ChunkData>();
    public Dictionary<Vector3Int,ChunkRenderer> chunkDict = new Dictionary<Vector3Int,ChunkRenderer>();
}
public static class WorldDataHelper
{
    public static ChunkRenderer GetChunk(World w,Vector3Int p){return w.worldData.chunkDict[p];}
    public static void SetBlock(World w,Vector3Int p,BlockType type){var c=w.worldData.chunkDataDict[GetChunkPosition(w,p)];c.SetBlock(p-c.worldPos,type);}
    public static Vector3Int GetChunkPosition(World w,Vector3Int p) { return new Vector3Int((int)Math.Floor((double)p.x/w.chunkSize)*w.chunkSize,0,(int)Math.Floor((double)p.z/w.chunkSize)*w.chunkSize); }
}
public class ChunkData
{
    public int chunkSize,chunkHeight=2;
    public World worldRef;
    public Vector3Int worldPos;
    public bool isGenerated=true,modifiedAfterSave=false,lightingInitialized;
    public ChunkSection[] sections;
    public Queue<BlockLightNode> blockLightUpdateQueue=new Queue<BlockLightNode>(),blockLightRemoveQueue=new Queue<BlockLightNode>(),skyLightUpdateQueue=new Queue<BlockLightNode>(),skyLightRemoveQueue=new Queue<BlockLightNode>();
    public List<Block> skyRemoveList=new List<Block>(),skyExtendList=new List<Block>();
    public ChunkData(World w,Vector3Int origin)
    {
        worldRef=w;worldPos=origin;chunkSize=w.chunkSize;
        sections=new ChunkSection[w.worldHeight/chunkHeight];
        for(int i=0;i<sections.Length;i++) sections[i]=new ChunkSection(this,i*chunkHeight,BlockType.Air);
    }
    public Vector3Int GetGlobalBlockCoords(Vector3Int p) { return worldPos+p; }
    public Block GetBlock(Vector3Int p)
    {
        if(p.y<0 || p.y>=worldRef.worldHeight) return new Block(BlockType.Nothing,p,null);
        if(p.x<0 || p.x>=chunkSize || p.z<0 || p.z>=chunkSize) return worldRef.GetBlock(worldPos+p);
        return sections[p.y/chunkHeight].GetBlock(p);
    }
    public bool IsOnEdge(Vector3Int p)=>false;
    public List<ChunkData> GetNeighbourChunk(Vector3Int p)=>new List<ChunkData>();
    public void SetBlock(Vector3Int p,BlockType type) { sections[p.y/chunkHeight].SetBlock(p,type); }
}
public partial class World
{
    public static World Instance;
    public int chunkSize=2,worldHeight=4,playerEdits,meshRequests;
    public bool IsWorldCreated=true,disabled=false;
    public WorldData worldData=new WorldData();
    public Queue<Block> blockToUpdate=new Queue<Block>();
    public void ExecuteAfterFrames(int frames,Action action) { action(); }
    public void AddChunkToUpdate(ChunkRenderer c) { meshRequests++; }
    public Block GetBlock(Vector3Int p)
    {
        ChunkData c;
        if(worldData.chunkDataDict.TryGetValue(WorldDataHelper.GetChunkPosition(this,p),out c)) return c.GetBlock(p-c.worldPos);
        return new Block(BlockType.Nothing,p,null);
    }
    public void SetBlock(Vector3Int p,BlockType t,byte stairFacing=0) { playerEdits++;SetBlock(WorldDataHelper.GetChunk(this,WorldDataHelper.GetChunkPosition(this,p)),p,t,stairFacing); }
    private System.Threading.Tasks.Task<Dictionary<ChunkRenderer,MeshData>> UpdateMeshDataAsync(List<ChunkRenderer> chunks, bool markModifiedByPlayer = true)
    {
        var result=new Dictionary<ChunkRenderer,MeshData>();foreach(var c in chunks) result[c]=new MeshData(false);
        return System.Threading.Tasks.Task.FromResult(result);
    }
    public void TestUpdate() { UpdateWater(); }
    public void TestReset() { ResetWater(); ResetTreeMeshChanges(); }
    public void TestRefreshTrees() { RefreshTreeMeshes().GetAwaiter().GetResult(); }
    public void TestActivate(ChunkData c) { QueueWaterChunk(c); }
}
public partial class WorldServer
{
    public static WorldServer instance;
    public int sends;
    public Vector3Int[] lastPositions;
    public byte[] lastLevels;
    // Production BroadcastWaterUpdates calls this production RPC; record the
    // send for integration checks. Host RPC doesn't apply the update twice.
    public void RecordSend(Vector3Int[] p,byte[] l) { sends++; lastPositions=p;lastLevels=l; }
}
public static class IntegrationCheck
{
    private static Vector3Int V(int x,int y,int z) { return new Vector3Int(x,y,z); }
    private static ChunkData AddChunk(World w,Vector3Int origin)
    {
        var c=new ChunkData(w,origin); w.worldData.chunkDataDict[origin]=c;w.worldData.chunkDict[origin]=new ChunkRenderer { ChunkData=c }; return c;
    }
    public static void Run()
    {
        var w = World.Instance = new World();
        var c = AddChunk(w,V(0,0,0));
        // Newly cataloged torches have the same opacity as air. Removing an
        // emitter must still queue its block-light removal.
        BlockDataManager.blockTypeDataDictionary[(int)BlockType.Torch].lightEmission=14;
        c.SetBlock(V(0,2,0),BlockType.Torch);
        WaterCheck.Assert(c.GetBlock(V(0,2,0)).GetBlockLight()==14,"catalog torch seeds emitted light");
        c.blockLightRemoveQueue.Clear();
        c.SetBlock(V(0,2,0),BlockType.Air);
        WaterCheck.Assert(c.GetBlock(V(0,2,0)).GetBlockLight()==0&&c.blockLightRemoveQueue.Count>0&&c.blockLightRemoveQueue.Peek().lightLevel==14,"same-opacity emitter removal queued");
        c.SetBlock(V(0,2,0),BlockType.Water);
        var b=c.GetBlock(V(0,2,0)); b.waterLevel=12;
        WaterCheck.Assert(b is WaterBlock && b.globalWorldPosition.Equals(V(0,2,0)),"placed water mapping/global section coordinates");
        WaterCheck.Assert(new WaterBlock(b).waterLevel==12,"copied block lost level");
        c.SetBlock(V(1,2,0),BlockType.Water);c.GetBlock(V(1,2,0)).waterLevel=3;
        var save=new ChunkSectionSaveData(c.sections[1]);
        WaterCheck.Assert(save.blocks[0].waterLevel==12 && save.blocks[1].waterLevel==3 && save.blocks[0].length==1,"RLE merged unlike water levels");
        var restored=ChunkSection.Deserialize(save,c);
        WaterCheck.Assert(restored.blocks[0,0,0] is WaterBlock && restored.blocks[0,0,0].waterLevel==12 && restored.blocks[1,0,0].waterLevel==3,"save/load lost flow levels");
        var legacy=new ChunkSectionSaveData { yOffset=0,blocks=new[]{new BlockSaveData(BlockType.Water,8)} };
        WaterCheck.Assert(ChunkSection.Deserialize(legacy,c).blocks[0,0,0].waterLevel==0,"old water save must default to source");
        c.SetBlock(V(0,2,0),BlockType.Air);WaterCheck.Assert(c.GetBlock(V(0,2,0)).waterLevel==0,"air retained water metadata");
        c.SetBlock(V(0,2,0),BlockType.Water);WaterCheck.Assert(c.GetBlock(V(0,2,0)).waterLevel==0,"new water was not source");
        // Render/pick shapes are reconstructed from type on both placement and
        // load, without adding metadata or rewriting the completed block asset.
        foreach(BlockType type in Enum.GetValues(typeof(BlockType)))
        {
            c.SetBlock(V(1,0,1),type);
            var shapeSave=new ChunkSectionSaveData(c.sections[0]);
            var shapeLoad=ChunkSection.Deserialize(shapeSave,c);
            WaterCheck.Assert(shapeLoad.blocks[1,0,1].blockShape==BlockShapesExtensions.ForBlock(type),"saved catalog type reconstructs code-defined shape");
        }
        for(byte facing=0; facing<4; facing++)
        {
            c.SetBlock(V(0,0,0),BlockType.WoodenStairs);c.GetBlock(V(0,0,0)).blockShape=StairGeometry.Shape(facing);
            c.SetBlock(V(1,0,0),BlockType.WoodenStairs);c.GetBlock(V(1,0,0)).blockShape=StairGeometry.Shape((byte)((facing+1)&3));
            var stairSave=new ChunkSectionSaveData(c.sections[0]);
            WaterCheck.Assert(stairSave.blocks[0].length==1&&stairSave.blocks[0].stairFacing==facing,"RLE preserves different stair directions");
            var stairLoad=ChunkSection.Deserialize(stairSave,c);
            WaterCheck.Assert(stairLoad.blocks[0,0,0].blockShape==StairGeometry.Shape(facing)&&stairLoad.blocks[1,0,0].blockShape==StairGeometry.Shape((byte)((facing+1)&3)),"save/chunk snapshot retains orientation");
            var placed=c.GetBlock(V(0,0,0));
            WaterCheck.Assert(new Block(placed).blockShape==placed.blockShape&&new Block(BlockType.Air,V(0,0,0),c.sections[0]).Reset(placed).blockShape==placed.blockShape,"stair copies retain orientation");
            placed.SetType(BlockType.Air);placed=c.GetBlock(V(0,0,0));placed.SetType(BlockType.CobblestoneStairs);
            WaterCheck.Assert(c.GetBlock(V(0,0,0)).blockShape==BlockShapes.StairsEast,"fresh stair placement resets old orientation");
        }
        var legacyStairs=new ChunkSectionSaveData{yOffset=0,blocks=new[]{new BlockSaveData(BlockType.CobblestoneStairs,8)}};
        WaterCheck.Assert(ChunkSection.Deserialize(legacyStairs,c).blocks[0,0,0].blockShape==BlockShapes.StairsEast,"old saves without orientation remain loadable");
        // Exercise production face vertex adjustment with four equal flow cells.
        for(int x=0;x<2;x++) for(int z=0;z<2;z++)
        { c.SetBlock(V(x,1,z),BlockType.Water);c.GetBlock(V(x,1,z)).waterLevel=7;c.SetBlock(V(x,2,z),BlockType.Air); }
        var mesh=new MeshData(false);
        var shape=new BlockShape(0,0,0,16,16,16);
        shape.SetFaceVertices(Direction.up,V(0,1,0),mesh);
        WaterMeshCheck.SetWaterFaceHeight(V(0,1,0),mesh,c);
        WaterCheck.Assert(Math.Abs(mesh.vertices[1].y-(1+1/9f+0.0005f))<0.00001f,"production water face did not lower flow corner");
        mesh=new MeshData(false);shape.SetFaceVertices(Direction.down,V(0,1,0),mesh);
        WaterMeshCheck.SetWaterFaceHeight(V(0,1,0),mesh,c);
        WaterCheck.Assert(Math.Abs(mesh.vertices[0].y-1.0005f)<0.00001f,"water adjustment changed bottom face");
        WorldServer.instance=new WorldServer();
        w.SetWaterCell(new WaterPosition(1,2,0),5);w.TestUpdate();
        WaterCheck.Assert(c.GetBlock(V(1,2,0)).waterLevel==5 && c.modifiedAfterSave && w.meshRequests>0,"world adapter failed to save/mesh flow state");
        WaterCheck.Assert(WorldServer.instance.sends==1 && WorldServer.instance.lastLevels[0]==5,"server did not broadcast level update");
        WaterCheck.Assert(w.GetWaterCell(new WaterPosition(-1,2,0))==WaterSimulation.Unloaded,"unloaded chunk classified as air");
        WaterCheck.Assert(w.GetWaterCell(new WaterPosition(0,-1,0))==WaterSimulation.Solid,"world floor classified as air");
        Mirror.NetworkServer.active=false;
        w.ReceiveWaterState(V(2,2,0),14); var next=AddChunk(w,V(2,0,0));w.TestActivate(next);w.TestUpdate();
        WaterCheck.Assert(next.GetBlock(V(0,2,0)).waterLevel==14,"early network delta lost before chunk load");
        w.ReceiveWaterState(V(2,2,0),255);w.TestUpdate();
        WaterCheck.Assert(next.GetBlock(V(0,2,0)).type==BlockType.Air,"client did not apply draining update");
        w.TestReset();
        c.SetBlock(V(0,2,0),BlockType.Water); c.SetBlock(V(0,1,0),BlockType.Air);
        for(int i=0;i<10;i++) w.TestUpdate();
        WaterCheck.Assert(c.GetBlock(V(0,1,0)).type==BlockType.Air,"network client simulated authoritative water");
        int beforeMeshes=w.meshRequests;
        w.NotifyTreeChunkChanged(c.worldPos);w.NotifyTreeChunkChanged(c.worldPos);
        w.TestRefreshTrees();
        WaterCheck.Assert(w.meshRequests==beforeMeshes+1,"cross-border tree mesh notification was not deduplicated/refreshed");
        Mirror.NetworkServer.active=true;
        var server=new WorldServer();
        foreach(var method in typeof(WorldServer).GetMethods(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance))
            if(Attribute.IsDefined(method,typeof(Mirror.ClientRpcAttribute)))
                foreach(var parameter in method.GetParameters())
                    WaterCheck.Assert(!parameter.IsOptional,"Mirror RPC parameters must be explicit");
        int edits=w.playerEdits;
        server.SetBlockMessageHandler(null,new WorldServer.SetBlockMessage(V(0,2,0),BlockType.Water));
        WaterCheck.Assert(w.playerEdits==edits+1,"host player edit applied twice or server omitted edit");
        for(byte facing=0;facing<4;facing++)
        {
            server.SetBlockMessageHandler(null,new WorldServer.SetBlockMessage(V(0,2,0),BlockType.CobblestoneStairs,facing));
            WaterCheck.Assert(StairGeometry.Facing(w.GetBlock(V(0,2,0)).blockShape)==facing,"server receives placed stair direction");
            Mirror.NetworkServer.active=false;
            server.RpcSetBlock(V(1,2,0),BlockType.WoodenStairs,facing);
            WaterCheck.Assert(StairGeometry.Facing(w.GetBlock(V(1,2,0)).blockShape)==facing,"RPC replicates stair direction");
            Mirror.NetworkServer.active=true;
        }
    }
}

public static class Lighting { internal static readonly object SyncRoot = new object(); public static void RecastSunLight(ChunkData c, UnityEngine.Vector3Int p, bool changedColumn = false) {} }

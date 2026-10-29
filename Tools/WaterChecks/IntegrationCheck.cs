// Minimal Unity/Mirror storage doubles. Production Block, ChunkSection, saving,
// water simulation and World_Water code are compiled unchanged by run.py.
using System;
using System.Collections.Generic;
using UnityEngine;
namespace JetBrains.Annotations { public class CanBeNullAttribute : Attribute {} }
namespace UnityEngine
{
    public class AudioClip {}
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
        public static implicit operator Vector3(Vector3Int v)=>new Vector3(v.x,v.y,v.z);
        public float sqrMagnitude=>x*x+y*y+z*z;
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 one=>new Vector3(1,1,1);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
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
    public class NetworkConnectionToClient {public NetworkIdentity identity=new NetworkIdentity();}
    public class NetworkIdentity {public T GetComponent<T>()where T:new()=>new T();}
    public interface NetworkMessage {}
}
public class BlockTypeData { public byte opacity,lightEmission; }
public enum BlockTexturePart { FluidStill,FluidFlow }
public class TextureData
{
    public int PartIndex(BlockTexturePart part,int fallback=0) { return part==BlockTexturePart.FluidStill?10:20; }
    public int FaceIndex(Direction d) { return d==Direction.up?10:20; }
}
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
public static partial class WorldDataHelper
{
    public static ChunkRenderer GetChunk(World w,Vector3Int p){return w.worldData.chunkDict[p];}
    public static ChunkData GetChunkData(World w,Vector3Int p){ChunkData c;return w.worldData.chunkDataDict.TryGetValue(GetChunkPosition(w,p),out c)?c:null;}
    public static Vector3Int GetChunkPosition(World w,Vector3Int p) { return new Vector3Int((int)Math.Floor((double)p.x/w.chunkSize)*w.chunkSize,0,(int)Math.Floor((double)p.z/w.chunkSize)*w.chunkSize); }
}
public class ChunkData
{
    public int chunkSize,chunkHeight=2;
    public World worldRef;
    public Vector3Int worldPos;
    public bool isGenerated=true,modifiedAfterSave=false,lightingInitialized,betaPopulationDone;
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
    public List<BetaPopulationRecord> betaPopulationRecords=new List<BetaPopulationRecord>();
    public Vector3Int GetLocalBlockCoords(Vector3Int p)=>p-worldPos;
    public bool IsOnEdge(Vector3Int p)=>false;
    public List<ChunkData> GetNeighbourChunk(Vector3Int p)=>new List<ChunkData>();
    public void SetBlock(Vector3Int p,BlockType type) { sections[p.y/chunkHeight].SetBlock(p,type); }
}
public partial class World
{
 public bool GenerationBusy;
    private void QueueSurvivalAround(Vector3Int p) {}
    private void QueueLeavesAroundRemovedLog(Vector3Int p) {}
    private void QueueSnowSupportOnChunkActivation(Block block) {}
    public bool rejectOccupiedPlacement;
    public bool CanPlaceSurvivalBlock(Vector3Int p,BlockType t,byte state)=>!rejectOccupiedPlacement||GetBlock(p).type==BlockType.Air;
    public void DropWheat(Vector3Int p,int age) {}
    private void TickFarming(WaterPosition p,Block block) {}
    private void TickGrass(WaterPosition p) {}
    private void ValidateFarmNeighbors(Vector3Int p) {}
    public void EmitFluidEffect(Vector3Int p,byte kind) {}
    private void DisposeFluidEffects() {}
    public bool CanReplaceChest(Vector3Int p,BlockType t) { return true; }
    public static bool IsFurnace(BlockType t)=>t==BlockType.Furnace||t==BlockType.LitFurnace;
    private void RemoveFurnace(Vector3Int p){}
    private void TrackFurnace(Vector3Int p){}
    public class ChestStub { public void Removed(Vector3Int p) {} }
    public ChestStub chests=new ChestStub();
    public static World Instance;
    public WorldDimension dimension;public bool isSaving;public static World FindDimension(WorldDimension id)=>Instance;private void QueuePortalValidation(Vector3Int p){}
    public long betaWorldSeed;
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
    public void TestRequirePopulation(bool value){waitForBetaPopulation=value;}
    public bool TestPopulationPausesWater()
    {
        UpdateWater();long tick=waterSimulation.CurrentTick;
        betaPopulationJobs=1;UpdateWater();bool paused=waterSimulation.CurrentTick==tick;
        betaPopulationJobs=0;return paused;
    }
    public void TestReset() { ResetWater(); ResetTreeMeshChanges(); }
    public int TestNotificationCount=>waterNotifications.Count;
    public void TestRefreshTrees() { RefreshTreeMeshes().GetAwaiter().GetResult(); }
    public void TestActivate(ChunkData c) { QueueWaterChunk(c); }
}
public static class BlockSurvivalRules
{
    public static bool IsLeaves(BlockType t)=>t==BlockType.Leaves||t==BlockType.SpruceLeaves||t==BlockType.BirchLeaves;
    public static bool IsLog(BlockType t)=>t==BlockType.Log||t==BlockType.SpruceLog||t==BlockType.BirchLog;
    public static bool CanReplace(BlockType existing,BlockType placed)=>existing==BlockType.Air||existing==BlockType.Fire;
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
public static partial class IntegrationCheck
{
    private static Vector3Int V(int x,int y,int z) { return new Vector3Int(x,y,z); }
    private static void SetLevel(ChunkData chunk, Vector3Int position, byte level) { var block=chunk.GetBlock(position);block.waterLevel=level; }
    private static void SetShape(ChunkData chunk, Vector3Int position, BlockShapes shape) { var block=chunk.GetBlock(position);block.blockShape=shape; }
    private static ChunkData AddChunk(World w,Vector3Int origin)
    {
        var c=new ChunkData(w,origin); w.worldData.chunkDataDict[origin]=c;w.worldData.chunkDict[origin]=new ChunkRenderer { ChunkData=c }; return c;
    }
    public static void Run()
    {
        var cold=(World)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(World));
        cold.TestReset();cold.TestReset();
        WaterCheck.Assert(cold.TestNotificationCount==0,"cold world can reset water, fluid runtime and tree queues before field initializers");
        System.Threading.Tasks.Parallel.For(0,256,i=>cold.NotifyWaterBlockChanged(V(i,0,0)));
        WaterCheck.Assert(cold.TestNotificationCount==256,"concurrent first-use water notifications share one safely initialized queue");
        cold.TestReset();WaterCheck.Assert(cold.TestNotificationCount==0,"reset clears notifications after cold initialization");
        var w = World.Instance = new World();
        WaterCheck.Assert(w.TestPopulationPausesWater(),"population holds authoritative water ticks until the worker completes");
        var c = AddChunk(w,V(0,0,0));
        var cellField=typeof(ChunkSection.PackedBlockStorage).GetField("cells",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        var lightField=typeof(ChunkSection).GetField("lightMap",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        WaterCheck.Assert(cellField.FieldType==typeof(ushort[])&&((ushort[])cellField.GetValue(c.sections[0].blocks)).Length==8,"section cells are not a dense two-byte array");
        WaterCheck.Assert(lightField.FieldType==typeof(byte[])&&((byte[])lightField.GetValue(c.sections[0])).Length==8,"section light is not a dense one-byte array");
        var liveHandle=c.GetBlock(V(0,0,0));c.SetBlock(V(0,0,0),BlockType.Stone);
        WaterCheck.Assert(liveHandle.type==BlockType.Stone,"queued block handle became a stale value snapshot");
        var handleSet=new HashSet<Block>{liveHandle};c.SetBlock(V(0,0,0),BlockType.Dirt);
        WaterCheck.Assert(handleSet.Contains(c.GetBlock(V(0,0,0))),"live handle identity changed with cell contents");
        WaterCheck.Assert(default(Block)==null&&c.GetBlock(V(0,0,0))!=null,"value handles broke legacy no-result checks");
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
        WaterCheck.Assert(b.type==BlockType.Water && b.globalWorldPosition.Equals(V(0,2,0)),"placed water mapping/global section coordinates");
        WaterCheck.Assert(new Block(b).waterLevel==12,"copied block lost level");
        c.SetBlock(V(1,2,0),BlockType.Water);SetLevel(c,V(1,2,0),3);
        var save=new ChunkSectionSaveData(c.sections[1]);
        WaterCheck.Assert(save.blocks[0].waterLevel==12 && save.blocks[1].waterLevel==3 && save.blocks[0].length==1,"RLE merged unlike water levels");
        var restored=ChunkSection.Deserialize(save,c);
        WaterCheck.Assert(restored.blocks[0,0,0].type==BlockType.Water && restored.blocks[0,0,0].waterLevel==12 && restored.blocks[1,0,0].waterLevel==3,"save/load lost flow levels");
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
            WaterCheck.Assert(shapeLoad.blocks[1,0,1].blockShape==(WheatCropRules.IsCrop(type)?SpecialBlockGeometry.Shape(WheatCropRules.Crop,WheatCropRules.LegacyAge(type)):(WheatCropRules.IsSoil(type)||BlockSurvivalRules.IsLeaves(type))?SpecialBlockGeometry.Shape(type,SpecialBlockGeometry.DefaultState(type)):BlockShapesExtensions.ForBlock(type)),"saved catalog type reconstructs shape or migrated crop/leaf state: "+type);
        }
        for(byte age=0;age<8;age++)
        {
            c.SetBlock(V(0,0,0),WheatCropRules.Crop);SetShape(c,V(0,0,0),SpecialBlockGeometry.Shape(WheatCropRules.Crop,age));
            c.SetBlock(V(1,0,0),WheatCropRules.Crop);SetShape(c,V(1,0,0),SpecialBlockGeometry.Shape(WheatCropRules.Crop,(byte)((age+1)&7)));
            var cropSave=new ChunkSectionSaveData(c.sections[0]);
            WaterCheck.Assert(cropSave.blocks[0].length==1&&cropSave.blocks[0].type==WheatCropRules.Crop&&cropSave.blocks[0].stairFacing==age,"RLE preserves wheat ages without separate IDs");
            var cropLoad=ChunkSection.Deserialize(cropSave,c);
            WaterCheck.Assert(cropLoad.blocks[0,0,0].type==WheatCropRules.Crop&&SpecialBlockGeometry.State(cropLoad.blocks[0,0,0].type,cropLoad.blocks[0,0,0].blockShape)==age,"wheat age survives chunk save/network snapshot");
            var oldCrop=new ChunkSectionSaveData{yOffset=0,blocks=new[]{new BlockSaveData(WheatCropRules.LegacyTextureType(age),8){hasBlockState=true,stairFacing=0}}};
            var migrated=ChunkSection.Deserialize(oldCrop,c).blocks[0,0,0];
            WaterCheck.Assert(migrated.type==WheatCropRules.Crop&&SpecialBlockGeometry.State(migrated.type,migrated.blockShape)==age,"legacy ID migrates even with old zero metadata");
            WaterCheck.Assert(new Block(migrated).blockShape==migrated.blockShape&&new Block(BlockType.Air,V(0,0,0),c.sections[0]).Reset(migrated).blockShape==migrated.blockShape,"crop copies retain age");
        }
        for(byte facing=0; facing<4; facing++)
        {
            c.SetBlock(V(0,0,0),BlockType.WoodenStairs);SetShape(c,V(0,0,0),StairGeometry.Shape(facing));
            c.SetBlock(V(1,0,0),BlockType.WoodenStairs);SetShape(c,V(1,0,0),StairGeometry.Shape((byte)((facing+1)&3)));
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
        foreach(BlockType type in Enum.GetValues(typeof(BlockType))) if(SpecialBlockGeometry.Handles(type))
        for(byte state=0;state<16;state++)
        {
            c.SetBlock(V(0,0,0),type);SetShape(c,V(0,0,0),SpecialBlockGeometry.Shape(type,state));
            c.SetBlock(V(1,0,0),type);SetShape(c,V(1,0,0),SpecialBlockGeometry.Shape(type,(byte)((state+1)&15)));
            var specialSave=new ChunkSectionSaveData(c.sections[0]);var specialLoad=ChunkSection.Deserialize(specialSave,c);
            WaterCheck.Assert(specialSave.blocks[0].length==1&&specialSave.blocks[0].hasBlockState,"RLE separates all special metadata states");
            WaterCheck.Assert(SpecialBlockGeometry.State(type,specialLoad.blocks[0,0,0].blockShape)==state,"all special states survive actual save/load");
            var copy=new Block(c.GetBlock(V(0,0,0)));var reset=new Block(BlockType.Air,V(0,0,0),c.sections[0]).Reset(copy);
            WaterCheck.Assert(SpecialBlockGeometry.State(type,copy.blockShape)==state&&reset.blockShape==copy.blockShape,"mapping/copy/reset retains all metadata bits");
        }
        var oldTorch=new ChunkSectionSaveData{yOffset=0,blocks=new[]{new BlockSaveData{type=BlockType.Torch,length=8}}};
        WaterCheck.Assert(SpecialBlockGeometry.State(BlockType.Torch,ChunkSection.Deserialize(oldTorch,c).blocks[0,0,0].blockShape)==5,"legacy torch saves default to upright");
        WaterCheck.Assert(ChunkSection.Deserialize(legacyStairs,c).blocks[0,0,0].blockShape==BlockShapes.StairsEast,"old saves without orientation remain loadable");
        // Exercise production face vertex adjustment with four equal flow cells.
        for(int x=0;x<2;x++) for(int z=0;z<2;z++)
        { c.SetBlock(V(x,1,z),BlockType.Water);SetLevel(c,V(x,1,z),7);c.SetBlock(V(x,2,z),BlockType.Air); }
        var mesh=new MeshData(false);
        var shape=new BlockShape(0,0,0,16,16,16);
        shape.SetFaceVertices(Direction.up,V(0,1,0),mesh);
        WaterMeshCheck.SetWaterFaceHeight(V(0,1,0),mesh,c);
        WaterCheck.Assert(Math.Abs(mesh.vertices[1].y-(1+1/9f+0.0005f))<0.00001f,"production water face did not lower flow corner");
        mesh=new MeshData(false);shape.SetFaceVertices(Direction.down,V(0,1,0),mesh);
        WaterMeshCheck.SetWaterFaceHeight(V(0,1,0),mesh,c);
        WaterCheck.Assert(Math.Abs(mesh.vertices[0].y-1.0005f)<0.00001f,"water adjustment changed bottom face");
        mesh=new MeshData(false);shape.SetFaceVertices(Direction.right,V(0,1,0),mesh);
        WaterMeshCheck.SetWaterFaceHeight(V(0,1,0),mesh,c);
        WaterMeshCheck.AddFluidUVs(Direction.right,V(0,1,0),mesh,c,new TextureData());
        WaterCheck.Assert(mesh.uv.Count==4&&mesh.uv[0].z==20,"fluid sides must use flow layer");
        WaterCheck.Assert(mesh.faceCoordinates.Count==mesh.vertices.Count,"fluid side UV5 coordinates match every emitted vertex");
        WaterCheck.Assert(Math.Abs(mesh.uv[1].y-(.5f+1/18f))<.00001f,"shallow fluid sides must crop instead of stretch");
        WaterCheck.Assert(Math.Abs(mesh.uv[0].y-.5f)<.00001f&&Math.Abs(mesh.uv[2].x-.4996875f)<.00001f,"flow sheet two-block scale");
        mesh=new MeshData(false);shape.SetFaceVertices(Direction.up,V(0,1,0),mesh);
        WaterMeshCheck.AddFluidUVs(Direction.up,V(0,1,0),mesh,c,new TextureData());
        WaterCheck.Assert(mesh.uv[0].z==10,"level fluid top uses still sheet");
        WaterCheck.Assert(mesh.faceCoordinates.Count==mesh.vertices.Count,"fluid top UV5 coordinates match every emitted vertex");
        SetLevel(c,V(0,1,0),0);SetLevel(c,V(1,1,0),1);
        SetLevel(c,V(0,1,1),0);
        mesh=new MeshData(false);shape.SetFaceVertices(Direction.up,V(0,1,0),mesh);
        WaterMeshCheck.AddFluidUVs(Direction.up,V(0,1,0),mesh,c,new TextureData());
        WaterCheck.Assert(mesh.uv[0].z==20&&Math.Abs(mesh.uv[0].x-.25f)<.00001f&&Math.Abs(mesh.uv[0].y-.75f)<.00001f,"flowing top uses rotated flow sheet");
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
        var populationWorld=new World();
        var contributors=new List<ChunkData>();
        for(int dx=0;dx<=1;dx++)for(int dz=0;dz<=1;dz++)contributors.Add(AddChunk(populationWorld,V(dx*2,0,-dz*2)));
        populationWorld.TestRequirePopulation(true);
        WaterCheck.Assert(populationWorld.GetWaterCell(new WaterPosition(0,1,0))==WaterSimulation.Unloaded,"water waits for unpopulated contributors");
        foreach(var contributor in contributors)contributor.betaPopulationDone=true;
        WaterCheck.Assert(populationWorld.GetWaterCell(new WaterPosition(0,1,0))==WaterSimulation.Air,"water resumes after every contributing owner completes");
        int guardedEdits=populationWorld.playerEdits;
        populationWorld.SetBlock((ChunkRenderer)null,V(0,1,0),BlockType.Stone);
        populationWorld.SetBlock(new ChunkRenderer(),V(0,1,0),BlockType.Stone);
        WaterCheck.Assert(populationWorld.playerEdits==guardedEdits,"released or missing chunk renderers reject late block edits safely");
        contributors[3].betaPopulationDone=false;
        WaterCheck.Assert(populationWorld.GetWaterCell(new WaterPosition(0,1,0))==WaterSimulation.Unloaded,"completed target owner alone is insufficient for water");
        var recordChunk=contributors[0];var recordPosition=V(0,1,0);
        WorldDataHelper.SetBlock(populationWorld,recordPosition,BlockType.Pumpkin);
        recordChunk.betaPopulationRecords.Add(new BetaPopulationRecord{position=recordPosition,kind="metadata",metadata=3});
        WorldDataHelper.SetBlock(populationWorld,recordPosition,BlockType.Pumpkin);
        WaterCheck.Assert(recordChunk.betaPopulationRecords.Count==1,"same-type writes preserve population metadata");
        WorldDataHelper.SetBlock(populationWorld,recordPosition,BlockType.Air);
        WaterCheck.Assert(recordChunk.betaPopulationRecords.Count==0,"block replacement clears old dungeon/pumpkin records");
        WorldDataHelper.SetBlock(populationWorld,V(0,-1,0),BlockType.Stone);
        WorldDataHelper.SetBlock(populationWorld,V(0,4,0),BlockType.Stone);
        WaterCheck.Assert(recordChunk.GetBlock(recordPosition).type==BlockType.Air,"metadata cleanup preserves out-of-height placement guard");
        var server=new WorldServer();
        var fireWorld=new World();var fireChunk=AddChunk(fireWorld,V(0,0,0));
        fireWorld.ReceiveWaterState(V(0,1,0),87);
        WaterCheck.Assert(fireChunk.GetBlock(V(0,1,0)).type==BlockType.Fire&&fireChunk.GetBlock(V(0,1,0)).waterLevel==7,"fire age arrives through authoritative delta");
        fireWorld.ReceiveWaterState(V(1,1,0),89);
        var fireSave=new ChunkSectionSaveData(fireChunk.sections[0]);var fireLoaded=ChunkSection.Deserialize(fireSave,fireChunk);
        WaterCheck.Assert(fireLoaded.blocks[0,1,0].waterLevel==7&&fireLoaded.blocks[1,1,0].waterLevel==9,"RLE fire metadata survives save/load");
        var pendingWorld=new World();pendingWorld.ReceiveWaterState(V(0,1,0),93);var pendingChunk=AddChunk(pendingWorld,V(0,0,0));
        Mirror.NetworkServer.active=false;pendingWorld.TestActivate(pendingChunk);pendingWorld.TestUpdate();Mirror.NetworkServer.active=true;
        WaterCheck.Assert(pendingChunk.GetBlock(V(0,1,0)).type==BlockType.Fire&&pendingChunk.GetBlock(V(0,1,0)).waterLevel==13,"joining client defers fire delta until its chunk is available");
        foreach(var method in typeof(WorldServer).GetMethods(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance))
            if(Attribute.IsDefined(method,typeof(Mirror.ClientRpcAttribute)))
                foreach(var parameter in method.GetParameters())
                    WaterCheck.Assert(!parameter.IsOptional,"Mirror RPC parameters must be explicit");
        w.SetBlock(V(0,2,0),BlockType.Air);
        int edits=w.playerEdits;
        server.SetBlockMessageHandler(new Mirror.NetworkConnectionToClient(),new WorldServer.SetBlockMessage(V(0,2,0),BlockType.Water));
        WaterCheck.Assert(w.playerEdits==edits+1,"host player edit applied twice or server omitted edit");
        for(byte facing=0;facing<4;facing++)
        {
            w.SetBlock(V(0,2,0),BlockType.Air);
            server.SetBlockMessageHandler(new Mirror.NetworkConnectionToClient(),new WorldServer.SetBlockMessage(V(0,2,0),BlockType.CobblestoneStairs,facing));
            WaterCheck.Assert(StairGeometry.Facing(w.GetBlock(V(0,2,0)).blockShape)==facing,"server receives placed stair direction");
            Mirror.NetworkServer.active=false;
            server.RpcSetBlock(V(1,2,0),BlockType.WoodenStairs,facing);
            WaterCheck.Assert(StairGeometry.Facing(w.GetBlock(V(1,2,0)).blockShape)==facing,"RPC replicates stair direction");
            Mirror.NetworkServer.active=true;
        }
        foreach(var type in new[]{BlockType.Torch,BlockType.StandingSign,BlockType.Trapdoor,BlockType.Lever})
        for(byte state=0;state<16;state++)
        {
            w.SetBlock(V(0,2,0),BlockType.Air);
            server.SetBlockMessageHandler(new Mirror.NetworkConnectionToClient(),new WorldServer.SetBlockMessage(V(0,2,0),type,state));
            WaterCheck.Assert(SpecialBlockGeometry.State(type,w.GetBlock(V(0,2,0)).blockShape)==state,"server receives all four state bits");
            Mirror.NetworkServer.active=false;server.RpcSetBlock(V(1,2,0),type,state);
            WaterCheck.Assert(SpecialBlockGeometry.State(type,w.GetBlock(V(1,2,0)).blockShape)==state,"RPC replicates all four state bits");
            Mirror.NetworkServer.active=true;
        }
        w.rejectOccupiedPlacement=true;
        foreach(var type in new[]{BlockType.WoodenDoor,BlockType.Trapdoor})
        {
            w.SetBlock(V(0,1,0),BlockType.Air);w.SetBlock(V(0,2,0),BlockType.Air);w.SetBlock(V(0,3,0),BlockType.Air);
            w.SetBlock(V(0,1,0),type,1);
            server.SetBlockMessageHandler(new Mirror.NetworkConnectionToClient(),new WorldServer.SetBlockMessage(V(0,1,0),type,5));
            WaterCheck.Assert(SpecialBlockGeometry.State(type,w.GetBlock(V(0,1,0)).blockShape)==5,"host interaction opens an occupied door/trapdoor");
            server.SetBlockMessageHandler(new Mirror.NetworkConnectionToClient(),new WorldServer.SetBlockMessage(V(0,1,0),type,2));
            WaterCheck.Assert(SpecialBlockGeometry.State(type,w.GetBlock(V(0,1,0)).blockShape)==5,"host rejects interaction that changes hinge orientation");
            server.SetBlockMessageHandler(new Mirror.NetworkConnectionToClient(),new WorldServer.SetBlockMessage(V(0,1,0),type,1));
            WaterCheck.Assert(SpecialBlockGeometry.State(type,w.GetBlock(V(0,1,0)).blockShape)==1,"host interaction closes an occupied door/trapdoor");
        }
        w.rejectOccupiedPlacement=false;
        var pairs=new World();for(int x=-2;x<=2;x+=2)for(int z=-2;z<=2;z+=2)AddChunk(pairs,V(x,0,z));
        for(int order=0;order<2;order++)
        {
            var left=V(0,1,0);var right=V(1,1,0);
            pairs.SetBlock(left,BlockType.Air);pairs.SetBlock(right,BlockType.Air);
            pairs.SetBlock(order==0?left:right,BlockType.WoodenDoor,1);
            pairs.SetBlock(order==0?right:left,BlockType.WoodenDoor,order==0?(byte)1:(byte)4);
            WaterCheck.Assert(SpecialBlockGeometry.State(BlockType.WoodenDoor,pairs.GetBlock(left).blockShape)==4&&SpecialBlockGeometry.State(BlockType.WoodenDoor,pairs.GetBlock(right).blockShape)==1,"actual world placement rehinges double doors outward in either order");
            WaterCheck.Assert(SpecialBlockGeometry.State(BlockType.WoodenDoor,pairs.GetBlock(left+Vector3Int.up).blockShape)==12&&SpecialBlockGeometry.State(BlockType.WoodenDoor,pairs.GetBlock(right+Vector3Int.up).blockShape)==9,"double door rehinging updates the existing upper half too");
            pairs.SetBlock(left,BlockType.Air);pairs.SetBlock(right,BlockType.Air);
        }
        foreach(var type in new[]{BlockType.WoodenDoor,BlockType.IronDoor,BlockType.Bed})
        for(byte state=0;state<4;state++)
        {
            var p=V(0,1,0);var other=p+SpecialBlockGeometry.PartnerOffset(type,state);
            pairs.SetBlock(p,type,state);
            WaterCheck.Assert(pairs.GetBlock(p).type==type&&pairs.GetBlock(other).type==type,"paired model places both halves");
            WaterCheck.Assert(SpecialBlockGeometry.State(type,pairs.GetBlock(other).blockShape)==(state|8),"paired model preserves upper/head bit");
            if(type==BlockType.WoodenDoor)
            {
                pairs.SetBlock(p,type,(byte)(state|4));
                WaterCheck.Assert((SpecialBlockGeometry.State(type,pairs.GetBlock(p).blockShape)&4)!=0&&(SpecialBlockGeometry.State(type,pairs.GetBlock(other).blockShape)&4)!=0,"door opening updates both halves");
            }
            pairs.SetBlock(other,BlockType.Air);
            WaterCheck.Assert(pairs.GetBlock(p).type==BlockType.Air&&pairs.GetBlock(other).type==BlockType.Air,"breaking either half removes its matching partner");
            pairs.SetBlock(other,BlockType.Stone);pairs.SetBlock(p,type,state);
            WaterCheck.Assert(pairs.GetBlock(p).type==BlockType.Air&&pairs.GetBlock(other).type==BlockType.Stone,"paired placement never overwrites occupied partner");
            pairs.SetBlock(other,BlockType.Air);
        }
        CheckLavaWorld();
    }
}

public static class Lighting { internal static readonly object SyncRoot = new object(); public static void RecastSunLight(ChunkData c, UnityEngine.Vector3Int p, bool changedColumn = false) {} }

public enum WorldDimension { Nether=-1,Overworld=0 }

public class Player {public World CurrentWorld=>World.Instance;public bool DimensionTransferInProgress;public WorldDimension dimension;public Transform transform=new Transform();}
public class Transform {public UnityEngine.Vector3 position;}

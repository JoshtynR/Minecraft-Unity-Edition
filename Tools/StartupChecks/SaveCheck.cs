using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public static class SaveEnvironment
{
    public enum SpecialFolder{ApplicationData}
    public static string root;
    public static string GetFolderPath(SpecialFolder folder)=>root;
}
namespace UnityEngine
{
    public struct Vector3Int{public int x,y,z;}
    public static class Debug{public static void Log(object message){}}
    public static class JsonUtility
    {
        public static Action duringChunkWrite;
        public static string ToJson(object value)
        {
            if(value is ChunkSaveData){duringChunkWrite?.Invoke();return "{\"chunk\":true}";}
            var world=(WorldSaveData)value;
            return "{\"worldName\":\""+world.worldName+"\",\"betaWorldSeed\":"+world.betaWorldSeed+",\"hasWorldTime\":"+world.hasWorldTime.ToString().ToLowerInvariant()+",\"worldTimeTicks\":"+world.worldTimeTicks+"}";
        }
    }
}
public enum WorldDimension{Overworld,Nether}
public class ChunkData{public bool modifiedAfterSave=true;public UnityEngine.Vector3Int worldPos;}
public class ChunkSaveData
{
    public static int captures;
    public UnityEngine.Vector3Int position;
    public static ChunkSaveData Serialize(ChunkData chunk){captures++;return new ChunkSaveData{position=chunk.worldPos};}
}
public class WorldSaveData
{
    public bool hasWorldTime;public double worldTimeTicks;
    public ChunkSaveData[] chunks;public string worldName;public WorldDimension dimension;
    public UnityEngine.Vector3Int seedOffset;public long betaWorldSeed;public object fluidTicks,droppedItems,fallingBlocks,mobs;
}
public class Store{public void SaveWorld(string name){}}
public class Player{public object inventory;public bool DimensionTransferInProgress;public WorldServer.SavePlayerMessage SavePlayer()=>new WorldServer.SavePlayerMessage();}
public class WorldServer{public struct SavePlayerMessage{}}
public class GameManager{public static GameManager Instance;public List<Player> players=new List<Player>();}
public class WorldData{public Dictionary<int,ChunkData> chunkDataDict=new Dictionary<int,ChunkData>();}
public partial class World
{
    public double CurrentWorldTicks=>1000;
    public string worldName="test";public string StorageName=>worldName;public WorldDimension dimension;
    public UnityEngine.Vector3Int mapSeedOffset;public long betaWorldSeed=123;
    public bool isSaving,binaryCompressSaves;public WorldData worldData=new WorldData();
    private readonly SemaphoreSlim generationGate=new SemaphoreSlim(1,1);
    private readonly CancellationToken worldCancellationToken=CancellationToken.None;
    public Store chests=new Store(),furnaces=new Store();
    public bool GateReleased=>generationGate.CurrentCount==1;
    public object CaptureFluidTicks()=>null;public object CaptureDroppedItems()=>null;public object CaptureFallingBlocks()=>null;public object CaptureMobs()=>null;
    public void SavePlayer(WorldServer.SavePlayerMessage player){}
    private ParallelOptions ChunkWorkerOptions()=>new ParallelOptions{MaxDegreeOfParallelism=2};
    public static string Compress(string value)=>value;
    public static bool failWrites;
    public static string GetChunkPath(string name,UnityEngine.Vector3Int pos,WorldDimension dimension)=>Path.Combine(SaveEnvironment.root,".minecraftUnity/saves",name,"chunks",failWrites?"missing/chunk.json":"chunk.json");
}
public static class SaveCheck
{
    static int checks;
    static void Check(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
    public static void Main(string[] args)
    {
        SaveEnvironment.root=args[0];var world=new World();var chunk=new ChunkData();world.worldData.chunkDataDict[0]=chunk;
        string metadata=Path.Combine(args[0],".minecraftUnity/saves/test/world.json");
        world.EnsureWorldMetadataAsync().GetAwaiter().GetResult();
        Check(File.Exists(metadata)&&File.ReadAllText(metadata).Contains("123"),"initial metadata contains seed and list entry");
        Check(File.ReadAllText(metadata).Contains("\"hasWorldTime\":true")&&File.ReadAllText(metadata).Contains("\"worldTimeTicks\":1000"),"initial metadata persists sky clock");
        Check(chunk.modifiedAfterSave&&ChunkSaveData.captures==0,"metadata creation skips full chunk capture and retains dirty terrain");
        File.WriteAllText(metadata,"existing-world");world.EnsureWorldMetadataAsync().GetAwaiter().GetResult();
        Check(File.ReadAllText(metadata)=="existing-world"&&world.GateReleased,"metadata creation preserves existing saves and releases gate");
        world.SaveDimensionAsync().GetAwaiter().GetResult();
        Check(File.Exists(World.GetChunkPath(world.worldName,chunk.worldPos,world.dimension)),"normal save writes chunk file");
        Check(!chunk.modifiedAfterSave&&!world.isSaving&&world.GateReleased,"successful unchanged snapshot becomes clean");
        chunk.modifiedAfterSave=true;UnityEngine.JsonUtility.duringChunkWrite=()=>chunk.modifiedAfterSave=true;
        world.SaveDimensionAsync().GetAwaiter().GetResult();
        Check(chunk.modifiedAfterSave,"edits arriving during IO remain dirty for next save");
        UnityEngine.JsonUtility.duringChunkWrite=null;World.failWrites=true;bool failed=false;
        try{world.SaveDimensionAsync().GetAwaiter().GetResult();}catch(Exception){failed=true;}
        Check(failed&&chunk.modifiedAfterSave&&!world.isSaving&&world.GateReleased,"failed chunk IO restores dirty state and releases save gate");
        World.failWrites=false;world.SaveDimensionAsync().GetAwaiter().GetResult();
        Check(!chunk.modifiedAfterSave,"failed chunk save can be retried");
        Console.WriteLine(checks+" production initial-metadata/save transaction checks passed (Unity serialization mocked, real filesystem IO).");
    }
}

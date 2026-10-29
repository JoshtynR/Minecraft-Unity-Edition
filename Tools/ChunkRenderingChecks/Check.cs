using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
namespace UnityEngine
{
    public class HideInInspector:Attribute {}
    public class Camera {}
    public class MonoBehaviour {}
    public class GameObject {public bool active; public void SetActive(bool value){active=value;}}
    public static class Application {public static bool isPlaying;}
    public static class Debug {public static void Log(object value){}public static void LogException(Exception error){throw error;}}
}
public class Block {}
public class ChunkData
{
    public Vector3Int worldPos;
    public bool modifiedByPlayer;
    public bool fail;
    public MeshData built;
    public MeshData GetMeshData()
    {
        if(fail)throw new Exception("injected build failure");
        return built=new MeshData{sourceChunk=this};
    }
}
public class ChunkRenderer
{
    public ChunkData ChunkData;
    public int uploads;
    public GameObject gameObject=new GameObject();
    public void UpdateChunk(MeshData data){uploads++;}
}
public class MeshData
{
    public ChunkData sourceChunk;
    public bool released;
    public void ReleaseAfterUpload(){released=true;}
}
public struct WorldData
{
    public Dictionary<Vector3Int,ChunkRenderer> chunkDict;
    public Dictionary<Vector3Int,ChunkData> chunkDataDict;
}
public static class UniTask
{
    public static Task<T> RunOnThreadPool<T>(Func<T> work,bool context,CancellationToken token)=>Task.Run(work,token);
}
public class WorldRenderer
{
    public int acquired,removed;
    public ChunkRenderer AcquireChunk(WorldData world,Vector3Int position)
    {
        acquired++;
        return world.chunkDataDict.TryGetValue(position,out var data)?new ChunkRenderer{ChunkData=data}:null;
    }
    public void RemoveChunk(ChunkRenderer renderer){removed++;}
}
public static class WorldDataHelper
{
    public static Vector3Int GetChunkPosition(World w,Vector3Int p) =>
        new Vector3Int((int)Math.Floor((double)p.x/w.chunkSize)*w.chunkSize,0,(int)Math.Floor((double)p.z/w.chunkSize)*w.chunkSize);
}
public partial class World
{
    public bool disabled,IsWorldCreated=true,GenerationBusy;
    public bool logGenerationTimings;
    public byte chunksPerFrame=1;
    public int chunkSize=16;
    private CancellationToken worldCancellationToken;
    public WorldData worldData=new WorldData{chunkDict=new Dictionary<Vector3Int,ChunkRenderer>(),chunkDataDict=new Dictionary<Vector3Int,ChunkData>()};
    public WorldRenderer worldRenderer=new WorldRenderer();
    private int waterActivations;
    private void QueueWaterChunk(ChunkData data){waterActivations++;}
    private ParallelOptions ChunkWorkerOptions()=>new ParallelOptions{MaxDegreeOfParallelism=1,CancellationToken=worldCancellationToken};
    public TaskCompletionSource<Dictionary<ChunkRenderer,MeshData>> pending;
    public int jobs;
    public Queue<Block> blockToUpdate=new Queue<Block>();
    void ResetBlockSurvival(){}
    void PrepareRebuildPriority(){}
    int SelectNextRebuild()=>0;
    Task<Dictionary<ChunkRenderer,MeshData>> UpdateMeshDataAsync(List<ChunkRenderer> chunks,bool modified)
    {
        jobs++;pending=new TaskCompletionSource<Dictionary<ChunkRenderer,MeshData>>();return pending.Task;
    }
    static void Check(bool value,string why){if(!value)throw new Exception(why);}
    public static void Main()
    {
        Check(RenderRebuildPriority.IsBetter(4,false,1000,true),"nearby edits precede distant visible work");
        Check(RenderRebuildPriority.IsBetter(1000,true,500,false),"visible work precedes distant hidden work");
        Check(RenderRebuildPriority.IsBetter(500,true,1000,true),"nearest visible candidate wins");
        Check(!RenderRebuildPriority.IsBetter(500,true,500,true),"ties preserve queue order");
        var w=new World();
        w.worldCancellationToken=w.taskTokenSource.Token;
        var c=new ChunkRenderer{ChunkData=new ChunkData{worldPos=Vector3Int.zero}};
        w.worldData.chunkDict[Vector3Int.zero]=c;
        w.AddChunkToUpdate(c);w.AddChunkToUpdate(c);
        Check(w.chunksToUpdate.Count==1,"deduplication");
        w.UpdateChunks();w.UpdateChunks();Check(w.jobs==1,"shared job gate");
        w.AddChunkToUpdate(c);var mesh=new MeshData();
        w.pending.SetResult(new Dictionary<ChunkRenderer,MeshData>{{c,mesh}});
        Check(c.uploads==1&&mesh.released&&w.chunksToUpdate.Count==1,"dirty during build");
        w.UpdateChunks();c.ChunkData=null;mesh=new MeshData();
        w.pending.SetResult(new Dictionary<ChunkRenderer,MeshData>{{c,mesh}});
        Check(c.uploads==1&&mesh.released,"pooling releases rejected results");
        w.AddChunkToUpdate(c);w.UpdateChunks();Check(w.jobs==2,"pooled queued entries skipped");
        c.ChunkData=new ChunkData{worldPos=Vector3Int.zero};w.AddChunkToUpdate(c);w.UpdateChunks();
        var pending=w.pending;int stale=w.workLifetime.Version;w.ResetChunkUpdates();mesh=new MeshData();
        pending.SetResult(new Dictionary<ChunkRenderer,MeshData>{{c,mesh}});
        Check(c.uploads==1&&mesh.released,"reset rejects and releases old batch");
        Check(!w.IsWorkSessionCurrent(stale),"shared lifetime epoch invalidation");
        mesh=new MeshData{sourceChunk=new ChunkData()};
        Check(!w.TryApplyChunkMesh(c,mesh,w.workLifetime.Version)&&mesh.released,"source identity validated");
        mesh=new MeshData{sourceChunk=c.ChunkData};w.disabled=true;
        Check(w.TryApplyChunkMesh(c,mesh,w.workLifetime.Version),"snapshot pause still allows owned uploads");
        w.disabled=false;
        var first=new ChunkData();var failed=new ChunkData{fail=true};
        try
        {
            ChunkMeshPipeline.Build(new[]{first,failed},new ParallelOptions{MaxDegreeOfParallelism=1},CancellationToken.None);
            throw new Exception("expected build failure");
        }
        catch(AggregateException) {Check(first.built!=null&&first.built.released,"failed batch releases successful sibling");}
        for(int x=-16;x<=0;x+=16)for(int z=-16;z<=0;z+=16)
        {
            var p=new Vector3Int(x,0,z);
            w.worldData.chunkDict[p]=new ChunkRenderer{ChunkData=new ChunkData{worldPos=p}};
        }
        var neighbors=new HashSet<ChunkRenderer>();
        w.CollectRenderUpdatesAround(Vector3Int.zero,neighbors);
        Check(neighbors.Count==4,"AO/fluid corner includes diagonal across negative boundary");
        neighbors.Clear();w.CollectRenderUpdatesAround(new Vector3Int(5,64,5),neighbors);
        Check(neighbors.Count==1,"interior invalidation visits only owner");
        neighbors.Clear();w.CollectRenderUpdatesAround(new Vector3Int(-1,64,5),neighbors);
        Check(neighbors.Count==2,"negative-coordinate edge invalidation");
        neighbors.Clear();var owner=w.worldData.chunkDict[Vector3Int.zero].ChunkData;
        w.CollectRenderUpdatesAround(Vector3Int.zero,neighbors,owner);
        Check(neighbors.Count==3,"lighting excludes requesting chunk");
        var lease=w.workLifetime.BeginWorker();
        Check(w.workLifetime.HasActiveWorkers&&w.HasPendingWorldWork,"queued workers prevent resident-memory release");
        lease.Dispose();lease.Dispose();
        Check(!w.workLifetime.HasActiveWorkers,"worker leases release exactly once");
        var real=new World();real.worldCancellationToken=real.taskTokenSource.Token;
        var realData=new ChunkData();var realRenderer=new ChunkRenderer{ChunkData=realData};
        real.worldData.chunkDict[Vector3Int.zero]=realRenderer;real.worldData.chunkDataDict[Vector3Int.zero]=realData;
        var runtime=real.BuildRuntimeMeshesForTestAsync(new List<ChunkRenderer>{realRenderer,realRenderer},false).GetAwaiter().GetResult();
        Check(runtime.Count==1&&!realData.modifiedByPlayer&&!real.workLifetime.HasActiveWorkers,"actual runtime wrapper deduplicates and releases worker lease");
        ChunkMeshPipeline.Release(runtime.Values);
        var startup=real.CreateMeshDataAsync(new List<ChunkData>{realData}).GetAwaiter().GetResult();
        Check(startup.Count==1&&!real.workLifetime.HasActiveWorkers,"actual initial-generation wrapper uses common builder");
        ChunkMeshPipeline.Release(startup.Values);
        var fresh=new World();var freshData=new ChunkData();fresh.worldData.chunkDataDict[Vector3Int.zero]=freshData;
        fresh.CreateChunk(fresh.worldData,Vector3Int.zero,new MeshData{sourceChunk=freshData},fresh.workLifetime.Version);
        Check(fresh.worldRenderer.acquired==1&&fresh.waterActivations==1&&fresh.worldData.chunkDict[Vector3Int.zero].gameObject.active,"initial chunk uses common upload validation and activation");
        fresh.CreateChunk(fresh.worldData,Vector3Int.zero,new MeshData{sourceChunk=freshData},fresh.workLifetime.Version);
        Check(fresh.worldRenderer.acquired==1,"initial upload reuses existing resident renderer");
        real.taskTokenSource.Cancel();
        try{real.CreateMeshDataAsync(new List<ChunkData>{realData}).GetAwaiter().GetResult();throw new Exception("expected cancelled build");}
        catch(OperationCanceledException){}
        Check(!real.workLifetime.HasActiveWorkers,"pre-cancelled scheduling releases worker lease");
        w.chunkUpdateInProgress=true;var wait=w.WaitForChunkRenderingAsync();w.taskTokenSource.Cancel();
        try {wait.GetAwaiter().GetResult();throw new Exception("expected cancellation");}
        catch(OperationCanceledException){}
        Check(!w.IsWorkSessionCurrent(w.workLifetime.Version),"cancel invalidates all pending uploads");
        Console.WriteLine("PASS: scheduler deduplication, pooling, reset/cancellation, source identity, failed-batch cleanup and 1/2/4-chunk invalidation.");
    }
}

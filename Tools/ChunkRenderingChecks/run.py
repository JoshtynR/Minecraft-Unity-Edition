"""Exercise the production rebuild scheduler with controlled asynchronous jobs."""
import argparse
from pathlib import Path
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
ap = argparse.ArgumentParser()
ap.add_argument('--mcs', default='mcs')
ap.add_argument('--mono', default='mono')
args = ap.parse_args()
source = (root/'Assets/_Scripts/World/World_UpdateLoop.cs').read_text()
source = source[:source.index('    private void Update()')] + '}\n'
stub = r'''
using System; using System.Collections.Generic; using System.Threading.Tasks;
namespace UnityEngine { public class HideInInspector:Attribute {} public class Camera {}
 public class MonoBehaviour {} public static class Debug {public static void LogException(Exception e){throw e;}}
}
public class Block {}
public class ChunkData {public int worldPos;}
public class ChunkRenderer {public ChunkData ChunkData; public int uploads; public void UpdateChunk(MeshData data){uploads++;}}
public class MeshData {public bool released;public void ReleaseAfterUpload(){released=true;}}
public struct WorldData {public Dictionary<int,ChunkRenderer> chunkDict;}
public partial class World {
 public bool disabled,IsWorldCreated=true,GenerationBusy;public byte chunksPerFrame=1;
 public WorldData worldData=new WorldData{chunkDict=new Dictionary<int,ChunkRenderer>()};
 public TaskCompletionSource<Dictionary<ChunkRenderer,MeshData>> pending;public int jobs;
 void ResetBlockSurvival(){}
 Task<Dictionary<ChunkRenderer,MeshData>> UpdateMeshDataAsync(List<ChunkRenderer> chunks,bool modified){
 jobs++;pending=new TaskCompletionSource<Dictionary<ChunkRenderer,MeshData>>();return pending.Task;}
 static void Check(bool value){if(!value)throw new Exception("scheduler assertion failed");}
 public static void Main(){
 var w=new World();var c=new ChunkRenderer{ChunkData=new ChunkData{worldPos=0}};w.worldData.chunkDict[0]=c;
 w.AddChunkToUpdate(c);w.AddChunkToUpdate(c);Check(w.chunksToUpdate.Count==1);
 w.UpdateChunks();w.UpdateChunks();Check(w.jobs==1);
 w.AddChunkToUpdate(c);var mesh=new MeshData();w.pending.SetResult(new Dictionary<ChunkRenderer,MeshData>{{c,mesh}});
 Check(c.uploads==1&&mesh.released&&w.chunksToUpdate.Count==1);
 w.UpdateChunks();c.ChunkData=null;mesh=new MeshData();w.pending.SetResult(new Dictionary<ChunkRenderer,MeshData>{{c,mesh}});
 Check(c.uploads==1&&mesh.released);
 w.AddChunkToUpdate(c);w.UpdateChunks();Check(w.jobs==2);
 c.ChunkData=new ChunkData{worldPos=0};w.AddChunkToUpdate(c);w.UpdateChunks();var pending=w.pending;
 w.ResetChunkUpdates();mesh=new MeshData();pending.SetResult(new Dictionary<ChunkRenderer,MeshData>{{c,mesh}});
 Check(c.uploads==1&&mesh.released);Console.WriteLine("PASS: deduplication, shared job gate, dirty-during-build, pooling and reset cleanup.");
 }
}
'''
with tempfile.TemporaryDirectory() as temp:
    temp=Path(temp)
    (temp/'Scheduler.cs').write_text(source)
    (temp/'Check.cs').write_text(stub)
    subprocess.run([args.mcs,'-langversion:latest','-out:'+str(temp/'check.exe'),str(temp/'Scheduler.cs'),str(temp/'Check.cs')],check=True)
    subprocess.run([args.mono,str(temp/'check.exe')],check=True)

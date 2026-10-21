#!/usr/bin/env python3
import argparse,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--mcs',default='mcs');ap.add_argument('--mono',default='mono');args=ap.parse_args()
 files=['Tools/MemoryChecks/LifecycleCheck.cs','Assets/_Scripts/World/Rendering/MeshData.cs','Assets/_Scripts/World/Rendering/ChunkRenderer.cs','Assets/_Scripts/World/World_UpdateLoop.cs','Assets/_Scripts/World/SkyClock.cs']
 with tempfile.TemporaryDirectory() as tmp:
  player=(ROOT/"Assets/_Scripts/Entity's/Player/Player.cs").read_text()
  method=player[player.index('    public void SetMeshLight'):player.index('    // Name plate name')]
  cleanup=player[player.index('    private void OnDestroy()'):].replace('private void OnDestroy()', 'public void DestroyForTest()')
  stub=Path(tmp)/'Player.cs'
  stub.write_text('using System.Collections.Generic;using UnityEngine; public class Player : MonoBehaviour { public MeshFilter[] meshFilters; private readonly Dictionary<MeshFilter,Mesh> ownedLightingMeshes=new Dictionary<MeshFilter,Mesh>();private readonly List<Vector2> meshLightingUVs=new List<Vector2>();private void DisposeMining(){}private void DisposeFluidView(){}private void CancelDimensionTransfer(){}'+method+cleanup+' public class GameManager {public static GameManager Instance;public List<Player> players=new List<Player>();}')
  chunk=(ROOT/'Assets/_Scripts/World/ChunkData.cs').read_text()
  prop=chunk[chunk.index('    public bool HasPendingLighting'):chunk.index('    public void UpdateLight()')]
  dirty=Path(tmp)/'ChunkDirty.cs'
  dirty.write_text('using System.Collections.Generic;public partial class ChunkData { public List<int> skyRemoveList=new List<int>(),skyExtendList=new List<int>(),skyLightUpdateQueue=new List<int>(),skyLightRemoveQueue=new List<int>(),blockLightUpdateQueue=new List<int>(),blockLightRemoveQueue=new List<int>(),chunkToUpdateAfterLighting=new List<int>();'+prop+'}')
  world=(ROOT/'Assets/_Scripts/World/World.cs').read_text()
  worker_options=world[world.index('    private ParallelOptions ChunkWorkerOptions()'):world.index('    // public void UpdateChunk(')]
  production=Path(tmp)/'Generation.cs'
  mesh=chunk[chunk.index('    private MeshData BuildMeshData()'):chunk.index('    public bool HasPendingLighting')]
  production.write_text('using System;using System.Threading;using System.Threading.Tasks;using UnityEngine;public partial class World{public int chunkWorkerThreads=2;private CancellationToken worldCancellationToken;public ParallelOptions OptionsForTest()=>ChunkWorkerOptions();'+worker_options+'}public partial class ChunkData{public int chunkSize=2,chunkHeight=2;public ChunkSection[] sections;public void UpdateLight(){}public MeshData BuildForTest()=>BuildMeshData();'+mesh+'}public class ChunkSection{public int yOffset;public Block[,,] blocks=new Block[2,2,2];}public static class BlockHelper{public static System.Collections.Generic.List<Vector3Int> visited=new System.Collections.Generic.List<Vector3Int>();public static MeshData GetMeshData(ChunkData c,Vector3Int p,MeshData mesh,BlockType type){visited.Add(p);mesh.vertices.Add(p);return mesh;}}')
  binary=Path(tmp)/'memory.exe'
  subprocess.run([args.mcs,'-langversion:latest','-out:'+str(binary),*[str(ROOT/p) for p in files],str(stub),str(dirty),str(production)],check=True)
  subprocess.run([args.mono,str(binary)],check=True)
if __name__=='__main__':main()

"""Exercise production scheduling, lifetime, mesh ownership and invalidation with controlled jobs."""
import argparse
from pathlib import Path
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
ap = argparse.ArgumentParser()
ap.add_argument('--mcs', default='mcs')
ap.add_argument('--mono', default='mono')
args = ap.parse_args()
source = (root/'Assets/_Scripts/World/World_ChunkUpdates.cs').read_text()
world = (root/'Assets/_Scripts/World/World.cs').read_text()
runtime_jobs = world[world.index('    private async Task<ConcurrentDictionary<ChunkRenderer, MeshData>> UpdateMeshDataAsync'):world.index('    private ParallelOptions ChunkWorkerOptions')]
runtime_jobs = runtime_jobs.replace('UpdateMeshDataAsync(', 'BuildRuntimeMeshesForTestAsync(')
generation = (root/'Assets/_Scripts/World/World_Generation.cs').read_text()
startup_jobs = generation[generation.index('    public async UniTask<ConcurrentDictionary<Vector3Int, MeshData>> CreateMeshDataAsync'):generation.index('    private UniTask<ConcurrentDictionary<Vector3Int, ChunkData>> CalculateWorldChunkData')]
startup_jobs = startup_jobs.replace('async UniTask<', 'async Task<')
create_chunk = generation[generation.index('    private void CreateChunk('):generation.rfind('}')]
with tempfile.TemporaryDirectory() as temp:
    temp=Path(temp)
    (temp/'Scheduler.cs').write_text(source)
    (temp/'MeshJobs.cs').write_text('using System.Collections.Concurrent;using System.Collections.Generic;using System.Linq;using System.Threading.Tasks;using UnityEngine;public partial class World{'+runtime_jobs+startup_jobs+create_chunk+'}')
    files=['Tools/ChunkRenderingChecks/Check.cs','Tools/Shared/UnityVectors.cs',
           'Assets/_Scripts/World/WorldWorkLifetime.cs','Assets/_Scripts/World/World_WorkLifecycle.cs',
           'Assets/_Scripts/World/World_MeshPipeline.cs','Assets/_Scripts/World/World_RenderInvalidation.cs',
           'Assets/_Scripts/World/Rendering/ChunkMeshPipeline.cs']
    subprocess.run([args.mcs,'-langversion:latest','-out:'+str(temp/'check.exe'),str(temp/'Scheduler.cs'),str(temp/'MeshJobs.cs'),
                    *[str(root/f) for f in files]],check=True)
    subprocess.run([args.mono,str(temp/'check.exe')],check=True)

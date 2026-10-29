using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;

public partial class World
{
    [System.NonSerialized] private Queue<Vector3Int> runtimePortalChecks;
    private Queue<Vector3Int> portalChecks => System.Threading.LazyInitializer.EnsureInitialized(ref runtimePortalChecks, () => new Queue<Vector3Int>());
    private static readonly Vector3Int[] portalNeighbors={Vector3Int.zero,Vector3Int.left,Vector3Int.right,Vector3Int.up,Vector3Int.down,new Vector3Int(0,0,-1),new Vector3Int(0,0,1)};
    private BlockType PortalBlock(Vector3Int p)=>GetBlock(p).type;
    public bool TryIgnitePortal(Vector3Int p)
    {
        if(!DropAuthority||isSaving)return false;
        if(!PortalRules.FindFrame(PortalBlock,p,false,out var frame))return false;
        SetPortalFrame(frame,false);
        return true;
    }
    private void QueuePortalValidation(Vector3Int changed)
    {if(DropAuthority)portalChecks.Enqueue(changed);}
    private void UpdatePortals()
    {
        if(!DropAuthority||disabled||!IsWorldCreated||isSaving||GenerationBusy)return;
        int count=Math.Min(32,portalChecks.Count);
        for(int i=0;i<count;i++)
        {
            var changed=portalChecks.Dequeue();
            foreach(var d in portalNeighbors)
            {
                var p=changed+d;var type=PortalBlock(p);
                if(type==BlockType.Fire){TryIgnitePortal(p);continue;}
                if(type!=BlockType.NetherPortal)continue;
                if(PortalRules.FindFrame(PortalBlock,p,true,out _))continue;
                // Unknown neighbor data pauses validation; streaming is not a broken frame.
                bool unknown=false;
                for(int x=-2;!unknown&&x<=2;x++)for(int y=-3;!unknown&&y<=3;y++)for(int z=-2;!unknown&&z<=2;z++)
                    if(PortalBlock(p+new Vector3Int(x,y,z))==BlockType.Nothing)unknown=true;
                if(unknown)continue;
                SetBlock(p,BlockType.Air);
                if(NetworkServer.active&&WorldServer.instance!=null)WorldServer.instance.BroadcastBlock(this,p,BlockType.Air,0);
            }
        }
    }
    private void SetPortalFrame(PortalRules.Frame frame,bool buildFrame)
    {
        for(int x=-1;x<=2;x++)for(int y=-1;y<=3;y++)
        {
            bool edge=x==-1||x==2||y==-1||y==3;
            if(edge&&!buildFrame)continue;
            var p=frame.bottom+frame.along*x+Vector3Int.up*y;
            WritePortalBlock(p,edge?BlockType.Obsidian:BlockType.NetherPortal,frame.State);
        }
    }
    private void WritePortalBlock(Vector3Int p,BlockType type,byte state=0)
    {
        var block=GetBlock(p);if(block==null||block.type==BlockType.Nothing)return;
        WorldDataHelper.SetBlock(this,p,type);
        block=GetBlock(p);block.blockShape=SpecialBlockGeometry.Shape(type,state);
        block.chunkData.modifiedByPlayer=true;block.chunkData.modifiedAfterSave=true;
        QueuePortalValidation(p);
        foreach(var d in portalNeighbors)
        {
            var chunk=WorldDataHelper.GetChunk(this,WorldDataHelper.GetChunkPosition(this,p+d));
            if(chunk!=null)AddChunkToUpdate(chunk,true);
        }
        if(NetworkServer.active&&WorldServer.instance!=null)WorldServer.instance.BroadcastBlock(this,p,type,state);
    }
    public async Task<Vector3> FindOrCreateExitAsync(Vector3 desired)
    {
        var center=Vector3Int.FloorToInt(desired);
        await EnsureAreaAsync(center,true);
        // Portals only occur in saved/player-edited chunks. Load saved candidates
        // beyond the visible halo without meshing an entire 128-block search square.
        var candidates=new List<Vector3Int>();
        int minX=Mathf.FloorToInt((center.x-128)/16f)*16,maxX=Mathf.FloorToInt((center.x+128)/16f)*16;
        int minZ=Mathf.FloorToInt((center.z-128)/16f)*16,maxZ=Mathf.FloorToInt((center.z+128)/16f)*16;
        for(int x=minX;x<=maxX;x+=16)for(int z=minZ;z<=maxZ;z+=16)
        {
            var p=new Vector3Int(x,0,z);
            if(!worldData.chunkDataDict.ContainsKey(p)&&File.Exists(GetChunkPath(worldName,p,dimension)))candidates.Add(p);
        }
        if(candidates.Count>0)
        {
            await generationGate.WaitAsync(worldCancellationToken);
            try
            {
                var loaded=await LoadChunksAsync(candidates,worldName);
                foreach(var pair in loaded)if(!worldData.chunkDataDict.ContainsKey(pair.Key))worldData.chunkDataDict.Add(pair.Key,pair.Value);
            }
            finally{generationGate.Release();}
        }
        PortalRules.Frame nearest=null;float best=float.MaxValue;
        // Search loaded raw terrain within the original 128-block portal radius.
        foreach(var data in worldData.chunkDataDict.Values.ToArray())
        {
            if(Math.Abs(data.worldPos.x-center.x)>144||Math.Abs(data.worldPos.z-center.z)>144)continue;
            for(int x=0;x<16;x++)for(int z=0;z<16;z++)
            {
                int wx=data.worldPos.x+x,wz=data.worldPos.z+z;
                if(Math.Abs(wx-center.x)>128||Math.Abs(wz-center.z)>128)continue;
                for(int y=1;y<Math.Min(127,worldHeight-1);y++)
                {
                    var p=new Vector3Int(wx,y,wz);
                    if(PortalBlock(p)!=BlockType.NetherPortal||!PortalRules.FindFrame(PortalBlock,p,true,out var frame))continue;
                    float distance=(frame.Center-desired).sqrMagnitude;
                    if(distance<best){best=distance;nearest=frame;}
                    y+=2;
                }
            }
        }
        if(nearest==null)
        {
            // Prefer a naturally supported opening in a 16-block radius.
            for(int x=-16;x<=16;x++)for(int z=-16;z<=16;z++)
            for(int y=worldHeight-5;y>=1;y--)
            foreach(var axis in new[]{Vector3Int.right,new Vector3Int(0,0,1)})
            {
                var p=new Vector3Int(center.x+x,y,center.z+z);
                float distance=((Vector3)p-desired).sqrMagnitude;if(distance>=best)continue;
                if(!PortalSiteClear(p,axis))continue;
                best=distance;nearest=new PortalRules.Frame{bottom=p,along=axis};
            }
            if(nearest==null)
            {
                nearest=new PortalRules.Frame{bottom=new Vector3Int(center.x,Mathf.Clamp(center.y,70,worldHeight-10),center.z),along=Vector3Int.right};
                // Forced exits get an obsidian landing, with two blocks of approach clearance.
                for(int x=-1;x<=2;x++)for(int z=-1;z<=1;z++)for(int y=-1;y<=3;y++)
                    WritePortalBlock(nearest.bottom+new Vector3Int(x,y,z),y==-1?BlockType.Obsidian:BlockType.Air);
            }
            SetPortalFrame(nearest,true);
        }
        await EnsureAreaAsync(Vector3Int.FloorToInt(nearest.Center),true);
        await RebuildDimensionMeshesAsync();
        return nearest.Center;
    }
    private bool PortalSiteClear(Vector3Int p,Vector3Int along)
    {
        var normal=new Vector3Int(along.z,0,along.x);
        for(int x=-1;x<=2;x++)for(int z=-1;z<=1;z++)for(int y=-1;y<=3;y++)
        {
            var type=PortalBlock(p+along*x+normal*z+Vector3Int.up*y);
            if(y==-1){if(!SpecialBlockGeometry.NormalCube(type))return false;}
            else if(type!=BlockType.Air&&type!=BlockType.Fire)return false;
        }
        return true;
    }
    public async Task RebuildDimensionMeshesAsync()
    {
        await generationGate.WaitAsync(worldCancellationToken);
        try
        {
            await WaitForChunkRenderingAsync();
            await RebuildDimensionMeshesCoreAsync();
        }
        finally{generationGate.Release();}
    }
    private async Task RebuildDimensionMeshesCoreAsync()
    {
        int session=workLifetime.Version;
        var chunks=worldData.chunkDict.Values.Where(c=>c!=null).Select(c=>c.ChunkData).ToList();
        await CastLightFirstTime(chunks);
        // Changes from a built portal may have queued incremental light transport.
        foreach(var data in chunks)if(data.HasPendingLighting)data.UpdateLight();
        int batchSize=Math.Max(1,ChunkWorkerOptions().MaxDegreeOfParallelism);
        for(int offset=0;offset<chunks.Count;offset+=batchSize)
        {
            var batch=chunks.GetRange(offset,Math.Min(batchSize,chunks.Count-offset));
            var meshes=await CreateMeshDataAsync(batch);
            try
            {
                if (!IsWorkSessionCurrent(session)) return;
                for(int i=0;i<batch.Count;i++)
                {
                    if (!IsWorkSessionCurrent(session)) return;
                    var data=batch[i];
                    ChunkRenderer renderer;
                    if(worldData.chunkDict.TryGetValue(data.worldPos,out renderer))
                        TryApplyChunkMesh(renderer,meshes[data.worldPos],session);
                    await YieldBetweenMeshUploadsAsync(i+1,batch.Count);
                }
                if(Application.isPlaying)await Task.Yield();
                worldCancellationToken.ThrowIfCancellationRequested();
            }
            finally { ChunkMeshPipeline.Release(meshes.Values); }
        }
    }
}

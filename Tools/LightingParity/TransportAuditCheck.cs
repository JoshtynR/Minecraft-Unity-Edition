using System;
using System.IO;
using UnityEngine;

public static partial class LightingCheck
{
    static void CheckTransportAudit(string columns)
    {
        // The old chunk has already drained its queues before the adjacent
        // dark chunk becomes resident. New first-light must import its border.
        foreach(bool sky in new[]{false,true})
        {
            var world=new World{worldHeight=4,chunkSize=1};
            var old=new ChunkData{worldRef=world};
            for(int y=0;y<4;y++)Add(old,new Vector3Int(0,y,0),sky?BlockType.Air:y>=2?BlockType.Stone:BlockType.Air,!sky&&y>=2?15:0);
            if(!sky){var torch=old.GetBlock(new Vector3Int(0,1,0));torch.type=BlockType.Torch;torch.BlockData.lightEmission=14;}
            Lighting.InitializeLoadedChunks(new[]{old});Check(!old.HasPendingLighting,"old chunk has no queued transport before streaming");
            var added=new ChunkData{worldRef=world,worldPos=new Vector3Int(1,0,0),blocks=old.blocks};
            for(int y=0;y<4;y++)Add(added,new Vector3Int(0,y,0),y>=2?BlockType.Stone:BlockType.Air,y>=2?15:0);
            Lighting.InitializeLoadedChunks(new[]{added});
            var sample=added.GetBlock(new Vector3Int(0,1,0));
            Check((sky?sample.sky:sample.light)==(sky?14:13),"new dark chunk imports existing "+(sky?"sky":"torch")+" light without waiting for an edit");
        }
        // A new direct-lit boundary is itself an AO/face light sample. Its
        // neighbor's stored 15 may stay unchanged, but that mesh must refresh.
        var edgeWorld=new World{worldHeight=4,chunkSize=1};var visible=new ChunkData{worldRef=edgeWorld};
        for(int y=0;y<4;y++)Add(visible,new Vector3Int(0,y,0));Lighting.InitializeLoadedChunks(new[]{visible});
        var renderer=new ChunkRenderer{ChunkData=visible};edgeWorld.worldData.chunkDict[visible.worldPos]=renderer;
        var boundary=new ChunkData{worldRef=edgeWorld,worldPos=new Vector3Int(1,0,0),blocks=visible.blocks};
        for(int y=0;y<4;y++)Add(boundary,new Vector3Int(0,y,0));
        int refreshes=edgeWorld.updates;Lighting.InitializeLoadedChunks(new[]{boundary});
        Check(edgeWorld.updates>refreshes,"direct border skylight changes invalidate existing neighbor corner samples");
        // Population/reseeding may remove a source without incremental callbacks.
        var stale=new ChunkData();stale.worldRef.worldHeight=4;
        for(int y=0;y<4;y++)Add(stale,new Vector3Int(0,y,0),y>=2?BlockType.Stone:BlockType.Air,y>=2?15:0);
        stale.GetBlock(new Vector3Int(0,1,0)).light=14;
        Lighting.InitializeLoadedChunks(new[]{stale});
        Check(stale.GetBlock(new Vector3Int(0,1,0)).light==0,"initial/reseed pass removes light with no remaining source");
        // Beta ChunkCache.getLightValueExt includes tilledField as well as
        // single slabs and stairs. Its top is inset and samples its own cell.
        foreach(var type in new[]{BlockType.Farmland,BlockType.MoistFarmland})
        {
            var chunk=new ChunkData();var p=new Vector3Int(0,64,0);
            var soil=Add(chunk,p,type,15);soil.blockShape.shape=new BlockShape(0,0,0,16,15,16);
            Add(chunk,p+Vector3Int.up).sky=15;
            var mesh=new MeshData(false);BlockHelper.GetFaceDataIn(Direction.up,p,mesh,soil,soil.BlockData,chunk);
            for(int i=0;i<4;i++)Check(mesh.skyLight[i]==15,"inset "+type+" top uses surrounding skylight rather than opaque-cell zero");
        }
        foreach(var row in File.ReadAllLines(columns))
        {
            var fields=row.Split('\t');var opacity=Array.ConvertAll(fields[2].Split(','),int.Parse);var expected=Array.ConvertAll(fields[3].Split(','),int.Parse);
            var data=new ChunkData{worldRef=new World{worldHeight=16,dimension=fields[0]=="1"?WorldDimension.Nether:WorldDimension.Overworld}};
            for(int y=0;y<16;y++)Add(data,new Vector3Int(0,y,0),opacity[y]==15?BlockType.Stone:BlockType.Air,opacity[y]);
            Lighting.RecastSunLight(data,new Vector3Int(0,15,0),fields[1]=="1");
            for(int y=0;y<16;y++)Check(data.GetBlock(new Vector3Int(0,y,0)).sky==expected[y],"vertical skylight attenuation agrees with independent Beta Java reference");
        }
        CheckBlockLightReplay();
    }
    static void CheckBlockLightReplay()
    {
        const int size=7,height=6;
        var data=new ChunkData{chunkSize=size,worldRef=new World{chunkSize=size,worldHeight=height}};
        for(int x=0;x<size;x++)for(int y=0;y<height;y++)for(int z=0;z<size;z++)
        {
            bool wall=x==0||x==size-1||z==0||z==size-1||y==0||y==height-1;
            Add(data,new Vector3Int(x,y,z),wall?BlockType.Stone:BlockType.Air,wall?15:0);
        }
        Lighting.InitializeLoadedChunks(new[]{data});var random=new Random(173);
        for(int edit=0;edit<60;edit++)
        {
            int x=1+random.Next(size-2),y=1+random.Next(height-2),z=1+random.Next(size-2),kind=random.Next(5);
            var cell=data.GetBlock(new Vector3Int(x,y,z));int previous=cell.light;
            cell.type=kind==0?BlockType.Torch:kind==1?BlockType.GlowStone:kind==2?BlockType.Stone:kind==3?BlockType.Water:BlockType.Air;
            cell.BlockData.opacity=(byte)(kind==1||kind==2?15:kind==3?3:0);cell.BlockData.lightEmission=(byte)(kind==0?14:kind==1?15:0);
            cell.light=cell.BlockData.lightEmission;
            if(previous>0)data.blockLightRemoveQueue.Enqueue(new BlockLightNode(cell,(byte)previous));
            if(cell.light>0)data.blockLightUpdateQueue.Enqueue(new BlockLightNode(cell,(byte)cell.light));
            for(int n=0;n<6;n++){var other=cell.GetNeighbor(n);if(other.type!=BlockType.Nothing&&other.light>0)data.blockLightUpdateQueue.Enqueue(new BlockLightNode(other,(byte)other.light));}
            if(edit%9==0){data.lightingInitialized=false;Lighting.InitializeLoadedChunks(new[]{data});}else Lighting.CalculateLight(data);
            var values=new int[size,height,size];bool changed=true;int iterations=0;
            while(changed)
            {
                changed=false;if(++iterations>32)throw new Exception("reference transport did not converge");
                var next=new int[size,height,size];
                for(int xx=0;xx<size;xx++)for(int yy=0;yy<height;yy++)for(int zz=0;zz<size;zz++)
                {
                    var block=data.GetBlock(new Vector3Int(xx,yy,zz));int emission=block.BlockData.lightEmission;
                    int brightest=0;
                    if(xx>0)brightest=Math.Max(brightest,values[xx-1,yy,zz]);if(xx+1<size)brightest=Math.Max(brightest,values[xx+1,yy,zz]);
                    if(yy>0)brightest=Math.Max(brightest,values[xx,yy-1,zz]);if(yy+1<height)brightest=Math.Max(brightest,values[xx,yy+1,zz]);
                    if(zz>0)brightest=Math.Max(brightest,values[xx,yy,zz-1]);if(zz+1<size)brightest=Math.Max(brightest,values[xx,yy,zz+1]);
                    int level=block.BlockData.opacity>=15&&emission==0?0:Math.Max(emission,Math.Max(0,brightest-Math.Max(1,(int)block.BlockData.opacity)));
                    next[xx,yy,zz]=level;if(level!=values[xx,yy,zz])changed=true;
                }
                values=next;
            }
            for(int xx=0;xx<size;xx++)for(int yy=0;yy<height;yy++)for(int zz=0;zz<size;zz++)
                Check(data.GetBlock(new Vector3Int(xx,yy,zz)).light==values[xx,yy,zz],"live source/block edits and reseeding match Beta's independent fixed-point transport oracle");
        }
    }

}

using System;
using System.Threading;
using UnityEngine;

public partial class ChunkData
{
    private int dirtyRenderSections = int.MaxValue;
    public void MarkAllRenderSectionsDirty(){Interlocked.Exchange(ref dirtyRenderSections,int.MaxValue);}

    public void MarkRenderSectionsDirty(int worldY)
    {
        int mask=0;
        for(int y=worldY-1;y<=worldY+1;y++)
            if(y>=0&&y<worldRef.worldHeight)mask|=1<<(y/16);
        int previous;
        do {previous=dirtyRenderSections;}
        while(Interlocked.CompareExchange(ref dirtyRenderSections,previous|mask,previous)!=previous);
    }

    private MeshData BuildSectionMeshData()
    {
        if(HasPendingLighting)UpdateLight();
        int mask=Interlocked.Exchange(ref dirtyRenderSections,0);
        // Callers without a cell footprint (snapshots, trees, portals) request
        // a complete rebuild. Known cell changes retain their smaller mask.
        if(mask==0)mask=int.MaxValue;
        var batch=new MeshData(false){sourceChunk=this,renderSections=new MeshData[sections.Length]};
        try
        {
            for(int index=0;index<sections.Length;index++)
            {
                if((mask&(1<<index))==0)continue;
                var section=sections[index];
                var mesh=new MeshData(true){sourceChunk=this};
                batch.renderSections[index]=mesh;
                for(int x=0;x<chunkSize;x++)for(int y=0;y<16;y++)for(int z=0;z<chunkSize;z++)
                {
                    var type=section.blocks[x,y,z].type;
                    if(type==BlockType.Air||type==BlockType.Nothing)continue;
                    BlockHelper.GetMeshData(this,new Vector3Int(x,y+section.yOffset,z),mesh,type);
                }
                mesh.PrepareForRendering();
            }
            return batch;
        }
        catch
        {
            // Failed builds must retry every claimed section.
            int previous;
            do {previous=dirtyRenderSections;}
            while(Interlocked.CompareExchange(ref dirtyRenderSections,previous|mask,previous)!=previous);
            batch.ReleaseAfterUpload();throw;
        }
    }
}

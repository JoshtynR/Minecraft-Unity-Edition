using System;
using UnityEngine;
public enum BlockType {Stone,Air,Nothing}
public struct Block {public BlockType type;}
public class World {public int worldHeight=128;}
public class ChunkSection {public int yOffset;public Block[,,] blocks=new Block[16,16,16];}
public class MeshData
{
    public ChunkData sourceChunk;public MeshData[] renderSections;public bool released;
    public MeshData(bool main){}
    public void PrepareForRendering(){}
    public void ReleaseAfterUpload(){released=true;if(renderSections!=null)foreach(var section in renderSections)section?.ReleaseAfterUpload();}
}
public static class BlockHelper
{
    public static int visited;public static bool fail;
    public static void GetMeshData(ChunkData data,Vector3Int p,MeshData mesh,BlockType type)
    {if(fail)throw new Exception("injected");visited++;}
}
public partial class ChunkData
{
    public World worldRef=new World();public int chunkSize=16;
    public ChunkSection[] sections=new ChunkSection[8];public bool HasPendingLighting;
    public void UpdateLight(){}
    public MeshData Build()=>BuildSectionMeshData();
}
public static class SectionCheck
{
    static void Check(bool value,string why){if(!value)throw new Exception(why);}
    public static void Main()
    {
        var data=new ChunkData();for(int i=0;i<8;i++)data.sections[i]=new ChunkSection{yOffset=i*16};
        var initial=data.Build();Check(BlockHelper.visited==32768,"initial build visits all sections");
        data.MarkRenderSectionsDirty(40);BlockHelper.visited=0;var changed=data.Build();
        Check(BlockHelper.visited==4096&&changed.renderSections[2]!=null&&changed.renderSections[1]==null,"interior edit builds only one section");
        data.MarkRenderSectionsDirty(31);BlockHelper.visited=0;changed=data.Build();
        Check(BlockHelper.visited==8192&&changed.renderSections[1]!=null&&changed.renderSections[2]!=null,"boundary edit dirties both vertical neighbors");
        data.MarkRenderSectionsDirty(0);BlockHelper.visited=0;changed=data.Build();
        Check(BlockHelper.visited==4096&&changed.renderSections[0]!=null,"bottom halo clamps safely");
        data.MarkRenderSectionsDirty(127);BlockHelper.visited=0;changed=data.Build();
        Check(BlockHelper.visited==4096&&changed.renderSections[7]!=null,"top halo clamps safely");
        data.MarkRenderSectionsDirty(50);BlockHelper.fail=true;
        try{data.Build();throw new Exception("expected failure");}catch(Exception error){if(error.Message!="injected")throw;}
        BlockHelper.fail=false;BlockHelper.visited=0;changed=data.Build();
        Check(BlockHelper.visited==4096&&changed.renderSections[3]!=null,"failed build restores dirty mask");
        changed.ReleaseAfterUpload();Check(changed.renderSections[3].released,"batch release owns section buffers");
        data.MarkAllRenderSectionsDirty();BlockHelper.visited=0;data.Build();
        Check(BlockHelper.visited==32768,"explicit full rebuild remains available");
        Console.WriteLine("8 production render-section scan/dirty-boundary/failure/cleanup checks passed.");
    }
}

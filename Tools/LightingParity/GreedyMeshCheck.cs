using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class LightingCheck
{
    private static void GreedyQuad(MeshData mesh,Direction direction,Vector3Int position,int layer=1,
        float sky=15,float light=0,float ao=1,float tint=1,bool mark=true,bool gradient=false)
    {
        new BlockShape(0,0,0,16,16,16).SetFaceVertices(direction,position,mesh);
        mesh.AddQuadTriangles();mesh.SetLastQuadNormals();mesh.AddFaceUVs(layer);
        mesh.AddQuadColor(new Color(tint,1,1,1));
        for(int i=0;i<4;i++)
        {
            mesh.skyLight.Add(gradient&&i==2?sky-1:sky);mesh.blockLight.Add(light);
            mesh.sides.Add(new Vector3(ao,1,4));mesh.smoothLightCorners.Add(new Vector4(sky,light,0,0));
        }
        if(mark)mesh.MarkLastQuadGreedy();
    }
    private static int GreedyCoordinate(float value)=>(int)Math.Round(value);
    // Rasterize every emitted rectangle back into oriented unit faces. Comparing
    // this set against the unmerged reference catches lost/overlapping faces,
    // winding changes and accidental merges across holes or parallel planes.
    private static Dictionary<string,int> GreedyCoverage(MeshData mesh)
    {
        var result=new Dictionary<string,int>();
        for(int first=0;first<mesh.vertices.Count;first+=4)
        {
            var origin=mesh.vertices[first];var du=mesh.vertices[first+3]-origin;var dv=mesh.vertices[first+1]-origin;
            int width=GreedyCoordinate(Math.Abs(du.x)+Math.Abs(du.y)+Math.Abs(du.z));
            int height=GreedyCoordinate(Math.Abs(dv.x)+Math.Abs(dv.y)+Math.Abs(dv.z));
            var u=new Vector3(du.x/width,du.y/width,du.z/width);var v=new Vector3(dv.x/height,dv.y/height,dv.z/height);
            var normal=new Vector3(v.y*u.z-v.z*u.y,v.z*u.x-v.x*u.z,v.x*u.y-v.y*u.x);
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                var center=new Vector3(origin.x+u.x*(x+.5f)+v.x*(y+.5f),origin.y+u.y*(x+.5f)+v.y*(y+.5f),origin.z+u.z*(x+.5f)+v.z*(y+.5f));
                string key=GreedyCoordinate(center.x*2)+","+GreedyCoordinate(center.y*2)+","+GreedyCoordinate(center.z*2)+":"+
                    GreedyCoordinate(normal.x)+","+GreedyCoordinate(normal.y)+","+GreedyCoordinate(normal.z)+":"+mesh.uv[first].z;
                Check(!result.ContainsKey(key),"greedy coverage contains no duplicate oriented unit face");result[key]=1;
            }
        }
        return result;
    }
    private static void SameGreedyCoverage(Dictionary<string,int> expected,MeshData actual)
    {
        var got=GreedyCoverage(actual);Check(expected.Count==got.Count,"greedy area equals unmerged area");
        foreach(var key in expected.Keys)Check(got.ContainsKey(key),"greedy preserves every oriented unit face and texture layer");
    }
    private static void GreedyChannels(MeshData mesh)
    {
        int count=mesh.vertices.Count;
        Check(mesh.uv.Count==count&&mesh.faceCoordinates.Count==count&&mesh.colors.Count==count&&mesh.normals.Count==count&&
            mesh.lightingUVs.Count==count&&mesh.sides.Count==count&&mesh.smoothLightCorners.Count==count,"greedy retains complete upload channels");
        foreach(int index in mesh.triangles)Check(index>=0&&index<count,"greedy indices are valid");
    }
    private static void CheckGreedyMeshing()
    {
        int before=checks;
        foreach(Direction direction in Enum.GetValues(typeof(Direction)))
        {
            var mesh=new MeshData(true){greedyMeshing=true};
            // Choose both tangent axes independently of mesh winding.
            var normal=direction.GetVector();
            for(int a=0;a<16;a++)for(int b=0;b<16;b++)
                GreedyQuad(mesh,direction,normal.x!=0?new Vector3Int(0,a,b):normal.y!=0?new Vector3Int(a,64,b):new Vector3Int(a,b,0));
            var coverage=GreedyCoverage(mesh);mesh.PrepareForRendering();
            Check(mesh.vertices.Count==4&&mesh.triangles.Count==6&&mesh.greedyQuadsRemoved==255,"16x16 surface becomes one quad in every winding");
            SameGreedyCoverage(coverage,mesh);GreedyChannels(mesh);
            Check(mesh.uv[2].x==16&&mesh.uv[2].y==16&&mesh.sides[0].y==2,"merged texture repeats once per block");
            Check(mesh.normals[0].x==normal.x&&mesh.normals[0].y==normal.y&&mesh.normals[0].z==normal.z,"merged face keeps outward normals");
            mesh.PrepareForRendering();Check(mesh.vertices.Count==4&&mesh.greedyQuadsRemoved==255,"greedy preparation is idempotent");
        }
        foreach(Direction direction in Enum.GetValues(typeof(Direction)))
        {
            var template=new MeshData(false);new BlockShape(0,0,0,16,16,16).SetFaceVertices(direction,new Vector3Int(),template);
            var u=template.vertices[3]-template.vertices[0];var v=template.vertices[1]-template.vertices[0];
            var mesh=new MeshData(true){greedyMeshing=true};
            for(int x=0;x<3;x++)for(int y=0;y<5;y++)GreedyQuad(mesh,direction,new Vector3Int(GreedyCoordinate(u.x*x+v.x*y),64+GreedyCoordinate(u.y*x+v.y*y),GreedyCoordinate(u.z*x+v.z*y)));
            var coverage=GreedyCoverage(mesh);mesh.PrepareForRendering();SameGreedyCoverage(coverage,mesh);
            Check(mesh.vertices.Count==4&&mesh.uv[2].x==3&&mesh.uv[2].y==5,"asymmetric rectangles retain texture orientation on every face");
        }
        // Every input used by either AO mode must agree. A flat AO-off sample
        // can differ even when the smoothed sky/block levels happen to match.
        for(int barrier=0;barrier<7;barrier++)
        {
            var mesh=new MeshData(true){greedyMeshing=true};
            GreedyQuad(mesh,Direction.backwards,new Vector3Int(0,64,0));
            GreedyQuad(mesh,Direction.backwards,new Vector3Int(1,64,0),barrier==0?2:1,
                barrier==1?14:15,barrier==2?1:0,barrier==3?.8f:1,barrier==4?.5f:1,true,barrier==5);
            if(barrier==6)for(int i=4;i<8;i++)mesh.smoothLightCorners[i]=new Vector4(14,0,0,0);
            // Re-mark after altering metadata so the merge compares live inputs.
            mesh.PrepareForRendering();Check(mesh.vertices.Count==8,"material/light/AO/tint/gradient/flat-light barrier prevents merging "+barrier);
        }
        var disabled=new MeshData(true);GreedyQuad(disabled,Direction.up,new Vector3Int(0,64,0));GreedyQuad(disabled,Direction.up,new Vector3Int(1,64,0));
        disabled.PrepareForRendering();Check(disabled.vertices.Count==8&&disabled.greedyQuadsRemoved==0,"disabled path retains unit quads");
        var mixed=new MeshData(true){greedyMeshing=true};
        GreedyQuad(mixed,Direction.up,new Vector3Int(0,64,0));GreedyQuad(mixed,Direction.up,new Vector3Int(1,64,0));
        GreedyQuad(mixed,Direction.up,new Vector3Int(2,64,0),mark:false);
        GreedyQuad(mixed.transparentMesh,Direction.up,new Vector3Int(3,64,0));
        GreedyQuad(mixed.waterMesh,Direction.up,new Vector3Int(4,64,0));
        GreedyQuad(mixed.portalMesh,Direction.up,new Vector3Int(5,64,0));
        mixed.PrepareForRendering();Check(mixed.vertices.Count==20&&mixed.triangles.Count==12,"unmarked geometry survives alongside merged cubes");
        Check(mixed.transparentMesh.triangles[0]==8&&mixed.waterMesh.triangles[0]==12&&mixed.portalMesh.triangles[0]==16,"secondary passes offset against compacted opaque vertices");
        GreedyChannels(mixed);mixed.PrepareForRendering();Check(mixed.vertices.Count==20,"mixed-pass repeated preparation does not append twice");
        mixed.ReleaseAfterUpload();Check(mixed.vertices.Count==0&&mixed.normals.Count==0&&mixed.transparentMesh.vertices.Count==0,"greedy staging still releases recursively");
        var malformed=new MeshData(true){greedyMeshing=true};GreedyQuad(malformed,Direction.up,new Vector3Int(0,64,0));GreedyQuad(malformed,Direction.up,new Vector3Int(1,64,0));
        malformed.triangles[0]=1;malformed.PrepareForRendering();Check(malformed.vertices.Count==8&&malformed.greedyQuadsRemoved==0,"unexpected topology falls back without dropping geometry");
        var random=new Random(173);
        for(int trial=0;trial<80;trial++)
        {
            var mesh=new MeshData(true){greedyMeshing=true};
            foreach(Direction direction in Enum.GetValues(typeof(Direction)))
            {
                var n=direction.GetVector();
                for(int a=0;a<8;a++)for(int b=0;b<8;b++)
                {
                    if(random.Next(5)==0)continue;
                    var p=n.x!=0?new Vector3Int(0,a,b):n.y!=0?new Vector3Int(a,0,b):new Vector3Int(a,b,0);
                    GreedyQuad(mesh,direction,p,random.Next(3)+1);
                }
            }
            var coverage=GreedyCoverage(mesh);mesh.PrepareForRendering();SameGreedyCoverage(coverage,mesh);GreedyChannels(mesh);
        }
        // Exercise actual cube emission/eligibility and real AO sampling.
        var chunk=new ChunkData{chunkSize=16};
        for(int x=-2;x<18;x++)for(int y=62;y<68;y++)for(int z=-2;z<18;z++)Add(chunk,new Vector3Int(x,y,z)).sky=15;
        for(int x=0;x<16;x++)for(int z=0;z<16;z++)Add(chunk,new Vector3Int(x,64,z),BlockType.Stone,15);
        var production=new MeshData(true){greedyMeshing=true};
        for(int x=0;x<16;x++)for(int z=0;z<16;z++)BlockHelper.GetMeshData(chunk,new Vector3Int(x,64,z),production,BlockType.Stone);
        int original=production.vertices.Count/4;var productionCoverage=GreedyCoverage(production);production.PrepareForRendering();
        SameGreedyCoverage(productionCoverage,production);Check(production.greedyQuadsRemoved>250,"production emitter feeds lit flat terrain into greedy pass");
        Console.WriteLine("Greedy flat stone plate: "+original+" -> "+production.triangles.Count/6+" opaque quads (same oriented surface coverage).");
        Console.WriteLine((checks-before)+" greedy coverage/channel/winding/lighting/material/fallback checks passed.");
    }
}

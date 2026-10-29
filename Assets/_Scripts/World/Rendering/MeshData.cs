using System.Collections.Generic;
using UnityEngine;

public class MeshData
{
    public List<Vector3> vertices   = new List<Vector3>();
    public List<Vector3Int> chests;
    public List<int>     triangles  = new List<int>();
    public List<Vector2> faceCoordinates = new List<Vector2>(); // normalized quad coordinates, independent of cropped image UVs
    public List<Vector3> uv         = new List<Vector3>(); // xy = face UV, z = image layer
    public List<Color>   colors     = new List<Color>();   // ← keep only this ONE
    public List<float>   skyLight   = new List<float>();
    public List<float>   blockLight = new List<float>();
    public List<Vector4> smoothLightCorners = new List<Vector4>();
    public List<Vector3> sides      = new List<Vector3>();

    public int subMeshCount;
    public ChunkData sourceChunk;

    public MeshData waterMesh;         // alpha-blended water only
    public MeshData transparentMesh;   // secondary container for transparent faces (if you use it)
    public MeshData portalMesh;        // alpha-blended Nether portal surfaces

    public MeshData(bool isMainMesh)
    {
        if (isMainMesh)
        {
            transparentMesh = new MeshData(false);
            waterMesh = new MeshData(false);
            portalMesh = new MeshData(false);
        }
    }

    private bool prepared;
    public readonly List<Vector2> lightingUVs = new List<Vector2>();
    public readonly List<Vector3> normals = new List<Vector3>();
    private readonly List<bool> directNormals = new List<bool>();

    // Called only after a face's final vertex positions have been established.
    public void SetLastQuadNormals()
    {
        while(normals.Count<vertices.Count)normals.Add(new Vector3());
        while(directNormals.Count<vertices.Count)directNormals.Add(false);
        int first=vertices.Count-4;
        var a=TriangleNormal(first,first+1,first+2);
        var b=TriangleNormal(first,first+2,first+3);
        var shared=Normalize(new Vector3(a.x+b.x,a.y+b.y,a.z+b.z));
        normals[first]=shared;normals[first+1]=Normalize(a);
        normals[first+2]=shared;normals[first+3]=Normalize(b);
        for(int i=first;i<first+4;i++)directNormals[i]=true;
    }

    private static Vector3 Normalize(Vector3 n)
    {
        double length=System.Math.Sqrt((double)n.x*n.x+(double)n.y*n.y+(double)n.z*n.z);
        return length>0?new Vector3((float)(n.x/length),(float)(n.y/length),(float)(n.z/length)):n;
    }

    private Vector3 TriangleNormal(int a,int b,int c)
    {
        var p=vertices[a];var q=vertices[b];var r=vertices[c];
        float ux=q.x-p.x,uy=q.y-p.y,uz=q.z-p.z,vx=r.x-p.x,vy=r.y-p.y,vz=r.z-p.z;
        return new Vector3(uy*vz-uz*vy,uz*vx-ux*vz,ux*vy-uy*vx);
    }
    public Vector3 boundsCenter, boundsSize;

    // Upload preparation is idempotent: a repeated upload must not duplicate
    // transparent geometry or offset its indices for a second time.
    public void PrepareForRendering()
    {
        if (prepared) return;
        AppendGeometry(transparentMesh);
        AppendGeometry(waterMesh);
        AppendGeometry(portalMesh);
        lightingUVs.Capacity = skyLight.Count;
        for (int i = 0; i < skyLight.Count; i++)
            lightingUVs.Add(new Vector2(skyLight[i], blockLight[i]));
        PrepareNormalsAndBounds();
        prepared = true;
    }

    private void PrepareNormalsAndBounds()
    {
        if (vertices.Count == 0) return;
        while(normals.Count<vertices.Count)normals.Add(new Vector3());
        while(directNormals.Count<vertices.Count)directNormals.Add(false);
        var min = vertices[0];
        var max = min;
        foreach (var v in vertices)
        {
            min = new Vector3(System.Math.Min(min.x, v.x), System.Math.Min(min.y, v.y), System.Math.Min(min.z, v.z));
            max = new Vector3(System.Math.Max(max.x, v.x), System.Math.Max(max.y, v.y), System.Math.Max(max.z, v.z));
        }
        AccumulateNormals(triangles);
        if (transparentMesh != null) AccumulateNormals(transparentMesh.triangles);
        if (waterMesh != null) AccumulateNormals(waterMesh.triangles);
        if (portalMesh != null) AccumulateNormals(portalMesh.triangles);
        for (int i = 0; i < normals.Count; i++)
        {
            if(directNormals[i])continue;
            var n = normals[i];
            double length = System.Math.Sqrt((double)n.x*n.x + (double)n.y*n.y + (double)n.z*n.z);
            if (length > 0) normals[i] = new Vector3((float)(n.x/length), (float)(n.y/length), (float)(n.z/length));
        }
        boundsCenter = new Vector3((min.x+max.x)*.5f, (min.y+max.y)*.5f, (min.z+max.z)*.5f);
        boundsSize = new Vector3(max.x-min.x, max.y-min.y, max.z-min.z);
    }

    private void AccumulateNormals(List<int> indices)
    {
        for (int i = 0; i < indices.Count; i += 3)
        {
            int a=indices[i], b=indices[i+1], c=indices[i+2];
            if(directNormals[a]&&directNormals[b]&&directNormals[c])continue;
            var p=vertices[a]; var q=vertices[b]; var r=vertices[c];
            float ux=q.x-p.x, uy=q.y-p.y, uz=q.z-p.z;
            float vx=r.x-p.x, vy=r.y-p.y, vz=r.z-p.z;
            var face = new Vector3(uy*vz-uz*vy, uz*vx-ux*vz, ux*vy-uy*vx);
            AddNormal(a, face); AddNormal(b, face); AddNormal(c, face);
        }
    }

    private void AddNormal(int index, Vector3 face)
    {
        if(directNormals[index])return;
        var current = normals[index];
        normals[index] = new Vector3(current.x+face.x, current.y+face.y, current.z+face.z);
    }

    private void AppendGeometry(MeshData part)
    {
        if (part == null) return;
        int offset = vertices.Count;
        while(normals.Count<offset)normals.Add(new Vector3());
        while(directNormals.Count<offset)directNormals.Add(false);
        while(part.normals.Count<part.vertices.Count)part.normals.Add(new Vector3());
        while(part.directNormals.Count<part.vertices.Count)part.directNormals.Add(false);
        normals.AddRange(part.normals);directNormals.AddRange(part.directNormals);
        for (int i = 0; i < part.triangles.Count; i++) part.triangles[i] += offset;
        vertices.AddRange(part.vertices);
        uv.AddRange(part.uv);
        faceCoordinates.AddRange(part.faceCoordinates);
        colors.AddRange(part.colors);
        skyLight.AddRange(part.skyLight);
        blockLight.AddRange(part.blockLight);
        sides.AddRange(part.sides);
        smoothLightCorners.AddRange(part.smoothLightCorners);
    }

    public void AddFaceUVs(int layer) { AddFaceUVs(layer, new Vector4(1,1,0,0)); }

    public void AddFaceUVs(int layer, Vector4 mapping)
    {
        uv.Add(new Vector3(mapping.z, mapping.w, layer));
        uv.Add(new Vector3(mapping.z, mapping.w+mapping.y, layer));
        uv.Add(new Vector3(mapping.z+mapping.x, mapping.w+mapping.y, layer));
        uv.Add(new Vector3(mapping.z+mapping.x, mapping.w, layer));
        AddQuadFaceCoordinates();
    }

    public void AddQuadFaceCoordinates()
    {
        faceCoordinates.Add(new Vector2(0,0));
        faceCoordinates.Add(new Vector2(0,1));
        faceCoordinates.Add(new Vector2(1,1));
        faceCoordinates.Add(new Vector2(1,0));
    }

    public void AddVertex(Vector3 v)
    {
        vertices.Add(v);
    }

    public void AddColor(Color c)
    {
        colors.Add(c);
    }

    // Add one color per vertex in the face (4 verts per quad)
    public void AddQuadColor(Color c)
    {
        colors.Add(c);
        colors.Add(c);
        colors.Add(c);
        colors.Add(c);
    }

    public void AddQuadTriangles()
    {
        // assumes the last 4 added vertices belong to this face
        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 2);

        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);
    }

    // Unity copies these channels into native Mesh storage during upload. Drop
    // the managed backing arrays immediately so dense Nether chunks do not keep
    // a second complete copy alive until a later full GC.
    public void ReleaseAfterUpload()
    {
        ReleaseLists(this);
        if(transparentMesh!=null)ReleaseLists(transparentMesh);
        if(waterMesh!=null)ReleaseLists(waterMesh);
        if(portalMesh!=null)ReleaseLists(portalMesh);
        chests=null;
    }

    private static void ReleaseLists(MeshData data)
    {
        Release(data.vertices);Release(data.triangles);Release(data.faceCoordinates);Release(data.uv);Release(data.colors);
        Release(data.skyLight);Release(data.blockLight);Release(data.smoothLightCorners);Release(data.sides);
        Release(data.lightingUVs);Release(data.normals);
        Release(data.directNormals);
    }

    private static void Release<T>(List<T> list)
    {
        list.Clear();list.Capacity=0;
    }
}

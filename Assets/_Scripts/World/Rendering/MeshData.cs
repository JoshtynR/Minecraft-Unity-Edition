using System.Collections.Generic;
using UnityEngine;

public partial class MeshData
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
    public List<Vector3> sides      = new List<Vector3>(); // x=AO/shade, y=1 normal/2 tiled UVs, z=lighting mode

    public int subMeshCount;
    public ChunkData sourceChunk;
    public long buildSequence;
    public MeshData[] renderSections;

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
        MergeGreedyFaces();
        greedyFaces?.Clear();
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
        if(renderSections!=null)
            foreach(var section in renderSections)section?.ReleaseAfterUpload();
        ReleaseLists(this);
        if(transparentMesh!=null)ReleaseLists(transparentMesh);
        if(waterMesh!=null)ReleaseLists(waterMesh);
        if(portalMesh!=null)ReleaseLists(portalMesh);
        greedyFaces=null;
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

// Conservative section-local rectangle merging. Only explicitly marked,
// full-cube faces with constant shader inputs participate. Special geometry,
// gradients, overlays and all secondary draw passes retain their original quads.
public partial class MeshData
{
    public bool greedyMeshing;
    public int greedyQuadsRemoved { get; private set; }
    private List<int> greedyFaces;

    public void MarkLastQuadGreedy()
    {
        if(!greedyMeshing)return;
        int first=vertices.Count-4;
        if(first<0||uv.Count!=vertices.Count||colors.Count!=vertices.Count||
            skyLight.Count!=vertices.Count||blockLight.Count!=vertices.Count||
            sides.Count!=vertices.Count||smoothLightCorners.Count!=vertices.Count)return;
        // Only ordinary world-cube lighting and complete, unrotated image UVs.
        if(sides[first].z!=4||sides[first].y!=1)return;
        for(int i=0;i<4;i++)
        {
            var texture=uv[first+i];
            if(texture.x!=(i>=2?1:0)||texture.y!=(i==1||i==2?1:0)||texture.z!=uv[first].z)return;
            if(!SameInputs(first,first+i))return;
        }
        if(greedyFaces==null)greedyFaces=new List<int>();
        greedyFaces.Add(first);
    }

    private bool SameInputs(int a,int b)
    {
        var ca=colors[a];var cb=colors[b];var sa=sides[a];var sb=sides[b];
        var fa=smoothLightCorners[a];var fb=smoothLightCorners[b];
        return ca.r==cb.r&&ca.g==cb.g&&ca.b==cb.b&&ca.a==cb.a&&
            skyLight[a]==skyLight[b]&&blockLight[a]==blockLight[b]&&
            sa.x==sb.x&&sa.y==sb.y&&sa.z==sb.z&&
            fa.x==fb.x&&fa.y==fb.y&&fa.z==fb.z&&fa.w==fb.w&&uv[a].z==uv[b].z;
    }

    private struct FaceCell : System.IEquatable<FaceCell>,System.IComparable<FaceCell>
    {
        public int direction,plane,u,v;
        public FaceCell Offset(int x,int y)=>new FaceCell{direction=direction,plane=plane,u=u+x,v=v+y};
        public bool Equals(FaceCell other)=>direction==other.direction&&plane==other.plane&&u==other.u&&v==other.v;
        public override bool Equals(object other)=>other is FaceCell&&Equals((FaceCell)other);
        public override int GetHashCode()=>unchecked(((direction*397+plane)*397+u)*397+v);
        public int CompareTo(FaceCell other)
        {
            int order=direction.CompareTo(other.direction);if(order!=0)return order;
            order=plane.CompareTo(other.plane);if(order!=0)return order;
            order=v.CompareTo(other.v);return order!=0?order:u.CompareTo(other.u);
        }
    }
    private struct GreedyFace : System.IComparable<GreedyFace>
    {
        public FaceCell cell;public int first;
        public int CompareTo(GreedyFace other)=>cell.CompareTo(other.cell);
    }
    private static int Rounded(float value)=>(int)System.Math.Round(value);
    private static bool UnitEdge(Vector3 edge,out Vector3 unit)
    {
        unit=new Vector3(Rounded(edge.x),Rounded(edge.y),Rounded(edge.z));
        return System.Math.Abs(unit.x)+System.Math.Abs(unit.y)+System.Math.Abs(unit.z)==1&&
            System.Math.Abs(edge.x-unit.x)<.00001f&&System.Math.Abs(edge.y-unit.y)<.00001f&&System.Math.Abs(edge.z-unit.z)<.00001f;
    }
    private bool TryFaceCell(int first,out FaceCell cell)
    {
        cell=new FaceCell();
        var p=vertices[first];
        if(!UnitEdge(vertices[first+3]-p,out var u)||!UnitEdge(vertices[first+1]-p,out var v)||Vector3.Dot(u,v)!=0)return false;
        var expected=new Vector3(p.x+u.x+v.x,p.y+u.y+v.y,p.z+u.z+v.z);
        var diagonal=vertices[first+2]-expected;
        if(System.Math.Abs(diagonal.x)+System.Math.Abs(diagonal.y)+System.Math.Abs(diagonal.z)>.00003f)return false;
        // Winding is v cross u, consistent with AddQuadTriangles.
        var normal=new Vector3(v.y*u.z-v.z*u.y,v.z*u.x-v.x*u.z,v.x*u.y-v.y*u.x);
        int axis=normal.x!=0?0:normal.y!=0?1:2;
        cell=new FaceCell{direction=axis*2+(Vector3.Dot(normal,new Vector3(1,1,1))<0?1:0),
            plane=Rounded(Vector3.Dot(p,normal)),u=Rounded(Vector3.Dot(p,u)),v=Rounded(Vector3.Dot(p,v))};
        return true;
    }

    private void MergeGreedyFaces()
    {
        if(!greedyMeshing||greedyFaces==null||greedyFaces.Count<2)return;
        int count=vertices.Count;
        // The current emitter uses independent quads. Fail closed if a future
        // model introduces shared vertices or a different topology/channel set.
        if(count%4!=0||triangles.Count!=count/4*6||uv.Count!=count||colors.Count!=count||
            faceCoordinates.Count!=count||skyLight.Count!=count||blockLight.Count!=count||sides.Count!=count||smoothLightCorners.Count!=count)return;
        for(int first=0;first<count;first+=4)
        {
            int t=first/4*6;
            if(triangles[t]!=first||triangles[t+1]!=first+1||triangles[t+2]!=first+2||
                triangles[t+3]!=first||triangles[t+4]!=first+2||triangles[t+5]!=first+3)return;
        }
        var cells=new Dictionary<FaceCell,int>(greedyFaces.Count);
        var ordered=new List<GreedyFace>(greedyFaces.Count);
        foreach(int first in greedyFaces)
        {
            if(!TryFaceCell(first,out var cell))continue;
            // Duplicated coplanar surfaces must never disappear accidentally.
            if(cells.ContainsKey(cell))return;
            cells.Add(cell,first);ordered.Add(new GreedyFace{cell=cell,first=first});
        }
        ordered.Sort();
        var used=new bool[count/4];var widths=new int[count/4];var heights=new int[count/4];
        foreach(var face in ordered)
        {
            int q=face.first/4;if(used[q])continue;
            int width=1,height=1;
            while(MatchesCell(cells,face.cell.Offset(width,0),face.first,used))width++;
            bool extend=true;
            while(extend)
            {
                for(int x=0;x<width;x++)if(!MatchesCell(cells,face.cell.Offset(x,height),face.first,used)){extend=false;break;}
                if(extend)height++;
            }
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)used[cells[face.cell.Offset(x,y)]/4]=true;
            widths[q]=width;heights[q]=height;greedyQuadsRemoved+=width*height-1;
        }
        if(greedyQuadsRemoved==0)return;
        while(normals.Count<count)normals.Add(new Vector3());
        while(directNormals.Count<count)directNormals.Add(false);
        triangles.Clear();int destination=0;
        // Compact in place: destination never runs ahead of the source. This
        // avoids allocating a second complete set of vertex staging channels.
        for(int first=0;first<count;first+=4)
        {
            int q=first/4;if(used[q]&&widths[q]==0)continue;
            int width=widths[q]==0?1:widths[q],height=heights[q]==0?1:heights[q];
            bool merged=width>1||height>1;
            var origin=vertices[first];var u=vertices[first+3]-origin;var v=vertices[first+1]-origin;
            // Use exact grid steps instead of multiplying tiny float edge errors.
            if(merged){u=new Vector3(Rounded(u.x),Rounded(u.y),Rounded(u.z));v=new Vector3(Rounded(v.x),Rounded(v.y),Rounded(v.z));}
            for(int i=0;i<4;i++)
            {
                int source=first+i,target=destination+i;
                vertices[target]=vertices[source];uv[target]=uv[source];colors[target]=colors[source];
                faceCoordinates[target]=faceCoordinates[source];skyLight[target]=skyLight[source];blockLight[target]=blockLight[source];
                sides[target]=sides[source];smoothLightCorners[target]=smoothLightCorners[source];normals[target]=normals[source];directNormals[target]=directNormals[source];
                if(merged)
                {
                    int x=i>=2?width:0,y=i==1||i==2?height:0;
                    vertices[target]=new Vector3(origin.x+u.x*x+v.x*y,origin.y+u.y*x+v.y*y,origin.z+u.z*x+v.z*y);
                    uv[target]=new Vector3(x,y,uv[target].z);
                    var side=sides[target];side.y=2;sides[target]=side; // repeated block UVs, same lighting mode
                }
            }
            triangles.Add(destination);triangles.Add(destination+1);triangles.Add(destination+2);
            triangles.Add(destination);triangles.Add(destination+2);triangles.Add(destination+3);destination+=4;
        }
        Trim(vertices,destination);Trim(uv,destination);Trim(colors,destination);Trim(faceCoordinates,destination);
        Trim(skyLight,destination);Trim(blockLight,destination);Trim(sides,destination);Trim(smoothLightCorners,destination);
        Trim(normals,destination);Trim(directNormals,destination);
    }
    private bool MatchesCell(Dictionary<FaceCell,int> cells,FaceCell cell,int first,bool[] used)
    {
        return cells.TryGetValue(cell,out var other)&&!used[other/4]&&SameInputs(first,other);
    }
    private static void Trim<T>(List<T> list,int count){if(list.Count>count)list.RemoveRange(count,list.Count-count);}
}

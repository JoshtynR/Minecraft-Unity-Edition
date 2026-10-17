using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Submit the paper doll through the canvas so it follows UI sorting and clipping.
[RequireComponent(typeof(CanvasRenderer))]
public class InventoryPlayerGraphic : MaskableGraphic
{
    public int PopulateCount { get; private set; }
    public int PreviewVertexCount { get; private set; }
    private MeshData geometry;
    private Texture skin;
    private float pitch, yaw;
    private readonly List<Face> visible = new List<Face>(72);
    private struct Face
    {
        public int a,b,c;
        public Vector3 p,q,r;
        public float depth,shade;
    }
    public override Texture mainTexture => skin!=null?skin:base.mainTexture;
    public void Initialize(MeshData data,Texture texture)
    {
        geometry=data;skin=texture;raycastTarget=false;
        SetMaterialDirty();SetVerticesDirty();
    }
    public void SetLook(float x,float y)
    {
        if(pitch==x&&yaw==y)return;
        pitch=x;yaw=y;SetVerticesDirty();
    }
    private Vector3 Project(Vector3 v)
    {
        // Rotate around the player's center to keep it within the preview window.
        double px=pitch*Math.PI/180,py=yaw*Math.PI/180;
        float x=v.x,y=v.y-1,z=v.z;
        float ry=(float)(y*Math.Cos(px)-z*Math.Sin(px));
        float rz=(float)(y*Math.Sin(px)+z*Math.Cos(px));
        return new Vector3((float)(x*Math.Cos(py)+rz*Math.Sin(py))*29,ry*29,
            (float)(-x*Math.Sin(py)+rz*Math.Cos(py))*29);
    }
    protected override void OnPopulateMesh(VertexHelper helper)
    {
        PopulateCount++;
        PreviewVertexCount=0;
        helper.Clear();visible.Clear();
        if(geometry==null)return;
        for(int i=0;i<geometry.triangles.Count;i+=3)
        {
            int a=geometry.triangles[i],b=geometry.triangles[i+1],c=geometry.triangles[i+2];
            Vector3 p=Project(geometry.vertices[a]),q=Project(geometry.vertices[b]),r=Project(geometry.vertices[c]);
            float ux=q.x-p.x,uy=q.y-p.y,uz=q.z-p.z,vx=r.x-p.x,vy=r.y-p.y,vz=r.z-p.z;
            float nx=uy*vz-uz*vy,ny=uz*vx-ux*vz,nz=ux*vy-uy*vx;
            if(nz>=0)continue;
            double length=Math.Sqrt(nx*nx+ny*ny+nz*nz);
            float shade=.6f+.4f*(float)Math.Max(0,(-.3*nx+.8*ny-.6*nz)/(length*Math.Sqrt(1.09)));
            visible.Add(new Face{a=a,b=b,c=c,p=p,q=q,r=r,depth=(p.z+q.z+r.z)/3,shade=shade});
        }
        // Canvas geometry has no model depth buffer: draw the far faces first.
        visible.Sort((a,b)=>b.depth.CompareTo(a.depth));
        foreach(var face in visible)
        {
            int first=helper.currentVertCount;
            Add(helper,face.a,face.p,face.shade);Add(helper,face.b,face.q,face.shade);Add(helper,face.c,face.r,face.shade);
            helper.AddTriangle(first,first+1,first+2);
        }
        PreviewVertexCount=helper.currentVertCount;
    }
    private void Add(VertexHelper helper,int index,Vector3 p,float shade)
    {
        var vertex=UIVertex.simpleVert;
        vertex.position=new Vector3(p.x,p.y,0);
        vertex.uv0=new Vector2(geometry.uv[index].x,geometry.uv[index].y);
        vertex.color=new Color(color.r*shade,color.g*shade,color.b*shade,color.a);
        helper.AddVert(vertex);
    }
}

using UnityEngine;

// Classic arrow sheet: two crossed strips and an end cap, aligned to +Z.
public static class MobProjectileGeometry
{
    public static MeshData Build()
    {
        var data=new MeshData(false);const float w=.1125f,l=.45f;
        Quad(data,new[]{new Vector3(0,-w,-l),new Vector3(0,-w,l),new Vector3(0,w,l),new Vector3(0,w,-l)},0,.5f,0,5/32f);
        Quad(data,new[]{new Vector3(-w,0,-l),new Vector3(-w,0,l),new Vector3(w,0,l),new Vector3(w,0,-l)},0,.5f,0,5/32f);
        Quad(data,new[]{new Vector3(-w,-w,-l),new Vector3(w,-w,-l),new Vector3(w,w,-l),new Vector3(-w,w,-l)},0,5/32f,5/32f,10/32f);
        return data;
    }
    private static void Quad(MeshData data,Vector3[] points,float left,float right,float top,float bottom)
    {
        var uv=new[]{new Vector3(left,1-bottom,0),new Vector3(right,1-bottom,0),new Vector3(right,1-top,0),new Vector3(left,1-top,0)};
        for(int side=0;side<2;side++)
        {for(int i=0;i<4;i++){int index=side==0?i:3-i;data.AddVertex(points[index]);data.uv.Add(uv[index]);}data.AddQuadTriangles();}
    }
}

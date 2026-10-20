using UnityEngine;

// Classic ModelCow dimensions and box UVs, converted from Y-down/-Z-forward
// into Unity Y-up/+Z-forward. No atlas coordinates from Block Data are used.
public static class CowModelGeometry
{
    public const int PartCount = 9;
    public static Vector3 Pivot(int part)
    {
        if (part == 0) return Point(0,5,2);
        if (part == 1) return Point(0,4,-8);
        if (part == 2 || part == 3) return Point(0,3,-7);
        if (part == 4) return Point(0,14,6);
        return Point((part == 5 || part == 7) ? -4 : 4, 12, part < 7 ? 7 : -6);
    }
    private static Vector3 Point(float x,float y,float z) => new Vector3(x,24-y,-z)/16f;
    public static MeshData Build(int part, float textureHeight = 32)
    {
        var data = new MeshData(false);
        if (part == 0) Box(data,-6,-10,-7,12,18,10,18,4,textureHeight);
        else if (part == 1) Box(data,-4,-4,-6,8,8,6,0,0,textureHeight);
        else if (part == 2) Box(data,-4,-5,-4,1,3,1,22,0,textureHeight);
        else if (part == 3) Box(data,3,-5,-4,1,3,1,22,0,textureHeight);
        else if (part == 4) Box(data,-2,-3,0,4,6,2,52,0,textureHeight);
        else Box(data,-2,0,-2,4,12,4,0,16,textureHeight);
        return data;
    }
    private static void Box(MeshData data,float x,float y,float z,int w,int h,int d,int u,int v,float texHeight)
    {
        var p = new[] {
            new Vector3(x,-y,-z)/16f,new Vector3(x+w,-y,-z)/16f,
            new Vector3(x+w,-y-h,-z)/16f,new Vector3(x,-y-h,-z)/16f,
            new Vector3(x,-y,-z-d)/16f,new Vector3(x+w,-y,-z-d)/16f,
            new Vector3(x+w,-y-h,-z-d)/16f,new Vector3(x,-y-h,-z-d)/16f };
        Face(data,p,5,1,2,6,u+d+w,v+d,d,h,texHeight);
        Face(data,p,0,4,7,3,u,v+d,d,h,texHeight);
        Face(data,p,5,4,0,1,u+d,v,w,d,texHeight);
        Face(data,p,2,3,7,6,u+d+w,v,w,d,texHeight);
        Face(data,p,1,0,3,2,u+d,v+d,w,h,texHeight);
        Face(data,p,4,5,6,7,u+2*d+w,v+d,w,h,texHeight);
    }
    private static void Face(MeshData data,Vector3[] p,int a,int b,int c,int d,float u,float v,float w,float h,float texHeight)
    {
        data.AddVertex(p[a]); data.AddVertex(p[b]); data.AddVertex(p[c]); data.AddVertex(p[d]);
        data.AddQuadTriangles();
        // Beta's 0.1-pixel inset keeps adjacent box faces out of point samples.
        float left=(u+.1f)/64f,right=(u+w-.1f)/64f,top=1-(v+.1f)/texHeight,bottom=1-(v+h-.1f)/texHeight;
        data.uv.Add(new Vector3(right,top,0)); data.uv.Add(new Vector3(left,top,0));
        data.uv.Add(new Vector3(left,bottom,0)); data.uv.Add(new Vector3(right,bottom,0));
    }
}

using UnityEngine;

// Modern chest and paired halves: model units are sixteenths, atlas units sixty-fourths.
public static class ChestModelGeometry
{
    public static MeshData Build(int part,int half=0)
    {
        var data=new MeshData(false);
        int x=half>0?0:1,width=half==0?14:15;
        if(part==0)Box(data,x,0,1,width,10,14,0,19,false);
        else if(part==1)Box(data,x,9,1,width,5,14,0,0,true);
        else Box(data,half==0?7:half<0?15:0,7,0,half==0?2:1,4,1,0,0,true);
        return data;
    }
    private static void Box(MeshData data,float x,float y,float z,int width,int height,int depth,int u,int v,bool hinged)
    {
        var box=new BlockShape(x,y,z,x+width,y+height,z+depth);
        foreach(var direction in BlockHelper.directions)
        {
            int start=data.vertices.Count;box.SetFaceVertices(direction,Vector3Int.zero,data);data.AddQuadTriangles();
            float sx=u+depth,sy=v+depth,sw=width,sh=height;
            // Entity box caps use the opposite atlas rectangles to terrain cubes.
            // The right cap is the body's dark interior; the left cap is its base.
            if(direction==Direction.up){sx=u+depth+width;sy=v;sh=depth;}
            if(direction==Direction.down){sy=v;sh=depth;}
            if(direction==Direction.left){sx=u;sw=depth;}
            if(direction==Direction.right){sx=u+depth+width;sw=depth;}
            // Modern chest front is the local -Z face with the latch. The
            // opposite atlas strip carries the rear hinge artwork.
            if(direction==Direction.backwards)sx=u+2*depth+width;
            float left=sx/64f,right=(sx+sw)/64f,top=1-sy/64f,bottom=1-(sy+sh)/64f;
            // Chest entity sides run from model Y-min at the atlas top to Y-max
            // at the atlas bottom. Block-face UVs have the reverse V direction.
            data.AddFaceUVs(0,new Vector4(sw/64f,-sh/64f,left,top));
            if(direction==Direction.up)
            {
                data.uv[start]=new Vector3(left,top,0);data.uv[start+1]=new Vector3(right,top,0);
                data.uv[start+2]=new Vector3(right,bottom,0);data.uv[start+3]=new Vector3(left,bottom,0);
            }
            else if(direction==Direction.down)
            {
                data.uv[start]=new Vector3(left,bottom,0);data.uv[start+1]=new Vector3(right,bottom,0);
                data.uv[start+2]=new Vector3(right,top,0);data.uv[start+3]=new Vector3(left,top,0);
            }
            for(int i=start;i<data.vertices.Count;i++)
            {
                var p=data.vertices[i];
                // BlockShape's seam offset is for chunks, not separate entity meshes.
                p.x-=.5005f;p.z-=.5005f;p.y-=.0005f;
                if(hinged){p.y-=9/16f;p.z-=7/16f;}
                data.vertices[i]=p;
            }
        }
    }
    public static float LidAngle(float progress)
    {progress=Mathf.Clamp01(progress);return (1-Mathf.Pow(1-progress,3))*90;}
    public static float Facing(byte state) => state==3?180:state==4?90:state==5?-90:0;
}

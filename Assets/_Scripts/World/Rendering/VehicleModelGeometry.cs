using System;
using System.Collections.Generic;
using UnityEngine;

// Classic 64x32 ModelRenderer box layout. Geometry and UVs are transformed
// together, including the rotated floor and minecart's separate inner liner.
public static class VehicleModelGeometry
{
    public sealed class Data
    {
        public readonly List<Vector3> vertices=new List<Vector3>();
        public readonly List<Vector2> uv=new List<Vector2>();
        public readonly List<int> triangles=new List<int>();
    }
    public static Data Build(VehicleKind kind)
    {
        bool boat=kind==VehicleKind.Boat;
        int length=boat?24:20,height=boat?6:8,width=boat?20:16;
        var data=new Data();
        Box(data,-length/2,-width/2+(boat?2:0),boat?-3:-1,length,width-(boat?4:0),boat?4:2,0,boat?8:10,0,4,0,Math.PI/2,0,boat);
        for(int side=0;side<4;side++)
        {
            float px=side==0?-length/2+1:side==1?length/2-1:0;
            float pz=side==2?-width/2+1:side==3?width/2-1:0;
            double angle=side==0?Math.PI*1.5:side==1?Math.PI/2:side==2?Math.PI:0;
            Box(data,-length/2+2,-height-1,-1,length-4,height,2,0,0,px,4,pz,0,angle,boat);
        }
        if(!boat)Box(data,-length/2+1,-width/2+1,-1,length-2,width-2,1,44,10,0,4.1f,0,-Math.PI/2,0,false,true);
        return data;
    }
    private static void Box(Data data,float x,float y,float z,int w,int h,int d,int u,int v,float px,float py,float pz,double rx,double ry,bool boat,bool liner=false)
    {
        var corners=new[]{new Vector3(x,y,z),new Vector3(x+w,y,z),new Vector3(x+w,y+h,z),new Vector3(x,y+h,z),
            new Vector3(x,y,z+d),new Vector3(x+w,y,z+d),new Vector3(x+w,y+h,z+d),new Vector3(x,y+h,z+d)};
        int[,] faces={{5,1,2,6},{0,4,7,3},{5,4,0,1},{2,3,7,6},{1,0,3,2},{4,5,6,7}};
        int[,] rect={{u+d+w,v+d,u+d+w+d,v+d+h},{u,v+d,u+d,v+d+h},{u+d,v,u+d+w,v+d},
            {u+d+w,v,u+d+w+w,v+d},{u+d,v+d,u+d+w,v+d+h},{u+d+w+d,v+d,u+d+w+d+w,v+d+h}};
        for(int face=0;face<6;face++)
        {
            // Only the liner's upper surface is exposed; the other five faces
            // are buried in the floor and some wrap outside the 64-pixel skin.
            if(liner&&face!=4)continue;
            int first=data.vertices.Count;
            for(int j=0;j<4;j++)
            {
                var p=corners[faces[face,j]];
                float yy=(float)(p.y*Math.Cos(rx)-p.z*Math.Sin(rx)),zz=(float)(p.y*Math.Sin(rx)+p.z*Math.Cos(rx));
                float xx=(float)(p.x*Math.Cos(ry)+zz*Math.Sin(ry));zz=(float)(-p.x*Math.Sin(ry)+zz*Math.Cos(ry));
                // Convert model Y-down to world Y-up, put the bottom at zero,
                // and orient the long axis along the vehicle's forward axis.
                data.vertices.Add(new Vector3(-(zz+pz),(boat?7:5)-(yy+py),xx+px)/16f);
                float s=(j==0||j==3?rect[face,2]-.1f:rect[face,0]+.1f)/64f;
                float t=1-(j<2?rect[face,1]+.1f:rect[face,3]-.1f)/32f;
                data.uv.Add(new Vector2(s,t));
            }
            // Reflection of Y reverses the original entity winding.
            data.triangles.Add(first);data.triangles.Add(first+2);data.triangles.Add(first+1);
            data.triangles.Add(first);data.triangles.Add(first+3);data.triangles.Add(first+2);
        }
    }
}

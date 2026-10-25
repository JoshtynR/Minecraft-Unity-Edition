using System;
using UnityEngine;

public static class BlockTargeting
{
    private static float Axis(Vector3 v,int a) { return a==0?v.x:a==1?v.y:v.z; }
    private static Vector3Int Normal(int axis,int sign) { return new Vector3Int(axis==0?sign:0,axis==1?sign:0,axis==2?sign:0); }

    private static bool RayBox(BlockShape s,Vector3Int cell,Vector3 origin,Vector3 direction,out float distance,out Vector3Int normal)
    {
        var min=new Vector3(cell.x+s.xMin/16f,cell.y+s.yMin/16f,cell.z+s.zMin/16f);
        var max=new Vector3(cell.x+s.xMax/16f,cell.y+s.yMax/16f,cell.z+s.zMax/16f);
        float near=float.NegativeInfinity,far=float.PositiveInfinity;
        var entry=new Vector3Int();var exit=new Vector3Int();
        for(int axis=0;axis<3;axis++)
        {
            float d=Axis(direction,axis),o=Axis(origin,axis);
            if(Math.Abs(d)<0.0000001f)
            {
                if(o<Axis(min,axis)||o>Axis(max,axis)) {distance=0;normal=new Vector3Int();return false;}
                continue;
            }
            float a=(Axis(min,axis)-o)/d,b=(Axis(max,axis)-o)/d;
            var an=Normal(axis,-1);var bn=Normal(axis,1);
            if(a>b) {float t=a;a=b;b=t;var n=an;an=bn;bn=n;}
            if(a>near) {near=a;entry=an;}
            if(b<far) {far=b;exit=bn;}
            if(near>far) {distance=0;normal=new Vector3Int();return false;}
        }
        distance=near>=0?near:far;normal=near>=0?entry:exit;
        return distance>=0 && !float.IsInfinity(distance);
    }

    public static Block Raycast(World world,Vector3 origin,Vector3 direction,float reach,out Vector3Int adjacent,out Vector3 hitPoint)
    {
        adjacent=new Vector3Int();hitPoint=new Vector3();
        float length=direction.magnitude;
        if(world==null||length<0.0000001f||reach<=0) return default(Block);
        direction=new Vector3(direction.x/length,direction.y/length,direction.z/length);
        var cell=new Vector3Int(Mathf.FloorToInt(origin.x),Mathf.FloorToInt(origin.y),Mathf.FloorToInt(origin.z));
        int sx=Math.Sign(direction.x),sy=Math.Sign(direction.y),sz=Math.Sign(direction.z);
        float tx=sx==0?float.PositiveInfinity:(cell.x+(sx>0?1:0)-origin.x)/direction.x;
        float ty=sy==0?float.PositiveInfinity:(cell.y+(sy>0?1:0)-origin.y)/direction.y;
        float tz=sz==0?float.PositiveInfinity:(cell.z+(sz>0?1:0)-origin.z)/direction.z;
        float dx=sx==0?float.PositiveInfinity:Math.Abs(1/direction.x);
        float dy=sy==0?float.PositiveInfinity:Math.Abs(1/direction.y);
        float dz=sz==0?float.PositiveInfinity:Math.Abs(1/direction.z);
        float entered=0;
        for(int visited=0;visited<512&&entered<=reach;visited++)
        {
            var block=world.GetBlock(cell);
            if(block!=null&&HandMining.IsTarget(block.type))
            {
                var shape=block.blockShape.GetShape();float distance;Vector3Int normal;
                if(RayBox(shape,cell,origin,direction,out distance,out normal)&&distance<=reach)
                {
                    adjacent=new Vector3Int(cell.x+normal.x,cell.y+normal.y,cell.z+normal.z);
                    var p=origin+direction*distance;
                    // Keep the reported point just inside the box for server
                    // validation despite floating-point rounding at the face.
                    hitPoint=new Vector3(
                        Mathf.Clamp(p.x-normal.x*0.0001f,cell.x+shape.xMin/16f,cell.x+shape.xMax/16f),
                        Mathf.Clamp(p.y-normal.y*0.0001f,cell.y+shape.yMin/16f,cell.y+shape.yMax/16f),
                        Mathf.Clamp(p.z-normal.z*0.0001f,cell.z+shape.zMin/16f,cell.z+shape.zMax/16f));
                    return block;
                }
            }
            if(tx<=ty&&tx<=tz) {entered=tx;tx+=dx;cell.x+=sx;}
            else if(ty<=tz) {entered=ty;ty+=dy;cell.y+=sy;}
            else {entered=tz;tz+=dz;cell.z+=sz;}
        }
        return default(Block);
    }
}

using System;
using UnityEngine;

public enum VehicleKind : byte { Minecart, Boat }

[Serializable]
public sealed class VehicleState
{
    public int id;
    public VehicleKind kind;
    public Vector3 position, velocity;
    public float yaw;
    public bool occupied;
    public uint riderId;
    [NonSerialized] public Player driver;
    [NonSerialized] public float throttle, steer, inputYaw, inputExpires;
    public VehicleState Copy(bool saving=false)=>new VehicleState{id=id,kind=kind,position=position,velocity=velocity,yaw=yaw,occupied=!saving&&occupied,riderId=saving?0:riderId};
}

// Fixed 20 Hz simulation; velocities are blocks/second, never frame deltas.
// No Unity physics colliders or rigidbodies are required by the simulation.
public static class VehiclePhysics
{
    public const float Step=.05f;
    public static float Radius(VehicleKind kind)=>kind==VehicleKind.Boat?.7f:.49f;
    public static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
    public static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
    public static void RailEnds(int shape,out Vector3 a,out Vector3 b)
    {
        a=new Vector3(.5f,0,0);b=new Vector3(.5f,0,1);
        switch(shape)
        {
            case 1:a=new Vector3(0,0,.5f);b=new Vector3(1,0,.5f);break;
            case 2:a=new Vector3(0,0,.5f);b=new Vector3(1,1,.5f);break;
            case 3:a=new Vector3(0,1,.5f);b=new Vector3(1,0,.5f);break;
            case 4:a.y=1;break;
            case 5:b.y=1;break;
            case 6:a=new Vector3(.5f,0,1);b=new Vector3(1,0,.5f);break;
            case 7:a=new Vector3(.5f,0,1);b=new Vector3(0,0,.5f);break;
            case 8:a=new Vector3(.5f,0,0);b=new Vector3(0,0,.5f);break;
            case 9:a=new Vector3(.5f,0,0);b=new Vector3(1,0,.5f);break;
        }
    }
    public static Vector3 RailPoint(int shape,float t)
    {
        RailEnds(shape,out var a,out var b);
        if(shape<6)return Vector3.Lerp(a,b,t);
        var c=new Vector3(.5f,0,.5f);float u=1-t;
        return a*(u*u)+c*(2*u*t)+b*(t*t);
    }
    public static Vector3 RailTangent(int shape,float t)
    {
        RailEnds(shape,out var a,out var b);
        return shape<6?(b-a).normalized:((new Vector3(.5f,0,.5f)-a)*(1-t)+(b-new Vector3(.5f,0,.5f))*t).normalized;
    }
    public static float RailParameter(int shape,Vector3 point)
    {
        // Bounded search also handles curved track without a per-cart allocation.
        float best=0,distance=float.MaxValue;
        for(int i=0;i<=32;i++)
        {
            float t=i/32f;var delta=RailPoint(shape,t)-point;
            float d=delta.x*delta.x+delta.z*delta.z;
            if(d<distance){distance=d;best=t;}
        }
        float lo=Mathf.Max(0,best-1/32f),hi=Mathf.Min(1,best+1/32f);
        for(int i=0;i<10;i++)
        {
            float l=(2*lo+hi)/3,r=(lo+2*hi)/3;
            var dl=RailPoint(shape,l)-point;var dr=RailPoint(shape,r)-point;
            if(dl.x*dl.x+dl.z*dl.z<dr.x*dr.x+dr.z*dr.z)hi=r;else lo=l;
        }
        return (lo+hi)*.5f;
    }
    public static void BoatVelocity(VehicleState v,float submerged,float dt)
    {
        submerged=Mathf.Clamp01(submerged);
        // Empty hull settles partly submerged; upward acceleration is capped
        // and damped, so a full water voxel cannot launch it out of the lake.
        v.velocity.y+=(-9.8f+submerged*24f)*dt;
        v.velocity.y*=Mathf.Pow(submerged>0?.75f:.98f,dt/Step);
        if(submerged>0)
        {
            v.yaw+=v.steer*75f*dt;
            float angle=v.yaw*Mathf.Deg2Rad;
            v.velocity+=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(v.throttle*4f*dt);
        }
        float drag=Mathf.Pow(submerged>0?.97f:.9f,dt/Step);
        v.velocity.x*=drag;v.velocity.z*=drag;
        var horizontal=new Vector3(v.velocity.x,0,v.velocity.z);
        if(horizontal.magnitude>7){horizontal=horizontal.normalized*7;v.velocity.x=horizontal.x;v.velocity.z=horizontal.z;}
    }
}

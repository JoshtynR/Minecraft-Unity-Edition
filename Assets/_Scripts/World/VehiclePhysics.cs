using System;
using UnityEngine;

public enum VehicleKind : byte { Minecart, Boat }

[Serializable]
public sealed class VehicleTuning
{
    // Converted from blocks/tick to blocks/second. Turn speed follows travel,
    // not the steering key; buoyancy is the fully submerged rise acceleration.
    public float boatMaxSpeed=8,boatThrust=1.42688f,boatTurnSpeed=400,boatBuoyancy=2.8f,boatGravity=16,boatWaterDrag=.99f;
    public float cartMaxSpeed=8,cartSlopeAcceleration=8,cartPoweredAcceleration=6;
    public static VehicleTuning Current=new VehicleTuning();
    private static float Limit(float value,float fallback,float min,float max)=>VehiclePhysics.Finite(value)?Mathf.Clamp(value,min,max):fallback;
    public VehicleTuning Validated()=>new VehicleTuning{
        boatMaxSpeed=Limit(boatMaxSpeed,8,.1f,20),boatThrust=Limit(boatThrust,1.42688f,0,50),boatTurnSpeed=Limit(boatTurnSpeed,400,0,1440),
        boatBuoyancy=Limit(boatBuoyancy,2.8f,0,100),boatGravity=Limit(boatGravity,16,0,50),boatWaterDrag=Limit(boatWaterDrag,.99f,0,1),
        cartMaxSpeed=Limit(cartMaxSpeed,8,.1f,20),cartSlopeAcceleration=Limit(cartSlopeAcceleration,8,0,50),cartPoweredAcceleration=Limit(cartPoweredAcceleration,6,0,50)};
}

[Serializable]
public sealed class VehicleState
{
    public int id;
    public VehicleKind kind;
    public Vector3 position, velocity;
    public float yaw;
    public bool occupied;
    public uint riderId;
    public int damage,hitTicks,rockDirection=1;
    [NonSerialized] public Player driver;
    [NonSerialized] public float throttle, steer, inputYaw, inputExpires;
    [NonSerialized] public bool onGround;
    public VehicleState Copy(bool saving=false)=>new VehicleState{id=id,kind=kind,position=position,velocity=velocity,yaw=yaw,occupied=!saving&&occupied,riderId=saving?0:riderId,damage=saving?0:damage,hitTicks=saving?0:hitTicks,rockDirection=saving?1:(rockDirection<0?-1:1)};
}

// Fixed 20 Hz simulation; velocities are blocks/second, never frame deltas.
// No Unity physics colliders or rigidbodies are required by the simulation.
public static class VehiclePhysics
{
    public const float Step=.05f;
    public static float Radius(VehicleKind kind)=>kind==VehicleKind.Boat?.75f:.49f;
    public static Vector3 SeatPosition(VehicleState v)
    {
        float a=v.yaw*Mathf.Deg2Rad;
        return v.position+(v.kind==VehicleKind.Boat?new Vector3(-Mathf.Sin(a)*.4f,-.5f,-Mathf.Cos(a)*.4f):Vector3.up*-.45f);
    }
    public static float BoatRock(VehicleState v)=>v.hitTicks>0?Mathf.Sin(v.hitTicks)*v.hitTicks*v.damage/10f*v.rockDirection:0;
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
        var settings=VehicleTuning.Current;
        submerged=Mathf.Clamp01(submerged);
        if(submerged<1)v.velocity.y+=settings.boatGravity*(submerged*2-1)*dt;
        else
        {
            if(v.velocity.y<0)v.velocity.y*=Mathf.Pow(.5f,dt/Step);
            v.velocity.y+=settings.boatBuoyancy*dt;
        }
        // Entity.updateRidden resets rider motion each tick. Airborne
        // moveFlying(.02), input damping .98 and air/water friction feed the boat
        // at 20%, expressed here as a camera-relative acceleration.
        if(v.occupied)
        {
            float length=Mathf.Max(1,(float)Math.Sqrt(v.steer*v.steer+v.throttle*v.throttle));
            float a=v.inputYaw*Mathf.Deg2Rad,s=Mathf.Sin(a),c=Mathf.Cos(a);
            float riderDrag=submerged>0?.8f/.91f:1;
            v.velocity+=new Vector3(v.steer*c+v.throttle*s,0,v.throttle*c-v.steer*s)*(settings.boatThrust*riderDrag*dt/length);
        }
        // The reference caps each axis independently, not the radial speed.
        v.velocity.x=Mathf.Clamp(v.velocity.x,-settings.boatMaxSpeed,settings.boatMaxSpeed);
        v.velocity.z=Mathf.Clamp(v.velocity.z,-settings.boatMaxSpeed,settings.boatMaxSpeed);
        if(v.onGround)v.velocity*=Mathf.Pow(.5f,dt/Step);
    }
    public static void BoatDrag(VehicleState v)
    {
        v.velocity.x*=VehicleTuning.Current.boatWaterDrag;
        v.velocity.z*=VehicleTuning.Current.boatWaterDrag;
        v.velocity.y*=.95f;
    }
    public static void BoatHeading(VehicleState v,Vector3 travel)
    {
        if(travel.x*travel.x+travel.z*travel.z<=.001f)return;
        float desired=Mathf.Atan2(travel.x,travel.z)*Mathf.Rad2Deg;
        float delta=(desired-v.yaw)%360;
        if(delta>=180)delta-=360;if(delta< -180)delta+=360;
        float limit=VehicleTuning.Current.boatTurnSpeed*Step;
        v.yaw+=Mathf.Clamp(delta,-limit,limit);
    }
}

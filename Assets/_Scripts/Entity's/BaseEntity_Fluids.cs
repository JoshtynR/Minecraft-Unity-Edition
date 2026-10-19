using UnityEngine;

public abstract partial class BaseEntity
{
    public bool InWater {get;private set;}
    public bool InLava {get;private set;}
    protected bool swimJumpHeld;
    private FluidVector fluidMotion,fluidCurrent;
    private float fluidMovementTime;
    private bool wasInFluid;
    protected void ResetFluidMovement()
    {
        fluidMotion=new FluidVector();fluidMovementTime=0;wasInFluid=false;
        velocity=extraForce=Vector3.zero;verticalMomentum=0;jumpRequest=swimJumpHeld=false;
        RefreshFluidContact();
    }

    private void RefreshFluidContact()
    {
        InWater=FluidWorldQueries.Touches(world,transform.position,entityWidth,entityHeight,BlockType.Water,out fluidCurrent);
        InLava=FluidWorldQueries.Touches(world,transform.position,entityWidth,entityHeight,BlockType.Lava,out _);
    }

    private bool CalculateFluidVelocity()
    {
        bool immersed=!isFlying&&(InWater||InLava);
        if(!immersed)
        {
            if(wasInFluid)verticalMomentum=(float)fluidMotion.y*20;
            wasInFluid=false;fluidMovementTime=0;return false;
        }
        if(!wasInFluid)
        {
            fluidMotion=new FluidVector(velocity.x/Mathf.Max(Time.fixedDeltaTime,.001f)/20,verticalMomentum/20,velocity.z/Mathf.Max(Time.fixedDeltaTime,.001f)/20);
            wasInFluid=true;
        }
        isSprinting=false;jumpRequest=false;fluidMovementTime+=Time.fixedDeltaTime;
        velocity=Vector3.zero;
        while(fluidMovementTime+.000001f>=.05f)
        {
            fluidMovementTime=Mathf.Max(0,fluidMovementTime-.05f);
            var r=direction.right;var f=direction.forward;
            // Sneaking reduces the input as in EntityPlayerSP; it is not modern
            // sprint swimming or a separate dive acceleration.
            double input=isCrouching?.3:1;
            fluidMotion=BetaFluidPhysics.Accelerate(fluidMotion,horizontal*input,vertical*input,
                new FluidVector(r.x,0,r.z),new FluidVector(f.x,0,f.z),swimJumpHeld,fluidCurrent,!InWater);
            var desired=new Vector3((float)fluidMotion.x,(float)fluidMotion.y,(float)fluidMotion.z);
            var start=transform.position+velocity;
            var moved=BlockCollision.Resolve(world,start,entityWidth,entityHeight,desired,false);
            bool wall=Mathf.Abs(moved.x-desired.x)>.00001f||Mathf.Abs(moved.z-desired.z)>.00001f;
            if(moved.x!=desired.x)fluidMotion.x=0;
            if(moved.y!=desired.y)fluidMotion.y=0;
            if(moved.z!=desired.z)fluidMotion.z=0;
            velocity+=moved;
            fluidMotion=BetaFluidPhysics.Drag(fluidMotion,!InWater);
            if(wall)
            {
                var rise=new Vector3(desired.x,.6f+ (float)fluidMotion.y-moved.y,desired.z);
                var clear=BlockCollision.Resolve(world,start+moved,entityWidth,entityHeight,rise,false);
                if((clear-rise).sqrMagnitude<.000001f&&
                    !FluidWorldQueries.Touches(world,start+moved+rise,entityWidth,entityHeight,BlockType.Water,out _)&&
                    !FluidWorldQueries.Touches(world,start+moved+rise,entityWidth,entityHeight,BlockType.Lava,out _))fluidMotion.y=.3;
            }
        }
        verticalMomentum=(float)fluidMotion.y*20;
        isGrounded=BlockCollision.Supported(world,transform.position+velocity,entityWidth,entityHeight);
        return true;
    }
}

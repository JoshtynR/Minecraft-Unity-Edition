using Mirror;
using UnityEngine;

public partial class Player
{
    [SyncVar] public int airSupply=300;
    [SyncVar] public bool burning;
    private readonly FluidHazards fluidHazards=new FluidHazards();
    private float fluidHazardTime;
    private FluidCameraEffects fluidView;
    private bool fluidViewContactKnown,fluidViewWater;
    private float fluidBubbleTime;
    private void AdvanceFluidHazards(float dt)
    {
        if(world==null||!world.IsWorldCreated||health<=0)return;
        fluidHazardTime+=dt;
        bool immune=(!networkStarted||isLocalPlayer)?isFlying:serverFlying;
        bool water=FluidWorldQueries.Touches(world,transform.position,entityWidth,entityHeight,BlockType.Water,out _);
        bool lava=FluidWorldQueries.Touches(world,transform.position,entityWidth,entityHeight,BlockType.Lava,out _);
        bool fire=FluidWorldQueries.Touches(world,transform.position,entityWidth,entityHeight,BlockType.Fire,out _);
        bool head=FluidWorldQueries.AtEye(world,transform.position+Vector3.up*eyeHeight)==BlockType.Water;
        while(fluidHazardTime+.000001f>=.05f)
        {
            fluidHazardTime=Mathf.Max(0,fluidHazardTime-.05f);
            health=Mathf.Max(0,health-fluidHazards.Tick(water,head,lava,fire,immune));
        }
        airSupply=fluidHazards.air;burning=fluidHazards.fireTicks>0;
    }
    private void UpdateFluidView()
    {
        if(cam==null)return;
        if(fluidView==null)
        {
            fluidView=cam.GetComponent<FluidCameraEffects>();if(fluidView==null)fluidView=cam.gameObject.AddComponent<FluidCameraEffects>();
            if(world!=null)world.PlayFluidEffect(transform.position,255);
        }
        fluidView.player=this;
        if(world==null)return;
        if(fluidViewContactKnown&&InWater&&!fluidViewWater)world.PlayFluidEffect(transform.position+Vector3.up,1);
        fluidViewContactKnown=true;fluidViewWater=InWater;
        fluidBubbleTime+=Time.deltaTime;
        if(fluidBubbleTime>.5f)
        {fluidBubbleTime=0;if(FluidWorldQueries.AtEye(world,cam.position)==BlockType.Water)world.PlayFluidEffect(cam.position,3);}
    }
    private void DisposeFluidView(){if(fluidView!=null)Destroy(fluidView);fluidView=null;}
}

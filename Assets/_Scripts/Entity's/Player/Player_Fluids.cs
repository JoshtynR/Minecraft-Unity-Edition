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
    private Vector3 lastFluidSafePosition;
    private bool hasFluidSafePosition,deathCursorReleased;
    private CursorLockMode previousDeathCursor;
    private bool previousDeathCursorVisible;
    public bool fluidRespawnUnavailable {get;private set;}
    private void AdvanceFluidHazards(float dt)
    {
        if(world==null||!world.IsWorldCreated||health<=0)return;
        fluidHazardTime+=dt;
        bool immune=(!networkStarted||isLocalPlayer)?isFlying:serverFlying;
        bool water=FluidWorldQueries.Touches(world,transform.position,entityWidth,entityHeight,BlockType.Water,out _);
        bool lava=FluidWorldQueries.Touches(world,transform.position,entityWidth,entityHeight,BlockType.Lava,out _);
        bool fire=FluidWorldQueries.Touches(world,transform.position,entityWidth,entityHeight,BlockType.Fire,out _);
        bool head=FluidWorldQueries.AtEye(world,transform.position+Vector3.up*eyeHeight)==BlockType.Water;
        if(!water&&!lava&&!fire&&!immune&&SafeFluidRespawn(transform.position))
        {lastFluidSafePosition=transform.position;hasFluidSafePosition=true;}
        while(fluidHazardTime+.000001f>=.05f)
        {
            fluidHazardTime=Mathf.Max(0,fluidHazardTime-.05f);
            health=Mathf.Max(0,health-fluidHazards.Tick(water,head,lava,fire,immune));
        }
        airSupply=fluidHazards.air;burning=fluidHazards.fireTicks>0;
    }
    private void UpdateFluidView()
    {
        if(health<=0&&!deathCursorReleased)
        {
            previousDeathCursor=Cursor.lockState;previousDeathCursorVisible=Cursor.visible;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;deathCursorReleased=true;
        }
        else if(health>0)RestoreDeathCursor();
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
    private void RestoreDeathCursor()
    {if(!deathCursorReleased)return;Cursor.lockState=previousDeathCursor;Cursor.visible=previousDeathCursorVisible;deathCursorReleased=false;}
    private bool SafeFluidRespawn(Vector3 p)
    {
        if(world==null)return false;
        var below=world.GetBlock(Vector3Int.FloorToInt(p+new Vector3(0,-.05f,0)));
        var feet=world.GetBlock(Vector3Int.FloorToInt(p));
        var head=world.GetBlock(Vector3Int.FloorToInt(p+Vector3.up*entityHeight));
        return below!=null&&SpecialBlockGeometry.NormalCube(below.type)&&feet!=null&&feet.type==BlockType.Air&&head!=null&&head.type==BlockType.Air;
    }
    public void RequestFluidRespawn()
    {
        if(health>0)return;
        if(networkStarted&&!isServer){CmdFluidRespawn();return;}
        RespawnAfterFluidDeath();
    }
    [Command] private void CmdFluidRespawn(){if(health<=0)RespawnAfterFluidDeath();}
    private void RespawnAfterFluidDeath()
    {
        if(dimension==WorldDimension.Nether){BeginDimensionTravel(WorldDimension.Overworld,new Vector3(8.5f,80,8.5f),false);return;}
        if(world==null)return;
        Vector3 destination=lastFluidSafePosition;
        bool found=hasFluidSafePosition&&SafeFluidRespawn(destination);
        for(int radius=0;!found&&radius<=8;radius++)
        for(int x=-radius;!found&&x<=radius;x++)for(int z=-radius;!found&&z<=radius;z++)
        {
            if(Mathf.Abs(x)!=radius&&Mathf.Abs(z)!=radius)continue;
            for(int y=world.worldHeight-2;y>=1;y--)
            {
                var p=new Vector3(8+x+.5f,y,8+z+.5f);
                if(!SafeFluidRespawn(p))continue;
                destination=p;found=true;break;
            }
        }
        if(!found){fluidRespawnUnavailable=true;return;}
        ApplyFluidRespawn(destination);
        if(networkStarted)TargetFluidRespawn(connectionToClient,destination);
    }
    [TargetRpc] private void TargetFluidRespawn(NetworkConnection target,Vector3 destination){ApplyFluidRespawn(destination);}
    private void ApplyFluidRespawn(Vector3 destination)
    {
        transform.position=destination;health=hunger=20;saturation=5;exhaustion=0;
        fluidHazards.Reset();fluidHazardTime=0;airSupply=300;burning=false;
        fluidRespawnUnavailable=false;fluidViewContactKnown=false;ResetFluidMovement();RestoreDeathCursor();
    }
    private void DisposeFluidView(){RestoreDeathCursor();if(fluidView!=null)Destroy(fluidView);fluidView=null;}
}

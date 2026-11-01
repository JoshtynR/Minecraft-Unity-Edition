using Mirror;
using UnityEngine;

public partial class Player
{
    private float nextVehicleControl;
    // Animation consumers depend on mounted state, not a particular vehicle.
    // Future pig mounting can extend this query and reuse BetaMotionPose.Seated.
    public bool IsMounted=>world!=null&&world.RiddenVehicle(this)!=null;
    private int TargetedVehicle()=>world==null||cam==null?0:world.TargetVehicle(cam.position,cam.forward,reach);
    private bool TryUseVehicle(bool attack)
    {
        int id=TargetedVehicle();var held=inventory.HeldItem().type;
        if(id==0&&(attack||(held!=ItemType.Boat&&held!=ItemType.Minecart)))return false;
        if(inventory.DropRequestPending||(networkStarted&&!isServer&&!NetworkClient.ready))return true;
        CancelMining();int request=++dropRequestId;
        inventory.DropRequestPending=true;awaitingNetworkDrop=networkStarted&&!isServer;
        var snapshot=inventory.DropInventorySnapshot();
        if(networkStarted&&!isServer)CmdVehicleAction(request,id,attack,inventory.selectedSlotIndex,snapshot,knownItemWearRevision,cam.forward);
        else ProcessVehicleAction(request,id,attack,inventory.selectedSlotIndex,snapshot,knownItemWearRevision,cam.forward,false);
        return true;
    }
    [Command] private void CmdVehicleAction(int request,int id,bool attack,int slot,InventoryStack[] snapshot,int revision,Vector3 look)
    {ProcessVehicleAction(request,id,attack,slot,snapshot,revision,look,true);}
    private void ProcessVehicleAction(int request,int id,bool attack,int slot,InventoryStack[] snapshot,int revision,Vector3 look,bool network)
    {
        if(request==lastServerDropRequest&&lastServerDropReply!=null){CompleteDropAction(request,lastServerDropReply,lastServerDropAccepted,network);return;}
        bool accepted=false;
        if(request>lastServerDropRequest&&AcceptsItemRevision(revision)&&!dimensionTransfer&&health>0&&world!=null&&world.IsWorldCreated&&!world.disabled&&!world.isSaving&&
           slot>=0&&slot<9&&ChestTransactions.Valid(snapshot,new InventoryStack())&&VehiclePhysics.Finite(look)&&look.sqrMagnitude>.5f&&world.RiddenVehicle(this)==null)
        {
            var eye=transform.position+Vector3.up*eyeHeight;
            if(id!=0&&world.TargetVehicle(eye,look,reach)==id)accepted=world.UseVehicle(this,id,attack,MobRules.AttackDamage(snapshot[slot].type));
            else if(id==0&&!attack&&(snapshot[slot].type==ItemType.Boat||snapshot[slot].type==ItemType.Minecart)&&!snapshot[slot].Empty)
            {
                var hit=BlockTargeting.Raycast(world,eye,look,reach,out _,out _,true);
                if(hit!=null)
                {
                bool boat=snapshot[slot].type==ItemType.Boat;
                var p=hit.globalWorldPosition;var position=(Vector3)p+new Vector3(.5f,.0625f,.5f);
                bool allowed=boat?hit.type!=BlockType.Lava:SpecialBlockGeometry.IsRail(hit.type);
                if(boat)position.y=p.y+1;
                else if(allowed)
                {int shape=SpecialBlockGeometry.State(hit.type,hit.blockShape);if(hit.type!=BlockType.Rail)shape&=7;position=(Vector3)p+VehiclePhysics.RailPoint(shape,.5f)+Vector3.up*.0625f;}
                if(allowed&&world.SpawnVehicle(boat?VehicleKind.Boat:VehicleKind.Minecart,position,Mathf.Atan2(look.x,look.z)*Mathf.Rad2Deg))
                {snapshot[slot].count--;if(snapshot[slot].count==0)snapshot[slot]=new InventoryStack();accepted=true;}
                }
            }
            if(accepted){serverMining=false;inventory.ApplyDropInventory(snapshot);}
        }
        if(request>lastServerDropRequest){lastServerDropRequest=request;lastServerDropAccepted=accepted;lastServerDropReply=snapshot==null?null:(InventoryStack[])snapshot.Clone();}
        CompleteDropAction(request,snapshot,accepted,network);
    }
    private void UpdateVehicleControls()
    {
        if(world==null)return;
        var v=world.RiddenVehicle(this);if(v==null)return;
        bool leave=Input.GetKeyDown(KeyCode.LeftShift);
        if(!leave&&Time.unscaledTime<nextVehicleControl)return;
        nextVehicleControl=Time.unscaledTime+.1f;
        float throttle=inventory.isOpen||escapeMenuOpen?0:vertical,steer=inventory.isOpen||escapeMenuOpen?0:horizontal;
        float yaw=cam.eulerAngles.y;
        if(networkStarted&&!isServer)
        {if(NetworkClient.ready)CmdVehicleControls(v.id,throttle,steer,yaw,leave);}
        else if(leave)world.LeaveVehicle(this);
        else world.SetVehicleInput(this,v.id,throttle,steer,yaw);
    }
    [Command] private void CmdVehicleControls(int id,float throttle,float steer,float yaw,bool leave)
    {
        if(world==null||dimensionTransfer||health<=0)return;
        if(leave){var v=world.RiddenVehicle(this);if(v!=null&&v.id==id)world.LeaveVehicle(this);}
        else world.SetVehicleInput(this,id,throttle,steer,yaw);
    }
    public void VehicleDismounted(Vector3 position)
    {
        transform.position=position;verticalMomentum=0;velocity=Vector3.zero;falling.Reset();ResetWalkMotion();
        if(networkStarted&&isServer&&!isLocalPlayer)TargetVehicleDismounted(position);
    }
    [TargetRpc] private void TargetVehicleDismounted(Vector3 position)
    {var v=world==null?null:world.RiddenVehicle(this);if(v!=null){v.occupied=false;v.riderId=0;}transform.position=position;verticalMomentum=0;velocity=Vector3.zero;falling.Reset();}
    private bool UpdateVehicleSeat()
    {
        if(world==null||health<=0)return false;
        var v=world.RiddenVehicle(this);if(v==null)return false;
        if(!world.VehicleAreaReady(v.position))return true;
        var target=VehiclePhysics.SeatPosition(v);
        transform.position=(transform.position-target).sqrMagnitude>9?target:Vector3.Lerp(transform.position,target,1-Mathf.Exp(-20*Time.fixedDeltaTime));
        verticalMomentum=0;velocity=Vector3.zero;isFlying=false;isSprinting=false;jumpRequest=false;falling.Reset();ResetWalkMotion();
        return true;
    }
}

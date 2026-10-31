using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public struct VehicleBatchMessage : NetworkMessage { public WorldDimension dimension;public VehicleState[] vehicles; }

public partial class World
{
    [NonSerialized] private List<VehicleState> runtimeVehicles;
    private List<VehicleState> vehicles=>runtimeVehicles??(runtimeVehicles=new List<VehicleState>());
    [NonSerialized] private Dictionary<int,VehicleVisual> runtimeVehicleVisuals;
    private Dictionary<int,VehicleVisual> vehicleVisuals=>runtimeVehicleVisuals??(runtimeVehicleVisuals=new Dictionary<int,VehicleVisual>());
    private int nextVehicleId=1;
    private float vehicleClock,vehicleBroadcast;
    private void InitializeVehicles()
    {
        NetworkClient.RegisterHandler<VehicleBatchMessage>(message=>
        {
            if(NetworkServer.active)return;
            var realm=FindDimension(message.dimension);if(realm==null)return;
            realm.vehicles.Clear();
            if(message.vehicles!=null)realm.vehicles.AddRange(message.vehicles);
        });
    }
    public VehicleState Vehicle(int id)=>vehicles.Find(v=>v.id==id);
    public VehicleState RiddenVehicle(Player player)=>vehicles.Find(v=>v.occupied&&(DropAuthority?v.driver==player:v.riderId==player.netId));
    public VehicleState[] CaptureVehicles()
    {var copy=new VehicleState[vehicles.Count];for(int i=0;i<copy.Length;i++)copy[i]=vehicles[i].Copy(true);return copy;}
    public void RestoreVehicles(VehicleState[] saved)
    {
        ResetVehicles(true);
        if(!DropAuthority||saved==null)return;
        foreach(var v in saved)
        {
            if(v==null||v.id<=0||v.id==int.MaxValue||Vehicle(v.id)!=null||!VehiclePhysics.Finite(v.position)||!VehiclePhysics.Finite(v.velocity)||!VehiclePhysics.Finite(v.yaw)||v.kind>VehicleKind.Boat||vehicles.Count>=128)continue;
            vehicles.Add(v.Copy(true));nextVehicleId=Math.Max(nextVehicleId,v.id+1);
        }
    }
    private void ResetVehicles(bool discard)
    {
        if(runtimeVehicleVisuals!=null){foreach(var visual in vehicleVisuals.Values)if(visual!=null)Destroy(visual.gameObject);vehicleVisuals.Clear();}
        if(runtimeVehicles!=null)foreach(var v in vehicles){v.driver=null;v.occupied=false;v.riderId=0;v.throttle=v.steer=0;}
        if(discard){runtimeVehicles?.Clear();nextVehicleId=1;}
        vehicleClock=vehicleBroadcast=0;
    }
    // Guard the complete swept body, not just its centre chunk. Missing chunks
    // are not air: suspend until normal player streaming has made them ready.
    public bool VehicleAreaReady(Vector3 position,float radius=.8f)
    {
        if(worldData.chunkDict==null||position.y<0||position.y+2>=worldHeight)return false;
        for(int x=Mathf.FloorToInt(position.x-radius);x<=Mathf.FloorToInt(position.x+radius);x++)
        for(int z=Mathf.FloorToInt(position.z-radius);z<=Mathf.FloorToInt(position.z+radius);z++)
        {
            var key=WorldDataHelper.GetChunkPosition(this,new Vector3Int(x,0,z));
            if(!worldData.chunkDict.TryGetValue(key,out var chunk)||chunk==null||chunk.ChunkData==null)return false;
            var b=GetBlock(new Vector3Int(x,Mathf.FloorToInt(position.y),z));
            if(b==null||b.type==BlockType.Nothing)return false;
        }
        return true;
    }
    public bool VehicleBodyClear(Vector3 position,float radius,float height,bool railSupports=false)
    {
        if(!VehicleAreaReady(position,radius))return false;
        for(int x=Mathf.FloorToInt(position.x-radius);x<=Mathf.FloorToInt(position.x+radius);x++)
        for(int y=Mathf.FloorToInt(position.y);y<=Mathf.FloorToInt(position.y+height);y++)
        for(int z=Mathf.FloorToInt(position.z-radius);z<=Mathf.FloorToInt(position.z+radius);z++)
        {
            var p=new Vector3Int(x,y,z);var b=GetBlock(p);
            if(b==null||b.type==BlockType.Nothing)return false;
            if(railSupports&&SpecialBlockGeometry.IsRail(GetBlock(p+Vector3Int.up).type))continue;
            if(b.type==BlockType.Air||b.type==BlockType.Water||b.type==BlockType.Lava||BlockShapesExtensions.IsPlant(b.type)||SpecialBlockGeometry.NoCollision(b.type)||!b.BlockData.generateCollider)continue;
            for(int part=0;part<SpecialBlockGeometry.CollisionCount(b.type);part++)
                if(BlockCollision.OverlapsBody(position,radius,height,p,SpecialBlockGeometry.CollisionPart(b.type,b.blockShape,part)))return false;
        }
        return true;
    }
    public int TargetVehicle(Vector3 eye,Vector3 look,float reach)
    {
        if(!VehiclePhysics.Finite(eye)||!VehiclePhysics.Finite(look)||look.sqrMagnitude<.5f)return 0;
        var ray=new Ray(eye,look.normalized);float limit=reach;int id=0;
        var block=BlockTargeting.Raycast(this,eye,look,reach,out _,out var hit);
        if(block!=null)limit=(hit-eye).magnitude;
        foreach(var v in vehicles)
            if(VehicleAreaReady(v.position)&&new Bounds(v.position+Vector3.up*.3f,new Vector3(VehiclePhysics.Radius(v.kind)*2,.6f,VehiclePhysics.Radius(v.kind)*2)).IntersectRay(ray,out float d)&&d>=0&&d<limit){limit=d;id=v.id;}
        return id;
    }
    public bool SpawnVehicle(VehicleKind kind,Vector3 position,float yaw)
    {
        if(!DropAuthority||kind>VehicleKind.Boat||vehicles.Count>=128||nextVehicleId==int.MaxValue||!VehiclePhysics.Finite(position)||!VehiclePhysics.Finite(yaw)||!VehicleBodyClear(position,VehiclePhysics.Radius(kind),.6f,kind==VehicleKind.Minecart))return false;
        foreach(var other in vehicles)if((other.position-position).sqrMagnitude<1)return false;
        vehicles.Add(new VehicleState{id=nextVehicleId++,kind=kind,position=position,yaw=yaw});vehicleBroadcast=0;return true;
    }
    public bool UseVehicle(Player player,int id,bool attack)
    {
        if(!DropAuthority||isSaving)return false;
        var v=Vehicle(id);if(v==null)return false;
        if(attack)
        {
            if(v.occupied)return false;
            if(!SpawnItemDrop(new InventoryStack(v.kind==VehicleKind.Boat?ItemType.Boat:ItemType.Minecart,1),v.position+Vector3.up*.3f))return false;
            vehicles.Remove(v);vehicleBroadcast=0;return true;
        }
        if(v.occupied||RiddenVehicle(player)!=null||!VehicleBodyClear(v.position+Vector3.up*.45f,.3f,1.8f))return false;
        v.driver=player;v.occupied=true;v.riderId=player.netId;v.inputExpires=Time.unscaledTime+.3f;vehicleBroadcast=0;return true;
    }
    public bool LeaveVehicle(Player player,bool force=false)
    {
        var v=RiddenVehicle(player);if(v==null)return true;
        for(int y=0;y<=2;y++)for(int side=0;side<4;side++)
        {
            var p=v.position+new Vector3(side==0?1.5f:side==1?-1.5f:0,y,side==2?1.5f:side==3?-1.5f:0);
            if(!VehicleBodyClear(p,.3f,1.8f))continue;
            p.y+=BlockCollision.Clip(this,p,.3f,1.8f,-2,1);
            if(!VehicleBodyClear(p,.3f,1.8f)||(!force&&!BlockCollision.Supported(this,p,.3f,1.8f)))continue;
            v.driver=null;v.occupied=false;v.riderId=0;v.throttle=v.steer=0;
            player.transform.position=p;player.VehicleDismounted(p);vehicleBroadcast=0;return true;
        }
        if(force){v.driver=null;v.occupied=false;v.riderId=0;v.throttle=v.steer=0;vehicleBroadcast=0;return true;}
        return false;
    }
    public void SetVehicleInput(Player player,int id,float throttle,float steer,float yaw)
    {
        var v=Vehicle(id);if(v==null||v.driver!=player||!v.occupied||!VehiclePhysics.Finite(throttle)||!VehiclePhysics.Finite(steer)||!VehiclePhysics.Finite(yaw))return;
        v.throttle=Mathf.Clamp(throttle,-1,1);v.steer=Mathf.Clamp(steer,-1,1);v.inputYaw=yaw;v.inputExpires=Time.unscaledTime+.3f;
    }
    private bool TryVehicleRail(Vector3 position,out Vector3Int cell,out Block rail)
    {
        cell=Vector3Int.FloorToInt(position);rail=GetBlock(cell);
        if(rail!=null&&SpecialBlockGeometry.IsRail(rail.type))return true;
        cell+=Vector3Int.down;rail=GetBlock(cell);
        return rail!=null&&SpecialBlockGeometry.IsRail(rail.type);
    }
    private float BoatSubmersion(VehicleState v)
    {
        int wet=0;
        for(int x=0;x<2;x++)for(int z=0;z<2;z++)for(int y=0;y<5;y++)
        {
            var p=v.position+new Vector3(x==0?-.45f:.45f,(y+.5f)*.12f,z==0?-.45f:.45f);
            var cell=Vector3Int.FloorToInt(p);var b=GetBlock(cell);
            if(b!=null&&b.type==BlockType.Water&&p.y<BetaFluidPhysics.EyeSurface(cell.y,b.waterLevel))wet++;
        }
        return wet/20f;
    }
    private void TickVehicle(VehicleState v)
    {
        if(!VehicleAreaReady(v.position))return;
        if(v.inputExpires<Time.unscaledTime)v.throttle=v.steer=0;
        const float dt=VehiclePhysics.Step/5;
        for(int sub=0;sub<5;sub++)
        {
            bool onRail=v.kind==VehicleKind.Minecart&&TryVehicleRail(v.position,out _,out _);
            Vector3 requested;
            if(onRail&&TryVehicleRail(v.position,out var cell,out var rail))
            {
                int shape=SpecialBlockGeometry.State(rail.type,rail.blockShape);if(rail.type!=BlockType.Rail)shape&=7;
                var local=v.position-(Vector3)cell;float t=VehiclePhysics.RailParameter(shape,local);
                var tangent=VehiclePhysics.RailTangent(shape,t);
                var horizontal=new Vector3(tangent.x,0,tangent.z).normalized;
                float speed=Vector3.Dot(v.velocity,horizontal);
                if(v.occupied&&Mathf.Abs(speed)<.1f&&v.throttle!=0)
                {float a=v.inputYaw*Mathf.Deg2Rad;speed=Vector3.Dot(new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),horizontal)*v.throttle*.8f;}
                var tuning=VehicleTuning.Current;
                speed-=tangent.y*tuning.cartSlopeAcceleration*dt;
                bool powered=rail.type==BlockType.PoweredRail&&(SpecialBlockGeometry.State(rail.type,rail.blockShape)&8)!=0;
                if(rail.type==BlockType.PoweredRail&&!powered)speed*=Mathf.Pow(.5f,dt/.05f);
                else if(powered&&Mathf.Abs(speed)>.01f)speed+=Mathf.Sign(speed)*tuning.cartPoweredAcceleration*dt;
                speed=Mathf.Clamp(speed,-tuning.cartMaxSpeed,tuning.cartMaxSpeed)*Mathf.Pow(v.occupied?.997f:.96f,dt/.05f);
                v.velocity=horizontal*speed;
                var projected=(Vector3)cell+VehiclePhysics.RailPoint(shape,t)+Vector3.up*.0625f;
                requested=projected-v.position+tangent*(speed*dt/Mathf.Max(.5f,new Vector3(tangent.x,0,tangent.z).magnitude));
                if(Mathf.Abs(speed)>.01f)v.yaw=Mathf.Atan2(v.velocity.x,v.velocity.z)*Mathf.Rad2Deg;
            }
            else
            {
                if(v.kind==VehicleKind.Boat)VehiclePhysics.BoatVelocity(v,BoatSubmersion(v),dt);
                else{v.velocity.y-=16f*dt;v.velocity.x*=.995f;v.velocity.z*=.995f;}
                requested=v.velocity*dt;
            }
            if(!VehicleAreaReady(v.position+requested,VehiclePhysics.Radius(v.kind)+.1f))return;
            // On slopes the hull overlaps the stair-step support beneath the
            // track. Ignore only rail-bearing supports, never ordinary walls.
            Func<Vector3Int,bool> support=onRail?(Func<Vector3Int,bool>)(p=>{var b=GetBlock(p+Vector3Int.up);return b!=null&&SpecialBlockGeometry.IsRail(b.type);}):null;
            var moved=BlockCollision.Resolve(this,v.position,VehiclePhysics.Radius(v.kind),.6f,requested,false,support);
            v.position+=moved;
            if(Mathf.Abs(moved.x-requested.x)>.0001f)v.velocity.x=0;
            if(Mathf.Abs(moved.y-requested.y)>.0001f)v.velocity.y=0;
            if(Mathf.Abs(moved.z-requested.z)>.0001f)v.velocity.z=0;
        }
    }
    private void UpdateVehicles()
    {
        if(disabled||!IsWorldCreated||isSaving||System.Threading.Volatile.Read(ref betaPopulationJobs)!=0)return;
        if(DropAuthority)
        {
            foreach(var v in vehicles)if(v.occupied&&(v.driver==null||v.driver.health<=0||v.driver.CurrentWorld!=this||v.driver.DimensionTransferInProgress))
            {v.driver=null;v.occupied=false;v.riderId=0;v.throttle=v.steer=0;}
            vehicleClock=Mathf.Min(vehicleClock+Time.deltaTime,.15f);
            while(vehicleClock>=VehiclePhysics.Step){vehicleClock-=VehiclePhysics.Step;foreach(var v in vehicles)TickVehicle(v);}
            vehicleBroadcast-=Time.unscaledDeltaTime;
            if(vehicleBroadcast<=0&&NetworkServer.active&&NetworkServer.connections.Count>(NetworkClient.active?1:0))
            {vehicleBroadcast=.1f;var copy=new VehicleState[vehicles.Count];for(int i=0;i<copy.Length;i++)copy[i]=vehicles[i].Copy();NetworkServer.SendToAll(new VehicleBatchMessage{dimension=dimension,vehicles=copy});}
        }
        foreach(var v in vehicles)
        {
            if(!vehicleVisuals.TryGetValue(v.id,out var visual)||visual==null)
            {
                var root=new GameObject(v.kind.ToString());root.transform.SetParent(worldRenderer.transform,false);
                visual=root.AddComponent<VehicleVisual>();visual.Initialize(v);vehicleVisuals[v.id]=visual;
            }
            visual.gameObject.SetActive(VehicleAreaReady(v.position));visual.SetTarget(v);
        }
        // Small bounded collection; no iterator mutation and no per-frame list.
        int removed;
        do{removed=0;foreach(var entry in vehicleVisuals)if(Vehicle(entry.Key)==null){Destroy(entry.Value.gameObject);removed=entry.Key;break;}if(removed!=0)vehicleVisuals.Remove(removed);}while(removed!=0);
    }
}

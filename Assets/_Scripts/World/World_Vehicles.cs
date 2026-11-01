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
    public bool UseVehicle(Player player,int id,bool attack,int attackDamage=1)
    {
        if(!DropAuthority||isSaving)return false;
        var v=Vehicle(id);if(v==null)return false;
        if(attack)
        {
            v.rockDirection=-v.rockDirection;v.hitTicks=10;
            v.damage=Math.Min(1000,v.damage+Math.Max(1,Math.Min(20,attackDamage))*10);
            vehicleBroadcast=0;
            if(v.damage<=40)return true;
            // Intentional exception to Beta's planks/sticks loot: recover
            // exactly one usable boat, and never destroy it on a wall impact.
            if(!SpawnItemDrop(new InventoryStack(v.kind==VehicleKind.Boat?ItemType.Boat:ItemType.Minecart,1),v.position+Vector3.up*.3f))return false;
            if(v.driver!=null)LeaveVehicle(v.driver,true);
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
            var waterCell=Vector3Int.FloorToInt(p-Vector3.up*.1f);
            var surface=GetBlock(waterCell);
            if(surface.type==BlockType.Water)p.y=Mathf.Max(p.y,BetaFluidPhysics.EyeSurface(waterCell.y,surface.waterLevel));
            else p.y+=BlockCollision.Clip(this,p,.3f,1.8f,-2,1);
            var water=GetBlock(Vector3Int.FloorToInt(p-Vector3.up*.1f));
            if(!VehicleBodyClear(p,.3f,1.8f)||(!force&&!BlockCollision.Supported(this,p,.3f,1.8f)&&water.type!=BlockType.Water))continue;
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
    public float VehiclePitch(VehicleState v)
    {
        if(v.kind!=VehicleKind.Minecart||!TryVehicleRail(v.position,out _,out var rail))return 0;
        int shape=SpecialBlockGeometry.State(rail.type,rail.blockShape);if(rail.type!=BlockType.Rail)shape&=7;
        var tangent=VehiclePhysics.RailTangent(shape,.5f);
        float yaw=v.yaw*Mathf.Deg2Rad;
        float facing=Vector3.Dot(tangent,new Vector3(Mathf.Sin(yaw),0,Mathf.Cos(yaw)))<0?-1:1;
        return -Mathf.Atan2(tangent.y*facing,new Vector3(tangent.x,0,tangent.z).magnitude)*Mathf.Rad2Deg;
    }
    private float BoatSubmersion(VehicleState v)
    {
        int wet=0;
        for(int layer=0;layer<5;layer++)
        {
            // EntityBoat's five horizontal AABB slices, shifted down 1/8.
            // World.isAABBInMaterial uses metadata/8, not eye immersion /9.
            float minY=v.position.y+layer*.12f-.125f,maxY=minY+.12f;
            bool water=false;
            for(int x=Mathf.FloorToInt(v.position.x-.75f);x<=Mathf.FloorToInt(v.position.x+.75f)&&!water;x++)
            for(int z=Mathf.FloorToInt(v.position.z-.75f);z<=Mathf.FloorToInt(v.position.z+.75f)&&!water;z++)
            for(int y=Mathf.Max(0,Mathf.FloorToInt(minY));y<=Mathf.Min(worldHeight-1,Mathf.FloorToInt(maxY));y++)
            {
                var b=GetBlock(new Vector3Int(x,y,z));
                if(b.type==BlockType.Water&&y+1-(b.waterLevel<8?b.waterLevel/8f:0)>=minY){water=true;break;}
            }
            if(water)wet++;
        }
        return wet/5f;
    }
    private void TickBoat(VehicleState v)
    {
        if(v.hitTicks>0)v.hitTicks--;
        if(v.damage>0)v.damage--;
        VehiclePhysics.BoatVelocity(v,BoatSubmersion(v),VehiclePhysics.Step);
        var requested=v.velocity*VehiclePhysics.Step;
        if(!VehicleAreaReady(v.position+requested,VehiclePhysics.Radius(v.kind)+.1f))return;
        var moved=BlockCollision.Resolve(this,v.position,.75f,.6f,requested,false);
        v.position+=moved;
        v.onGround=requested.y<0&&moved.y>requested.y+.0001f;
        if(Mathf.Abs(moved.x-requested.x)>.0001f)v.velocity.x=0;
        if(Mathf.Abs(moved.y-requested.y)>.0001f)v.velocity.y=0;
        if(Mathf.Abs(moved.z-requested.z)>.0001f)v.velocity.z=0;
        // Unlike Beta, horizontal collision never deletes the boat or its rider.
        VehiclePhysics.BoatDrag(v);
        VehiclePhysics.BoatHeading(v,moved);
        foreach(var other in vehicles)
        {
            if(ReferenceEquals(v,other)||other.kind!=VehicleKind.Boat||
                Math.Abs(other.position.y-v.position.y)>=.6f)continue;
            var delta=other.position-v.position;
            if(Math.Abs(delta.x)>=1.7f||Math.Abs(delta.z)>=1.7f)continue;
            if(!VehicleAreaReady(other.position))continue;
            float extent=Mathf.Max(Mathf.Abs(delta.x),Mathf.Abs(delta.z));
            if(extent<.01f)continue;
            float root=(float)Math.Sqrt(extent);
            var impulse=new Vector3(delta.x,0,delta.z)*(.05f*Mathf.Min(1,1/root)/root/VehiclePhysics.Step);
            v.velocity-=impulse;other.velocity+=impulse;
        }
        // Beta boats clear thin snow at the four hull corners.
        for(int i=0;i<4;i++)
        {
            var cell=Vector3Int.FloorToInt(v.position+new Vector3((i%2-.5f)*.8f,.3f,(i/2-.5f)*.8f));
            if(GetBlock(cell).type==BlockType.SnowLayer)SetFarmBlock(cell,BlockType.Air);
        }
    }
    private void TickVehicle(VehicleState v)
    {
        if(!VehicleAreaReady(v.position))return;
        if(v.inputExpires<Time.unscaledTime)v.throttle=v.steer=0;
        if(v.kind==VehicleKind.Boat){TickBoat(v);return;}
        if(v.hitTicks>0)v.hitTicks--;
        if(v.damage>0)v.damage--;
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
                float speed=VehiclePhysics.CartSpeed(v.velocity,horizontal);
                if(v.occupied&&Mathf.Abs(speed)<.1f&&v.throttle!=0)
                {float a=v.inputYaw*Mathf.Deg2Rad;speed=Vector3.Dot(new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),horizontal)*v.throttle*.8f;}
                var tuning=VehicleTuning.Current;
                speed-=Mathf.Sign(tangent.y)* (Mathf.Abs(tangent.y)>.001f?tuning.cartSlopeAcceleration*dt:0);
                bool powered=rail.type==BlockType.PoweredRail&&(SpecialBlockGeometry.State(rail.type,rail.blockShape)&8)!=0;
                if(rail.type==BlockType.PoweredRail&&!powered)
                    speed=Mathf.Abs(speed)<.6f?0:speed*Mathf.Pow(.5f,dt/VehiclePhysics.Step);
                else if(powered)
                {
                    if(Mathf.Abs(speed)>.2f)speed+=Mathf.Sign(speed)*tuning.cartPoweredAcceleration*dt;
                    else if(shape==0||shape==1)
                    {
                        var axis=shape==1?Vector3Int.right:new Vector3Int(0,0,1);
                        // A solid bumper supplies the starting direction. Open
                        // powered track must not pick an arbitrary direction.
                        if(SpecialBlockGeometry.NormalCube(GetBlock(cell-axis).type))speed=.4f;
                        else if(SpecialBlockGeometry.NormalCube(GetBlock(cell+axis).type))speed=-.4f;
                    }
                }
                speed=Mathf.Clamp(speed,-tuning.cartMaxSpeed,tuning.cartMaxSpeed)*Mathf.Pow(v.occupied?.997f:.96f,dt/.05f);
                v.velocity=horizontal*speed;
                var projected=(Vector3)cell+VehiclePhysics.RailPoint(shape,t)+Vector3.up*.0625f;
                requested=projected-v.position+tangent*(speed*dt/Mathf.Max(.5f,new Vector3(tangent.x,0,tangent.z).magnitude));
                if(Mathf.Abs(speed)>.01f)v.yaw=Mathf.Atan2(v.velocity.x,v.velocity.z)*Mathf.Rad2Deg;
            }
            else
            {
                v.velocity.y-=16f*dt;
                v.velocity.x=Mathf.Clamp(v.velocity.x,-VehicleTuning.Current.cartMaxSpeed,VehicleTuning.Current.cartMaxSpeed);
                v.velocity.z=Mathf.Clamp(v.velocity.z,-VehicleTuning.Current.cartMaxSpeed,VehicleTuning.Current.cartMaxSpeed);
                v.velocity*=Mathf.Pow(v.onGround?.5f:.95f,dt/VehiclePhysics.Step);
                requested=v.velocity*dt;
            }
            if(!VehicleAreaReady(v.position+requested,VehiclePhysics.Radius(v.kind)+.1f))return;
            // On slopes the hull overlaps the stair-step support beneath the
            // track. Ignore only rail-bearing supports, never ordinary walls.
            Func<Vector3Int,bool> support=onRail?(Func<Vector3Int,bool>)(p=>{var b=GetBlock(p+Vector3Int.up);return b!=null&&SpecialBlockGeometry.IsRail(b.type);}):null;
            var moved=BlockCollision.Resolve(this,v.position,VehiclePhysics.Radius(v.kind),.6f,requested,false,support);
            v.position+=moved;
            v.onGround=requested.y<0&&moved.y>requested.y+.0001f;
            if(Mathf.Abs(moved.x-requested.x)>.0001f)v.velocity.x=0;
            if(Mathf.Abs(moved.y-requested.y)>.0001f)v.velocity.y=0;
            if(Mathf.Abs(moved.z-requested.z)>.0001f)v.velocity.z=0;
        }
    }
    private void CollideMinecarts()
    {
        for(int i=0;i<vehicles.Count;i++)for(int j=i+1;j<vehicles.Count;j++)
        {
            var a=vehicles[i];var b=vehicles[j];
            if(a.kind!=VehicleKind.Minecart||b.kind!=VehicleKind.Minecart||
                !VehicleAreaReady(a.position)||!VehicleAreaReady(b.position)||Mathf.Abs(a.position.y-b.position.y)>=.6f)continue;
            var delta=b.position-a.position;delta.y=0;
            if(Mathf.Abs(delta.x)>1.18f||Mathf.Abs(delta.z)>1.18f||delta.sqrMagnitude<.0001f)continue;
            float distance=delta.magnitude;
            var direction=delta/distance;
            float yaw=a.yaw*Mathf.Deg2Rad;
            var heading=new Vector3(Mathf.Sin(yaw),0,Mathf.Cos(yaw));
            if(TryVehicleRail(a.position,out _,out var rail))
            {
                int shape=SpecialBlockGeometry.State(rail.type,rail.blockShape);if(rail.type!=BlockType.Rail)shape&=7;
                heading=VehiclePhysics.RailTangent(shape,.5f);heading.y=0;heading=heading.normalized;
            }
            if(Mathf.Abs(Vector3.Dot(direction,heading))<.8f)continue;
            var impulse=direction*(Mathf.Min(1,1/distance)*1f);
            var average=(a.velocity+b.velocity)*.5f;average.y=0;
            a.velocity=new Vector3(a.velocity.x*.2f,a.velocity.y,a.velocity.z*.2f)+average-impulse;
            b.velocity=new Vector3(b.velocity.x*.2f,b.velocity.y,b.velocity.z*.2f)+average+impulse;
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
            while(vehicleClock>=VehiclePhysics.Step){vehicleClock-=VehiclePhysics.Step;foreach(var v in vehicles)TickVehicle(v);CollideMinecarts();}
            vehicleBroadcast-=Time.unscaledDeltaTime;
            if(vehicleBroadcast<=0&&NetworkServer.active&&NetworkServer.connections.Count>(NetworkClient.active?1:0))
            {vehicleBroadcast=.1f;var copy=new VehicleState[vehicles.Count];for(int i=0;i<copy.Length;i++)copy[i]=vehicles[i].Copy();NetworkServer.SendToAll(new VehicleBatchMessage{dimension=dimension,vehicles=copy});}
        }
        foreach(var v in vehicles)
        {
            if(!vehicleVisuals.TryGetValue(v.id,out var visual)||visual==null)
            {
                var root=new GameObject(v.kind.ToString());root.transform.SetParent(worldRenderer.transform,false);
                visual=root.AddComponent<VehicleVisual>();visual.Initialize(v,this);vehicleVisuals[v.id]=visual;
            }
            visual.gameObject.SetActive(VehicleAreaReady(v.position));visual.SetTarget(v);
        }
        // Small bounded collection; no iterator mutation and no per-frame list.
        int removed;
        do{removed=0;foreach(var entry in vehicleVisuals)if(Vehicle(entry.Key)==null){Destroy(entry.Value.gameObject);removed=entry.Key;break;}if(removed!=0)vehicleVisuals.Remove(removed);}while(removed!=0);
    }
}

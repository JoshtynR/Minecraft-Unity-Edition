using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public struct MobArrowMessage : NetworkMessage
{public WorldDimension dimension;public int id;public Vector3 position,direction;public bool removed,embedded;}

public partial class World
{
    [Header("Hostile mobs")]
    public bool spawnHostileMobs=true;
    [Range(0,128)] public int maximumActiveHostiles=32;
    private sealed class MobArrow
    {public int id,owner,ticks;public Vector3 position,motion;public Vector3Int block;public bool embedded;}
    private readonly List<MobArrow> mobArrows=new List<MobArrow>();
    private readonly Dictionary<int,GameObject> mobArrowVisuals=new Dictionary<int,GameObject>();
    private int nextMobArrowId;
    private Mesh mobArrowMesh;
    private readonly MaterialPropertyBlock mobArrowProperties=new MaterialPropertyBlock();
    private void DropHostileLoot(MobState mob)
    {
        ItemType type=mob.species==MobType.Zombie?ItemType.RottenFlesh:mob.species==MobType.Skeleton?ItemType.Arrow:mob.species==MobType.Creeper?ItemType.Gunpowder:ItemType.String;
        int count=mobRandom.NextInt(3);if(count>0)SpawnItemDrop(new InventoryStack(type,count),mob.position+Vector3.up*.5f);
        if(mob.species==MobType.Skeleton){int bones=mobRandom.NextInt(3);if(bones>0)SpawnItemDrop(new InventoryStack(ItemType.Bone,bones),mob.position+Vector3.up*.5f);}
    }

    private bool MobCanSee(Vector3 from,Vector3 to)
    {
        var delta=to-from;float distance=delta.magnitude;
        return distance<.01f||BlockTargeting.Raycast(this,from,delta/distance,distance,out _,out _)==null;
    }
    private void SpawnNaturalHostiles()
    {
        if(!spawnHostileMobs||dimension!=WorldDimension.Overworld||mobPlayers.Count==0)return;
        int count=0;foreach(var animal in activeMobs)if(MobRules.Hostile(animal.species))count++;
        if(count>=maximumActiveHostiles||mobs.Count>=MobStore.Capacity)return;
        mobSpawnCandidates.Clear();
        foreach(var chunk in worldData.chunkDict.Keys)
        {float d=MobPlayerDistance((Vector3)chunk+Vector3.one*8);if(d>24*24&&d<72*72)mobSpawnCandidates.Add(chunk);}
        for(int attempt=0;attempt<24&&mobSpawnCandidates.Count>0;attempt++)
        {
            var chunk=mobSpawnCandidates[mobRandom.NextInt(mobSpawnCandidates.Count)];
            var feet=(Vector3)(chunk+new Vector3Int(mobRandom.NextInt(16),mobRandom.NextInt(16),mobRandom.NextInt(16)))+new Vector3(.5f,0,.5f);
            if(MobPlayerDistance(feet)<24*24||!MobAreaLoaded(feet))continue;
            var block=GetBlock(feet);if(block==null||block.type!=BlockType.Air||Math.Max(block.GetBlockLight(),block.GetSkyLight()-betaSkylightSubtracted)>7)continue;
            var species=(MobType)(4+mobRandom.NextInt(4));
            var candidate=new MobState{species=species,position=feet};
            float width=MobRules.Width(candidate),height=MobRules.BodyHeight(candidate);
            bool clear=BlockCollision.Supported(this,feet,width,height);
            for(int x=Mathf.FloorToInt(feet.x-width);clear&&x<=Mathf.FloorToInt(feet.x+width);x++)
            for(int z=Mathf.FloorToInt(feet.z-width);clear&&z<=Mathf.FloorToInt(feet.z+width);z++)
            for(int y=Mathf.FloorToInt(feet.y);clear&&y<=Mathf.FloorToInt(feet.y+height);y++)
                if(GetBlock(new Vector3Int(x,y,z)).type!=BlockType.Air)clear=false;
            foreach(var existing in mobs.Entities)if(MobRules.Bounds(existing).Intersects(MobRules.Bounds(candidate)))clear=false;
            if(!clear)continue;
            var mob=mobs.Spawn(feet,mobRandom.NextInt(360),species);if(mob!=null)BroadcastMob(mob);break;
        }
    }
    private bool TickMobBrain(MobState mob)
    {
        if(mob.attackTicks>0)mob.attackTicks--;
        if(mob.angerTicks>0)mob.angerTicks--;
        if(mob.targetMemoryTicks>0)mob.targetMemoryTicks--;
        return MobBrains.Get(mob.species).Tick(this,mob);
    }
    internal void TickSunlight(MobState mob)
    {
        var eye=mob.position+Vector3.up*(MobRules.BodyHeight(mob)*.85f);
        if(betaSkylightSubtracted<4&&GetBlock(eye).GetSkyLight()==15)
            mob.fireTicks=Math.Max(mob.fireTicks,160);
    }
    internal Player FindMobTarget(MobState mob,bool daylightNeutral)
    {
        var agent=GetMobAgent(mob);
        var eye=mob.position+Vector3.up*(MobRules.BodyHeight(mob)*.85f);
        if(daylightNeutral&&mob.angerTicks==0&&GetBlock(eye).GetSkyLight()-betaSkylightSubtracted>7)
        {agent.Target=null;mob.targetMemoryTicks=0;return null;}
        Player target=null;float nearest=16*16;
        foreach(var player in mobPlayers)
        {
            if(!player.IsMobTarget)continue;
            float d=(player.transform.position-mob.position).sqrMagnitude;
            if(d>=nearest||!MobTargetVisible(mob,player))continue;
            target=player;nearest=d;
        }
        if(target!=null){agent.Target=target;mob.targetMemoryTicks=200;return target;}
        target=agent.Target;
        if(target!=null&&target.IsMobTarget&&mob.targetMemoryTicks>0&&(target.transform.position-mob.position).sqrMagnitude<32*32)return target;
        agent.Target=null;return null;
    }
    internal bool MobTargetVisible(MobState mob,Player target)=>MobCanSee(mob.position+Vector3.up*(MobRules.BodyHeight(mob)*.85f),target.transform.position+Vector3.up*1.5f);
    internal void PursueMobTarget(MobState mob,Player target,float stopDistance)
    {SteerMobTo(mob,target.transform.position,stopDistance,MobTargetVisible(mob,target));LookMobAt(mob,target.transform.position+Vector3.up*1.5f);}
    internal void TryMobMelee(MobState mob,Player target,int damage)
    {
        var delta=target.transform.position-mob.position;
        if(mob.attackTicks!=0||delta.sqrMagnitude>=2.25f||!MobTargetVisible(mob,target))return;
        mob.attackTicks=20;target.ApplyMobDamage(MobDifficultyDamage(damage),delta);
    }
    internal void TryShootMobArrow(MobState mob,Player target)
    {
        var eye=mob.position+Vector3.up*(MobRules.BodyHeight(mob)*.85f);
        if(mob.attackTicks!=0||mobArrows.Count>=128||(target.transform.position-mob.position).sqrMagnitude>=10*10)return;
        mob.attackTicks=30;
        var aim=target.transform.position+Vector3.up-eye;aim.y+=Mathf.Sqrt(aim.x*aim.x+aim.z*aim.z)*.2f;
        mobArrows.Add(new MobArrow{id=++nextMobArrowId,owner=mob.id,position=eye,motion=aim.normalized*.6f});
        GameAudio.Broadcast("random/bow",eye,1,1,this);
    }
    internal bool TickCreeperFuse(MobState mob,Player target)
    {
        bool ignite=target!=null&&MobTargetVisible(mob,target)&&(target.transform.position-mob.position).sqrMagnitude<(mob.fuseTicks>0?7*7:3*3);
        if(ignite&&mob.fuseTicks==0)GameAudio.Broadcast("random/fuse",mob.position,1,.5f,this);
        mob.fuseTicks=Mathf.Clamp(mob.fuseTicks+(ignite?1:-1),0,30);
        if(ignite)mob.walking=false;
        if(mob.fuseTicks<30)return true;
        mobs.Remove(mob.id);mob.health=0;BroadcastMob(mob,true);ExplodeMob(mob.position,3);return false;
    }
    private int MobDifficultyDamage(int normal)=>survivalDifficulty==SurvivalDifficulty.Easy?normal/3+1:survivalDifficulty==SurvivalDifficulty.Hard?normal*3/2:normal;
    private void ExplodeMob(Vector3 center,float strength)
    {
        // Sample cover BEFORE removing blocks so a wall actually protects players.
        foreach(var player in mobPlayers)
        {
            float distance=(player.transform.position-center).magnitude/(strength*2);if(distance>1)continue;
            float exposure=0;
            for(int i=0;i<8;i++)
            {var sample=player.transform.position+new Vector3((i&1)==0?-.25f:.25f,(i&2)==0?.1f:1.6f,(i&4)==0?-.25f:.25f);if(MobCanSee(center+Vector3.up*.5f,sample))exposure+=.125f;}
            float impact=(1-distance)*exposure;
            if(impact>0)player.ApplyMobDamage(MobDifficultyDamage((int)((impact*impact+impact)*.5f*8*strength*2+1)),player.transform.position-center);
        }
        foreach(var animal in new List<MobState>(mobs.Entities))
        {
            float distance=(animal.position-center).magnitude/(strength*2);
            if(distance>=1||!MobCanSee(center+Vector3.up*.5f,animal.position+Vector3.up*.5f))continue;
            float impact=1-distance;DamageMob(animal,(int)((impact*impact+impact)*.5f*8*strength*2+1),animal.position-center);
        }
        var destroyed=new HashSet<Vector3Int>();
        for(int x=0;x<16;x++)for(int y=0;y<16;y++)for(int z=0;z<16;z++)
        {
            if(x!=0&&x!=15&&y!=0&&y!=15&&z!=0&&z!=15)continue;
            var direction=new Vector3(x/15f*2-1,y/15f*2-1,z/15f*2-1).normalized;
            float energy=strength*(.7f+mobRandom.NextFloat()*.6f);var ray=center;
            while(energy>0)
            {
                var p=Vector3Int.FloorToInt(ray);var type=GetBlock(p).type;
                if(type==BlockType.Nothing||type==BlockType.Bedrock)break;
                if(type!=BlockType.Air)energy-=(BedBlastResistance(type)+.3f)*.3f;
                if(energy>0&&type!=BlockType.Air)destroyed.Add(p);
                ray+=direction*.3f;energy-=.225f;
            }
        }
        foreach(var p in destroyed)
        {
            // Respect the existing non-empty/open chest protection; otherwise
            // broadcasting Air after a rejected write would desync clients.
            if(!CanReplaceChest(p,BlockType.Air))continue;
            var type=GetBlock(p).type;if(type==BlockType.Air||type==BlockType.Nothing)continue;
            SetFarmBlock(p,BlockType.Air);
            if(mobRandom.NextFloat()<1/strength)
            {var loot=MiningItemDrop(type,ItemType.DiamondPickaxe);if(!loot.Empty)SpawnItemDrop(loot,(Vector3)p+Vector3.one*.5f);}
        }
        GameAudio.Broadcast("random/explode",center,1,1,this);
    }
    private void TickMobArrows()
    {
        for(int i=mobArrows.Count-1;i>=0;i--)
        {
            var arrow=mobArrows[i];
            if(arrow.embedded)
            {
                if(GetBlock(arrow.block).type==BlockType.Air){arrow.embedded=false;arrow.ticks=0;arrow.motion*=.1f;}
                else if(++arrow.ticks<=1200)continue;
            }
            var next=arrow.position+arrow.motion;
            bool remove=++arrow.ticks>200||!MobAreaLoaded(next);
            float distance=arrow.motion.magnitude;
            bool hitBlock=false;Vector3Int hitCell=default;Vector3 point=next;
            if(!remove&&BlockTargeting.Raycast(this,arrow.position,arrow.motion.normalized,distance,out hitCell,out point)!=null)
            {hitBlock=true;distance=Math.Min(distance,(point-arrow.position).magnitude);}
            if(!remove)
            {
                var ray=new Ray(arrow.position,arrow.motion.normalized);Player victim=null;float nearest=distance;
                foreach(var player in mobPlayers)
                {
                    if(player.DimensionTransferInProgress||player.isFlying)continue;
                    var bounds=new Bounds(player.transform.position+Vector3.up*.9f,new Vector3(.6f,1.8f,.6f));
                    if(bounds.IntersectRay(ray,out float hit)&&hit>=0&&hit<nearest){nearest=hit;victim=player;}
                }
                MobState entityVictim=null;
                foreach(var entity in mobs.Entities)
                {if(entity.id!=arrow.owner&&MobRules.Bounds(entity).IntersectRay(ray,out float hit)&&hit>=0&&hit<nearest){nearest=hit;entityVictim=entity;victim=null;}}
                if(entityVictim!=null)
                {
                    bool record=entityVictim.species==MobType.Creeper&&entityVictim.health<=4;
                    if(DamageMob(entityVictim,4,arrow.motion)&&record&&entityVictim.health==0)
                        SpawnItemDrop(new InventoryStack(mobRandom.NextInt(2)==0?ItemType.MusicDisc13:ItemType.MusicDiscCat,1),entityVictim.position);
                    remove=true;
                }
                else if(victim!=null){victim.ApplyMobDamage(MobDifficultyDamage(4),arrow.motion);remove=true;}
                else if(hitBlock){arrow.position=point-arrow.motion.normalized*.02f;arrow.block=hitCell;arrow.embedded=true;arrow.ticks=0;}
            }
            if(!arrow.embedded){arrow.position=next;arrow.motion*=.99f;arrow.motion.y-=.03f;}
            var message=new MobArrowMessage{dimension=dimension,id=arrow.id,position=arrow.position,direction=arrow.motion,removed=remove,embedded=arrow.embedded};
            if(NetworkServer.active)NetworkServer.SendToAll(message);else ReceiveMobArrow(message);
            if(remove)mobArrows.RemoveAt(i);
        }
    }
    private void ReceiveMobArrow(MobArrowMessage message)
    {
        if(message.removed)
        {if(mobArrowVisuals.TryGetValue(message.id,out var old)){Destroy(old);mobArrowVisuals.Remove(message.id);}return;}
        if(!mobArrowVisuals.TryGetValue(message.id,out var visual))
        {
            if(mobArrowVisuals.Count>=128)return;
            if(mobArrowMesh==null)
            {
                var data=MobProjectileGeometry.Build();mobArrowMesh=new Mesh{name="Owned mob arrow"};mobArrowMesh.SetVertices(data.vertices);mobArrowMesh.SetTriangles(data.triangles,0);
                var uv=new List<Vector2>();foreach(var p in data.uv)uv.Add(new Vector2(p.x,p.y));mobArrowMesh.SetUVs(0,uv);mobArrowMesh.RecalculateNormals();mobArrowMesh.RecalculateBounds();
            }
            visual=new GameObject("Skeleton arrow",typeof(MeshFilter),typeof(MeshRenderer));
            visual.transform.SetParent(worldRenderer.transform,false);visual.GetComponent<MeshFilter>().sharedMesh=mobArrowMesh;
            visual.GetComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>("MobArrowMaterial");mobArrowVisuals.Add(message.id,visual);
        }
        visual.transform.position=message.position;
        if(message.direction.sqrMagnitude>.0001f)visual.transform.rotation=Quaternion.LookRotation(message.direction);
        var light=GetBlock(message.position);mobArrowProperties.SetVector("_LightLevels",new Vector4(light.GetSkyLight(),light.GetBlockLight(),1,0));mobArrowProperties.SetFloat("_Brightness",Beta173Lighting.Brightness(light.GetSkyLight(),light.GetBlockLight(),betaSkylightSubtracted,0,dimension==WorldDimension.Nether));visual.GetComponent<MeshRenderer>().SetPropertyBlock(mobArrowProperties);
    }
    private void SendMobArrows(NetworkConnectionToClient connection)
    {foreach(var arrow in mobArrows)connection.Send(new MobArrowMessage{dimension=dimension,id=arrow.id,position=arrow.position,direction=arrow.motion,embedded=arrow.embedded});}
    private void ResetMobArrows()
    {mobArrows.Clear();foreach(var visual in mobArrowVisuals.Values)if(visual!=null)Destroy(visual);mobArrowVisuals.Clear();if(mobArrowMesh!=null)Destroy(mobArrowMesh);mobArrowMesh=null;}
}

public partial class Player
{
    public bool IsMobTarget=>health>0&&!dimensionTransfer&&!((!networkStarted||isLocalPlayer)?isFlying:serverFlying);
    public bool ApplyMobDamage(int damage,Vector3 away)
    {
        if((networkStarted&&!isServer)||health<=0||dimensionTransfer||damage<=0||NetworkTime.time<hurtUntil||(!networkStarted||isLocalPlayer?isFlying:serverFlying))return false;
        hurtUntil=NetworkTime.time+.5;health=Mathf.Max(0,health-damage);exhaustion=Mathf.Min(40,exhaustion+.3f);
        away.y=.4f;if(networkStarted)RpcAddForce(connectionToClient,away,6);else AddForce(away,6);
        GameAudio.Broadcast("damage/hit",transform.position,1,1,CurrentWorld);return true;
    }
}

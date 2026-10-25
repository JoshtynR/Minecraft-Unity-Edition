using UnityEngine;

public partial class World
{
    private int mobNavigationBudget=4;
    private MobEntity GetMobAgent(MobState mob)=>mobEntities.Runtime(this,mob);
    internal bool MobBodyClear(MobState mob,Vector3 position)
    {
        float radius=MobRules.Width(mob),height=MobRules.BodyHeight(mob);
        if(!MobAreaLoaded(position)||position.y<1||position.y+height>=worldHeight)return false;
        for(int x=Mathf.FloorToInt(position.x-radius);x<=Mathf.FloorToInt(position.x+radius-.0001f);x++)
        for(int z=Mathf.FloorToInt(position.z-radius);z<=Mathf.FloorToInt(position.z+radius-.0001f);z++)
        for(int y=Mathf.FloorToInt(position.y);y<=Mathf.FloorToInt(position.y+height-.0001f);y++)
        {
            var p=new Vector3Int(x,y,z);var block=GetBlock(p);
            if(block==null||block.type==BlockType.Nothing||block.type==BlockType.Lava||block.type==BlockType.Fire)return false;
            if(block.type==BlockType.Water)continue;
            if(SpecialBlockGeometry.NoCollision(block.type)||!block.BlockData.generateCollider)continue;
            for(int part=0;part<SpecialBlockGeometry.CollisionCount(block.type);part++)
                if(BlockCollision.OverlapsBody(position,radius,height,p,SpecialBlockGeometry.CollisionPart(block.type,block.blockShape,part)))return false;
        }
        return true;
    }
    internal bool MobWalkable(MobState mob,Vector3 position)=>MobBodyClear(mob,position)&&BlockCollision.Supported(this,position,MobRules.Width(mob),MobRules.BodyHeight(mob));
    internal bool MobPathable(MobState mob,Vector3 position)
    {
        if(!MobBodyClear(mob,position))return false;
        float width=MobRules.Width(mob),height=MobRules.BodyHeight(mob);
        if(BlockCollision.Supported(this,position,width,height))return true;
        return FluidWorldQueries.Touches(this,position,width,height,BlockType.Water,out _);
    }
    private void SteerMobTo(MobState mob,Vector3 goal,float stopDistance,bool visible=true)
    {
        var delta=goal-mob.position;
        float facing=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
        mob.headYaw=Mathf.Clamp(Mathf.DeltaAngle(mob.yaw,facing),-75,75);
        mob.headPitch=Mathf.Clamp(-Mathf.Atan2(delta.y,Mathf.Sqrt(delta.x*delta.x+delta.z*delta.z))*Mathf.Rad2Deg,-40,40);
        if(visible&&delta.sqrMagnitude<stopDistance*stopDistance){mob.walking=false;return;}
        var agent=GetMobAgent(mob);bool due=agent.Navigation.SearchDue(goal,mobTick);
        bool search=due&&mobNavigationBudget>0;if(search)mobNavigationBudget--;
        var waypoint=agent.Navigation.Steer(mob,goal,mobTick,agent.Walkable,agent.Clear,search);
        var direction=waypoint-mob.position;
        mob.walking=direction.sqrMagnitude>.04f;
        if(mob.walking)mob.targetYaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
        if(!mob.walking&&mob.species==MobType.Spider&&delta.sqrMagnitude>stopDistance*stopDistance)
        {mob.walking=true;mob.targetYaw=facing;}
        if(mob.grounded&&direction.y>.2f)mob.motion.y=.42f;
    }
    internal void TickIdleWander(MobState mob)
    {
        var agent=GetMobAgent(mob);
        if((mob.position-agent.LastPosition).sqrMagnitude<.0025f&&mob.walking)agent.StuckTicks++;
        else agent.StuckTicks=0;
        agent.LastPosition=mob.position;
        if(mob.decisionTicks>0)mob.decisionTicks--;
        if(agent.HasWanderGoal&&((agent.WanderGoal-mob.position).sqrMagnitude<1||mob.decisionTicks<=0||agent.StuckTicks>30))
            agent.HasWanderGoal=false;
        if(!agent.HasWanderGoal&&mob.decisionTicks<=0)
        {
            float best=float.MinValue;Vector3 choice=mob.position;
            for(int i=0;i<10;i++)
            {
                var candidate=Vector3Int.FloorToInt(mob.position)+new Vector3Int(mobRandom.NextInt(21)-10,mobRandom.NextInt(7)-3,mobRandom.NextInt(21)-10);
                var feet=(Vector3)candidate+new Vector3(.5f,0,.5f);
                if(!MobPathable(mob,feet))continue;
                var sample=GetBlock(candidate);float light=sample==null?0:Mathf.Max(sample.GetBlockLight(),sample.GetSkyLight()-skyLightReduction);
                float score=mobRandom.NextFloat()*4+(MobRules.Hostile(mob.species)?15-light:light)*.1f;
                if(score>best){best=score;choice=feet;}
            }
            mob.decisionTicks=40+mobRandom.NextInt(80);
            agent.WanderGoal=choice;agent.HasWanderGoal=best>float.MinValue;agent.StuckTicks=0;
            if(!agent.HasWanderGoal)
            {mob.walking=false;mob.targetYaw+=mobRandom.NextInt(121)-60;mob.headYaw=Mathf.Clamp(Mathf.DeltaAngle(mob.yaw,mob.targetYaw),-60,60);}
        }
        if(agent.HasWanderGoal)SteerMobTo(mob,agent.WanderGoal,.75f);
        else
        {
            mob.walking=false;
            if(agent.LookTicks>0&&agent.WatchTarget!=null&&agent.WatchTarget.IsMobTarget&&
               (agent.WatchTarget.transform.position-mob.position).sqrMagnitude<8*8)
            {agent.LookTicks--;LookMobAt(mob,agent.WatchTarget.transform.position+Vector3.up*1.5f);}
            else
            {
                agent.LookTicks=0;agent.WatchTarget=null;
                if(mobRandom.NextInt(50)==0)
                {
                    float nearest=8*8;
                    foreach(var player in mobPlayers)
                    {float d=(player.transform.position-mob.position).sqrMagnitude;if(player.IsMobTarget&&d<nearest){nearest=d;agent.WatchTarget=player;}}
                    if(agent.WatchTarget!=null){agent.LookTicks=40+mobRandom.NextInt(40);LookMobAt(mob,agent.WatchTarget.transform.position+Vector3.up*1.5f);}
                }
                if(agent.WatchTarget==null)
                {mob.headYaw=Mathf.Lerp(mob.headYaw,Mathf.Clamp(Mathf.DeltaAngle(mob.yaw,mob.targetYaw),-60,60),.1f);mob.headPitch=Mathf.Lerp(mob.headPitch,0,.1f);}
            }
        }
    }
    internal void TickFarmAnimal(MobState mob)
    {
        if(mob.ageTicks>0)mob.ageTicks--;
        if(mob.breedCooldown>0)mob.breedCooldown--;
        if(mob.loveTicks>0)mob.loveTicks--;
        if(mob.fleeTicks>0)
        {
            mob.courtshipTicks=0;float angle=mob.targetYaw*Mathf.Deg2Rad;
            SteerMobTo(mob,mob.position+new Vector3(Mathf.Sin(angle)*8,0,Mathf.Cos(angle)*8),.5f,false);return;
        }
        if(mob.eatTicks>0){mob.courtshipTicks=0;return;}
        MobState mate=null;float nearest=8*8;
        if(mob.loveTicks>0)foreach(var other in activeMobs)
        {float d=(other.position-mob.position).sqrMagnitude;if(d<nearest&&MobRules.CanBreed(mob,other)){mate=other;nearest=d;}}
        if(mate!=null)
        {
            bool visible=MobCanSee(mob.position+Vector3.up*.5f,mate.position+Vector3.up*.5f);
            SteerMobTo(mob,mate.position,1.2f,visible);
            mob.courtshipTicks=nearest<2.25f&&visible?Mathf.Min(60,mob.courtshipTicks+1):0;
            if(mob.courtshipTicks>=60&&MobWalkable(mob,mob.position))
            {
                var baby=mobs.Spawn(mob.position,mob.yaw,mob.species);
                if(baby!=null)
                {
                    baby.ageTicks=24000;baby.fleeceColor=mobRandom.NextInt(2)==0?mob.fleeceColor:mate.fleeceColor;
                    mob.loveTicks=mate.loveTicks=mob.courtshipTicks=mate.courtshipTicks=0;
                    mob.breedCooldown=mate.breedCooldown=6000;BroadcastMob(baby);BroadcastMob(mate);BroadcastMob(mob);
                }
            }
            return;
        }
        mob.courtshipTicks=0;
        Player tempting=null;nearest=8*8;
        foreach(var player in mobPlayers)
        {
            if(player.DimensionTransferInProgress||player.inventory==null)continue;
            var held=player.inventory.HeldItem();if(held.Empty||held.type!=MobRules.BreedingFood(mob))continue;
            float d=(player.transform.position-mob.position).sqrMagnitude;
            if(d<nearest){nearest=d;tempting=player;}
        }
        if(tempting!=null){SteerMobTo(mob,tempting.transform.position,2);LookMobAt(mob,tempting.transform.position+Vector3.up*1.5f);return;}
        if(mob.ageTicks>0)
        {
            MobState parent=null;nearest=8*8;
            foreach(var other in activeMobs)
            {float d=(other.position-mob.position).sqrMagnitude;if(other.species==mob.species&&other.ageTicks==0&&d<nearest){nearest=d;parent=other;}}
            if(parent!=null&&nearest>3*3){SteerMobTo(mob,parent.position,2.5f);return;}
        }
        TickIdleWander(mob);
    }
    internal void TickSheepFleece(MobState mob)
    {
        var ground=Vector3Int.FloorToInt(mob.position+Vector3.down*.05f);
        if(mob.eatTicks==0&&mob.sheared&&mobRandom.NextInt(1000)==0&&GetBlock(ground).type==BlockType.Grass)mob.eatTicks=40;
        if(mob.eatTicks<=0)return;
        mob.walking=false;mob.eatTicks--;
        if(mob.eatTicks==4&&GetBlock(ground).type==BlockType.Grass)
        {SetFarmBlock(ground,BlockType.Dirt);mob.sheared=false;BroadcastMob(mob);}
    }
    internal void TickChickenEgg(MobState mob)
    {
        if(mob.ageTicks>0||--mob.eggTicks>0)return;
        mob.eggTicks=6000+mobRandom.NextInt(6000);SpawnItemDrop(new InventoryStack(ItemType.Egg,1),mob.position);
        GameAudio.Broadcast("mob/chicken/plop",mob.position,1,GameAudio.Variation(.9f,1.1f),this);
    }
    private void LookMobAt(MobState mob,Vector3 point)
    {
        var delta=point-(mob.position+Vector3.up*(MobRules.BodyHeight(mob)*.85f));
        mob.headYaw=Mathf.Clamp(Mathf.DeltaAngle(mob.yaw,Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg),-75,75);
        mob.headPitch=Mathf.Clamp(-Mathf.Atan2(delta.y,Mathf.Sqrt(delta.x*delta.x+delta.z*delta.z))*Mathf.Rad2Deg,-40,40);
    }
}

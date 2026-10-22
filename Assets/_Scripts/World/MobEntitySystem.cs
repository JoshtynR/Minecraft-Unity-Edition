using System;
using System.Collections.Generic;
using UnityEngine;

// A world owns one entity system. Persistent state, fixed ticks and transient
// agents are independent of renderer GameObjects and chunk pooling.
public sealed class MobEntitySystem
{
    public readonly MobStore Store=new MobStore();
    private readonly Dictionary<int,MobEntity> entities=new Dictionary<int,MobEntity>();
    private readonly Dictionary<Vector3Int,List<MobState>> pushBuckets=new Dictionary<Vector3Int,List<MobState>>();
    private readonly List<List<MobState>> pushPool=new List<List<MobState>>();
    private float accumulator;
    public int Tick {get;private set;}
    public int RuntimeCount=>entities.Count;
    public void Accumulate(float seconds)
    {if(!float.IsNaN(seconds)&&seconds>0)accumulator=Math.Min(.25f,accumulator+seconds);}
    public bool TryStep()
    {if(accumulator+.000001f<.05f)return false;accumulator=Math.Max(0,accumulator-.05f);Tick=unchecked(Tick+1);return true;}
    public MobEntity Runtime(World world,MobState state)
    {
        if(state==null||!ReferenceEquals(Store.Get(state.id),state))throw new InvalidOperationException("Mob runtime requires a live entity from this world.");
        if(!entities.TryGetValue(state.id,out var entity)||!ReferenceEquals(entity.State,state))
        {entity=new MobEntity(world,state);entities[state.id]=entity;}
        return entity;
    }
    public void RemoveRuntime(int id)=>entities.Remove(id);
    public static void PushApart(MobState a,MobState b)
    {
        if(a.health<=0||b.health<=0||a.id==b.id||a.position.y+MobRules.BodyHeight(a)<=b.position.y||b.position.y+MobRules.BodyHeight(b)<=a.position.y)return;
        float dx=b.position.x-a.position.x,dz=b.position.z-a.position.z,radius=MobRules.Width(a)+MobRules.Width(b);
        float square=dx*dx+dz*dz;if(square>=radius*radius)return;
        float distance=(float)Math.Sqrt(square);
        if(distance<.001f){dx=a.id<b.id?1:-1;dz=0;distance=.001f;}else{dx/=distance;dz/=distance;}
        float push=.025f*(1-distance/radius);var force=new Vector3(dx*push,0,dz*push);
        a.motion-=force;b.motion+=force;
    }
    public void Separate(IList<MobState> active)
    {
        foreach(var bucket in pushPool)bucket.Clear();pushBuckets.Clear();int used=0;
        foreach(var mob in active)
        {
            if(mob.health<=0)continue;var cell=Vector3Int.FloorToInt(mob.position*.5f);
            if(!pushBuckets.TryGetValue(cell,out var bucket))
            {if(used==pushPool.Count)pushPool.Add(new List<MobState>());bucket=pushPool[used++];pushBuckets[cell]=bucket;}
            bucket.Add(mob);
        }
        foreach(var mob in active)
        {
            if(mob.health<=0)continue;var cell=Vector3Int.FloorToInt(mob.position*.5f);int inspected=0;
            for(int y=-1;y<=1&&inspected<32;y++)for(int x=-1;x<=1&&inspected<32;x++)for(int z=-1;z<=1&&inspected<32;z++)
            {
                if(!pushBuckets.TryGetValue(cell+new Vector3Int(x,y,z),out var bucket))continue;
                int candidates=Math.Min(8,bucket.Count);
                for(int i=0;i<candidates&&inspected++<32;i++)
                {var other=bucket[(mob.id+i)%bucket.Count];if(other.id>mob.id)PushApart(mob,other);}
            }
        }
    }
    public void Clear()
    {Store.Restore(null);entities.Clear();pushBuckets.Clear();pushPool.Clear();accumulator=0;Tick=0;}
}

public sealed class MobEntity
{
    public readonly MobState State;
    public readonly MobNavigation Navigation=new MobNavigation();
    public readonly Func<Vector3,bool> Walkable,Clear;
    public Player Target;
    internal MobEntity(World world,MobState state)
    {State=state;Walkable=position=>world.MobWalkable(state,position);Clear=position=>world.MobBodyClear(state,position);}
}

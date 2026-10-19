using System;
using System.Collections.Generic;

[Serializable]
public sealed class FluidTickEntry
{
    public int x,y,z,material;
    public long due,order;
}
[Serializable]
public sealed class FluidTickSave
{
    public long tick,sequence,randomState;
    public int randomTickState;
    public FluidTickEntry[] entries;
}

// One ordered queue for both materials, matching NextTickListEntry's scheduled
// time then insertion ID ordering. Deduplicate by position AND material.
public sealed class FluidTickScheduler
{
    private sealed class EntryComparer : IComparer<FluidTickEntry>
    {public int Compare(FluidTickEntry a,FluidTickEntry b){int c=a.due.CompareTo(b.due);return c!=0?c:a.order.CompareTo(b.order);}}
    private readonly SortedSet<FluidTickEntry> pending=new SortedSet<FluidTickEntry>(new EntryComparer());
    private readonly HashSet<Tuple<int,int,int,int>> keys=new HashSet<Tuple<int,int,int,int>>();
    private readonly Action<WaterPosition>[] handlers=new Action<WaterPosition>[3];
    public long Tick {get;private set;}
    private long sequence;
    public void Bind(int material,Action<WaterPosition> update){handlers[material]=update;}
    private static Tuple<int,int,int,int> Key(FluidTickEntry e)=>Tuple.Create(e.x,e.y,e.z,e.material);
    public int Count(int material){int n=0;foreach(var e in pending)if(e.material==material)n++;return n;}
    public void Schedule(WaterPosition p,int material,int delay)
    {
        var e=new FluidTickEntry{x=p.x,y=p.y,z=p.z,material=material,due=Tick+Math.Max(1,delay),order=sequence++};
        if(keys.Add(Key(e)))pending.Add(e);
    }
    public void Advance(int budget=1000)
    {
        Tick++;
        // Vanilla captures the starting size: callbacks cannot consume the
        // budget with newly scheduled entries during the same tick.
        int count=Math.Min(budget,pending.Count);
        for(int i=0;i<count&&pending.Count>0;i++)
        {
            var e=pending.Min;if(e.due>Tick)break;
            pending.Remove(e);keys.Remove(Key(e));
            handlers[e.material]?.Invoke(new WaterPosition(e.x,e.y,e.z));
        }
    }
    public FluidTickSave Capture(long randomState,int randomTickState)
    {
        var entries=new List<FluidTickEntry>();
        foreach(var e in pending)entries.Add(new FluidTickEntry{x=e.x,y=e.y,z=e.z,material=e.material,due=e.due,order=e.order});
        return new FluidTickSave{tick=Tick,sequence=sequence,randomState=randomState,randomTickState=randomTickState,entries=entries.ToArray()};
    }
    public void Restore(FluidTickSave save)
    {
        pending.Clear();keys.Clear();Tick=Math.Max(0,save.tick);sequence=Math.Max(0,save.sequence);
        if(save.entries==null)return;
        foreach(var e in save.entries)
        {
            if(e==null||e.material<0||e.material>=handlers.Length)continue;
            var copy=new FluidTickEntry{x=e.x,y=e.y,z=e.z,material=e.material,due=e.due,order=sequence++};
            if(keys.Add(Key(copy)))pending.Add(copy);
        }
    }
}

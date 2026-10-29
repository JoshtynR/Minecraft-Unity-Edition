using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DroppedItemState
{
    public int id;
    public InventoryStack stack;
    public Vector3 position,motion; // feet position; motion in blocks per 20 Hz tick
    public int age,pickupDelay,health=5;
    public float hover;
    public DroppedItemState Copy() => (DroppedItemState)MemberwiseClone();
}

public static class DroppedItemRules
{
    public const float HalfWidth=.125f,Height=.25f;
    public const int Lifetime=6000;
    public static bool Finite(Vector3 v)=>!(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z));
    public static bool Valid(DroppedItemState item)=>item!=null&&item.id>0&&!item.stack.Empty&&ItemCatalog.Valid(item.stack.type)&&
        item.stack.count<=InventoryRules.Limit(item.stack.type)&&item.stack.damage>=0&&item.age>=0&&item.age<Lifetime&&item.health>0&&!float.IsNaN(item.hover)&&!float.IsInfinity(item.hover)&&Finite(item.position)&&Finite(item.motion);
    public static int Copies(int count)=>count>20?4:count>5?3:count>1?2:1;
    public static float Bob(float age,float hover)=>(float)Math.Sin(age/10f+hover)*.1f+.1f;
    public static float Rotation(float age,float hover)=>(age/20f+hover)*57.295776f;
    public static bool CanPickup(DroppedItemState item,Vector3 feet,float radius=.32f,float height=1.8f)
    {
        return Valid(item)&&item.pickupDelay<=0&&Finite(feet)&&
            Math.Abs(item.position.x-feet.x)<radius+1+HalfWidth&&Math.Abs(item.position.z-feet.z)<radius+1+HalfWidth&&
            item.position.y<feet.y+height&&item.position.y+Height>feet.y;
    }
    // Collision is supplied by the world's existing swept voxel collision code.
    public static bool Tick(DroppedItemState item,Func<Vector3,Vector3,Vector3> move,float slipperiness,Vector3 current,bool lava,bool fire)
    {
        if(!Valid(item))return false;
        if(item.pickupDelay>0)item.pickupDelay--;
        item.motion+=current*.014f;
        item.motion.y-=.04f;
        if(lava){item.motion.y=.2f;item.health-=4;}
        else if(fire)item.health--;
        var requested=item.motion;var moved=move(item.position,requested);item.position+=moved;
        bool grounded=requested.y<0&&moved.y!=requested.y;
        if(moved.x!=requested.x)item.motion.x=0;
        if(moved.y!=requested.y)item.motion.y=0;
        if(moved.z!=requested.z)item.motion.z=0;
        float drag=grounded?slipperiness*.98f:.98f;
        item.motion.x*=drag;item.motion.z*=drag;item.motion.y*=.98f;
        if(grounded)item.motion.y*=-.5f;
        item.age++;
        return Valid(item)&&item.position.y>=-64;
    }
}

// This store is authoritative on the server (or offline world). It never accepts
// client-supplied item contents/positions, only a request for an existing ID.
public sealed class DroppedItemStore
{
    private readonly Dictionary<int,DroppedItemState> items=new Dictionary<int,DroppedItemState>();
    private readonly Dictionary<Vector3Int,HashSet<int>> spatialBuckets=new Dictionary<Vector3Int,HashSet<int>>();
    private readonly Dictionary<int,Vector3Int> spatialCells=new Dictionary<int,Vector3Int>();
    public int LastSpatialCandidates {get;private set;}
    private static Vector3Int SpatialCell(Vector3 p)=>Vector3Int.FloorToInt(p*.125f);
    private void RemoveSpatial(int id)
    {
        if(!spatialCells.TryGetValue(id,out var cell))return;
        if(spatialBuckets.TryGetValue(cell,out var bucket))
        {bucket.Remove(id);if(bucket.Count==0)spatialBuckets.Remove(cell);}
        spatialCells.Remove(id);
    }
    public void UpdatePosition(DroppedItemState item)
    {
        if(item==null||!ReferenceEquals(Get(item.id),item))return;
        var cell=SpatialCell(item.position);
        if(spatialCells.TryGetValue(item.id,out var previous)&&previous.Equals(cell))return;
        RemoveSpatial(item.id);
        if(!spatialBuckets.TryGetValue(cell,out var bucket))spatialBuckets[cell]=bucket=new HashSet<int>();
        bucket.Add(item.id);spatialCells[item.id]=cell;
    }
    public void QueryNearby(Vector3 position,float radius,List<DroppedItemState> output)
    {
        output.Clear();LastSpatialCandidates=0;
        if(!DroppedItemRules.Finite(position)||float.IsNaN(radius)||float.IsInfinity(radius)||radius<0)return;
        var extent=new Vector3(radius,radius,radius);var min=SpatialCell(position-extent);var max=SpatialCell(position+extent);
        for(int x=min.x;x<=max.x;x++)for(int y=min.y;y<=max.y;y++)for(int z=min.z;z<=max.z;z++)
        {
            if(!spatialBuckets.TryGetValue(new Vector3Int(x,y,z),out var bucket))continue;
            foreach(var id in bucket){LastSpatialCandidates++;if(items.TryGetValue(id,out var item))output.Add(item);}
        }
    }
    private int nextId=1;
    public IEnumerable<DroppedItemState> Items=>items.Values;
    public int Count=>items.Count;
    public DroppedItemState Get(int id){items.TryGetValue(id,out var item);return item;}
    public DroppedItemState Spawn(InventoryStack stack,Vector3 position,Vector3 motion,int delay,float hover)
    {
        var item=new DroppedItemState{id=nextId,stack=stack,position=position,motion=motion,pickupDelay=Math.Max(0,delay),hover=hover};
        if(!DroppedItemRules.Valid(item)||nextId==int.MaxValue)return null;
        nextId++;items.Add(item.id,item);UpdatePosition(item);return item;
    }
    public void Apply(DroppedItemState state)
    {if(state==null)return;RemoveSpatial(state.id);if(!DroppedItemRules.Valid(state))items.Remove(state.id);else{items[state.id]=state.Copy();UpdatePosition(items[state.id]);}}
    public bool Remove(int id){RemoveSpatial(id);return items.Remove(id);}
    public int Pickup(int id,Vector3 player,InventoryStack[] inventory)
    {
        var item=Get(id);
        if(!DroppedItemRules.CanPickup(item,player)||!ChestTransactions.Valid(inventory,new InventoryStack()))return 0;
        int remaining=InventoryRules.Insert(inventory,item.stack.type,item.stack.count,0,36,item.stack.damage);
        int picked=item.stack.count-remaining;
        item.stack.count=remaining;
        if(remaining==0)Remove(id);
        return picked;
    }
    public DroppedItemState[] Capture()
    {
        var copy=new DroppedItemState[items.Count];int index=0;
        foreach(var item in items.Values)copy[index++]=item.Copy();
        return copy;
    }
    public void Restore(DroppedItemState[] saved)
    {
        items.Clear();spatialBuckets.Clear();spatialCells.Clear();LastSpatialCandidates=0;nextId=1;
        if(saved==null)return;
        foreach(var item in saved)
        {
            if(!DroppedItemRules.Valid(item)||item.id==int.MaxValue||items.ContainsKey(item.id))continue;
            items[item.id]=item.Copy();UpdatePosition(items[item.id]);nextId=Math.Max(nextId,item.id+1);
        }
    }
}

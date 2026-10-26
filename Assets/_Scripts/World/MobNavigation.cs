using System;
using System.Collections.Generic;
using UnityEngine;

// Bounded A* over walkable voxel cells. The world supplies collision/support
// tests, so stairs, fences, water, and unloaded chunks use the same safety rules.
public sealed class MobNavigation
{
    private struct OpenNode
    {
        public Vector3Int cell;
        public int cost,score,order;
    }
    private readonly List<OpenNode> open=new List<OpenNode>();
    private readonly Dictionary<Vector3Int,int> costs=new Dictionary<Vector3Int,int>();
    private readonly Dictionary<Vector3Int,Vector3Int> parents=new Dictionary<Vector3Int,Vector3Int>();
    private readonly HashSet<Vector3Int> closed=new HashSet<Vector3Int>();
    private readonly List<Vector3> path=new List<Vector3>();
    private Vector3Int previousGoal;
    private int nextSearch,index;
    private int openOrder;
    public int SearchCount {get;private set;}
    private static readonly Vector3Int[] directions={
        new Vector3Int(1,0,0),new Vector3Int(-1,0,0),new Vector3Int(0,0,1),new Vector3Int(0,0,-1),
        new Vector3Int(1,0,1),new Vector3Int(1,0,-1),new Vector3Int(-1,0,1),new Vector3Int(-1,0,-1)};
    private static readonly int[] vertical={0,1,-1,-2,-3};
    private static int Heuristic(Vector3Int a,Vector3Int b)
    {
        int dx=Math.Abs(a.x-b.x),dz=Math.Abs(a.z-b.z),diagonal=Math.Min(dx,dz);
        return diagonal*14+(Math.Max(dx,dz)-diagonal)*10+4*Math.Abs(a.y-b.y);
    }
    private static bool Before(OpenNode a,OpenNode b)=>a.score<b.score||(a.score==b.score&&a.order<b.order);
    private void Push(Vector3Int cell,int cost,int score)
    {
        var node=new OpenNode{cell=cell,cost=cost,score=score,order=openOrder++};
        int child=open.Count;open.Add(node);
        while(child>0)
        {
            int parent=(child-1)/2;if(Before(open[parent],node))break;
            open[child]=open[parent];child=parent;
        }
        open[child]=node;
    }
    private OpenNode Pop()
    {
        var root=open[0];int lastIndex=open.Count-1;var last=open[lastIndex];open.RemoveAt(lastIndex);
        if(lastIndex==0)return root;
        int parent=0;
        while(true)
        {
            int left=parent*2+1;if(left>=lastIndex)break;
            int right=left+1,child=right<lastIndex&&Before(open[right],open[left])?right:left;
            if(Before(last,open[child]))break;
            open[parent]=open[child];parent=child;
        }
        open[parent]=last;return root;
    }
    public bool SearchDue(Vector3 goal,int tick)=>tick>=nextSearch||(Vector3Int.FloorToInt(goal)-previousGoal).sqrMagnitude>4;
    public Vector3 Steer(MobState mob,Vector3 goal,int tick,Func<Vector3,bool> walkable,Func<Vector3,bool> clear,bool allowSearch=true)
    {
        var start=Vector3Int.FloorToInt(mob.position);var end=Vector3Int.FloorToInt(goal);
        if(allowSearch&&SearchDue(goal,tick))
        {Search(start,end,walkable,clear);previousGoal=end;nextSearch=tick+20;index=0;}
        while(index<path.Count&&(path[index]-mob.position).sqrMagnitude<.45f*.45f)index++;
        return index<path.Count?path[index]:mob.position;
    }
    private void Search(Vector3Int start,Vector3Int goal,Func<Vector3,bool> walkable,Func<Vector3,bool> clear)
    {
        SearchCount++;
        open.Clear();closed.Clear();costs.Clear();parents.Clear();path.Clear();openOrder=0;Push(start,0,Heuristic(start,goal));costs[start]=0;
        Vector3Int best=start;int bestDistance=Heuristic(start,goal);
        for(int expanded=0;expanded<192&&open.Count>0;)
        {
            var selected=Pop();var current=selected.cell;
            if(closed.Contains(current)||!costs.TryGetValue(current,out int currentCost)||currentCost!=selected.cost)continue;
            closed.Add(current);expanded++;
            int distance=Heuristic(current,goal);if(distance<bestDistance){best=current;bestDistance=distance;}
            if(current.x==goal.x&&current.z==goal.z&&Math.Abs(current.y-goal.y)<=1){best=current;break;}
            foreach(var direction in directions)
            {
                for(int candidate=0;candidate<vertical.Length;candidate++)
                {
                    int dy=vertical[candidate];var rise=new Vector3Int(0,dy,0);var next=current+direction+rise;
                    if(Math.Abs(next.x-start.x)>24||Math.Abs(next.z-start.z)>24||closed.Contains(next))continue;
                    if(!walkable((Vector3)next+new Vector3(.5f,0,.5f)))continue;
                    // Diagonal movement may not squeeze through the joined
                    // corners of two solid blocks.
                    if(direction.x!=0&&direction.z!=0&&
                       (!walkable((Vector3)(current+new Vector3Int(direction.x,0,0)+rise)+new Vector3(.5f,0,.5f))||
                        !walkable((Vector3)(current+new Vector3Int(0,0,direction.z)+rise)+new Vector3(.5f,0,.5f))))continue;
                    // A one-block ascent needs overhead clearance in the source cell.
                    if(dy==1&&!clear((Vector3)current+new Vector3(.5f,1,.5f)))continue;
                    int cost=currentCost+(direction.x!=0&&direction.z!=0?14:10)+Math.Abs(dy)*4;
                    if(costs.TryGetValue(next,out int old)&&old<=cost)break;
                    costs[next]=cost;parents[next]=current;Push(next,cost,cost+Heuristic(next,goal));break;
                }
            }
        }
        int guard=0;
        while(best!=start&&guard++<192)
        {path.Add((Vector3)best+new Vector3(.5f,0,.5f));if(!parents.TryGetValue(best,out best))break;}
        path.Reverse();
    }
}

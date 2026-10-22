using System;
using System.Collections.Generic;
using UnityEngine;

// Bounded A* over walkable voxel cells. The world supplies collision/support
// tests, so stairs, fences, water, and unloaded chunks use the same safety rules.
public sealed class MobNavigation
{
    private readonly List<Vector3Int> open=new List<Vector3Int>();
    private readonly Dictionary<Vector3Int,int> costs=new Dictionary<Vector3Int,int>();
    private readonly Dictionary<Vector3Int,Vector3Int> parents=new Dictionary<Vector3Int,Vector3Int>();
    private readonly HashSet<Vector3Int> closed=new HashSet<Vector3Int>();
    private readonly List<Vector3> path=new List<Vector3>();
    private Vector3Int previousGoal;
    private int nextSearch,index;
    public int SearchCount {get;private set;}
    private static readonly Vector3Int[] directions={new Vector3Int(1,0,0),new Vector3Int(-1,0,0),new Vector3Int(0,0,1),new Vector3Int(0,0,-1)};
    private static int Heuristic(Vector3Int a,Vector3Int b)=>10*(Math.Abs(a.x-b.x)+Math.Abs(a.z-b.z))+4*Math.Abs(a.y-b.y);
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
        open.Clear();closed.Clear();costs.Clear();parents.Clear();path.Clear();open.Add(start);costs[start]=0;
        Vector3Int best=start;int bestDistance=Heuristic(start,goal);
        for(int expanded=0;expanded<192&&open.Count>0;expanded++)
        {
            int selected=0,score=int.MaxValue;
            for(int i=0;i<open.Count;i++){int value=costs[open[i]]+Heuristic(open[i],goal);if(value<score){score=value;selected=i;}}
            var current=open[selected];open.RemoveAt(selected);closed.Add(current);
            int distance=Heuristic(current,goal);if(distance<bestDistance){best=current;bestDistance=distance;}
            if(current.x==goal.x&&current.z==goal.z&&Math.Abs(current.y-goal.y)<=1){best=current;break;}
            foreach(var direction in directions)
            {
                for(int candidate=0;candidate<3;candidate++)
                {
                    int dy=candidate==0?0:candidate==1?1:-1;var next=current+direction+new Vector3Int(0,dy,0);
                    if(Math.Abs(next.x-start.x)>16||Math.Abs(next.z-start.z)>16||closed.Contains(next))continue;
                    if(!walkable((Vector3)next+new Vector3(.5f,0,.5f)))continue;
                    // A one-block ascent needs overhead clearance in the source cell.
                    if(dy==1&&!clear((Vector3)current+new Vector3(.5f,1,.5f)))continue;
                    int cost=costs[current]+10+Math.Abs(dy)*4;
                    if(costs.TryGetValue(next,out int old)&&old<=cost)break;
                    costs[next]=cost;parents[next]=current;if(!open.Contains(next))open.Add(next);break;
                }
            }
        }
        int guard=0;
        while(best!=start&&guard++<192)
        {path.Add((Vector3)best+new Vector3(.5f,0,.5f));if(!parents.TryGetValue(best,out best))break;}
        path.Reverse();
    }
}

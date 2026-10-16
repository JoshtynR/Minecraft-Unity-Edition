using System;
using System.Collections.Generic;
public static partial class WaterCheck
{
 private sealed class LavaGrid : IWaterWorld
 {
  public readonly Dictionary<WaterPosition,int> cells=new Dictionary<WaterPosition,int>();
  public int writes;
  public int Raw(WaterPosition p){int value;return cells.TryGetValue(p,out value)?value:p.y<=0||p.y>12||Math.Abs(p.x)>12||Math.Abs(p.z)>12?WaterSimulation.Solid:WaterSimulation.Air;}
  public int GetWaterCell(WaterPosition p){int value=Raw(p);return value==WaterSimulation.Obsidian||value==WaterSimulation.Cobblestone?WaterSimulation.Solid:value;}
  public void SetWaterCell(WaterPosition p,int value){cells[p]=value;writes++;}
 }
 private static WaterSimulation Lava(LavaGrid g)=>new WaterSimulation(g,true,false,()=>0);
 private static void CheckLava()
 {
  var g=new LavaGrid();g.cells[P(0)]=0;var sim=Lava(g);sim.Schedule(P(0));
  Ticks(sim,29);Assert(g.writes==0,"lava waits thirty game ticks");Ticks(sim,1);
  Assert(g.Raw(P(1))==2&&g.Raw(P(-1))==2,"overworld lava spreads with decay two");
  Ticks(sim,600);
  for(int x=-5;x<=5;x++)for(int z=-5;z<=5;z++)
  {
   int distance=Math.Abs(x)+Math.Abs(z);
   Assert(g.Raw(P(x,1,z))==(distance<=3?distance*2:WaterSimulation.Air),"flat source lava has three-block horizontal reach");
  }
  Assert(sim.PendingCount==0,"settled lava stops ticking");
  g.cells[P(0)]=WaterSimulation.Air;sim.NotifyChanged(P(0));Ticks(sim,1500);
  foreach(var c in g.cells)Assert(c.Value<0,"lava drains after source removal");

  g=new LavaGrid();g.cells[P(0,5)]=0;
  for(int y=2;y<=5;y++){g.cells[P(-1,y)]=WaterSimulation.Solid;g.cells[P(1,y)]=WaterSimulation.Solid;g.cells[P(0,y,-1)]=WaterSimulation.Solid;g.cells[P(0,y,1)]=WaterSimulation.Solid;}
  sim=Lava(g);sim.Schedule(P(0,5));Ticks(sim,150);
  Assert(g.Raw(P(1))==1,"Beta falling lava initially emits horizontal level one");Ticks(sim,900);
  for(int y=1;y<5;y++)Assert(g.Raw(P(0,y))==8,"falling lava keeps the falling flag");
  Assert(g.Raw(P(1))==2&&g.Raw(P(2))==4&&g.Raw(P(3))==6&&g.Raw(P(4))==WaterSimulation.Air,"waterfall lava recalculates to the normal two-level decay after its first update");

  g=new LavaGrid();g.cells[P(-1)]=0;g.cells[P(1)]=0;g.cells[P(0)]=2;
  sim=Lava(g);sim.Schedule(P(0));Ticks(sim,30);Assert(g.Raw(P(0))==2,"two lava sources never regenerate a third source");

  g=new LavaGrid();g.cells[P(0)]=2;g.cells[P(-1)]=4;g.cells[P(1)]=WaterSimulation.Solid;g.cells[P(0,1,-1)]=WaterSimulation.Solid;g.cells[P(0,1,1)]=WaterSimulation.Solid;
  int draws=0;sim=new WaterSimulation(g,true,false,()=>draws++==0?1:0);sim.Schedule(P(0));Ticks(sim,30);
  Assert(g.Raw(P(0))==2&&draws==1&&sim.PendingCount>0,"lava three-in-four hesitation reschedules itself");Ticks(sim,30);
  Assert(g.Raw(P(0))==6&&draws==2,"lava progresses after successful hesitation draw");

  var neighbors=new[]{P(-1),P(1),P(0,1,-1),P(0,1,1),P(0,2),P(0,0)};
  for(int level=0;level<16;level++)for(int d=0;d<neighbors.Length;d++)
  {
   g=new LavaGrid();g.cells[P(0)]=level;g.cells[neighbors[d]]=WaterSimulation.Water;
   sim=Lava(g);sim.NotifyChanged(neighbors[d]);
   int expected=d<5&&level<=4?(level==0?WaterSimulation.Obsidian:WaterSimulation.Cobblestone):level;
   Assert(g.Raw(P(0))==expected,"Beta hardening matrix for level "+level+" direction "+d);
   Assert(g.Raw(neighbors[d])==WaterSimulation.Water,"hardening does not consume the touching water");
  }
  g=new LavaGrid();g.cells[P(0,2)]=0;g.cells[P(0)]=WaterSimulation.Water;
  sim=Lava(g);sim.Schedule(P(0,2));Ticks(sim,30);
  Assert(g.Raw(P(0))==8&&g.Raw(P(0,2))==0,"Beta lava falling into water replaces it, without the later stone-generator rule");
  g=new LavaGrid();g.cells[P(0)]=0;g.cells[P(1)]=WaterSimulation.Unloaded;sim=Lava(g);sim.Schedule(P(0));Ticks(sim,120);
  Assert(g.writes==0,"lava cannot flow across unknown chunk boundary");g.cells[P(1)]=WaterSimulation.Air;sim.NotifyChanged(P(1));Ticks(sim,30);
  Assert(g.Raw(P(1))==2,"lava resumes after missing neighbor loads");
  g=new LavaGrid();g.cells[P(0)]=0;sim=new WaterSimulation(g,true,true,()=>0);sim.Schedule(P(0));Ticks(sim,30);
  Assert(g.Raw(P(1))==1,"optional Nether lava decay is one, while tick rate remains thirty");
 }
}

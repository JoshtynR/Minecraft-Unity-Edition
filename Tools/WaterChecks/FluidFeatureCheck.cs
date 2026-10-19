using System;
using System.Collections.Generic;

public static partial class WaterCheck
{
    private sealed class FireGrid:IFireWorld
    {
        public Dictionary<WaterPosition,BlockType> blocks=new Dictionary<WaterPosition,BlockType>();
        public Dictionary<WaterPosition,int> ages=new Dictionary<WaterPosition,int>();
        public int schedules;
        public BlockType Type(WaterPosition p){BlockType t;return blocks.TryGetValue(p,out t)?t:BlockType.Air;}
        public int Age(WaterPosition p){int n;return ages.TryGetValue(p,out n)?n:0;}
        public bool Solid(WaterPosition p){var t=Type(p);return t!=BlockType.Air&&t!=BlockType.Fire&&t!=BlockType.Nothing;}
        public void SetFire(WaterPosition p,int age){blocks[p]=age<0?BlockType.Air:BlockType.Fire;ages[p]=Math.Max(0,age);}
        public void ScheduleFire(WaterPosition p){schedules++;}
    }
    private static void CheckFluidFeatures()
    {
        var p=new WaterPosition(0,0,0);
        Func<WaterPosition,int> sample=q=>q.Equals(p)?0:q.Equals(p.Offset(1,0,0))?1:WaterSimulation.Solid;
        var current=BetaFluidPhysics.Current(sample,q=>false,p);
        Assert(Math.Abs(current.x-1)<1e-8&&current.z==0&&current.y==0,"source-to-decay current points east");
        sample=q=>q.Equals(p)?8:q.Equals(p.Offset(1,0,0))?1:WaterSimulation.Solid;
        current=BetaFluidPhysics.Current(sample,q=>q.x==-1,p);
        Assert(current.y<-.98&&current.x>0&&Math.Abs(current.Length-1)<1e-8,"falling flow at wall pulls downward");
        var motion=BetaFluidPhysics.Accelerate(new FluidVector(),1,1,new FluidVector(1,0,0),new FluidVector(0,0,1),true,new FluidVector(1,0,0),false);
        Assert(Math.Abs(motion.x-(.014+.02/Math.Sqrt(2)))<1e-7&&Math.Abs(motion.z-.02/Math.Sqrt(2))<1e-7&&Math.Abs(motion.y-.04)<1e-7,"Beta diagonal acceleration, current, held jump");
        var lava=BetaFluidPhysics.Accelerate(new FluidVector(),0,0,new FluidVector(),new FluidVector(),false,new FluidVector(1,0,0),true);
        Assert(lava.Length==0,"lava does not apply water current acceleration");
        var waterDrag=BetaFluidPhysics.Drag(new FluidVector(1,1,1),false);var lavaDrag=BetaFluidPhysics.Drag(new FluidVector(1,1,1),true);
        Assert(Math.Abs(waterDrag.y-.78)<1e-8&&Math.Abs(lavaDrag.y-.48)<1e-8,"water and lava drag/gravity coefficients");
        Assert(BetaFluidPhysics.EyeSurface(3,0)==4&&BetaFluidPhysics.EyeSurface(3,8)==4&&Math.Abs(BetaFluidPhysics.EyeSurface(3,7)-(4-7/9f))<1e-6,"Beta eye submersion threshold differs from rendered corner height");
        var hazards=new FluidHazards();int damage=0;
        for(int t=0;t<319;t++)damage+=hazards.Tick(true,true,false,false,false);
        Assert(damage==0&&hazards.air==-19,"15 seconds of air then 19 drowning grace ticks");
        Assert(hazards.Tick(true,true,false,false,false)==2&&hazards.air==0,"drowning deals two points every 20 ticks after air expires");
        hazards.Tick(false,false,false,false,false);Assert(hazards.air==300,"surfacing refills air immediately");
        hazards=new FluidHazards();damage=0;
        for(int t=0;t<20;t++)damage+=hazards.Tick(false,false,true,false,false);
        Assert(damage==8&&hazards.fireTicks==600,"lava damage honors hurt resistance and refreshes 600 fire ticks");
        hazards.Tick(true,false,false,false,false);Assert(hazards.fireTicks==0,"water extinguishes the player");
        Assert(hazards.Tick(false,true,true,true,true)==0&&hazards.air==300&&hazards.fireTicks==0,"flight immunity cannot drown or retain fire");
        hazards=new FluidHazards();Assert(hazards.Tick(false,false,false,true,false)==1&&hazards.fireTicks==300,"touching fire ignites for 300 ticks");

        var scheduler=new FluidTickScheduler();var order=new List<int>();
        scheduler.Bind(0,q=>order.Add(0));scheduler.Bind(1,q=>order.Add(1));
        scheduler.Schedule(p,1,5);scheduler.Schedule(p,0,5);scheduler.Schedule(p,1,1);
        Assert(scheduler.Count(0)==1&&scheduler.Count(1)==1,"tick keys distinguish material and preserve first duplicate due time");
        for(int t=0;t<4;t++)scheduler.Advance();Assert(order.Count==0,"shared scheduler preserves delay");
        var random=new JavaRandom(17);random.NextInt(4);var save=scheduler.Capture(random.State,123);
        scheduler.Advance();Assert(order.Count==2&&order[0]==1&&order[1]==0,"same-tick lava/water use insertion order rather than water-first queues");
        var restored=new FluidTickScheduler();restored.Bind(0,q=>order.Add(0));restored.Bind(1,q=>order.Add(1));restored.Restore(save);order.Clear();restored.Advance();
        Assert(order.Count==2&&order[0]==1&&order[1]==0&&restored.Tick==5,"saved due times and cross-material ordering survive restore");
        var random2=new JavaRandom(0);random2.RestoreState(save.randomState);
        for(int t=0;t<100;t++)Assert(random.NextInt(1000)==random2.NextInt(1000),"restored gameplay Java RNG stream");
        scheduler=new FluidTickScheduler();int calls=0;scheduler.Bind(0,q=>{calls++;scheduler.Schedule(q,0,1);});scheduler.Schedule(p,0,1);scheduler.Advance();
        Assert(calls==1&&scheduler.Count(0)==1,"callback requeue cannot run twice in one tick");
        scheduler.Schedule(p.Offset(1,0,0),0,1);scheduler.Advance(1);Assert(calls==2&&scheduler.Count(0)==2,"global work budget defers excess without dropping it");

        var grid=new FireGrid();grid.blocks[p]=BlockType.Lava;grid.blocks[p.Offset(1,1,0)]=BlockType.WoodenPlanks;
        var fire=new BetaFireSimulation(grid,b=>b==3?1:0);fire.IgniteFromLava(p);
        Assert(grid.Type(p.Offset(0,1,0))==BlockType.Fire,"stationary lava ignites air adjacent to wood");
        grid=new FireGrid();grid.SetFire(p,0);fire=new BetaFireSimulation(grid,b=>0);fire.Tick(p);
        Assert(grid.Type(p)==BlockType.Air,"unsupported fire extinguishes");
        grid=new FireGrid();grid.SetFire(p,15);grid.blocks[p.Offset(0,-1,0)]=BlockType.Netherrack;fire=new BetaFireSimulation(grid,b=>0);fire.Tick(p);
        Assert(grid.Type(p)==BlockType.Fire&&grid.schedules>0,"netherrack maintains fire at maximum age");
        grid=new FireGrid();grid.SetFire(p,3);grid.blocks[p.Offset(1,0,0)]=BlockType.Leaves;fire=new BetaFireSimulation(grid,b=>0);fire.Tick(p);
        Assert(grid.Type(p.Offset(1,0,0))==BlockType.Fire,"fire consumes and spreads into flammable neighbors");
        Assert(BetaFireSimulation.Encouragement(BlockType.Log)==5&&BetaFireSimulation.Flammability(BlockType.Log)==5&&BetaFireSimulation.Flammability(BlockType.Leaves)==60&&BetaFireSimulation.Flammability(BlockType.TNT)==100,"Beta fire burn table");
        foreach(BlockType type in Enum.GetValues(typeof(BlockType)))Assert(BetaFireSimulation.Flammability(type)>=0,"all existing block types have a valid fire rule");
    }
}

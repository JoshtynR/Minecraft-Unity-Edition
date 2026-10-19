using System;

public interface IFireWorld
{
    BlockType Type(WaterPosition p);
    int Age(WaterPosition p);
    bool Solid(WaterPosition p);
    void SetFire(WaterPosition p,int age);
    void ScheduleFire(WaterPosition p);
}

// BlockStationary lava ignition and BlockFire's dry-weather rules. Weather and
// TNT explosions belong to their respective world systems, not this scheduler.
public sealed class BetaFireSimulation
{
    private readonly IFireWorld world;
    private readonly Func<int,int> random;
    private static readonly int[] X={1,-1,0,0,0,0},Y={0,0,-1,1,0,0},Z={0,0,0,0,-1,1};
    public BetaFireSimulation(IFireWorld world,Func<int,int> random){this.world=world;this.random=random;}
    public static int Encouragement(BlockType t)
    {
        switch(t)
        {
            case BlockType.WoodenPlanks:case BlockType.Fence:case BlockType.WoodenStairs:
            case BlockType.Log:case BlockType.SpruceLog:case BlockType.BirchLog:return 5;
            case BlockType.TNT:return 15;
            case BlockType.TallGrass:case BlockType.DryTallGrass:case BlockType.Fern:return 60;
            case BlockType.Bookshelf:case BlockType.Leaves:case BlockType.SpruceLeaves:case BlockType.BirchLeaves:case BlockType.WhiteWool:return 30;
            default:return t>=BlockType.OrangeWool&&t<=BlockType.BlackWool?30:0;
        }
    }
    public static int Flammability(BlockType t)
    {
        switch(t)
        {
            case BlockType.Log:case BlockType.SpruceLog:case BlockType.BirchLog:return 5;
            case BlockType.WoodenPlanks:case BlockType.Fence:case BlockType.WoodenStairs:case BlockType.Bookshelf:return 20;
            case BlockType.TNT:case BlockType.TallGrass:case BlockType.DryTallGrass:case BlockType.Fern:return 100;
            default:return Encouragement(t)>0?60:0;
        }
    }
    // Material.getBurning is broader than BlockFire's encouragement table.
    private static bool LavaFuel(BlockType t)=>Encouragement(t)>0||t==BlockType.Chest||t==BlockType.CraftingTable||t==BlockType.NoteBlock||t==BlockType.Jukebox||t==BlockType.WoodenSlab||t==BlockType.WoodenDoor||t==BlockType.StandingSign||t==BlockType.WallSign||t==BlockType.Trapdoor;
    private int NeighborFuel(WaterPosition p)
    {int best=0;for(int d=0;d<6;d++)best=Math.Max(best,Encouragement(world.Type(p.Offset(X[d],Y[d],Z[d]))));return best;}
    public bool Supported(WaterPosition p)=>world.Solid(p.Offset(0,-1,0))||NeighborFuel(p)>0;
    public void IgniteFromLava(WaterPosition p)
    {
        int steps=random(3);
        for(int i=0;i<steps;i++)
        {
            p=p.Offset(random(3)-1,1,random(3)-1);var t=world.Type(p);
            if(t==BlockType.Nothing)return;
            if(t==BlockType.Air)
            {
                for(int d=0;d<6;d++)if(LavaFuel(world.Type(p.Offset(X[d],Y[d],Z[d])))){world.SetFire(p,0);return;}
            }
            else if(world.Solid(p))return;
        }
    }
    public void Tick(WaterPosition p)
    {
        var type=world.Type(p);
        if(type==BlockType.Nothing){world.ScheduleFire(p);return;}
        if(type!=BlockType.Fire)return;
        bool eternal=world.Type(p.Offset(0,-1,0))==BlockType.Netherrack;
        if(!Supported(p)){world.SetFire(p,-1);return;}
        int age=world.Age(p);
        if(age<15)world.SetFire(p,age+random(3)/2);
        world.ScheduleFire(p);
        if(!eternal&&NeighborFuel(p)==0)
        {if(!world.Solid(p.Offset(0,-1,0))||age>3)world.SetFire(p,-1);return;}
        if(!eternal&&Encouragement(world.Type(p.Offset(0,-1,0)))==0&&age==15&&random(4)==0)
        {world.SetFire(p,-1);return;}
        for(int d=0;d<6;d++)
        {
            var q=p.Offset(X[d],Y[d],Z[d]);
            if(random(d==2||d==3?250:300)>=Flammability(world.Type(q)))continue;
            world.SetFire(q,random(age+10)<5?Math.Min(15,age+random(5)/4):-1);
        }
        for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)for(int y=-1;y<=4;y++)
        {
            if(x==0&&y==0&&z==0)continue;
            var q=p.Offset(x,y,z);if(world.Type(q)!=BlockType.Air)continue;
            int fuel=NeighborFuel(q);if(fuel<=0)continue;
            int chance=(fuel+40)/(age+30),bound=100+Math.Max(0,y-1)*100;
            if(chance>0&&random(bound)<=chance)world.SetFire(q,Math.Min(15,age+random(5)/4));
        }
    }
}

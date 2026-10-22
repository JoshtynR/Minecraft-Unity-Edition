// Brains are stateless and shared; all per-entity values belong to MobState or
// the world's transient navigation/target caches. No GameObject owns simulation.
public interface IMobBrain { bool Tick(World world,MobState mob); }

public abstract class FarmAnimalBrain : IMobBrain
{
    public virtual bool Tick(World world,MobState mob){world.TickFarmAnimal(mob);return mob.health>0;}
}
public sealed class CowBrain : FarmAnimalBrain { }
public sealed class PigBrain : FarmAnimalBrain { }
public sealed class SheepBrain : FarmAnimalBrain
{public override bool Tick(World world,MobState mob){world.TickSheepFleece(mob);return base.Tick(world,mob);}}
public sealed class ChickenBrain : FarmAnimalBrain
{public override bool Tick(World world,MobState mob){world.TickChickenEgg(mob);return base.Tick(world,mob);}}

public sealed class ZombieBrain : IMobBrain
{
    public bool Tick(World world,MobState mob)
    {
        world.TickSunlight(mob);
        var target=world.FindMobTarget(mob,false);
        if(target!=null){world.PursueMobTarget(mob,target,1.1f);world.TryMobMelee(mob,target,5);}
        return true;
    }
}
public sealed class SkeletonBrain : IMobBrain
{
    public bool Tick(World world,MobState mob)
    {
        world.TickSunlight(mob);
        var target=world.FindMobTarget(mob,false);
        if(target!=null)
        {
            world.PursueMobTarget(mob,target,6);
            if(world.MobTargetVisible(mob,target))world.TryShootMobArrow(mob,target);
        }
        return true;
    }
}
public sealed class CreeperBrain : IMobBrain
{
    public bool Tick(World world,MobState mob)
    {
        var target=world.FindMobTarget(mob,false);
        if(target!=null)world.PursueMobTarget(mob,target,2.5f);
        return world.TickCreeperFuse(mob,target);
    }
}
public sealed class SpiderBrain : IMobBrain
{
    public bool Tick(World world,MobState mob)
    {
        var target=world.FindMobTarget(mob,true);
        if(target!=null){world.PursueMobTarget(mob,target,1.1f);world.TryMobMelee(mob,target,2);}
        return true;
    }
}
public static class MobBrains
{
    private static readonly IMobBrain[] brains={new CowBrain(),new PigBrain(),new SheepBrain(),new ChickenBrain(),new ZombieBrain(),new SkeletonBrain(),new CreeperBrain(),new SpiderBrain()};
    public static IMobBrain Get(MobType type)=>brains[(int)MobDefinitions.Get(type).Type];
}

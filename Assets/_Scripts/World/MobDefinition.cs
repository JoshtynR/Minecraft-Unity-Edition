using System;

public enum MobBehaviorKind { FarmAnimal, Zombie, Skeleton, Creeper, Spider }

[Serializable]
public sealed class MobTuning
{
    public MobType species;
    public int maxHealth,spawnWeight,maxGroup;
    public float walkSpeed;
    public static MobTuning From(MobDefinition d)=>new MobTuning{species=d.Type,maxHealth=d.MaxHealth,spawnWeight=d.SpawnWeight,maxGroup=d.MaxGroup,walkSpeed=d.WalkSpeed};
}

// Immutable species data. Entity state contains only values that change during play.
public sealed class MobDefinition
{
    public readonly MobType Type;
    public readonly int MaxHealth;
    public readonly int SpawnWeight,MinGroup,MaxGroup;
    public readonly float Radius, Height, WalkSpeed;
    public readonly MobBehaviorKind Behavior;
    public readonly string MaterialResource, SoundFamily, AmbientSound,HurtSound,DeathSound,StepSound;
    public bool Hostile => Behavior != MobBehaviorKind.FarmAnimal;
    internal MobDefinition(MobType type,int health,float radius,float height,float speed,MobBehaviorKind behavior,
        int spawnWeight=10,int minGroup=4,int maxGroup=4)
    {
        Type=type;MaxHealth=health;Radius=radius;Height=height;WalkSpeed=speed;Behavior=behavior;
        SpawnWeight=spawnWeight;MinGroup=minGroup;MaxGroup=maxGroup;
        MaterialResource=type+"Material";SoundFamily="mob/"+type.ToString().ToLowerInvariant();
        AmbientSound=type==MobType.Creeper?null:SoundFamily+"/say";
        HurtSound=(type==MobType.Pig||type==MobType.Sheep||type==MobType.Spider||type==MobType.Creeper)?SoundFamily+"/say":SoundFamily+"/hurt";
        DeathSound=(type==MobType.Cow||type==MobType.Sheep||type==MobType.Chicken)?HurtSound:SoundFamily+"/death";
        StepSound=type==MobType.Creeper?null:SoundFamily+"/step";
    }
}

public static class MobDefinitions
{
    private static MobDefinition[] configured;
    public static void Configure(System.Collections.Generic.IEnumerable<MobTuning> entries)
    {
        configured=null;if(entries==null)return;
        var result=(MobDefinition[])definitions.Clone();var seen=new System.Collections.Generic.HashSet<MobType>();
        foreach(var entry in entries)
        {
            if(entry==null||!Valid(entry.species)||!seen.Add(entry.species))continue;
            var d=definitions[(int)entry.species];
            float speed=float.IsNaN(entry.walkSpeed)||float.IsInfinity(entry.walkSpeed)?d.WalkSpeed:Math.Max(0,Math.Min(1,entry.walkSpeed));
            int max=Math.Max(1,Math.Min(12,entry.maxGroup));
            result[(int)entry.species]=new MobDefinition(d.Type,Math.Max(1,Math.Min(1024,entry.maxHealth)),d.Radius,d.Height,speed,d.Behavior,
                Math.Max(0,Math.Min(1000,entry.spawnWeight)),Math.Min(d.MinGroup,max),max);
        }
        configured=result;
    }
    private static readonly MobType[] hostileSpawnOrder={MobType.Spider,MobType.Zombie,MobType.Skeleton,MobType.Creeper};
    private static readonly MobType[] passiveSpawnOrder={MobType.Sheep,MobType.Pig,MobType.Chicken,MobType.Cow};
    private static readonly MobDefinition[] definitions={
        // Beta creature-list weights: sheep 12, pigs/chickens 10, cows 8.
        new MobDefinition(MobType.Cow,10,.45f,1.3f,.065f,MobBehaviorKind.FarmAnimal,8),
        new MobDefinition(MobType.Pig,10,.45f,.9f,.07f,MobBehaviorKind.FarmAnimal,10),
        new MobDefinition(MobType.Sheep,8,.45f,1.3f,.07f,MobBehaviorKind.FarmAnimal,12),
        new MobDefinition(MobType.Chicken,4,.15f,.7f,.07f,MobBehaviorKind.FarmAnimal,10),
        new MobDefinition(MobType.Zombie,20,.3f,1.8f,.1f,MobBehaviorKind.Zombie),
        new MobDefinition(MobType.Skeleton,20,.3f,1.8f,.1f,MobBehaviorKind.Skeleton),
        new MobDefinition(MobType.Creeper,20,.3f,1.7f,.1f,MobBehaviorKind.Creeper),
        new MobDefinition(MobType.Spider,16,.7f,.9f,.16f,MobBehaviorKind.Spider)
    };
    public static bool Valid(MobType type)=>(int)type>=0&&(int)type<definitions.Length;
    public static MobDefinition Default(MobType type)
    {if(!Valid(type))throw new ArgumentOutOfRangeException(nameof(type));return definitions[(int)type];}
    public static MobDefinition Get(MobType type)
    {if(!Valid(type))throw new ArgumentOutOfRangeException(nameof(type));return (configured??definitions)[(int)type];}
    public static MobType SelectSpawn(bool hostile,int roll)
    {
        // BiomeGenBase list order matters to the weighted roll even though the
        // same species occur in every ordinary Beta overworld biome.
        var order=hostile?hostileSpawnOrder:passiveSpawnOrder;
        int total=0;for(int i=0;i<order.Length;i++)total+=Get(order[i]).SpawnWeight;
        if(total==0)return order[0]; // Spawn validation rejects its zero weight.
        roll=((roll%total)+total)%total;
        for(int i=0;i<order.Length;i++)if((roll-=Get(order[i]).SpawnWeight)<0)return order[i];
        return order[0];
    }
    public static int TotalSpawnWeight(bool hostile)
    {int total=0;foreach(var type in hostile?hostileSpawnOrder:passiveSpawnOrder)total+=Get(type).SpawnWeight;return total;}
}

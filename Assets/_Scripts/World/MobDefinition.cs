using System;

public enum MobBehaviorKind { FarmAnimal, Zombie, Skeleton, Creeper, Spider }

// Immutable species data. Entity state contains only values that change during play.
public sealed class MobDefinition
{
    public readonly MobType Type;
    public readonly int MaxHealth;
    public readonly float Radius, Height, WalkSpeed;
    public readonly MobBehaviorKind Behavior;
    public readonly string MaterialResource, SoundFamily, AmbientSound,HurtSound,DeathSound,StepSound;
    public bool Hostile => Behavior != MobBehaviorKind.FarmAnimal;
    internal MobDefinition(MobType type,int health,float radius,float height,float speed,MobBehaviorKind behavior)
    {
        Type=type;MaxHealth=health;Radius=radius;Height=height;WalkSpeed=speed;Behavior=behavior;
        MaterialResource=type+"Material";SoundFamily="mob/"+type.ToString().ToLowerInvariant();
        AmbientSound=type==MobType.Creeper?null:SoundFamily+"/say";
        HurtSound=(type==MobType.Pig||type==MobType.Sheep||type==MobType.Spider||type==MobType.Creeper)?SoundFamily+"/say":SoundFamily+"/hurt";
        DeathSound=(type==MobType.Cow||type==MobType.Sheep||type==MobType.Chicken)?HurtSound:SoundFamily+"/death";
        StepSound=type==MobType.Creeper?null:SoundFamily+"/step";
    }
}

public static class MobDefinitions
{
    private static readonly MobDefinition[] definitions={
        new MobDefinition(MobType.Cow,10,.45f,1.3f,.065f,MobBehaviorKind.FarmAnimal),
        new MobDefinition(MobType.Pig,10,.45f,.9f,.07f,MobBehaviorKind.FarmAnimal),
        new MobDefinition(MobType.Sheep,8,.45f,1.3f,.07f,MobBehaviorKind.FarmAnimal),
        new MobDefinition(MobType.Chicken,4,.15f,.7f,.07f,MobBehaviorKind.FarmAnimal),
        new MobDefinition(MobType.Zombie,20,.3f,1.8f,.1f,MobBehaviorKind.Zombie),
        new MobDefinition(MobType.Skeleton,20,.3f,1.8f,.1f,MobBehaviorKind.Skeleton),
        new MobDefinition(MobType.Creeper,20,.3f,1.7f,.1f,MobBehaviorKind.Creeper),
        new MobDefinition(MobType.Spider,16,.7f,.9f,.16f,MobBehaviorKind.Spider)
    };
    public static bool Valid(MobType type)=>(int)type>=0&&(int)type<definitions.Length;
    public static MobDefinition Get(MobType type)
    {if(!Valid(type))throw new ArgumentOutOfRangeException(nameof(type));return definitions[(int)type];}
}

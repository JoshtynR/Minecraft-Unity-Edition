using System;
using System.Collections.Generic;
using UnityEngine;

// Append only: these values are stored in existing animal saves and messages.
public enum MobType { Cow, Pig, Sheep, Chicken, Zombie, Skeleton, Creeper, Spider }

[Serializable]
public sealed class MobState
{
    // Zero remains Cow so old cow-only saves load as adult cows.
    public MobType species;
    public int ageTicks, loveTicks, breedCooldown, eggTicks=6000, fleeceColor;
    public int attackTicks, fuseTicks, angerTicks;
    public int targetMemoryTicks,courtshipTicks,eatTicks;
    public int lastDamage;
    public float headYaw,headPitch;
    public bool sheared;
    public int id, health = 10, hurtTicks, hurtCooldown, fleeTicks, decisionTicks, air = 300, fireTicks;
    public int despawnTicks;
    public Vector3 position, motion;
    public float yaw, targetYaw, fallDistance;
    public bool walking, grounded;
    public MobState Copy() => (MobState)MemberwiseClone();
}

[Serializable]
public sealed class MobSaveData
{
    public MobState[] entities;
    // Legacy payload used by the original cow system.
    public MobState[] animals;
    public Vector3Int[] populatedChunks;
    public int nextId = 1;
    public long randomState;
    public bool hasRandomState;
}

public static class MobRules
{
    private static readonly int[] fleeceColors={0xE6E6E6,0xD97F33,0xB24CD8,0x6699D8,0xE5E533,0x7FCC19,0xF27FA5,0x4C4C4C,0x999999,0x4C7F99,0x7F3FB2,0x334CB2,0x664C33,0x667F33,0x993333,0x191919};
    public static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
    public static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
    public static bool Valid(MobState mob) => mob != null && mob.id > 0 && mob.id < int.MaxValue &&
        (int)mob.species>=0 && (int)mob.species<=7 && mob.ageTicks>=0 && mob.ageTicks<=24000 && mob.loveTicks>=0 && mob.loveTicks<=600 && mob.breedCooldown>=0 && mob.breedCooldown<=6000 &&
        mob.fleeceColor>=0&&mob.fleeceColor<16&&mob.health > 0 && mob.health <= MaxHealth(mob.species) && mob.fuseTicks>=0&&mob.fuseTicks<=30&&Finite(mob.position) && Finite(mob.motion) &&
        Finite(mob.yaw) && Finite(mob.targetYaw) && Finite(mob.fallDistance)&&Finite(mob.headYaw)&&Finite(mob.headPitch)&&mob.eatTicks>=0&&mob.eatTicks<=40&&mob.courtshipTicks>=0&&mob.courtshipTicks<=60&&mob.despawnTicks>=0;
    public static bool Hostile(MobType species)=>MobDefinitions.Get(species).Hostile;
    public static int MaxHealth(MobType species)=>MobDefinitions.Get(species).MaxHealth;
    public static float Width(MobState mob)=>MobDefinitions.Get(mob.species).Radius*(mob.ageTicks>0?.5f:1);
    public static float BodyHeight(MobState mob)=>MobDefinitions.Get(mob.species).Height*(mob.ageTicks>0?.5f:1);
    public static Bounds Bounds(MobState mob) => new Bounds(mob.position + Vector3.up * (BodyHeight(mob) * .5f), new Vector3(Width(mob) * 2, BodyHeight(mob), Width(mob) * 2));
    public static int LeatherCount(Func<int,int> next) => next(3); // Beta: 0-2 leather, before beef existed.
    public static ItemType BreedingFood(MobState mob)=>Hostile(mob.species)?ItemType.Nothing:mob.species==MobType.Chicken?ItemType.Seeds:ItemType.Wheat;
    public static int DyeColor(ItemType item)
    {return BetaItemCatalog.TryGet(item,out var dye)&&dye.SourceId==351?15-dye.Metadata:-1;}
    public static ItemType Wool(MobState mob)=>mob.fleeceColor==0?ItemType.WhiteWool:(ItemType)((int)ItemType.OrangeWool+mob.fleeceColor-1);
    public static Color FleeceTint(int color)
    {
        int value=fleeceColors[System.Math.Max(0,System.Math.Min(15,color))];return new Color(((value>>16)&255)/255f,((value>>8)&255)/255f,(value&255)/255f,1);
    }
    public static bool Feed(MobState mob)
    {
        if(mob==null||Hostile(mob.species)||mob.health<=0||mob.ageTicks>0||mob.breedCooldown>0||mob.loveTicks>0)return false;
        mob.loveTicks=600;return true;
    }
    public static bool CanBreed(MobState a,MobState b)=>a!=null&&b!=null&&!Hostile(a.species)&&a.id!=b.id&&a.species==b.species&&a.ageTicks==0&&b.ageTicks==0&&a.loveTicks>0&&b.loveTicks>0&&a.breedCooldown==0&&b.breedCooldown==0;
    public static int AttackDamage(ItemType item)
    {
        switch (item)
        {
            case ItemType.WoodenSword: case ItemType.GoldenSword: return 4;
            case ItemType.StoneSword: return 6;
            case ItemType.IronSword: return 8;
            case ItemType.DiamondSword: return 10;
            case ItemType.WoodenAxe: case ItemType.GoldenAxe: return 3;
            case ItemType.StoneAxe: return 4;
            case ItemType.IronAxe: return 5;
            case ItemType.DiamondAxe: return 6;
            case ItemType.WoodenPickaxe: case ItemType.GoldenPickaxe: return 2;
            case ItemType.StonePickaxe: return 3;
            case ItemType.IronPickaxe: return 4;
            case ItemType.DiamondPickaxe: return 5;
            case ItemType.StoneShovel: return 2;
            case ItemType.IronShovel: return 3;
            case ItemType.DiamondShovel: return 4;
            default: return 1;
        }
    }
    public static bool Hurt(MobState mob, int damage, Vector3 away)
    {
        if (mob == null || mob.health <= 0 || damage <= 0) return false;
        if(mob.hurtCooldown>10)
        {
            if(damage<=mob.lastDamage)return false;
            mob.health=Math.Max(0,mob.health-(damage-mob.lastDamage));mob.lastDamage=damage;return true;
        }
        mob.health = Math.Max(0, mob.health - damage);
        mob.lastDamage=damage;mob.hurtTicks=10;mob.hurtCooldown=20; mob.fleeTicks = Hostile(mob.species)?0:100; mob.angerTicks=600;mob.loveTicks=mob.courtshipTicks=0;mob.walking = true;
        away.y = 0;
        if (away.sqrMagnitude > .00001f) away = away.normalized;
        mob.motion += away * .25f + Vector3.up * .3f;
        mob.targetYaw = Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg;
        return true;
    }
    public static bool Milk(InventoryStack[] inventory, int slot)
    {
        if (inventory == null || slot < 0 || slot >= inventory.Length || inventory[slot].type != ItemType.Bucket || inventory[slot].Empty) return false;
        if (inventory[slot].count == 1) { inventory[slot] = new InventoryStack(ItemType.MilkBucket, 1); return true; }
        // Preserve custom stackable-bucket settings and refuse when there is no room.
        if (InventoryRules.Capacity(inventory, ItemType.MilkBucket, 0, inventory.Length) < 1) return false;
        inventory[slot].count--;
        InventoryRules.Insert(inventory, ItemType.MilkBucket, 1, 0, inventory.Length);
        return true;
    }
}

public sealed class MobStore
{
    public const int Capacity = 4096;
    private readonly Dictionary<int,MobState> animals = new Dictionary<int,MobState>();
    private readonly EntitySpatialIndex<MobState> spatialIndex = new EntitySpatialIndex<MobState>();
    public int LastSpatialCandidates => spatialIndex.LastCandidates;
    private void RemoveSpatial(int id) => spatialIndex.Remove(id);
    public void UpdatePosition(MobState entity)
    {
        if(entity == null || !ReferenceEquals(Get(entity.id), entity)) return;
        spatialIndex.Update(entity.id, entity.position);
    }
    // Broad-phase only: callers retain their exact collision and pickup tests.
    public void QueryNearby(Vector3 position, float radius, List<MobState> output)
    {
        spatialIndex.Query(position, radius, animals, output);
    }
    public int NextId { get; private set; } = 1;
    public int Count => animals.Count;
    public IEnumerable<MobState> Entities => animals.Values;
    public MobState Get(int id) { animals.TryGetValue(id, out var mob); return mob; }
    public MobState Spawn(Vector3 position, float yaw)
    {return Spawn(position,yaw,MobType.Cow);}
    public MobState Spawn(Vector3 position,float yaw,MobType species)
    {
        if ((int)species<0||(int)species>7||!MobRules.Finite(position) || !MobRules.Finite(yaw) || Count >= Capacity || NextId >= int.MaxValue) return null;
        var mob = new MobState { id = NextId++, position = position, yaw = yaw, targetYaw = yaw,species=species,health=MobDefinitions.Get(species).MaxHealth };
        animals.Add(mob.id, mob);UpdatePosition(mob);return mob;
    }
    public bool Remove(int id) {RemoveSpatial(id);return animals.Remove(id);}
    public void Apply(MobState mob)
    {
        if (!MobRules.Valid(mob) || (!animals.ContainsKey(mob.id) && Count >= Capacity)) return;
        RemoveSpatial(mob.id);animals[mob.id] = mob.Copy();UpdatePosition(animals[mob.id]);NextId = Math.Max(NextId, mob.id + 1);
    }
    public MobState[] Capture()
    {
        var result = new MobState[Count]; int i = 0;
        foreach (var mob in animals.Values) result[i++] = mob.Copy();
        return result;
    }
    public void Restore(MobSaveData data)
    {
        animals.Clear();spatialIndex.Clear();NextId = 1;
        if (data == null) return;
        var saved=data.entities??data.animals;
        if (saved != null)
            foreach (var mob in saved)
                if (MobRules.Valid(mob) && !animals.ContainsKey(mob.id)) Apply(mob);
        NextId = Math.Max(NextId, Math.Max(1, data.nextId));
    }
}

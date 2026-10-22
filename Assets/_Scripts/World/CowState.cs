using System;
using System.Collections.Generic;
using UnityEngine;

// Append only: these values are stored in existing animal saves and messages.
public enum PassiveAnimal { Cow, Pig, Sheep, Chicken, Zombie, Skeleton, Creeper, Spider }

[Serializable]
public sealed class CowState
{
    // Missing fields in old cow saves default to an adult cow.
    public PassiveAnimal species;
    public int ageTicks, loveTicks, breedCooldown, eggTicks=6000, fleeceColor;
    public int attackTicks, fuseTicks, angerTicks;
    public bool sheared;
    public int id, health = 10, hurtTicks, hurtCooldown, fleeTicks, decisionTicks, air = 300, fireTicks;
    public Vector3 position, motion;
    public float yaw, targetYaw, fallDistance;
    public bool walking, grounded;
    public CowState Copy() => (CowState)MemberwiseClone();
}

[Serializable]
public sealed class CowSaveData
{
    public CowState[] animals;
    public Vector3Int[] populatedChunks;
    public int nextId = 1;
    public long randomState;
    public bool hasRandomState;
}

public static class CowRules
{
    private static readonly int[] fleeceColors={0xE6E6E6,0xD97F33,0xB24CD8,0x6699D8,0xE5E533,0x7FCC19,0xF27FA5,0x4C4C4C,0x999999,0x4C7F99,0x7F3FB2,0x334CB2,0x664C33,0x667F33,0x993333,0x191919};
    public const float HalfWidth = .45f, Height = 1.3f;
    public const int MaximumHealth = 10;
    public static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
    public static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
    public static bool Valid(CowState cow) => cow != null && cow.id > 0 && cow.id < int.MaxValue &&
        (int)cow.species>=0 && (int)cow.species<=7 && cow.ageTicks>=0 && cow.ageTicks<=24000 && cow.loveTicks>=0 && cow.loveTicks<=600 && cow.breedCooldown>=0 && cow.breedCooldown<=6000 &&
        cow.fleeceColor>=0&&cow.fleeceColor<16&&cow.health > 0 && cow.health <= MaxHealth(cow.species) && cow.fuseTicks>=0&&cow.fuseTicks<=30&&Finite(cow.position) && Finite(cow.motion) &&
        Finite(cow.yaw) && Finite(cow.targetYaw) && Finite(cow.fallDistance);
    public static bool Hostile(PassiveAnimal species)=>(int)species>=4;
    public static int MaxHealth(PassiveAnimal species)=>species==PassiveAnimal.Spider?16:Hostile(species)?20:species==PassiveAnimal.Chicken?4:species==PassiveAnimal.Sheep?8:10;
    public static float Width(CowState cow)=>(cow.species==PassiveAnimal.Spider?.7f:Hostile(cow.species)?.3f:cow.species==PassiveAnimal.Chicken?.15f:.45f)*(cow.ageTicks>0?.5f:1);
    public static float BodyHeight(CowState cow)=>(cow.species==PassiveAnimal.Spider?.9f:Hostile(cow.species)?1.8f:cow.species==PassiveAnimal.Chicken?.7f:cow.species==PassiveAnimal.Pig?.9f:1.3f)*(cow.ageTicks>0?.5f:1);
    public static Bounds Bounds(CowState cow) => new Bounds(cow.position + Vector3.up * (BodyHeight(cow) * .5f), new Vector3(Width(cow) * 2, BodyHeight(cow), Width(cow) * 2));
    public static int LeatherCount(Func<int,int> next) => next(3); // Beta: 0-2 leather, before beef existed.
    public static ItemType BreedingFood(CowState cow)=>Hostile(cow.species)?ItemType.Nothing:cow.species==PassiveAnimal.Chicken?ItemType.Seeds:ItemType.Wheat;
    public static int DyeColor(ItemType item)
    {return BetaItemCatalog.TryGet(item,out var dye)&&dye.SourceId==351?15-dye.Metadata:-1;}
    public static ItemType Wool(CowState cow)=>cow.fleeceColor==0?ItemType.WhiteWool:(ItemType)((int)ItemType.OrangeWool+cow.fleeceColor-1);
    public static Color FleeceTint(int color)
    {
        int value=fleeceColors[System.Math.Max(0,System.Math.Min(15,color))];return new Color(((value>>16)&255)/255f,((value>>8)&255)/255f,(value&255)/255f,1);
    }
    public static bool Feed(CowState cow)
    {
        if(cow==null||Hostile(cow.species)||cow.health<=0||cow.ageTicks>0||cow.breedCooldown>0||cow.loveTicks>0)return false;
        cow.loveTicks=600;return true;
    }
    public static bool CanBreed(CowState a,CowState b)=>a!=null&&b!=null&&!Hostile(a.species)&&a.id!=b.id&&a.species==b.species&&a.ageTicks==0&&b.ageTicks==0&&a.loveTicks>0&&b.loveTicks>0&&a.breedCooldown==0&&b.breedCooldown==0;
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
    public static bool Hurt(CowState cow, int damage, Vector3 away)
    {
        if (cow == null || cow.health <= 0 || damage <= 0 || cow.hurtCooldown > 0) return false;
        cow.health = Math.Max(0, cow.health - damage);
        cow.hurtTicks = cow.hurtCooldown = 10; cow.fleeTicks = Hostile(cow.species)?0:100; cow.angerTicks=600;cow.walking = true;
        away.y = 0;
        if (away.sqrMagnitude > .00001f) away = away.normalized;
        cow.motion += away * .25f + Vector3.up * .3f;
        cow.targetYaw = Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg;
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

public sealed class CowStore
{
    public const int Capacity = 4096;
    private readonly Dictionary<int,CowState> animals = new Dictionary<int,CowState>();
    public int NextId { get; private set; } = 1;
    public int Count => animals.Count;
    public IEnumerable<CowState> Animals => animals.Values;
    public CowState Get(int id) { animals.TryGetValue(id, out var cow); return cow; }
    public CowState Spawn(Vector3 position, float yaw)
    {return Spawn(position,yaw,PassiveAnimal.Cow);}
    public CowState Spawn(Vector3 position,float yaw,PassiveAnimal species)
    {
        if ((int)species<0||(int)species>7||!CowRules.Finite(position) || !CowRules.Finite(yaw) || Count >= Capacity || NextId >= int.MaxValue) return null;
        var cow = new CowState { id = NextId++, position = position, yaw = yaw, targetYaw = yaw,species=species,health=MaxHealthForSpawn(species) };
        animals.Add(cow.id, cow); return cow;
    }
    private static int MaxHealthForSpawn(PassiveAnimal species)=>CowRules.MaxHealth(species);
    public bool Remove(int id) => animals.Remove(id);
    public void Apply(CowState cow)
    {
        if (!CowRules.Valid(cow) || (!animals.ContainsKey(cow.id) && Count >= Capacity)) return;
        animals[cow.id] = cow.Copy(); NextId = Math.Max(NextId, cow.id + 1);
    }
    public CowState[] Capture()
    {
        var result = new CowState[Count]; int i = 0;
        foreach (var cow in animals.Values) result[i++] = cow.Copy();
        return result;
    }
    public void Restore(CowSaveData data)
    {
        animals.Clear(); NextId = 1;
        if (data == null) return;
        if (data.animals != null)
            foreach (var cow in data.animals)
                if (CowRules.Valid(cow) && !animals.ContainsKey(cow.id)) Apply(cow);
        NextId = Math.Max(NextId, Math.Max(1, data.nextId));
    }
}

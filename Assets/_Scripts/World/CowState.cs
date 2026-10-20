using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CowState
{
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
    public const float HalfWidth = .45f, Height = 1.3f;
    public const int MaximumHealth = 10;
    public static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
    public static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
    public static bool Valid(CowState cow) => cow != null && cow.id > 0 && cow.id < int.MaxValue &&
        cow.health > 0 && cow.health <= MaximumHealth && Finite(cow.position) && Finite(cow.motion) &&
        Finite(cow.yaw) && Finite(cow.targetYaw) && Finite(cow.fallDistance);
    public static Bounds Bounds(CowState cow) => new Bounds(cow.position + Vector3.up * (Height * .5f), new Vector3(HalfWidth * 2, Height, HalfWidth * 2));
    public static int LeatherCount(Func<int,int> next) => next(3); // Beta: 0-2 leather, before beef existed.
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
        cow.hurtTicks = cow.hurtCooldown = 10; cow.fleeTicks = 100; cow.walking = true;
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
    {
        if (!CowRules.Finite(position) || !CowRules.Finite(yaw) || Count >= Capacity || NextId >= int.MaxValue) return null;
        var cow = new CowState { id = NextId++, position = position, yaw = yaw, targetYaw = yaw };
        animals.Add(cow.id, cow); return cow;
    }
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

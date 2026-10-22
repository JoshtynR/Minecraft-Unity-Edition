using System;

// Beta 1.7.3 FurnaceRecipes and TileEntityFurnace rules; one tick is 1/20 second.
public static class BetaSmelting
{
    public const int CookTicks = 200;
    public static ItemType Result(ItemType input)
    {
        switch (input)
        {
            case ItemType.IronOre: return ItemType.IronIngot;
            case ItemType.GoldOre: return ItemType.GoldIngot;
            case ItemType.DiamondOre: return ItemType.Diamond;
            case ItemType.Sand: return ItemType.Glass;
            case ItemType.RawPorkchop: return ItemType.CookedPorkchop;
            case ItemType.RawBeef: return ItemType.CookedBeef;
            case ItemType.RawChicken: return ItemType.CookedChicken;
            case ItemType.RawFish: return ItemType.CookedFish;
            case ItemType.Cobblestone: return ItemType.Stone;
            case ItemType.ClayBall: return ItemType.Brick;
            case ItemType.Cactus: return ItemType.CactusGreen;
            case ItemType.Log: case ItemType.SpruceLog: case ItemType.BirchLog: return ItemType.Charcoal;
            default: return ItemType.Nothing;
        }
    }
    public static int FuelTicks(ItemType fuel)
    {
        switch (fuel)
        {
            case ItemType.Coal: case ItemType.Charcoal: return 1600;
            case ItemType.LavaBucket: return 20000;
            case ItemType.Stick: case ItemType.OakSapling: case ItemType.SpruceSapling: case ItemType.BirchSapling: return 100;
            case ItemType.WoodenPlanks: case ItemType.Log: case ItemType.SpruceLog: case ItemType.BirchLog:
            case ItemType.Bookshelf: case ItemType.Chest: case ItemType.CraftingTable:
            case ItemType.WoodenStairs: case ItemType.Fence: case ItemType.Jukebox:
            case ItemType.NoteBlock: case ItemType.Trapdoor: case ItemType.WoodenPressurePlate:
            case ItemType.WallSign: return 300;
            // Beta does not burn wooden tools, boats, wooden slabs or the sign/door items.
            default: return 0;
        }
    }
    [Serializable] public sealed class State
    {
        public InventoryStack[] items = new InventoryStack[3];
        public int burnTicks, fuelTicks, cookTicks;
        public bool Burning => burnTicks > 0;
        public State Copy() => new State { items = (InventoryStack[])items.Clone(), burnTicks = burnTicks, fuelTicks = fuelTicks, cookTicks = cookTicks };
    }
    public static bool CanSmelt(State state)
    {
        var input = state.items[0]; if (input.Empty) return false;
        var result = Result(input.type); if (result == ItemType.Nothing) return false;
        var output = state.items[2];
        return output.Empty || (output.type == result && output.damage == 0 && output.count < InventoryRules.Limit(result));
    }
    public static void Tick(State state)
    {
        if (state.burnTicks > 0) state.burnTicks--;
        if (state.burnTicks == 0 && CanSmelt(state))
        {
            state.fuelTicks = state.burnTicks = state.items[1].Empty ? 0 : FuelTicks(state.items[1].type);
            if (state.burnTicks > 0)
            {
                state.items[1].count--; state.items[1].Normalize();
                // In Beta the lava bucket is consumed, including its container.
            }
        }
        if (state.Burning && CanSmelt(state))
        {
            if (++state.cookTicks == CookTicks)
            {
                var result = Result(state.items[0].type);
                state.items[2] = new InventoryStack(result, (state.items[2].Empty ? 0 : state.items[2].count) + 1);
                state.items[0].count--; state.items[0].Normalize(); state.cookTicks = 0;
            }
        }
        else state.cookTicks = 0;
    }
    public static bool Click(State state, InventoryStack[] inventory, ref InventoryStack cursor, int index, bool right, bool shift)
    {
        if (state == null || state.items == null || state.items.Length != 3 || !ChestTransactions.Valid(inventory, cursor) || index < 0 || index >= 39) return false;
        if (shift)
        {
            if (index < 3)
            {
                var stack = state.items[index];
                stack.count = InventoryRules.Insert(inventory, stack.type, stack.count, 0, 36, stack.damage);
                stack.Normalize(); state.items[index] = stack;
            }
            else
            {
                int slot = index - 3; var stack = inventory[slot];
                int target = Result(stack.type) != ItemType.Nothing ? 0 : FuelTicks(stack.type) > 0 ? 1 : -1;
                if (target < 0) InventoryRules.Shift(inventory, slot);
                else
                {
                    stack.count = InventoryRules.Insert(state.items, stack.type, stack.count, target, target + 1, stack.damage);
                    stack.Normalize(); inventory[slot] = stack;
                }
            }
        }
        else if (index == 2)
        {
            var output = state.items[2];
            if (!output.Empty && (cursor.Empty || cursor.Matches(output)))
            {
                int take = Math.Min(right ? (output.count + 1) / 2 : output.count, InventoryRules.Limit(output.type) - (cursor.Empty ? 0 : cursor.count));
                if (take > 0)
                {
                    cursor = new InventoryStack(output.type, (cursor.Empty ? 0 : cursor.count) + take, output.damage);
                    state.items[2].count -= take; state.items[2].Normalize();
                }
            }
        }
        else if (index < 3) InventoryRules.Click(ref state.items[index], ref cursor, right);
        else InventoryRules.Click(ref inventory[index - 3], ref cursor, right);
        return true;
    }
}

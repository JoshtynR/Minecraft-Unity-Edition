using System;
using System.Collections.Generic;

// Independent of Unity: level 0 is a source, 1..7 is horizontal flow,
// and 8..15 is falling water. Negative values describe the environment.
public interface IWaterWorld
{
    int GetWaterCell(WaterPosition position);
    void SetWaterCell(WaterPosition position, int level);
}

public struct WaterPosition : IEquatable<WaterPosition>
{
    public readonly int x, y, z;
    public WaterPosition(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
    public WaterPosition Offset(int dx, int dy, int dz) { return new WaterPosition(x + dx, y + dy, z + dz); }
    public bool Equals(WaterPosition other) { return x == other.x && y == other.y && z == other.z; }
    public override bool Equals(object other) { return other is WaterPosition && Equals((WaterPosition)other); }
    public override int GetHashCode() { unchecked { return (x * 397 ^ y) * 397 ^ z; } }
}

public sealed class WaterSimulation
{
    public const int Air = -1, Solid = -2, Unloaded = -3;
    public const int TickDelay = 5;
    private static readonly int[] Dx = { -1, 1, 0, 0 };
    private static readonly int[] Dz = { 0, 0, -1, 1 };
    private readonly IWaterWorld world;
    private readonly Queue<ScheduledCell> pending = new Queue<ScheduledCell>();
    private readonly HashSet<WaterPosition> scheduled = new HashSet<WaterPosition>();
    public long CurrentTick { get; private set; }
    public int PendingCount { get { return pending.Count; } }

    private struct ScheduledCell { public WaterPosition position; public long tick; }
    public WaterSimulation(IWaterWorld world) { this.world = world; }

    public void Schedule(WaterPosition position)
    {
        if (world.GetWaterCell(position) >= 0 && scheduled.Add(position))
            pending.Enqueue(new ScheduledCell { position = position, tick = CurrentTick + TickDelay });
    }

    public void NotifyChanged(WaterPosition position)
    {
        Schedule(position);
        Schedule(position.Offset(0, 1, 0));
        Schedule(position.Offset(0, -1, 0));
        for (int d = 0; d < 4; d++) Schedule(position.Offset(Dx[d], 0, Dz[d]));
    }

    // The budget delays excess work without shortening any scheduled delay.
    public void AdvanceTick(int budget = 1000)
    {
        CurrentTick++;
        while (budget-- > 0 && pending.Count > 0 && pending.Peek().tick <= CurrentTick)
        {
            var cell = pending.Dequeue();
            scheduled.Remove(cell.position);
            Update(cell.position);
        }
    }

    private void Change(WaterPosition position, int level)
    {
        if (world.GetWaterCell(position) == level) return;
        world.SetWaterCell(position, level);
        NotifyChanged(position);
    }

    private void Update(WaterPosition p)
    {
        int level = world.GetWaterCell(p);
        if (level < 0) return;
        // A missing chunk is not air. Its load notification will wake this cell.
        if (world.GetWaterCell(p.Offset(0, 1, 0)) == Unloaded ||
            world.GetWaterCell(p.Offset(0, -1, 0)) == Unloaded) return;
        for (int d = 0; d < 4; d++)
            if (world.GetWaterCell(p.Offset(Dx[d], 0, Dz[d])) == Unloaded) return;

        if (level > 0)
        {
            int lowest = 100, sources = 0;
            for (int d = 0; d < 4; d++)
            {
                int adjacent = world.GetWaterCell(p.Offset(Dx[d], 0, Dz[d]));
                if (adjacent < 0) continue;
                if (adjacent == 0) sources++;
                lowest = Math.Min(lowest, adjacent >= 8 ? 0 : adjacent);
            }
            int next = lowest + 1;
            if (next >= 8) next = Air;
            int above = world.GetWaterCell(p.Offset(0, 1, 0));
            if (above >= 0) next = above >= 8 ? above : above + 8;
            int below = world.GetWaterCell(p.Offset(0, -1, 0));
            if (sources >= 2 && (below == Solid || below == 0)) next = 0;
            if (next != level) Change(p, next);
            level = next;
            if (level < 0) return;
        }

        var down = p.Offset(0, -1, 0);
        if (world.GetWaterCell(down) == Air)
        {
            Change(down, level >= 8 ? level : level + 8);
            return;
        }
        // Sources spread even above water. Flowing water spreads at the bottom
        // of a waterfall only when the block below actually blocks its path.
        if (level != 0 && world.GetWaterCell(down) != Solid) return;
        int spreadLevel = level >= 8 ? 1 : level + 1;
        if (spreadLevel >= 8) return;

        int[] costs = new int[4];
        int best = 1000;
        for (int d = 0; d < 4; d++)
        {
            var adjacent = p.Offset(Dx[d], 0, Dz[d]);
            int cell = world.GetWaterCell(adjacent);
            costs[d] = 1000;
            if (cell != Air && cell <= 0) continue;
            costs[d] = world.GetWaterCell(adjacent.Offset(0, -1, 0)) == Air
                ? 0 : DownhillCost(adjacent, 1, d);
            best = Math.Min(best, costs[d]);
        }
        for (int d = 0; d < 4; d++)
        {
            if (costs[d] != best) continue;
            var adjacent = p.Offset(Dx[d], 0, Dz[d]);
            // Water does not overwrite another water cell. Its own update
            // recalculates the level when the new upstream neighbor arrives.
            if (world.GetWaterCell(adjacent) == Air) Change(adjacent, spreadLevel);
        }
    }

    private int DownhillCost(WaterPosition p, int distance, int previousDirection)
    {
        int best = 1000;
        for (int d = 0; d < 4; d++)
        {
            if (d == (previousDirection ^ 1)) continue;
            var next = p.Offset(Dx[d], 0, Dz[d]);
            int cell = world.GetWaterCell(next);
            if (cell != Air && cell <= 0) continue;
            if (world.GetWaterCell(next.Offset(0, -1, 0)) == Air) return distance;
            if (distance < 4) best = Math.Min(best, DownhillCost(next, distance + 1, d));
        }
        return best;
    }
}

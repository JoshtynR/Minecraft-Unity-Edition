using System;
using System.Collections.Generic;

// Independent of Unity: level 0 is a source, 1..7 is horizontal flow,
// and 8..15 is falling fluid. This legacy-named scheduler also runs lava through
// a material-specific world view. Negative values describe the environment.
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
    // Only lava's world view exposes Water. Hardening outputs are not levels.
    public const int Water = -4, Obsidian = -5, Cobblestone = -6;
    public const int TickDelay = 5;
    public const int LavaTickDelay = 30;
    private static readonly int[] Dx = { -1, 1, 0, 0 };
    private static readonly int[] Dz = { 0, 0, -1, 1 };
    private readonly IWaterWorld world;
    private readonly bool lava;
    private readonly int delay, decay;
    private readonly Func<int> nextFour;
    private readonly Queue<ScheduledCell> pending = new Queue<ScheduledCell>();
    private readonly HashSet<WaterPosition> scheduled = new HashSet<WaterPosition>();
    public long CurrentTick { get; private set; }
    public int PendingCount { get { return pending.Count; } }

    private struct ScheduledCell { public WaterPosition position; public long tick; }
    public WaterSimulation(IWaterWorld world, bool lava = false, bool hellWorld = false, Func<int> nextFour = null)
    {
        this.world = world;
        this.lava = lava;
        delay = lava ? LavaTickDelay : TickDelay;
        decay = lava && !hellWorld ? 2 : 1;
        if (lava && nextFour == null)
        {
            var random = new Random(0);
            this.nextFour = () => random.Next(4);
        }
        else this.nextFour = nextFour;
    }

    public void Schedule(WaterPosition position)
    {
        if (world.GetWaterCell(position) >= 0 && scheduled.Add(position))
            pending.Enqueue(new ScheduledCell { position = position, tick = CurrentTick + delay });
    }

    public void NotifyChanged(WaterPosition position)
    {
        if (lava)
        {
            Harden(position);
            Harden(position.Offset(0,1,0));
            Harden(position.Offset(0,-1,0));
            for(int d=0;d<4;d++)Harden(position.Offset(Dx[d],0,Dz[d]));
        }
        ScheduleAround(position);
    }

    private void ScheduleAround(WaterPosition position)
    {
        Schedule(position);
        Schedule(position.Offset(0, 1, 0));
        Schedule(position.Offset(0, -1, 0));
        for (int d = 0; d < 4; d++) Schedule(position.Offset(Dx[d], 0, Dz[d]));
    }

    private bool Harden(WaterPosition p)
    {
        int level=world.GetWaterCell(p);
        if(level<0||level>4)return false;
        bool water=world.GetWaterCell(p.Offset(0,1,0))==Water;
        for(int d=0;d<4&&!water;d++)water=world.GetWaterCell(p.Offset(Dx[d],0,Dz[d]))==Water;
        if(!water)return false;
        world.SetWaterCell(p,level==0?Obsidian:Cobblestone);
        // Do not recurse along large shorelines through Change/NotifyChanged.
        ScheduleAround(p);
        return true;
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
        if (lava && Harden(p)) return;
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
            int next = lowest + decay;
            if (next >= 8) next = Air;
            int above = world.GetWaterCell(p.Offset(0, 1, 0));
            if (above >= 0) next = above >= 8 ? above : above + 8;
            int below = world.GetWaterCell(p.Offset(0, -1, 0));
            if (!lava && sources >= 2 && (below == Solid || below == 0)) next = 0;
            // Beta lava sometimes waits another tick cycle before increasing
            // its horizontal decay; do not create infinite lava sources.
            if(lava && level<8 && next<8 && next>level && nextFour()!=0)
            {
                next=level;
                Schedule(p);
            }
            if (next != level) Change(p, next);
            level = next;
            if (level < 0) return;
        }

        var down = p.Offset(0, -1, 0);
        if (CanDisplace(world.GetWaterCell(down)))
        {
            Change(down, level >= 8 ? level : level + 8);
            return;
        }
        // Sources spread even above water. Flowing water spreads at the bottom
        // of a waterfall only when the block below actually blocks its path.
        if (level != 0 && world.GetWaterCell(down) != Solid) return;
        int spreadLevel = level >= 8 ? 1 : level + decay;
        if (spreadLevel >= 8) return;

        int[] costs = new int[4];
        int best = 1000;
        for (int d = 0; d < 4; d++)
        {
            var adjacent = p.Offset(Dx[d], 0, Dz[d]);
            int cell = world.GetWaterCell(adjacent);
            costs[d] = 1000;
            if (!CanDisplace(cell) && cell <= 0) continue;
            costs[d] = CanDisplace(world.GetWaterCell(adjacent.Offset(0, -1, 0)))
                ? 0 : DownhillCost(adjacent, 1, d);
            best = Math.Min(best, costs[d]);
        }
        for (int d = 0; d < 4; d++)
        {
            if (costs[d] != best) continue;
            var adjacent = p.Offset(Dx[d], 0, Dz[d]);
            // Water does not overwrite another water cell. Its own update
            // recalculates the level when the new upstream neighbor arrives.
            if (CanDisplace(world.GetWaterCell(adjacent))) Change(adjacent, spreadLevel);
        }
    }

    private bool CanDisplace(int cell) { return cell == Air || (lava && cell == Water); }

    private int DownhillCost(WaterPosition p, int distance, int previousDirection)
    {
        int best = 1000;
        for (int d = 0; d < 4; d++)
        {
            if (d == (previousDirection ^ 1)) continue;
            var next = p.Offset(Dx[d], 0, Dz[d]);
            int cell = world.GetWaterCell(next);
            if (!CanDisplace(cell) && cell <= 0) continue;
            if (CanDisplace(world.GetWaterCell(next.Offset(0, -1, 0)))) return distance;
            if (distance < 4) best = Math.Min(best, DownhillCost(next, distance + 1, d));
        }
        return best;
    }
}

using System;
using System.Collections.Generic;

public static partial class WaterCheck
{
    private static int checks;
    public static void Assert(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static WaterPosition P(int x, int y = 1, int z = 0) { return new WaterPosition(x, y, z); }
    private sealed class Grid : IWaterWorld
    {
        public readonly Dictionary<WaterPosition, int> cells = new Dictionary<WaterPosition, int>();
        public int writes;
        public int GetWaterCell(WaterPosition p)
        {
            int value;
            if (cells.TryGetValue(p, out value)) return value;
            return p.y <= 0 || p.y > 12 || Math.Abs(p.x) > 12 || Math.Abs(p.z) > 12 ? WaterSimulation.Solid : WaterSimulation.Air;
        }
        public void SetWaterCell(WaterPosition p, int level) { cells[p] = level; writes++; }
    }
    private static void Ticks(WaterSimulation sim, int count) { for (int i = 0; i < count; i++) sim.AdvanceTick(); }
    public static void Main()
    {
        var g = new Grid(); g.cells[P(0)] = 0;
        var sim = new WaterSimulation(g); sim.Schedule(P(0)); sim.Schedule(P(0));
        Assert(sim.PendingCount == 1, "duplicate water update");
        Ticks(sim, 4); Assert(g.writes == 0, "water moved before five ticks");
        Ticks(sim, 1); Assert(g.GetWaterCell(P(1)) == 1, "first horizontal flow");
        Ticks(sim, 200);
        for (int x = -9; x <= 9; x++) for (int z = -9; z <= 9; z++)
        {
            int distance = Math.Abs(x) + Math.Abs(z);
            Assert(g.GetWaterCell(P(x, 1, z)) == (distance <= 7 ? distance : -1), "horizontal range/level at " + x + "," + z);
        }
        Assert(sim.PendingCount == 0, "settled water keeps ticking");
        g.cells[P(0)] = -1; sim.NotifyChanged(P(0)); Ticks(sim, 300);
        foreach (var cell in g.cells) Assert(cell.Value < 0, "water did not drain after source removal");

        g = new Grid(); g.cells[P(0, 5)] = 0;
        for(int y=2;y<=5;y++) { g.cells[P(-1,y)]=-2;g.cells[P(1,y)]=-2;g.cells[P(0,y,-1)]=-2;g.cells[P(0,y,1)]=-2; }
        sim = new WaterSimulation(g); sim.Schedule(P(0, 5)); Ticks(sim, 200);
        for (int y = 1; y < 5; y++) Assert(g.GetWaterCell(P(0, y)) == 8, "falling column");
        Assert(g.GetWaterCell(P(7)) == 7 && g.GetWaterCell(P(8)) == -1, "waterfall bottom range reset: " + g.GetWaterCell(P(7)) + "," + g.GetWaterCell(P(8)));
        Assert(g.GetWaterCell(P(1, 4)) == -2, "water must fall before spreading sideways");
        g.cells[P(0, 5)] = -1; sim.NotifyChanged(P(0, 5)); Ticks(sim, 400);
        foreach (var cell in g.cells) Assert(cell.Value < 0, "waterfall did not drain");

        g = new Grid(); g.cells[P(-1)] = 0; g.cells[P(1)] = 0; g.cells[P(0)] = 1;
        sim = new WaterSimulation(g); sim.Schedule(P(0)); Ticks(sim, 5);
        Assert(g.GetWaterCell(P(0)) == 0, "two sources did not regenerate over solid floor");
        g = new Grid(); g.cells[P(-1)] = 0; g.cells[P(1)] = 0; g.cells[P(0)] = 1; g.cells[P(0, 0)] = 0;
        sim = new WaterSimulation(g); sim.Schedule(P(0)); Ticks(sim, 5);
        Assert(g.GetWaterCell(P(0)) == 0, "two sources did not regenerate over source water");
        g = new Grid(); g.cells[P(-1)] = 0; g.cells[P(1)] = 0; g.cells[P(0)] = 1; g.cells[P(0, 0)] = -1;
        sim = new WaterSimulation(g); sim.Schedule(P(0)); Ticks(sim, 5);
        Assert(g.GetWaterCell(P(0)) == 1, "unsupported water became a source");

        g = new Grid(); g.cells[P(0)] = 0; g.cells[P(2, 0)] = -1;
        sim = new WaterSimulation(g); sim.Schedule(P(0)); Ticks(sim, 5);
        Assert(g.GetWaterCell(P(1)) == 1 && g.GetWaterCell(P(-1)) == -1 && g.GetWaterCell(P(0, 1, 1)) == -1, "shortest downhill path not preferred");
        Ticks(sim, 10); Assert(g.GetWaterCell(P(2, 0)) >= 8, "water did not reach downhill opening");
        g = new Grid(); g.cells[P(0)] = 0; g.cells[P(2, 0)] = -1; g.cells[P(-2, 0)] = -1;
        sim = new WaterSimulation(g); sim.Schedule(P(0)); Ticks(sim, 5);
        Assert(g.GetWaterCell(P(1)) == 1 && g.GetWaterCell(P(-1)) == 1, "equal downhill paths must both flow");

        g = new Grid(); g.cells[P(0)] = 0; g.cells[P(1)] = WaterSimulation.Unloaded;
        sim = new WaterSimulation(g); sim.Schedule(P(0)); Ticks(sim, 20);
        Assert(g.writes == 0 && g.GetWaterCell(P(1)) == WaterSimulation.Unloaded, "water changed across unavailable chunk");
        g.cells[P(1)] = -1; sim.NotifyChanged(P(1)); Ticks(sim, 5);
        Assert(g.GetWaterCell(P(1)) == 1, "water failed to resume when chunk loaded");
        g = new Grid(); g.cells[P(0)] = 0; g.cells[P(1)] = WaterSimulation.Solid;
        sim = new WaterSimulation(g); sim.Schedule(P(0)); Ticks(sim, 100);
        Assert(g.GetWaterCell(P(1)) == WaterSimulation.Solid, "water overwrote solid block");
        g.cells[P(1)] = -1; sim.NotifyChanged(P(1)); Ticks(sim, 5);
        Assert(g.GetWaterCell(P(1)) == 1, "water ignored removed obstruction");

        Func<WaterPosition,int> flat = p => p.y == 1 ? 0 : -1;
        Assert(Math.Abs(WaterSurface.CornerHeight(flat, P(0)) - 8 / 9f) < 0.00001f, "source surface height");
        Assert(Math.Abs(WaterSurface.CornerHeight(p => p.y == 1 ? 7 : -1, P(0)) - 1 / 9f) < 0.00001f, "flow surface height");
        Assert(WaterSurface.CornerHeight(p => p.y <= 2 ? 0 : -1, P(0)) == 1, "stacked water full height");
        float weighted = WaterSurface.CornerHeight(p => p.y != 1 ? -1 : p.x == 0 && p.z == 0 ? 0 : -1, P(0));
        Assert(Math.Abs(weighted - (1 - (11 / 9f + 3) / 14f)) < 0.00001f, "source weighted surface corner");
        IntegrationCheck.Run();
        CheckLava();
        Console.WriteLine("PASS: " + checks + " fluid checks, including production block, save, and world adapter integration.");
    }
}

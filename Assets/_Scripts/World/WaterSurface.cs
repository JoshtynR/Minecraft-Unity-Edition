using System;

public static class WaterSurface
{
    // BlockFluid.getFlowDirection: falling levels count as sources; an open
    // ledge also samples the fluid one block below. The sentinel selects still.
    public static double FlowAngle(Func<WaterPosition, int> sample, WaterPosition p)
    {
        int current = sample(p);
        if (current < 0) return -1000;
        current = current >= 8 ? 0 : current;
        int x = 0, z = 0;
        for (int d = 0; d < 4; d++)
        {
            int dx = d == 0 ? -1 : d == 2 ? 1 : 0;
            int dz = d == 1 ? -1 : d == 3 ? 1 : 0;
            var adjacent = p.Offset(dx, 0, dz);
            int level = sample(adjacent), difference = 0;
            if (level >= 0) difference = (level >= 8 ? 0 : level) - current;
            else if (level == WaterSimulation.Air)
            {
                level = sample(adjacent.Offset(0, -1, 0));
                if (level >= 0) difference = (level >= 8 ? 0 : level) - (current - 8);
            }
            x += dx * difference; z += dz * difference;
        }
        return x == 0 && z == 0 ? -1000 : Math.Atan2(z, x) - Math.PI / 2;
    }

    // UVs in a complete square frame (the flowing sheet covers two blocks).
    public static void TopUV(double angle, bool east, bool south, out float u, out float v)
    {
        bool flowing = angle > -999;
        double sine = flowing ? Math.Sin(angle) * .25 : 0;
        double cosine = flowing ? Math.Cos(angle) * .25 : .5;
        int x = east ? 1 : -1, z = south ? 1 : -1;
        u = (float)(.5 + x * cosine + z * sine);
        v = (float)(.5 - z * cosine + x * sine);
    }

    // All four water blocks sharing a corner use the same sample, avoiding
    // cracks between different flow levels and between chunks.
    public static float CornerHeight(Func<WaterPosition, int> sample, WaterPosition corner)
    {
        float emptyHeight = 0;
        int weight = 0;
        for (int dx = -1; dx <= 0; dx++)
            for (int dz = -1; dz <= 0; dz++)
            {
                var p = corner.Offset(dx, 0, dz);
                if (sample(p.Offset(0, 1, 0)) >= 0) return 1;
                int level = sample(p);
                if (level >= 0)
                {
                    int effectiveLevel = level >= 8 ? 0 : level;
                    int contribution = level == 0 || level >= 8 ? 11 : 1;
                    emptyHeight += (effectiveLevel + 1) / 9f * contribution;
                    weight += contribution;
                }
                else if (level == WaterSimulation.Air)
                {
                    emptyHeight += 1;
                    weight++;
                }
            }
        return weight == 0 ? 8 / 9f : 1 - emptyHeight / weight;
    }
}

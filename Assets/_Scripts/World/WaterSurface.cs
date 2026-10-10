using System;

public static class WaterSurface
{
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

using System;

public static class HostileSpawnRules
{
    // Beta hostile spawning uses the brightest gameplay light at the mob's feet.
    // Skylight is reduced by the day/night value, then the result must be no
    // brighter than a fresh 0..7 random roll. This makes darkness always valid
    // while light level seven succeeds only one eighth of the time.
    public static int EffectiveLight(int blockLight,int skyLight,int skyLightReduction)
    {
        blockLight=Math.Max(0,Math.Min(15,blockLight));
        skyLight=Math.Max(0,Math.Min(15,skyLight));
        skyLightReduction=Math.Max(0,Math.Min(15,skyLightReduction));
        return Math.Max(blockLight,Math.Max(0,skyLight-skyLightReduction));
    }

    public static bool Allows(int blockLight,int skyLight,int skyLightReduction,int randomLightLimit)
    {
        randomLightLimit=Math.Max(0,Math.Min(7,randomLightLimit));
        return EffectiveLight(blockLight,skyLight,skyLightReduction)<=randomLightLimit;
    }

    public static bool SurfaceEligible(int skyLightReduction)
    {return EffectiveLight(0,15,skyLightReduction)<=7;}
}

using System;

public static class HostileSpawnRules
{
    // EntityMob performs two independent checks: raw saved skylight against a
    // 0..31 roll, then final block light against a fresh 0..7 roll.
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

    public static bool Allows(int blockLight,int skyLight,int skyLightReduction,int skyRoll,int finalLightRoll)
    {
        skyLight=Math.Max(0,Math.Min(15,skyLight));
        skyRoll=Math.Max(0,Math.Min(31,skyRoll));
        return skyLight<=skyRoll&&Allows(blockLight,skyLight,skyLightReduction,finalLightRoll);
    }

    public static int CategoryCap(int betaCap,int eligibleChunkCount)
    {return Math.Max(0,betaCap)*Math.Max(0,eligibleChunkCount)/256;}

    public static bool SurfaceEligible(int skyLightReduction)
    {return EffectiveLight(0,15,skyLightReduction)<=7;}
}

using System;

public static class SurvivalHudValues
{
    public static int ClampPoints(int points) { return Math.Max(0, Math.Min(20, points)); }
    public static int IconFill(int points, int index)
    {
        return Math.Max(0, Math.Min(2, ClampPoints(points) - index * 2));
    }
    public static int ExperiencePixels(float progress)
    {
        if (float.IsNaN(progress) || progress <= 0) return 0;
        if (progress >= 1) return 182;
        return Math.Min(182, (int)(progress * 183));
    }
}

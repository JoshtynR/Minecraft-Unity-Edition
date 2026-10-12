using System;

// Rendering/light transport math, independent of Unity and shader color space.
public static class Beta173Lighting
{
    public static float Brightness(int sky, int block, int skyDarkness = 0, int emission = 0)
    {
        int level = Math.Max(Math.Max(0, sky - skyDarkness), Math.Max(block, emission));
        level = Math.Max(0, Math.Min(15, level));
        float darkness = 1.0f - level / 15.0f;
        return (1.0f - darkness) / (darkness * 3.0f + 1.0f) * 0.95f + 0.05f;
    }

    public static float InterpolateCorners(float negativeAnegativeB, float negativeApositiveB,
        float positiveApositiveB, float positiveAnegativeB, float a, float b)
    {
        a = Math.Max(0f, Math.Min(1f, a));
        b = Math.Max(0f, Math.Min(1f, b));
        float bottom = negativeAnegativeB + (positiveAnegativeB - negativeAnegativeB) * a;
        float top = negativeApositiveB + (positiveApositiveB - negativeApositiveB) * a;
        return bottom + (top - bottom) * b;
    }

    public static int Attenuate(int light, int opacity)
    {
        return Math.Max(0, light - Math.Max(1, opacity));
    }

    public static float CornerBrightness(float center, float sideA, float sideB, float diagonal, bool passA, bool passB)
    {
        // Beta's RenderBlocks reuses the first side sample when both adjacent
        // materials prevent the diagonal sample. Average brightness, not levels.
        if (!passA && !passB) diagonal = sideA;
        return (center + sideA + sideB + diagonal) * 0.25f;
    }
}

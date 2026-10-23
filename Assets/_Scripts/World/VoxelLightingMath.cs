using System;

// Shared integer light transport and mesh interpolation helpers.
public static class VoxelLightingMath
{
    public static float Interpolate(float negativeAnegativeB, float negativeApositiveB,
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
}

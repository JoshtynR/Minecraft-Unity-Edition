using System;

/// <summary>Numeric behavior used by Minecraft Beta 1.7.3 MathHelper.</summary>
public static class BetaMathHelper
{
    private static readonly float[] SinTable = BuildSinTable();

    private static float[] BuildSinTable()
    {
        var table = new float[65536];
        for (int i = 0; i < table.Length; i++)
            table[i] = (float)Math.Sin(i * Math.PI * 2.0 / 65536.0);
        return table;
    }

    public static float Sin(float value) => SinTable[(int)(value * 10430.378f) & 65535];
    public static float Cos(float value) => SinTable[(int)(value * 10430.378f + 16384.0f) & 65535];

    public static int Floor(double value)
    {
        int i = (int)value;
        return value < i ? i - 1 : i;
    }
}

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
        int i = JavaInt(value);
        return value < i ? unchecked(i - 1) : i;
    }

    // Java's narrowing conversion saturates outside the int range and maps
    // NaN to zero. CLR casts need not do that; this matters at the Far Lands.
    public static int JavaInt(double value)
    {
        if (double.IsNaN(value)) return 0;
        if (value >= int.MaxValue) return int.MaxValue;
        if (value <= int.MinValue) return int.MinValue;
        return (int)value;
    }
}

using System;

/// <summary>
/// Java-compatible 48-bit random used by classic Minecraft world generation.
/// Kept local so Beta terrain is deterministic and independent of UnityEngine.Random.
/// </summary>
public sealed class JavaRandom
{
    private const long Multiplier = 0x5DEECE66DL;
    private const long Addend = 0xBL;
    private const long Mask = (1L << 48) - 1;
    private long seed;

    public JavaRandom(long seed) { SetSeed(seed); }

    public void SetSeed(long value)
    {
        seed = (value ^ Multiplier) & Mask;
    }

    private int NextBits(int bits)
    {
        seed = (seed * Multiplier + Addend) & Mask;
        return (int)((ulong)seed >> (48 - bits));
    }

    public int NextInt(int bound)
    {
        if (bound <= 0) throw new ArgumentOutOfRangeException(nameof(bound));
        if ((bound & -bound) == bound)
            return (int)((bound * (long)NextBits(31)) >> 31);

        int bits, value;
        do
        {
            bits = NextBits(31);
            value = bits % bound;
        } while (bits - value + (bound - 1) < 0);
        return value;
    }

    public double NextDouble()
    {
        long high = (long)NextBits(26) << 27;
        long low = NextBits(27);
        return (high + low) / (double)(1L << 53);
    }
}

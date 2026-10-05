using System;
using System.Globalization;

/// <summary>
/// Classic Minecraft seed parsing: numeric text is used directly as a signed
/// 64-bit seed; non-numeric text falls back to Java String.hashCode().
/// </summary>
public static class MinecraftSeed
{
    public static long Parse(string text)
    {
        if (string.IsNullOrEmpty(text))
            return DateTime.UtcNow.Ticks;

        // Long.parseLong accepts a sign and decimal digits, not surrounding
        // whitespace or culture-specific separators.
        if (long.TryParse(text, NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out long numericSeed))
            return numericSeed;

        int hash = 0;
        unchecked
        {
            for (int i = 0; i < text.Length; i++)
                hash = 31 * hash + text[i];
        }

        return hash;
    }
}

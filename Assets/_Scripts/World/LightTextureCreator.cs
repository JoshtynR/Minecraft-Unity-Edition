using System.Diagnostics;
using UnityEngine;

public static class LightTextureCreator
{
    // New API (0..1 colors)
    public static readonly Color[] LightColors = new Color[16 * 16];

    // Legacy alias kept for compatibility with existing code (expects Vector4 with A≈255)
    public static readonly Vector4[] lightColors = new Vector4[16 * 16];

    public static int skyDarkness;
    public static bool nether;
    private static float[] lightBrightnessTable = new float[16];

    [Range(0f, 1f)] public static float gamma = 0f;     // 0 = none, 1 = full shaping
    public static float skyLightMultiplier   = 0.75f;      // cool skylight tint
    public static float blockLightMultiplier = 2f;      // warm torch tint

    public static void CreateLightTexture()
    {
        GenerateLightBrightnessTable();
        GenerateLightMapColors();
    }

    public static Color GetLightColor(byte sky, byte block)
    {
        int si = Mathf.Clamp(sky,   0, 15);
        int bi = Mathf.Clamp(block, 0, 15);
        return LightColors[(si * 16) + bi];
    }

    public static Texture2D BuildLightTex2D()
    {
        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false, /*linear*/ true);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode   = TextureWrapMode.Clamp;

        var pixels = new Color32[16 * 16];
        for (int i = 0; i < LightColors.Length; i++)
            pixels[i] = (Color32)LightColors[i];

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return tex;
    }

    public static void GenerateLightMapColors()
    {
        for (int sky = 0; sky < 16; sky++)
        for (int block = 0; block < 16; block++)
        {
            float brightness = Beta173Lighting.Brightness(sky, block, skyDarkness,0,nether);
            brightness = Mathf.Lerp(brightness, 1f, Mathf.Clamp01(gamma));
            int index = sky * 16 + block;
            LightColors[index] = new Color(brightness, brightness, brightness, 1f);
            lightColors[index] = new Vector4(brightness, brightness, brightness, 255f);
        }
    }

    public static Vector3 WeirdLerp(Vector3 a, Vector3 b, float t)
    {
        float f = 1f - t;
        return new Vector3(a.x * f + b.x * t, a.y * f + b.y * t, a.z * f + b.z * t);
    }

    private static float ApplyCurve(float v, float g)
    {
        if (g <= 0f) return v;
        float inv = 1f - v;
        float shaped = 1f - inv * inv * inv * inv; // same shape as your modifyVector
        return Mathf.LerpUnclamped(v, shaped, g);
    }

    public static void GenerateLightBrightnessTable()
    {
        float ambient = nether ? .1f : .05f; // expose later if you add dimensions
        var fs = new float[16];
        for (int i = 0; i <= 15; ++i)
        {
            float g = i / 15f;
            float h = g / (4f - 3f * g);          // classic MC-ish curve
            fs[i] = Mathf.Lerp(h, 1f, ambient);
        }
        lightBrightnessTable = fs;
    }
}

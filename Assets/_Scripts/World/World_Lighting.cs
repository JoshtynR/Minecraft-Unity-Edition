using UnityEngine;

public partial class World
{
    [Tooltip("Current Beta skylight subtraction: 0 is daytime; 11 is a clear night. Updated by the overworld clock during play.")]
    [Range(0, 11)] public int betaSkylightSubtracted;
    [Tooltip("Optional enhanced smoothing. Enable bilinear face interpolation and smoothed liquids; disable for Beta vertex interpolation and original liquid sampling.")]
    public bool fullSmoothLighting = false;
    private int lastFullSmoothLighting = -1;
    private int lastBetaSkyDarkness = -1;
    private float lastBetaBrightnessBoost = -1;

    private void UpdateBetaLightingSettings()
    {
        if (Instance != this) return;
        Shader.SetGlobalFloat("_BetaNether", dimension == WorldDimension.Nether ? 1 : 0);
        int darkness = dimension == WorldDimension.Nether ? 0 : Mathf.Clamp(betaSkylightSubtracted, 0, 11);
        int smoothMode = fullSmoothLighting ? 1 : 0;
        bool rebuild = lastBetaSkyDarkness != darkness || lastFullSmoothLighting != smoothMode;
        lastFullSmoothLighting = smoothMode;
        if (lastBetaSkyDarkness != darkness)
        {
            Shader.SetGlobalFloat("_BetaSkyDarkness", darkness);
            LightTextureCreator.skyDarkness = darkness;
            LightTextureCreator.CreateLightTexture();
            Shader.SetGlobalVectorArray("lightColors",LightTextureCreator.lightColors);
            lastBetaSkyDarkness = darkness;

        }
        if (rebuild && IsWorldCreated && worldData.chunkDict != null)
            foreach (var chunk in worldData.chunkDict.Values) AddChunkToUpdate(chunk);
        if (lastBetaBrightnessBoost != gamma)
        {
            // Preserve the user's brightness control. Zero uses the Beta curve.
            Shader.SetGlobalFloat("_BetaBrightnessBoost", Mathf.Clamp01(gamma));
            lastBetaBrightnessBoost = gamma;
        }
    }
}

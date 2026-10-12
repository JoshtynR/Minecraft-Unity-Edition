using UnityEngine;

public partial class World
{
    [Tooltip("Beta skylight subtraction: 0 is daytime; 11 is a clear night. This does not add a day/night clock.")]
    [Range(0, 11)] public int betaSkylightSubtracted;
    private int lastBetaSkyDarkness = -1;
    private float lastBetaBrightnessBoost = -1;

    private void UpdateBetaLightingSettings()
    {
        int darkness = Mathf.Clamp(betaSkylightSubtracted, 0, 11);
        if (lastBetaSkyDarkness != darkness)
        {
            Shader.SetGlobalFloat("_BetaSkyDarkness", darkness);
            LightTextureCreator.skyDarkness = darkness;
            lastBetaSkyDarkness = darkness;
            if (IsWorldCreated && worldData.chunkDict != null)
                foreach (var chunk in worldData.chunkDict.Values) AddChunkToUpdate(chunk);
        }
        if (lastBetaBrightnessBoost != gamma)
        {
            // Preserve the user's brightness control. Zero uses the Beta curve.
            Shader.SetGlobalFloat("_BetaBrightnessBoost", Mathf.Clamp01(gamma));
            lastBetaBrightnessBoost = gamma;
        }
    }
}

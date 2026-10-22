using UnityEngine;

public partial class World
{
    [Tooltip("Java Edition 1.8 colored lightmap and smooth ambient occlusion. Disable to use the previous Beta lighting.")]
    public bool java18Lighting = true;
    [Tooltip("Current Beta skylight subtraction: 0 is daytime; 11 is a clear night. Updated by the overworld clock during play.")]
    [Range(0, 11)] public int betaSkylightSubtracted;
    [Tooltip("Beta fallback only: optional bilinear face interpolation and smoothed liquids. Java 1.8 lighting uses independent light levels and vertex AO.")]
    public bool fullSmoothLighting = false;
    private int lastFullSmoothLighting = -1;
    private int lastBetaSkyDarkness = -1;
    private float lastBetaBrightnessBoost = -1;
    private int lastJava18Lighting = -1;
    private float lightmapClock,torchFlicker,torchVelocity;
    private readonly System.Random lightmapRandom = new System.Random();
    private Texture2D java18Lightmap;

    private void ReleaseLightmap()
    {
        if(java18Lightmap!=null)
        {if(Application.isPlaying)Destroy(java18Lightmap);else DestroyImmediate(java18Lightmap);}
        java18Lightmap=null;
    }

    private void PublishLightmap()
    {
        LightTextureCreator.java18=java18Lighting;
        LightTextureCreator.nether=dimension==WorldDimension.Nether;
        LightTextureCreator.gamma=gamma;
        LightTextureCreator.skyDarkness=betaSkylightSubtracted;
        LightTextureCreator.sunlight=dimension==WorldDimension.Nether?.2f:Java18Lighting.SunBrightness(SkyClock.Angle(CurrentWorldTicks));
        LightTextureCreator.torchFlicker=torchFlicker;
        LightTextureCreator.CreateLightTexture();
        Shader.SetGlobalVectorArray("lightColors",LightTextureCreator.lightColors);
        if(java18Lighting)
        {
            if(java18Lightmap==null)
            {
                java18Lightmap=new Texture2D(16,16,TextureFormat.RGBA32,false,true);
                java18Lightmap.name="Owned Java 1.8 lightmap";
                java18Lightmap.filterMode=FilterMode.Bilinear;java18Lightmap.wrapMode=TextureWrapMode.Clamp;
            }
            java18Lightmap.SetPixels(LightTextureCreator.LightColors);java18Lightmap.Apply(false,false);
            Shader.SetGlobalTexture("_Java18Lightmap",java18Lightmap);
        }
    }

    private void UpdateBetaLightingSettings()
    {
        if (Instance != this) return;
        Shader.SetGlobalFloat("_Java18Lighting",java18Lighting?1:0);
        Shader.SetGlobalFloat("_BetaNether", dimension == WorldDimension.Nether ? 1 : 0);
        int darkness = dimension == WorldDimension.Nether ? 0 : Mathf.Clamp(betaSkylightSubtracted, 0, 11);
        int smoothMode = fullSmoothLighting ? 1 : 0;
        int modern=java18Lighting?1:0;
        bool rebuild = lastJava18Lighting!=modern || lastFullSmoothLighting != smoothMode || (!java18Lighting&&lastBetaSkyDarkness!=darkness);
        lastJava18Lighting=modern;
        lastFullSmoothLighting = smoothMode;
        lightmapClock+=Time.deltaTime;
        if(lightmapClock>=.05f||lastBetaSkyDarkness!=darkness||lastBetaBrightnessBoost!=gamma||rebuild)
        {
            lightmapClock=0;
            torchVelocity+=(float)((lightmapRandom.NextDouble()-lightmapRandom.NextDouble())*lightmapRandom.NextDouble()*lightmapRandom.NextDouble());
            torchVelocity*=.9f;torchFlicker=torchVelocity;
            PublishLightmap();
        }
        if (lastBetaSkyDarkness != darkness)
        {
            Shader.SetGlobalFloat("_BetaSkyDarkness", darkness);
            lastBetaSkyDarkness = darkness;

        }
        if (rebuild && IsWorldCreated && worldData.chunkDict != null)
            foreach (var chunk in worldData.chunkDict.Values) AddChunkToUpdate(chunk);
        if (lastBetaBrightnessBoost != gamma)
        {
            // Beta fallback only; 1.8 gamma is applied while generating the RGB map.
            Shader.SetGlobalFloat("_BetaBrightnessBoost", Mathf.Clamp01(gamma));
            lastBetaBrightnessBoost = gamma;
        }
    }
}

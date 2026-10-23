using UnityEngine;

public partial class World
{
    [Tooltip("Amount removed from direct skylight for gameplay checks: 0 at day, 11 at night.")]
    [Range(0, 11)] public int skyLightReduction;
    private float lastLightmapGamma = -1;
    private float lightmapClock,torchFlicker,torchVelocity;
    private System.Random lightmapRandom;
    private Texture2D lightmapTexture;

    private void ReleaseLightmap()
    {
        if(lightmapTexture!=null)
        {if(Application.isPlaying)Destroy(lightmapTexture);else DestroyImmediate(lightmapTexture);}
        lightmapTexture=null;
    }

    private void PublishLightmap()
    {
        VoxelLightmap.Generate(
            dimension==WorldDimension.Nether?.2f:LightmapLighting.SunBrightness(SkyClock.Angle(CurrentWorldTicks)),
            gamma,dimension==WorldDimension.Nether,torchFlicker);
        if(lightmapTexture==null)
        {
            lightmapTexture=new Texture2D(16,16,TextureFormat.RGBA32,false,true);
            lightmapTexture.name="Owned voxel lightmap";
            lightmapTexture.filterMode=FilterMode.Bilinear;lightmapTexture.wrapMode=TextureWrapMode.Clamp;
        }
        lightmapTexture.SetPixels(VoxelLightmap.Colors);lightmapTexture.Apply(false,false);
        Shader.SetGlobalTexture("_VoxelLightmap",lightmapTexture);
    }

    private void UpdateLighting()
    {
        if (Instance != this) return;
        lightmapClock+=Time.deltaTime;
        if(lightmapClock>=.05f||lastLightmapGamma!=gamma||lightmapTexture==null)
        {
            lightmapClock=0;
            if(lightmapRandom==null)lightmapRandom=new System.Random();
            torchVelocity+=(float)((lightmapRandom.NextDouble()-lightmapRandom.NextDouble())*lightmapRandom.NextDouble()*lightmapRandom.NextDouble());
            torchVelocity*=.9f;torchFlicker=torchVelocity;
            PublishLightmap();
            lastLightmapGamma=gamma;
        }
    }
}

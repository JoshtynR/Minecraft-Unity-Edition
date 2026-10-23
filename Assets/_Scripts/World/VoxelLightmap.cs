using UnityEngine;

public static class VoxelLightmap
{
    public static readonly Color[] Colors = new Color[16 * 16];

    public static void Generate(float sunlight,float gamma,bool nether,float torchFlicker)
    {
        for (int sky = 0; sky < 16; sky++)
        for (int block = 0; block < 16; block++)
        {
            var rgb=LightmapLighting.Sample(sky,block,sunlight,gamma,nether,torchFlicker);
            Colors[sky*16+block]=new Color(rgb.r,rgb.g,rgb.b,1);
        }
    }
}

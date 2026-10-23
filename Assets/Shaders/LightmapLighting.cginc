#ifndef VOXEL_LIGHTMAP_INCLUDED
#define VOXEL_LIGHTMAP_INCLUDED
sampler2D _VoxelLightmap;
// x=sky, y=block; z marks an entity's explicit world-light sample.
float4 _LightLevels;
float3 SampleVoxelLightmap(float2 levels)
{
    // X is block light; Y is skylight. Sample texel centers, including fractional
    // levels interpolated across a face.
    return tex2Dlod(_VoxelLightmap,float4((clamp(levels.yx,0,15)+.5)/16,0,0)).rgb;
}
float3 EntityLight()
{
    return _LightLevels.z>.5?SampleVoxelLightmap(_LightLevels.xy):1;
}
#endif

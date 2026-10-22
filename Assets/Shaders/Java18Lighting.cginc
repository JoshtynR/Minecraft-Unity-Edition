#ifndef JAVA18_LIGHTING_INCLUDED
#define JAVA18_LIGHTING_INCLUDED
float _Java18Lighting;
sampler2D _Java18Lightmap;
// x=sky, y=block; z marks an entity's explicit world-light sample.
float4 _LightLevels;
float3 Java18Light(float2 levels)
{
    // X is block light; Y is skylight. Sample texel centers, including fractional
    // levels interpolated across a face, as Java's lightmap texture does.
    return tex2Dlod(_Java18Lightmap,float4((clamp(levels.yx,0,15)+.5)/16,0,0)).rgb;
}
float3 EntityLight(float fallback)
{
    return _Java18Lighting>.5&&_LightLevels.z>.5?Java18Light(_LightLevels.xy):fallback.xxx;
}
#endif

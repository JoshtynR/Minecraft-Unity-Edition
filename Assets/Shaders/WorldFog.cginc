#ifndef MINECRAFT_WORLD_FOG
#define MINECRAFT_WORLD_FOG
float4 _WorldFogColor, _WorldFogRange;
float3 ApplyWorldFog(float3 color,float distance,float4 fluid)
{
    if(fluid.w>0)return lerp(color,fluid.rgb,1-exp(-fluid.w*distance));
    float fade=saturate((distance-_WorldFogRange.x)/max(1,_WorldFogRange.y-_WorldFogRange.x))*_WorldFogRange.z;
    return lerp(color,_WorldFogColor.rgb,fade);
}
#endif

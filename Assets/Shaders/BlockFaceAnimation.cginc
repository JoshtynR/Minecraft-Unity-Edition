#ifndef BLOCK_FACE_ANIMATION_INCLUDED
#define BLOCK_FACE_ANIMATION_INCLUDED
sampler2D _BlockFaceAnimations;
float _BlockFaceAnimationWidth;

// Called per vertex, not per screen pixel. All four vertices of a face share
// a base layer and the same global clock, so no CPU updates are necessary.
float3 AnimatedBlockFaceUV(float3 uv)
{
    if (_BlockFaceAnimationWidth > 0)
    {
        float layer = floor(uv.z + 0.5);
        float3 timing = tex2Dlod(_BlockFaceAnimations,
            float4((layer + 0.5) / _BlockFaceAnimationWidth, 0.5, 0, 0)).rgb;
        uv.z = timing.x + fmod(floor(_Time.y * timing.z), max(1, timing.y));
    }
    return uv;
}
#endif

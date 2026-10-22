Shader "Voxel/DroppedBlock"
{
    Properties
    {
        _TextureOn ("Texture On", Float) = 1
        _LightingOn ("Lighting On", Float) = 1
        _AOOn ("Smooth Lighting", Float) = 0
        _FaceShadeOn ("Face Shade On", Float) = 1
        _ShadeUp ("Up", Float) = 1
        _ShadeSideNS ("North South", Float) = .8
        _ShadeSideEW ("East West", Float) = .6
        _ShadeDown ("Down", Float) = .5
        _Debug ("Debug", Float) = 0
        _Brightness ("Item light ratio", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Back ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma require 2darray
            #define frag DroppedBlockBaseFragment
            #define VOXEL_ENTITY_LIGHT
            #include "VoxelLighting.cginc"
            #undef frag
            float _Brightness;
            float4 frag(v2f i):SV_Target
            {float4 c=DroppedBlockBaseFragment(i);if(_Java18Lighting<.5)c.rgb*=_Brightness;return c;}
            ENDCG
        }
    }
}

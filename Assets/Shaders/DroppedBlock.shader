Shader "Voxel/DroppedBlock"
{
    Properties
    {
        _TextureOn ("Texture On", Float) = 1
        _LightingOn ("Lighting On", Float) = 1
        _AOOn ("Smooth Lighting", Float) = 0
        _AOIntensity ("Ambient Occlusion Intensity", Range(0,2)) = 1
        _FaceShadeOn ("Face Shade On", Float) = 1
        _ShadeUp ("Up", Float) = 1
        _ShadeSideNS ("North South", Float) = .8
        _ShadeSideEW ("East West", Float) = .6
        _ShadeDown ("Down", Float) = .5
        _Debug ("Debug", Float) = 0
        _FuseFlash ("Primed TNT flash", Range(0,1)) = 0
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
            #define VOXEL_ENTITY_LIGHT
            #define VOXEL_FUSE_FLASH
            #include "VoxelLighting.cginc"
            ENDCG
        }
    }
}

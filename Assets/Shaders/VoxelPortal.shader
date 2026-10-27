Shader "Voxel/Portal"
{
    Properties
    {
        _PortalOpacity ("Portal Opacity", Range(0,1)) = 0.72
        [MaterialToggle] _TextureOn ("Texture On", Float) = 1
        [MaterialToggle] _LightingOn ("Lighting On", Float) = 1
        [MaterialToggle] _AOOn ("Smooth Lighting", Float) = 0
        _AOIntensity ("Ambient Occlusion Intensity", Range(0,2)) = 0
        [MaterialToggle] _FaceShadeOn ("Face Shade On", Float) = 0
        _ShadeUp ("Up Brightness", Range(0,1.5)) = 1
        _ShadeSideNS ("North/South Brightness", Range(0,1.5)) = 1
        _ShadeSideEW ("East/West Brightness", Range(0,1.5)) = 1
        _ShadeDown ("Down Brightness", Range(0,1.5)) = 1
        _Debug ("Debug", Range(0,3)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma require 2darray
            #define VOXEL_PORTAL
            #include "VoxelLighting.cginc"
            ENDCG
        }
    }
}

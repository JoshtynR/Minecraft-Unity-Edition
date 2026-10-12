Shader "Voxel/Water"
{
    Properties
    {
        _WaterOpacity ("Water Opacity", Range(0,1)) = 0.6
        [MaterialToggle] _TextureOn ("Texture On", Float) = 1
        [MaterialToggle] _LightingOn ("Lighting On", Float) = 1
        [MaterialToggle] _AOOn ("Smooth Lighting", Float) = 1
        [MaterialToggle] _FaceShadeOn ("Face Shade On", Float) = 1
        _ShadeUp ("Up Brightness", Range(0,1.5)) = 1
        _ShadeSideNS ("North/South Brightness", Range(0,1.5)) = 0.8
        _ShadeSideEW ("East/West Brightness", Range(0,1.5)) = 0.6
        _ShadeDown ("Down Brightness", Range(0,1.5)) = 0.5
        _Debug ("Debug: 0 Off, 1 Sky, 2 Block, 3 Brightness", Range(0,3)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
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
            #define VOXEL_WATER
            #include "VoxelLighting.cginc"
            ENDCG
        }
    }
}

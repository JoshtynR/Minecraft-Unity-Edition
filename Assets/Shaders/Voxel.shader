Shader "Voxel"
{
    Properties
    {
        [MaterialToggle] _TextureOn ("Texture On", Float) = 1
        [MaterialToggle] _LightingOn ("Lighting On", Float) = 1
        [MaterialToggle] _AOOn ("Beta Smooth Lighting", Float) = 1
        [MaterialToggle] _FaceShadeOn ("Face Shade On", Float) = 1
        _ShadeUp ("Up Brightness", Range(0,1.5)) = 1
        _ShadeSideNS ("North/South Brightness", Range(0,1.5)) = 0.8
        _ShadeSideEW ("East/West Brightness", Range(0,1.5)) = 0.6
        _ShadeDown ("Down Brightness", Range(0,1.5)) = 0.5
        _Debug ("Debug: 0 Off, 1 Sky, 2 Block, 3 Brightness", Range(0,3)) = 0
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Back
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma require 2darray
            #include "UnityCG.cginc"
            UNITY_DECLARE_TEX2DARRAY(_BlockFaceTextures);
            float _TextureOn, _LightingOn, _AOOn, _FaceShadeOn;
            float _ShadeUp, _ShadeSideNS, _ShadeSideEW, _ShadeDown, _Debug;
            float _BetaSkyDarkness, _BetaBrightnessBoost;
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 uv : TEXCOORD0;
                float2 light : TEXCOORD1;
                float3 sides : TEXCOORD2; // smooth, flat, Beta sample marker
                float4 color : COLOR;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 uv : TEXCOORD0;
                float2 skyBlock : TEXCOORD1;
                float brightness : TEXCOORD2;
                float faceShade : TEXCOORD3;
                float4 color : COLOR;
            };
            float BetaBrightness(float sky, float block)
            {
                float level = max(max(0, floor(sky + 0.5) - _BetaSkyDarkness), floor(block + 0.5));
                float darkness = 1 - saturate(level / 15);
                return (1 - darkness) / (darkness * 3 + 1) * 0.95 + 0.05;
            }
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.skyBlock = v.light;
                o.color = v.color;
                float brightness = v.sides.z > 0.5
                    ? (_AOOn > 0.5 ? v.sides.x : v.sides.y)
                    : BetaBrightness(v.light.x, v.light.y);
                o.brightness = lerp(brightness, 1, saturate(_BetaBrightnessBoost));
                float3 normal = UnityObjectToWorldNormal(v.normal);
                float3 an = abs(normal);
                float shade = an.y >= an.x && an.y >= an.z ? (normal.y >= 0 ? _ShadeUp : _ShadeDown)
                    : (an.z >= an.x ? _ShadeSideNS : _ShadeSideEW);
                o.faceShade = _FaceShadeOn > 0.5 ? shade : 1;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float4 col = (_TextureOn > 0.5) ? UNITY_SAMPLE_TEX2DARRAY(_BlockFaceTextures, i.uv) : float4(1,1,1,1);
                clip(col.a - 0.5);
                // Beta multiplied vertex brightness/tint into sRGB texture colors.
                // Preserve that operation even in a Unity Linear project.
                #ifndef UNITY_COLORSPACE_GAMMA
                    col.rgb = LinearToGammaSpace(col.rgb);
                #endif
                col.rgb *= i.color.rgb;
                if (_LightingOn > 0.5) col.rgb *= i.brightness * i.faceShade;
                if (_Debug > 0.5 && _Debug < 1.5) col.rgb = (i.skyBlock.x / 15).xxx;
                if (_Debug > 1.5 && _Debug < 2.5) col.rgb = (i.skyBlock.y / 15).xxx;
                if (_Debug > 2.5) col.rgb = i.brightness.xxx;
                #ifndef UNITY_COLORSPACE_GAMMA
                    col.rgb = GammaToLinearSpace(col.rgb);
                #endif
                return col;
            }
            ENDCG
        }
    }
}

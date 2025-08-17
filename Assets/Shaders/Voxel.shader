Shader "Voxel"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        [MaterialToggle] _TextureOn  ("Texture On", Float) = 1
        [MaterialToggle] _LightingOn ("Lighting On", Float) = 1

        // ----- AO -----
        [MaterialToggle] _AOOn          ("AO On", Float) = 1
        _AOIntensity ("AO Intensity (0..2)", Range(0,2)) = 1.0

        // ----- Face directional shading (Minecraft-esque) -----
        [MaterialToggle] _FaceShadeOn   ("Face Shade On", Float) = 1
        _ShadeUp      ("Up Brightness",       Range(0.0,1.5)) = 1.00  // +Y
        _ShadeSideNS  ("North/South Bright.", Range(0.0,1.5)) = 0.80  // ±Z
        _ShadeSideEW  ("East/West Bright.",   Range(0.0,1.5)) = 0.60  // ±X
        _ShadeDown    ("Down Brightness",     Range(0.0,1.5)) = 0.50  // -Y

        // ----- Light shaping (procedural) -----
        _LT_Gamma       ("LT Gamma (signed, -1..1)", Range(-1,1)) = 0.0
        _LT_Exposure    ("LT Exposure", Range(0,2)) = 1.0
        _LT_NeutralLift ("LT Neutral Lift (0..0.2)", Range(0,0.2)) = 0.04
        _LT_SkyMul      ("LT Skylight Multiplier", Float) = 0.75
        _LT_BlockMul    ("LT Block Light Multiplier", Float) = 2.0

        // Optional LUT for debug=3
        _LightTex ("Light Texture (16x16 from LightTextureCreator)", 2D) = "white" {}

        // 0=None, 1=Sky, 2=Block, 3=LUT RGB
        _Debug ("Debug View (0/1/2/3)", Range(0,3)) = 0
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout" }
        Cull Back
        ZWrite On
        Lighting Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _LightTex;

            float _TextureOn, _LightingOn;
            float _AOOn, _AOIntensity;
            float _FaceShadeOn, _ShadeUp, _ShadeSideNS, _ShadeSideEW, _ShadeDown;

            float _LT_Gamma, _LT_Exposure, _LT_NeutralLift, _LT_SkyMul, _LT_BlockMul;
            float _Debug;

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
                float2 light  : TEXCOORD1;  // (sky, block) in 0..15
                float3 sides  : TEXCOORD2;  // AO flags: side1, side2, corner (0/1)
                float4 color  : COLOR;      // vertex tint
            };
            struct v2f {
                float4 pos       : SV_POSITION;
                float2 uv        : TEXCOORD0;
                float2 skyBlock  : TEXCOORD1;
                float  aoOcc     : TEXCOORD2;
                float  faceShade : TEXCOORD3;
                float4 color     : COLOR;
            };

            // ---------- AO ----------
            float MinecraftAO(bool s1, bool s2, bool sc, float k)
            {
                int sideCount = (s1 ? 1 : 0) + (s2 ? 1 : 0);
                int occSteps  = (sideCount == 2) ? 3 : sideCount + (sc ? 1 : 0); // 0..3

                // Base factors (MC-like)
                float base =
                    (occSteps == 0) ? 1.00 :
                    (occSteps == 1) ? 0.80 :
                    (occSteps == 2) ? 0.60 : 0.40;

                float t = saturate(k);
                float ao = lerp(1.0, base, t);
                if (k > 1.0) ao = lerp(ao, base * base, saturate(k - 1.0)); // optional exaggeration
                return ao;
            }

            // ---------- Face directional shading ----------
            float ComputeFaceShade(float3 worldN)
            {
                float3 an = abs(worldN);
                if (an.y >= an.x && an.y >= an.z)
                    return (worldN.y >= 0.0) ? _ShadeUp : _ShadeDown;
                else if (an.z >= an.x && an.z >= an.y)
                    return _ShadeSideNS; // ±Z
                else
                    return _ShadeSideEW; // ±X
            }

            // ---------- Light color (procedural; matches your C# ramps) ----------
            // brightness table: g=i/15; h=g/(4 - 3g)
            float BrightFromLevel(float level01)
            {
                float g = saturate(level01);
                return g / (4.0 - 3.0 * g);
            }

            // Signed gamma: >0 brightens mids (your shaped curve), <0 darkens mids (towards v^2)
            float3 ApplyGammaCurve(float3 v, float gammaSigned)
            {
                if (gammaSigned > 0.0001)
                {
                    float3 inv = 1.0 - v;
                    float3 shaped = 1.0 - inv*inv*inv*inv;
                    return lerp(v, shaped, saturate(gammaSigned));
                }
                else if (gammaSigned < -0.0001)
                {
                    float t = saturate(-gammaSigned);
                    float3 darker = v * v; // quadratic darken
                    return lerp(v, darker, t);
                }
                return v;
            }

            float3 ComputeLightRGB(float skyLevel15, float blockLevel15)
            {
                // 0..15 -> 0..1
                float sky01   = saturate(skyLevel15   / 15.0);
                float block01 = saturate(blockLevel15 / 15.0);

                // brightness
                float m = BrightFromLevel(sky01)   * _LT_SkyMul;    // sky
                float n = BrightFromLevel(block01) * _LT_BlockMul;  // torch

                // warm torch ramp
                float r = n;
                float g = n * ((n * 0.6 + 0.4) * 0.6 + 0.4);
                float b = n * (n * n * 0.6 + 0.4);
                float3 torch = float3(r, g, b);

                // slightly bluish skylight tint, then blend toward neutral
                float3 skyTint = float3(_LT_SkyMul, _LT_SkyMul, 1.0);
                skyTint = lerp(skyTint, 1.0.xxx, 0.35);

                float3 col = torch + skyTint * m;

                // neutral lift (tweakable)
                col = lerp(col, 0.75.xxx, _LT_NeutralLift);

                // signed gamma
                col = ApplyGammaCurve(saturate(col), _LT_Gamma);

                // second neutral pull
                col = lerp(col, 0.75.xxx, _LT_NeutralLift);

                // exposure
                col *= _LT_Exposure;

                return saturate(col);
            }

            // LUT sampling kept for debug=3
            float2 EncodeUV(float sky, float block) { return float2((block + 0.5)/16.0, (sky + 0.5)/16.0); }
            float3 SampleLightLUT_BiLerp(float sky, float block)
            {
                sky   = clamp(sky,   0.0, 15.0);
                block = clamp(block, 0.0, 15.0);
                int s0 = (int)floor(sky);
                int b0 = (int)floor(block);
                int s1 = min(s0 + 1, 15);
                int b1 = min(b0 + 1, 15);
                float ts = sky   - (float)s0;
                float tb = block - (float)b0;
                float3 c00 = tex2D(_LightTex, EncodeUV((float)s0, (float)b0)).rgb;
                float3 c10 = tex2D(_LightTex, EncodeUV((float)s1, (float)b0)).rgb;
                float3 c01 = tex2D(_LightTex, EncodeUV((float)s0, (float)b1)).rgb;
                float3 c11 = tex2D(_LightTex, EncodeUV((float)s1, (float)b1)).rgb;
                float3 cx0 = lerp(c00, c10, ts);
                float3 cx1 = lerp(c01, c11, ts);
                return lerp(cx0, cx1, tb);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.uv       = v.uv;
                o.skyBlock = v.light;
                o.color    = v.color;

                // AO
                if (_AOOn > 0.5)
                {
                    bool s1 = v.sides.x > 0.5;
                    bool s2 = v.sides.y > 0.5;
                    bool sc = v.sides.z > 0.5;
                    o.aoOcc = MinecraftAO(s1, s2, sc, _AOIntensity);
                }
                else o.aoOcc = 1.0;

                // Face shade
                if (_FaceShadeOn > 0.5)
                {
                    float3 wN = UnityObjectToWorldNormal(v.normal);
                    o.faceShade = ComputeFaceShade(normalize(wN));
                }
                else o.faceShade = 1.0;

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 col = (_TextureOn>0.5) ? tex2D(_MainTex, i.uv) : float4(1,1,1,1);
                clip(col.a - 1); // cutout atlas

                // vertex tint
                col.rgb *= i.color.rgb;

                // debug raw levels
                if (_Debug > 0.5 && _Debug < 1.5) return float4(i.skyBlock.x/15.0.xxx, 1); // Sky
                if (_Debug > 1.5 && _Debug < 2.5) return float4(i.skyBlock.y/15.0.xxx, 1); // Block

                // procedural light color
                float3 lightRGB = ComputeLightRGB(i.skyBlock.x, i.skyBlock.y);

                // optional: show LUT color for comparison
                if (_Debug > 2.5)
                {
                    float3 lutRGB = SampleLightLUT_BiLerp(i.skyBlock.x, i.skyBlock.y);
                    return float4(lutRGB, 1);
                }

                float shade = i.aoOcc * i.faceShade;
                float3 litRGB = lightRGB * shade;

                if (_LightingOn > 0.5) col.rgb *= litRGB;

                return col;
            }
            ENDCG
        }
    }
}

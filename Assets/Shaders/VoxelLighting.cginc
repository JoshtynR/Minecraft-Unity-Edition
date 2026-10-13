            #include "UnityCG.cginc"
            UNITY_DECLARE_TEX2DARRAY(_BlockFaceTextures);
            float _TextureOn, _LightingOn, _AOOn, _FaceShadeOn;
            float _ShadeUp, _ShadeSideNS, _ShadeSideEW, _ShadeDown, _Debug;
            float _BetaSkyDarkness, _BetaBrightnessBoost;
            #ifdef VOXEL_WATER
                float _WaterOpacity;
            #endif
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 uv : TEXCOORD0;
                float2 light : TEXCOORD1;
                float3 sides : TEXCOORD2; // smooth, flat, sampling mode
                float4 corners : TEXCOORD3; // face corners in UV winding order
                float4 color : COLOR;
                float2 faceUV : TEXCOORD4;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 uv : TEXCOORD0;
                float2 skyBlock : TEXCOORD1;
                float brightness : TEXCOORD2;
                float faceShade : TEXCOORD3;
                float4 color : COLOR;
                float4 corners : TEXCOORD4;
                float samplingMode : TEXCOORD5;
                float2 faceUV : TEXCOORD6;
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
                o.faceUV = v.faceUV;
                o.skyBlock = v.light;
                o.color = v.color;
                o.corners = v.corners;
                o.samplingMode = v.sides.z;
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
                #ifdef VOXEL_WATER
                    col.a *= saturate(_WaterOpacity) * i.color.a;
                #else
                    clip(col.a - 0.5);
                #endif
                // Beta multiplied vertex brightness/tint into sRGB texture colors.
                // Preserve that operation even in a Unity Linear project.
                #ifndef UNITY_COLORSPACE_GAMMA
                    col.rgb = LinearToGammaSpace(col.rgb);
                #endif
                col.rgb *= i.color.rgb;
                float brightness = i.brightness;
                if (_AOOn > 0.5 && i.samplingMode > 1.5)
                {
                    float bottom = lerp(i.corners.x, i.corners.w, i.faceUV.x);
                    float top = lerp(i.corners.y, i.corners.z, i.faceUV.x);
                    brightness = lerp(bottom, top, i.faceUV.y);
                    brightness = lerp(brightness, 1, saturate(_BetaBrightnessBoost));
                }
                if (_LightingOn > 0.5) col.rgb *= brightness * i.faceShade;
                if (_Debug > 0.5 && _Debug < 1.5) col.rgb = (i.skyBlock.x / 15).xxx;
                if (_Debug > 1.5 && _Debug < 2.5) col.rgb = (i.skyBlock.y / 15).xxx;
                if (_Debug > 2.5) col.rgb = brightness.xxx;
                #ifndef UNITY_COLORSPACE_GAMMA
                    col.rgb = GammaToLinearSpace(col.rgb);
                #endif
                return col;
            }

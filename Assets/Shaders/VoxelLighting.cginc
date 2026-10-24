            #include "UnityCG.cginc"
            #include "WorldFog.cginc"
            #include "LightmapLighting.cginc"
            UNITY_DECLARE_TEX2DARRAY(_BlockFaceTextures);
            #include "BlockFaceAnimation.cginc"
            float _TextureOn, _LightingOn, _AOOn, _AOIntensity, _FaceShadeOn;
            float _ShadeUp, _ShadeSideNS, _ShadeSideEW, _ShadeDown, _Debug;
            float4 _FluidFog;
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
                float faceShade : TEXCOORD3;
                float4 color : COLOR;
                float fogDistance : TEXCOORD7;
                float ao : TEXCOORD8;
            };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.fogDistance = length(UnityObjectToViewPos(v.vertex));
                o.uv = AnimatedBlockFaceUV(v.uv);
                o.skyBlock = v.light;
                o.color = v.color;
                float2 levels=v.light;
                #ifdef VOXEL_ENTITY_LIGHT
                    if(_LightLevels.z>.5)levels=_LightLevels.xy;
                #endif
                if(v.sides.z>3.5&&v.sides.z<4.5&&_AOOn<.5)levels=v.corners.xy;
                // Marker 4 carries true corner AO. Scale only its occlusion
                // amount so directional face shading and special-model shading
                // remain independent material controls.
                float ao=1;
                if(v.sides.z>4.5)ao=v.sides.x;
                else if(v.sides.z>3.5&&_AOOn>.5)
                    ao=saturate(lerp(1,v.sides.x,max(0,_AOIntensity)));
                o.skyBlock=levels;o.ao=ao;
                float3 normal = UnityObjectToWorldNormal(v.normal);
                float3 an = abs(normal);
                float shade = an.y >= an.x && an.y >= an.z ? (normal.y >= 0 ? _ShadeUp : _ShadeDown)
                    : (an.z >= an.x ? _ShadeSideNS : _ShadeSideEW);
                o.faceShade = _FaceShadeOn > 0.5 && (v.sides.z < 2.5||v.sides.z>3.5&&v.sides.z<4.5) ? shade : 1;
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
                // Voxel textures and vertex tints are authored in display color space.
                #ifndef UNITY_COLORSPACE_GAMMA
                    col.rgb = LinearToGammaSpace(col.rgb);
                #endif
                col.rgb *= i.color.rgb;
                float3 light=SampleVoxelLightmap(i.skyBlock)*i.ao;
                // Directional face shading is an art rule, not skylight. UI
                // block icons disable the lightmap but must keep their classic
                // bright top / darker side presentation.
                if (_LightingOn > 0.5) col.rgb *= light;
                col.rgb *= i.faceShade;
                if (_Debug > 0.5 && _Debug < 1.5) col.rgb = (i.skyBlock.x / 15).xxx;
                if (_Debug > 1.5 && _Debug < 2.5) col.rgb = (i.skyBlock.y / 15).xxx;
                if (_Debug > 2.5) col.rgb = dot(light,float3(.2126,.7152,.0722)).xxx;
                col.rgb = ApplyWorldFog(col.rgb,i.fogDistance,_FluidFog);
                #ifndef UNITY_COLORSPACE_GAMMA
                    col.rgb = GammaToLinearSpace(col.rgb);
                #endif
                return col;
            }

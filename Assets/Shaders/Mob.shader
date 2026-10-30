Shader "Voxel/Mob"
{
    Properties { _MainTex ("Mob texture",2D)="white" {} _Tint("Tint",Color)=(1,1,1,1) _Hurt ("Hurt flash",Float)=0 _Flash("Fuse flash",Float)=0 _Unlit("Unlit effect",Float)=0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"
            #include "WorldFog.cginc"
            #include "LightmapLighting.cginc"
            sampler2D _MainTex;float _Hurt,_Flash,_Unlit;float4 _FluidFog,_Tint;
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float3 normal:NORMAL;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float light:TEXCOORD1;float distance:TEXCOORD2;};
            v2f vert(appdata v)
            {
                v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.distance=length(UnityObjectToViewPos(v.vertex));
                float3 n=UnityObjectToWorldNormal(v.normal);
                float shade=n.x*n.x*.6+n.z*n.z*.8+n.y*n.y*(n.y<0?.5:1);
                o.light=shade;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);clip(c.a*_Tint.a-.1);
                // Classic entity shading multiplies texture colors in gamma space.
                #ifndef UNITY_COLORSPACE_GAMMA
                    c.rgb=LinearToGammaSpace(c.rgb);
                #endif
                c.rgb*=_Tint.rgb;
                float3 rgb=lerp(c.rgb*lerp(EntityLight()*i.light,1,_Unlit),float3(1,.2,.2),_Hurt*.45);
                rgb=lerp(rgb,float3(1,1,1),_Flash);
                rgb=ApplyWorldFog(rgb,i.distance,_FluidFog);
                #ifndef UNITY_COLORSPACE_GAMMA
                    rgb=GammaToLinearSpace(rgb);
                #endif
                return fixed4(rgb,1);
            }
            ENDCG
        }
    }
}

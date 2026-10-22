Shader "Voxel/ModernChest"
{
    Properties { _MainTex ("Chest skin",2D)="white" {} _Brightness ("Voxel light",Float)=1 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"
            #include "WorldFog.cginc"
            #include "Java18Lighting.cginc"
            sampler2D _MainTex;float _Brightness;float4 _FluidFog;
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float3 normal:NORMAL;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float light:TEXCOORD1;float distance:TEXCOORD2;};
            v2f vert(appdata v)
            {
                v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.distance=length(UnityObjectToViewPos(v.vertex));
                float3 n=abs(UnityObjectToWorldNormal(v.normal));
                float shade=n.y>.5?(UnityObjectToWorldNormal(v.normal).y<0?.5:1):n.x>.5?.6:.8;
                o.light=shade;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);clip(c.a-.1);
                #ifndef UNITY_COLORSPACE_GAMMA
                    c.rgb=LinearToGammaSpace(c.rgb);
                #endif
                c.rgb=ApplyWorldFog(c.rgb*EntityLight(_Brightness)*i.light,i.distance,_FluidFog);
                #ifndef UNITY_COLORSPACE_GAMMA
                    c.rgb=GammaToLinearSpace(c.rgb);
                #endif
                return c;
            }
            ENDCG
        }
    }
}

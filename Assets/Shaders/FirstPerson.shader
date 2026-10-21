Shader "Voxel/FirstPerson"
{
    Properties { _MainTex ("Skin or item", 2D) = "white" {} _Brightness ("Brightness", Float) = 1 }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Back ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float _Brightness;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float3 normal:NORMAL; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float shade:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv;
                float3 n=UnityObjectToWorldNormal(v.normal);
                o.shade=.6+.4*saturate(dot(n,normalize(float3(-.3,.8,-.6)))); return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv); clip(c.a-.1);
                #ifndef UNITY_COLORSPACE_GAMMA
                    c.rgb=LinearToGammaSpace(c.rgb);
                #endif
                c.rgb*=_Brightness*i.shade;
                #ifndef UNITY_COLORSPACE_GAMMA
                    c.rgb=GammaToLinearSpace(c.rgb);
                #endif
                return c;
            }
            ENDCG
        }
    }
}

Shader "Voxel/DroppedItem"
{
    Properties { _MainTex ("Item", 2D) = "white" {} _Brightness ("Brightness", Float) = 1 }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float _Brightness;float4 _FluidFog;
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float distance:TEXCOORD1;};
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.distance=length(UnityObjectToViewPos(v.vertex));return o;}
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);clip(c.a-.1);
                #ifndef UNITY_COLORSPACE_GAMMA
                    c.rgb=LinearToGammaSpace(c.rgb);
                #endif
                c.rgb*=_Brightness;c.rgb=lerp(c.rgb,_FluidFog.rgb,1-exp(-_FluidFog.w*i.distance));
                #ifndef UNITY_COLORSPACE_GAMMA
                    c.rgb=GammaToLinearSpace(c.rgb);
                #endif
                return c;
            }
            ENDCG
        }
    }
}

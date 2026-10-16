Shader "Voxel/InventoryPlayer"
{
    Properties { _MainTex ("Skin", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float3 normal:NORMAL; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float light:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;
                float3 n=UnityObjectToWorldNormal(v.normal);
                o.light=.6+.4*saturate(dot(n,normalize(float3(-.3,.8,-.6))));return o;
            }
            fixed4 frag(v2f i):SV_Target { fixed4 c=tex2D(_MainTex,i.uv);clip(c.a-.5);c.rgb*=i.light;return c; }
            ENDCG
        }
    }
}

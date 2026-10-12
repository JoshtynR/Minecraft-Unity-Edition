Shader "Voxel/BlockBreak"
{
    Properties { _MainTex ("Crack sheet", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="AlphaTest+10" "RenderType"="Transparent" }
        Blend DstColor Zero
        ZWrite Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Frame;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv * _Frame.xy + _Frame.zw; return o; }
            float4 frag(v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv);
                // Transparent pixels and white background preserve the block;
                // black crack pixels darken its existing texture and lighting.
                return float4(lerp(float3(1,1,1), tex.rgb, tex.a), 1);
            }
            ENDCG
        }
    }
}

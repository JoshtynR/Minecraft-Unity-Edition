Shader "Custom/VoxelVertexLit"
{
  Properties {}
  SubShader
  {
    Tags { "Queue"="Geometry" "RenderType"="Opaque" }
    LOD 100
    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma target 3.5
      #pragma require 2darray
      #include "UnityCG.cginc"
      #include "BlockFaceAnimation.cginc"

      UNITY_DECLARE_TEX2DARRAY(_BlockFaceTextures);

      struct appdata {
        float4 vertex : POSITION;
        float3 uv     : TEXCOORD0;
        fixed4 color  : COLOR;
        float3 sides  : TEXCOORD2;
      };
      struct v2f {
        float4 pos   : SV_POSITION;
        float3 uv    : TEXCOORD0;
        fixed4 color : COLOR;
        float repeatUV : TEXCOORD1;
      };

      v2f vert (appdata v) {
        v2f o;
        o.pos   = UnityObjectToClipPos(v.vertex);
        o.uv    = AnimatedBlockFaceUV(v.uv);
        o.repeatUV = v.sides.y > 1.5 ? 1 : 0;
        o.color = v.color;       // pass vertex color to fragment
        return o;
      }

      fixed4 frag (v2f i) : SV_Target {
        float3 textureUV=i.uv;
        if(i.repeatUV>.5)textureUV.xy=frac(textureUV.xy);
        fixed4 albedo = UNITY_SAMPLE_TEX2DARRAY(_BlockFaceTextures, textureUV);
        return albedo * i.color; // multiply by vertex color (smooth lighting / AO)
      }
      ENDCG
    }
  }
}

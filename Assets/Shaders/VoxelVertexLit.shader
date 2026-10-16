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
      };
      struct v2f {
        float4 pos   : SV_POSITION;
        float3 uv    : TEXCOORD0;
        fixed4 color : COLOR;
      };

      v2f vert (appdata v) {
        v2f o;
        o.pos   = UnityObjectToClipPos(v.vertex);
        o.uv    = AnimatedBlockFaceUV(v.uv);
        o.color = v.color;       // pass vertex color to fragment
        return o;
      }

      fixed4 frag (v2f i) : SV_Target {
        fixed4 albedo = UNITY_SAMPLE_TEX2DARRAY(_BlockFaceTextures, i.uv);
        return albedo * i.color; // multiply by vertex color (smooth lighting / AO)
      }
      ENDCG
    }
  }
}

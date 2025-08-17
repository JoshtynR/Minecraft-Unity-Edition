Shader "Custom/VoxelVertexLit"
{
  Properties { _MainTex ("Atlas", 2D) = "white" {} }
  SubShader
  {
    Tags { "Queue"="Geometry" "RenderType"="Opaque" }
    LOD 100
    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"

      sampler2D _MainTex;

      struct appdata {
        float4 vertex : POSITION;
        float2 uv     : TEXCOORD0;
        fixed4 color  : COLOR;
      };
      struct v2f {
        float4 pos   : SV_POSITION;
        float2 uv    : TEXCOORD0;
        fixed4 color : COLOR;
      };

      v2f vert (appdata v) {
        v2f o;
        o.pos   = UnityObjectToClipPos(v.vertex);
        o.uv    = v.uv;
        o.color = v.color;       // pass vertex color to fragment
        return o;
      }

      fixed4 frag (v2f i) : SV_Target {
        fixed4 albedo = tex2D(_MainTex, i.uv);
        return albedo * i.color; // multiply by vertex color (smooth lighting / AO)
      }
      ENDCG
    }
  }
}

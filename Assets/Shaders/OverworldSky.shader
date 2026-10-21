Shader "Voxel/OverworldSky"
{
 Properties {_MainTex("Texture",2D)="white"{} _Tint("Tint",Color)=(1,1,1,1) _FogOn("Cloud fog",Float)=0}
 SubShader
 {
  Tags {"Queue"="Background+10" "RenderType"="Transparent"}
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   #include "WorldFog.cginc"
   sampler2D _MainTex;float4 _MainTex_ST,_Tint,_FluidFog;float _FogOn;
   struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float distance:TEXCOORD1;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.distance=length(UnityObjectToViewPos(v.vertex));return o;}
   fixed4 frag(v2f i):SV_Target
   {
    fixed4 c=tex2D(_MainTex,i.uv)*_Tint;clip(c.a-.01);
    // Water/lava obscure celestial bodies as well as terrain.
    if(_FluidFog.w>0)c.a*=exp(-_FluidFog.w*i.distance);
    else if(_FogOn>.5)c.rgb=ApplyWorldFog(c.rgb,i.distance,float4(0,0,0,0));
    return c;
   }
   ENDCG
  }
 }
}

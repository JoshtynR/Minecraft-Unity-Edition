Shader "Voxel/OverworldSky"
{
 Properties {_MainTex("Texture",2D)="white"{} _Tint("Tint",Color)=(1,1,1,1) _FogOn("Cloud fog",Float)=0 _BlackTransparent("Black background transparent",Float)=0 _BlackCutoff("Black transparency cutoff",Range(0,0.5))=0.005 _HorizonBand("Horizon band",Float)=0 _VertexColorOn("Vertex shading",Float)=0 _ZWrite("Depth write",Float)=0}
 SubShader
 {
  Tags {"Queue"="Background+10" "RenderType"="Transparent"}
  Cull Off ZWrite [_ZWrite] Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   #include "WorldFog.cginc"
   sampler2D _MainTex;float4 _MainTex_ST,_Tint,_FluidFog,_SunsetDirection;float _FogOn,_BlackTransparent,_BlackCutoff,_HorizonBand,_VertexColorOn;
   struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float distance:TEXCOORD1;float2 horizonDirection:TEXCOORD2;float4 color:COLOR;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.distance=length(UnityObjectToViewPos(v.vertex));o.horizonDirection=normalize(v.vertex.xz);o.color=lerp(float4(1,1,1,1),v.color,_VertexColorOn);return o;}
   fixed4 frag(v2f i):SV_Target
   {
    fixed4 sample=tex2D(_MainTex,i.uv);
    fixed4 c=sample*_Tint*i.color;
    // Preserve authored pixel alpha exactly. RGB-only legacy sheets use a hard
    // black key so their halo stays pixel-stepped rather than becoming blurry.
    if(_BlackTransparent>.5)c.a*=step(_BlackCutoff,max(sample.r,max(sample.g,sample.b)));
    if(_HorizonBand>.5)
    {
     float vertical=smoothstep(0,.30,i.uv.y)*(1-smoothstep(.30,1,i.uv.y));
     float facing=saturate(dot(i.horizonDirection,normalize(_SunsetDirection.xz))*.5+.5);
     c.a*=vertical*facing*facing;
    }
    clip(c.a-.002);
    // Water/lava obscure celestial bodies as well as terrain.
    if(_FluidFog.w>0)c.a*=exp(-_FluidFog.w*i.distance);
    else if(_FogOn>.5)c.rgb=ApplyWorldFog(c.rgb,i.distance,float4(0,0,0,0));
    return c;
   }
   ENDCG
  }
 }
}

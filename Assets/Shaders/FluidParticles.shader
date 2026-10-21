Shader "Voxel/FluidParticles"
{
    Properties { _MainTex ("Particle",2D)="white" {} }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "WorldFog.cginc"
            sampler2D _MainTex;float4 _FluidFog;
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;float distance:TEXCOORD1;};
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;o.distance=length(UnityObjectToViewPos(v.vertex));return o;}
            fixed4 frag(v2f i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv)*i.color;c.rgb=ApplyWorldFog(c.rgb,i.distance,_FluidFog);return c;}
            ENDCG
        }
    }
}

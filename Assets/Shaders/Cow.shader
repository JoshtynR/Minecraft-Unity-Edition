Shader "Voxel/Cow"
{
    Properties { _MainTex ("Animal skin",2D)="white" {} _Tint("Tint",Color)=(1,1,1,1) _Brightness ("Voxel light",Float)=1 _Hurt ("Hurt flash",Float)=0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "WorldFog.cginc"
            sampler2D _MainTex;float _Brightness,_Hurt;float4 _FluidFog,_Tint;
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float3 normal:NORMAL;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float light:TEXCOORD1;float distance:TEXCOORD2;};
            v2f vert(appdata v)
            {
                v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.distance=length(UnityObjectToViewPos(v.vertex));
                float3 n=UnityObjectToWorldNormal(v.normal);
                float shade=n.x*n.x*.6+n.z*n.z*.8+n.y*n.y*(n.y<0?.5:1);
                o.light=_Brightness*shade;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv)*_Tint;clip(c.a-.1);
                float3 rgb=lerp(c.rgb*i.light,float3(1,.2,.2),_Hurt*.45);
                return fixed4(ApplyWorldFog(rgb,i.distance,_FluidFog),1);
            }
            ENDCG
        }
    }
}

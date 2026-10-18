Shader "Voxel/ModernChest"
{
    Properties { _MainTex ("Chest skin",2D)="white" {} _Brightness ("Voxel light",Float)=1 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float _Brightness;
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float3 normal:NORMAL;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float light:TEXCOORD1;};
            v2f vert(appdata v)
            {
                v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;
                float3 n=abs(UnityObjectToWorldNormal(v.normal));
                float shade=n.y>.5?(UnityObjectToWorldNormal(v.normal).y<0?.5:1):n.x>.5?.6:.8;
                o.light=_Brightness*shade;return o;
            }
            fixed4 frag(v2f i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv);clip(c.a-.1);return fixed4(c.rgb*i.light,1);}
            ENDCG
        }
    }
}

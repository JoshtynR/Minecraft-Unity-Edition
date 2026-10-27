Shader "Hidden/MinecraftPortalScreen"
{
    Properties
    {
        _MainTex ("Screen", 2D) = "white" {}
        _PortalTex ("Portal", 2D) = "black" {}
        _Intensity ("Intensity", Range(0,1)) = 0
        _PortalRows ("Rows", Float) = 1
        _PortalFrame ("Frame", Float) = 0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex,_PortalTex;
            float _Intensity,_PortalRows,_PortalFrame;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 centered=i.uv-.5;
                float strength=_Intensity*_Intensity;
                // Beta rotates a non-uniform camera scale around the view axis.
                // Reproduce that transform in screen space; its amplitude grows
                // non-linearly during the four seconds spent inside the portal.
                float axis=_Time.y*6.981317;
                float s=sin(axis),c=cos(axis);
                float2 warped=float2(centered.x*c-centered.y*s,centered.x*s+centered.y*c);
                float shape=5/(strength+5)-_Intensity*.04;
                float stretch=1/max(.35,shape*shape);
                warped.x/=stretch;
                warped=float2(warped.x*c+warped.y*s,-warped.x*s+warped.y*c);
                warped+=.5;
                fixed4 scene=tex2D(_MainTex,warped);

                float rows=max(1,_PortalRows);
                float2 portalUV=i.uv;
                // One crisp 16x16 animation frame fills the view, as in the
                // classic in-portal block overlay rather than sampling the strip.
                portalUV.y=(1-(_PortalFrame+1)/rows)+portalUV.y/rows;
                fixed4 portal=tex2D(_PortalTex,portalUV);
                float alpha=saturate((.2+.8*strength)*portal.a);
                return fixed4(lerp(scene.rgb,portal.rgb,alpha),scene.a);
            }
            ENDCG
        }
    }
}

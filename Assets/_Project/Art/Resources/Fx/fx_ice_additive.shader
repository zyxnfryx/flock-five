// b129: additive GUI blit for the badger thaw shatter (chunks + glow add their own icy light, never mix the backdrop).
// Graphics.DrawTexture passes the modulate colour as vertex colour with 0.5 = neutral, hence the x2.
Shader "FlockFive/IceAdditive"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct a2v { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            v2f vert (a2v v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * 2.0; return o; }
            fixed4 frag (v2f i) : SV_Target { fixed4 t = tex2D(_MainTex, i.uv); return fixed4(t.rgb * i.color.rgb, t.a * i.color.a); }
            ENDCG
        }
    }
}

// Chars a branch sprite from a hit point outward. _Spread is how far the
// ash has traveled in UV.x. A thin ember sits on the moving front.
Shader "FlockFive/WoodChar"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Spread ("Spread", Range(0, 1.2)) = 0
        _Hit ("Hit", Range(0, 1)) = 0.5
        // 0 whole limb, 1 trunk half, 2 tip half, 3 jagged stump sliver.
        _Mask ("Mask", Float) = 0
        _Seed ("Seed", Float) = 0
        _Clip ("Clip", Range(0, 1)) = 0.16
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Spread;
            float _Hit;
            float _Mask;
            float _Seed;
            float _Clip;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                float u = i.uv.x;
                float v = i.uv.y;
                float jag = frac(sin(v * 47.3 + _Seed * 1.7) * 127.1) - 0.5;
                float edge = 0.5 + jag * 0.09;
                if (_Mask > 0.5 && _Mask < 1.5)
                {
                    if (u > edge) discard;
                }
                else if (_Mask > 1.5 && _Mask < 2.5)
                {
                    if (u < edge) discard;
                }
                else if (_Mask > 2.5)
                {
                    float reach = _Clip * (0.55 + 0.9 * frac(sin(v * 61.0 + _Seed) * 91.7));
                    if (u > reach) discard;
                    edge = reach;
                }

                float d = abs(u - _Hit);
                float charAmt = saturate((_Spread - d) / 0.16);
                if (_Mask > 0.5) charAmt = 1.0;
                float front = exp(-(d - _Spread) * (d - _Spread) * 380.0);
                if (_Mask > 0.5 && _Mask < 2.5)
                    front = exp(-(u - edge) * (u - edge) * 900.0);
                else if (_Mask > 2.5)
                    front = exp(-(u - edge) * (u - edge) * 1400.0);
                fixed3 ash = fixed3(0.07, 0.028, 0.015);
                fixed3 grain = tex.rgb * fixed3(0.28, 0.12, 0.05);
                fixed3 ember = fixed3(1.0, 0.42, 0.06);
                fixed3 col = lerp(tex.rgb, ash, charAmt);
                col = lerp(col, grain, 0.42 * charAmt);
                float fringe = front * saturate(_Spread * 3.0) * (1.0 - charAmt * 0.45);
                if (_Mask > 2.5)
                {
                    // A stump has to read as bark, not as a black gap under the embers.
                    fixed3 bark = tex.rgb * fixed3(0.62, 0.34, 0.16);
                    col = lerp(bark, ash, 0.28);
                    fringe = saturate(front * 0.55);
                }
                else if (_Mask > 0.5)
                    fringe = saturate(front * 1.45);
                col = lerp(col, ember, saturate(fringe));
                col *= i.color.rgb;
                return fixed4(col, tex.a * i.color.a);
            }
            ENDCG
        }
    }
    Fallback Off
}

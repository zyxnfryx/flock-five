// Collectible-card slab (built-in render pipeline, unlit, Gamma-friendly).
// _Mode 0 = card face (front or back), 1 = brass edge wall.
// Faces: the captured flat card, a gentle glare band that follows _Tilt, and a faint tilt-driven prism for foil finishes.
// Nothing here reads _Time: with the tilt held still the card is still. The jewel rect (_Jewel, uv) is masked out of the
// glare/prism so the rarity jewel stays one clean oval.
Shader "FlockFive/CardFoil"
{
    Properties
    {
        _MainTex ("Face", 2D) = "white" {}
        _Mode ("Mode", Float) = 0
        _Tilt ("Tilt", Vector) = (0,0,0,0)
        _Glare ("Glare strength", Range(0,1)) = 0.6
        _Foil ("Foil (0 normal, 1 holo, 2 inverse)", Float) = 0
        _Jewel ("Jewel uv rect (x0,y0,x1,y1)", Vector) = (0.38,0.0,0.62,0.06)
        _Edge ("Edge brass", Color) = (0.88,0.66,0.24,1)
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Back
        Lighting Off
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Mode, _Glare, _Foil;
            float4 _Tilt, _Jewel;
            fixed4 _Edge;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 nrm : TEXCOORD1; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.nrm = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            float3 hue(float h)
            {
                float3 k = float3(1.0, 2.0/3.0, 1.0/3.0);
                return saturate(abs(frac(h + k) * 6.0 - 3.0) - 1.0);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 t = _Tilt.xy;
                if (_Mode > 0.5)
                {
                    // Brass wall: lambert from a fixed key light nudged by tilt, brushed grain along the edge.
                    float3 L = normalize(float3(-0.35 + t.x * 0.4, 0.55 + t.y * 0.3, -0.75));
                    float d = saturate(dot(normalize(i.nrm), L));
                    float grain = 0.94 + 0.06 * sin(i.uv.x * 260.0);
                    float rim = 0.68 + 0.32 * sin(saturate(i.uv.y) * 3.14159);
                    float3 c = _Edge.rgb * (0.45 + 0.75 * d) * grain * rim;
                    return fixed4(c, 1);
                }
                fixed4 col = tex2D(_MainTex, i.uv);
                // glare: one gaussian across a 28 degree axis, centre follows tilt
                float2 p = i.uv - 0.5;
                float ca = 0.8829, sa = 0.4695;                       // cos/sin 28 deg
                float along = p.x * ca + p.y * sa * 0.714;            // aspect 1:1.4
                float centre = (0.45 + t.x * 0.75 - t.y * 0.25) - 0.5;
                float gq = (along - centre) / 0.20;
                float g = exp(-gq * gq);
                float2 jc = (_Jewel.xy + _Jewel.zw) * 0.5;
                float2 jh = (_Jewel.zw - _Jewel.xy) * 0.5 + 0.02;
                float2 jd = abs(i.uv - jc) / jh;
                float jmask = 1.0 - smoothstep(0.85, 1.25, max(jd.x, jd.y));
                float k = _Glare * jmask;
                float3 tint = float3(1,1,1);
                float3 prism = 0;
                if (_Foil > 0.5)
                {
                    float h = frac(0.55 + t.x * 0.55 + t.y * 0.30 + i.uv.x * 0.25);
                    tint = lerp(float3(1,1,1), hue(h), _Foil > 1.5 ? 0.55 : 0.40);
                    prism = hue(frac(i.uv.x * 0.7 + i.uv.y * 0.9 + t.x * 0.8 + t.y * 0.45)) * (_Foil > 1.5 ? 0.09 : 0.07) * jmask;
                }
                float ga = g * k * (_Foil > 0.5 ? 0.36 : 0.28);
                col.rgb = col.rgb * (1.0 - ga) + tint * ga + prism * col.rgb;
                col.a = 1;
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}

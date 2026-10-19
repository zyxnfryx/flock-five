// Masks foliage on a garden painting and rotates those texels around the trunk crown.
// Displacement is zero at the pivot and at the mask edge, so the paint does not seam.
// _Wipe (w > 0.5) feathers the sprite away for a season crossfade. Sway is off when radii are 0.
Shader "FlockFive/FoliageSway"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _P0 ("P0", Vector) = (0,0,0,0)
        _P1 ("P1", Vector) = (0,0,0,0)
        _P2 ("P2", Vector) = (0,0,0,0)
        _P3 ("P3", Vector) = (0,0,0,0)
        _A0 ("A0", Vector) = (0,1,0,0)
        _A1 ("A1", Vector) = (0,1,0,0)
        _A2 ("A2", Vector) = (0,1,0,0)
        _A3 ("A3", Vector) = (0,1,0,0)
        _Wind ("Wind", Vector) = (0,0,0,0)
        _Wipe ("Wipe", Vector) = (0,0.12,0,0)
        _SwayMask ("Foliage mask", 2D) = "white" {}
        _SwayLegacy ("Legacy blend (clips only)", Float) = 0
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
            sampler2D _SwayMask;
            fixed4 _Color;
            float4 _P0, _P1, _P2, _P3;
            float4 _A0, _A1, _A2, _A3;
            float4 _Wind;
            float4 _Wipe;
            float _SwayLegacy;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            // p.xy pivot uv, p.z max radians, p.w radius. a.xy aim, a.z cos(half-angle), a.w phase.
            float2 Sway (float2 uv, float4 p, float4 a, float gust, float t)
            {
                if (p.w < 0.001) return uv;
                float2 d = uv - p.xy;
                float dist2 = dot(d, d);
                float dist = sqrt(dist2);
                float feather = smoothstep(p.w, p.w * 0.72, dist);
                if (feather <= 0.0001) return uv;
                float2 dir = d * rsqrt(max(dist2, 1e-8));
                float facing = dot(dir, a.xy);
                float sector = smoothstep(a.z, a.z + (1.0 - a.z) * 0.55, facing);
                float along = saturate(dist / p.w);
                float wave = sin(t * 0.72 + a.w) + 0.36 * sin(t * 1.31 + a.w * 1.7);
                float theta = p.z * along * feather * sector * wave * (1.0 + gust * 0.85);
                float cs = cos(theta);
                float sn = sin(theta);
                return p.xy + float2(cs * d.x - sn * d.y, sn * d.x + cs * d.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float speed = _Wind.y < 0.05 ? 1.0 : _Wind.y;
                float t = (_Wind.z > 0.0 ? _Wind.z : _Time.y) * speed;
                float gust = saturate(_Wind.x);
                float2 uv = i.uv;
                uv = Sway(uv, _P0, _A0, gust, t);
                uv = Sway(uv, _P1, _A1, gust, t);
                uv = Sway(uv, _P2, _A2, gust, t);
                uv = Sway(uv, _P3, _A3, gust, t);
                // Build 85: only foliage moves. Sky, clouds, moon/sun and the far background stay
                // rigid; a texel is displaced only if it is foliage or would show foliage.
                // b87: hard cutouts both ways. A texel moves only if it IS foliage and the paint
                // it would show is foliage too, so sky (and clouds) between leaves never move.
                float mHere = tex2D(_SwayMask, i.uv).r;
                float mFrom = tex2D(_SwayMask, saturate(uv)).r;
                float leaf = step(0.5, mHere) * step(0.5, mFrom);
                if (_SwayLegacy > 0.5) leaf = max(mHere, mFrom);
                uv = lerp(i.uv, uv, leaf);
                fixed4 col = tex2D(_MainTex, saturate(uv)) * i.color;
                if (_Wipe.w > 0.5)
                {
                    // z = mode: 0 from top, 1 from the left side, 2 from the bottom, 3 from the horizon (middle out).
                    float coord = 1.0 - i.uv.y;
                    if (_Wipe.z > 2.5) coord = abs(i.uv.y - 0.5) * 2.0;
                    else if (_Wipe.z > 1.5) coord = i.uv.y;
                    else if (_Wipe.z > 0.5) coord = i.uv.x;
                    col.a *= smoothstep(_Wipe.x - _Wipe.y, _Wipe.x + _Wipe.y, coord);
                }
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}

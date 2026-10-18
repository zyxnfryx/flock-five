// One bead on the screen glass (a teardrop once gathered, point up). The quad and the color copy share pos01
// (bottom-left, same as the sim). Inside the circle the sample is a
// magnifying lens that crosses the center a little, so the garden behind the
// drop is enlarged and slightly flipped. A thin rim and a glint sit on top.
// Pixels outside the drop and its short trail write alpha 0.
Shader "FlockFive/StormGlass"
{
    Properties
    {
        _BlitTexture ("", 2D) = "white" {}
        _BlitScaleBias ("", Vector) = (1, 1, 0, 0)
        _BlitScaleBiasRt ("", Vector) = (1, 1, 0, 0)
        _BlitMipLevel ("", Float) = 0
        _Bead ("", Vector) = (0.5, 0.5, 1, 0)
        _Rad ("", Vector) = (0.02, 0.02, 0, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "StormGlassBead"
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex VertQuad
            #pragma fragment Frag
            #pragma target 3.5

            float4 _Bead; // xy center in pos01, z alpha, w splat (1 impact, 0 bead)
            float4 _Rad;  // xy circle half-size in pos01, zw trail end offset

            float3 SampleScene(float2 pos01)
            {
                // A little soft, so a trunk edge inside the drop reads as water
                // rather than a cutout.
                float2 t = _BlitTexture_TexelSize.xy * 2.0;
                float3 c = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, pos01, _BlitMipLevel).rgb * 0.4;
                c += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, pos01 + float2(t.x, 0), _BlitMipLevel).rgb * 0.15;
                c += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, pos01 - float2(t.x, 0), _BlitMipLevel).rgb * 0.15;
                c += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, pos01 + float2(0, t.y), _BlitMipLevel).rgb * 0.15;
                c += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, pos01 - float2(0, t.y), _BlitMipLevel).rgb * 0.15;
                return c;
            }

            // A residual droplet on the wet trail. Same lens as the bead, much smaller.
            void TrailBead(float2 from, float2 rad, float2 at, float radMul, float body, float beadA, inout float a, inout float3 scene)
            {
                float2 local = from - at;
                float2 bn = local / max(rad * radMul, float2(1e-5, 1e-5));
                float br = length(bn);
                float disc = smoothstep(1.08, 0.55, br) * (1.0 - body);
                float ba = disc * beadA * 0.95;
                if (ba <= a) return;
                float reach = lerp(1.65, 2.15, saturate(br * br));
                scene = SampleScene(_Bead.xy + at - local * reach);
                scene *= 1.08;
                float rim = smoothstep(0.78, 1.0, br);
                scene = lerp(scene, scene * float3(0.90, 0.93, 0.97), rim);
                float2 spec = bn - float2(-0.32, 0.34);
                scene += exp(-dot(spec, spec) * 80.0) * 0.3 * disc * beadA * beadA;
                a = ba;
            }

            float4 Frag(Varyings input) : SV_Target0
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 pos = input.texcoord.xy;
                float2 rad = max(_Rad.xy, float2(1e-5, 1e-5));
                float2 from = pos - _Bead.xy;
                float2 n = from / rad;
                float splat = saturate(_Bead.w);
                // Teardrop body (mirrors StormGlass.TearK): pointed top, round belly at the bottom.
                // The impact splat stays a round disc; it gathers into the drop as splat -> 0.
                float tearU = saturate((1.0 - n.y) * 0.5);
                float tearK = lerp(1.0, pow(tearU, 0.8) * 1.12 + 0.04, 1.0 - splat);
                float2 nt = float2(n.x / tearK, n.y);
                float r = length(nt);
                float body = smoothstep(1.015, 0.96, r);
                // Specks arrive with w < 0. They get no glint, so a fading speck never leaves a white dot.
                float glint = _Bead.w < -0.5 ? 0.0 : 1.0;
                // Every highlight dies with the bead alpha, squared, so the last frames of a fade are plain glass.
                float fade = _Bead.z * _Bead.z;

                // One continuous inverted lens once the drop has gathered.
                // At the impact the reach eases off so the splat reads flat,
                // then the garden flips and enlarges inside the bead.
                float reach = lerp(1.85, 2.35, r * r);
                reach = lerp(reach, 0.28, splat);
                float3 scene = SampleScene(_Bead.xy - from * reach);
                scene *= lerp(1.07, 1.14, splat);

                float rim = smoothstep(0.94, 1.0, r) * body;
                scene = lerp(scene, scene * float3(0.86, 0.90, 0.96), rim * (1.0 - splat * 0.35));
                float lip = smoothstep(0.93, 1.0, r) * body * (1.0 - splat);
                scene += lip * float3(0.78, 0.88, 0.98) * 0.5 * fade;

                float ring = smoothstep(0.30, 0.55, r) * smoothstep(1.08, 0.70, r) * splat;
                scene += ring * float3(1.0, 0.98, 0.96) * 0.55 * fade;
                float splashEdge = smoothstep(0.72, 0.98, r) * splat * body;
                scene += splashEdge * 0.30 * fade;

                float2 spec = n - float2(-0.30, 0.36);
                float hot = exp(-dot(spec, spec) * 72.0);
                scene += hot * 0.9 * float3(1.0, 0.99, 0.96) * body * (1.0 - splat) * fade * glint;
                float2 spec2 = n - float2(-0.08, 0.12);
                scene += exp(-dot(spec2, spec2) * 200.0) * 0.3 * body * (1.0 - splat) * fade * glint;

                float a = _Bead.z * body;

                float2 trail = _Rad.zw;
                float trailLen2 = dot(trail, trail);
                if (trailLen2 > 1e-10)
                {
                    float along = dot(from, trail) / trailLen2;
                    float2 side = (from - trail * along) / rad;
                    // A thin sheen, not a white bar. Garden still shows through.
                    // Teardrop: the tail leaves the round head nearly as wide as it, then narrows to a thread.
                    float halfW = lerp(0.62, 0.05, pow(saturate(along), 0.55));
                    float streak = smoothstep(0.0, 0.05, along) * smoothstep(1.05, 0.32, along);
                    streak *= smoothstep(halfW, halfW * 0.22, length(side));
                    streak *= (1.0 - body) * _Bead.z;
                    if (streak > a)
                    {
                        float3 sheen = SampleScene(pos) * float3(1.10, 1.14, 1.22);
                        sheen += float3(0.42, 0.55, 0.68) * 0.40;
                        float core = smoothstep(halfW * 0.65, 0.0, length(side));
                        sheen += core * float3(0.85, 0.92, 1.0) * 0.22;
                        // Near the head the tail is liquid too: the same inverted lens, fading into the sheen.
                        float liquid = smoothstep(0.55, 0.0, along);
                        float3 lens = SampleScene(_Bead.xy - from * 1.35) * 1.08;
                        scene = lerp(sheen, lens, liquid);
                        a = streak * lerp(0.68, 0.92, liquid);
                    }

                    // Tiny beads riding the wet line, largest near the drop.
                    TrailBead(from, rad, trail * 0.30, 0.34, body, _Bead.z, a, scene);
                    TrailBead(from, rad, trail * 0.58, 0.24, body, _Bead.z, a, scene);
                    TrailBead(from, rad, trail * 0.84, 0.16, body, _Bead.z, a, scene);
                }

                if (a < 0.003)
                    return float4(0, 0, 0, 0);
                return float4(min(scene, float3(1.0, 1.0, 1.0)), a);
            }
            ENDHLSL
        }
    }
}

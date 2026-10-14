// One bead on the screen glass. The quad and the color copy share pos01
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

            float4 Frag(Varyings input) : SV_Target0
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 pos = input.texcoord.xy;
                float2 rad = max(_Rad.xy, float2(1e-5, 1e-5));
                float2 from = pos - _Bead.xy;
                float2 n = from / rad;
                float r = length(n);
                float body = smoothstep(1.015, 0.96, r);
                float splat = saturate(_Bead.w);

                // One continuous inverted lens once the drop has gathered.
                // At the impact the reach eases off so the splat reads flat,
                // then the garden flips and enlarges inside the bead.
                float reach = lerp(1.85, 2.35, r * r);
                reach = lerp(reach, 0.28, splat);
                float3 scene = SampleScene(_Bead.xy - from * reach);
                scene *= lerp(1.07, 1.14, splat);

                float rim = smoothstep(0.975, 1.0, r) * body;
                scene = lerp(scene, scene * float3(0.90, 0.92, 0.95), rim * (1.0 - splat * 0.35));

                float ring = smoothstep(0.55, 0.74, r) * smoothstep(1.02, 0.84, r) * splat;
                scene += ring * float3(1.0, 0.98, 0.96) * 0.42;

                float2 spec = n - float2(-0.30, 0.36);
                float hot = exp(-dot(spec, spec) * 140.0);
                scene += hot * 1.2 * float3(1.0, 0.99, 0.96) * body * (1.0 - splat);

                float a = _Bead.z * body;

                float2 trail = _Rad.zw;
                float trailLen2 = dot(trail, trail);
                if (trailLen2 > 1e-10)
                {
                    float along = dot(from, trail) / trailLen2;
                    float2 side = (from - trail * along) / rad;
                    float halfW = lerp(0.22, 0.035, saturate(along));
                    float streak = smoothstep(0.02, 0.10, along) * smoothstep(1.12, 0.62, along);
                    streak *= smoothstep(halfW, halfW * 0.28, length(side));
                    streak *= (1.0 - body) * _Bead.z * 0.38;
                    if (streak > a)
                    {
                        scene = SampleScene(pos) * float3(1.03, 1.05, 1.08);
                        a = streak;
                    }
                }

                if (a < 0.003)
                    return float4(0, 0, 0, 0);
                return float4(scene, a);
            }
            ENDHLSL
        }
    }
}

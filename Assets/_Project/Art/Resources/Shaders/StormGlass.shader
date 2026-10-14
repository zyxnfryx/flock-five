// One bead on the screen glass. The quad covers the bead; the sample is a
// slight magnifying lens of the copy behind it, with a thin dark rim and a
// specular highlight. Pixels outside the circle write alpha 0.
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

            float4 _Bead; // xy center UV, z alpha
            float4 _Rad;  // xy half-size in UV

            float4 Frag(Varyings input) : SV_Target0
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord.xy;
                float2 rad = max(_Rad.xy, float2(1e-5, 1e-5));
                float2 n = (uv - _Bead.xy) / rad;
                float r = length(n);
                float edge = smoothstep(1.005, 0.96, r);
                float a = _Bead.z * edge;
                if (a < 0.003)
                    return float4(0, 0, 0, 0);

                float bulge = sqrt(saturate(1.0 - min(r * r, 1.0)));
                float pull = 0.32 * bulge;
                float2 sampleUv = _Bead.xy + (uv - _Bead.xy) * (1.0 - pull);
                float3 lens = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, sampleUv, _BlitMipLevel).rgb;

                // A hairline shadow, not a drawn ring.
                float rim = smoothstep(0.93, 1.0, r);
                lens = lerp(lens, lens * float3(0.38, 0.42, 0.48), rim);
                lens *= lerp(1.05, 1.0, saturate(r));

                float2 spec = n - float2(-0.28, 0.32);
                float hot = exp(-dot(spec, spec) * 40.0);
                float soft = exp(-dot(spec, spec) * 8.0);
                lens += (hot * 0.70 + soft * 0.22) * float3(1.0, 0.985, 0.93);

                return float4(lens, a);
            }
            ENDHLSL
        }
    }
}

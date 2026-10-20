// Jagged lightning. Vertex colors only: no sprite texture, no unity_SpriteProps.
// A sprite-material mesh in this project collapses or paints the texture's
// corner and leaves the rest of the quad the border color (white or black).
// This pass adds the bolt in world space so the garden behind it stays full frame.
Shader "FlockFive/Bolt"
{
    Properties
    {
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One One

        Pass
        {
            Name "Bolt"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color * _Tint;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(i.color);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

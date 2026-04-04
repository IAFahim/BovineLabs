// Mostly from Unity.Physics
Shader "hidden/BovineLabs/Line"
{
    Properties {}

    // URP specific SubShader
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal": "17.0"
        }

        Tags
        {
            "IgnoreProjector" = "True"
            "RenderType" = "Overlay"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZWrite off
            ZTest Always
            Cull off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct line_data {
                float4 begin;
                float4 end;
                float4 color;
                float4 padding;
            };

            StructuredBuffer<line_data> position_buffer;
            int _BaseVertex;

            struct v2f {
                float4 pos : SV_POSITION;
                float4 color : TEXCOORD0;
            };

            v2f vert(uint vid : SV_VertexID)
            {
                const line_data id = position_buffer[(vid >> 1) + _BaseVertex];
                const float4 pos = (vid & 1) ? id.end : id.begin;

                v2f o;
                o.pos = TransformObjectToHClip(pos.xyz);
                o.color = id.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDHLSL
        }
    }

    // HDRP specific SubShader
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.high-definition": "17.0"
        }

        Tags
        {
            "IgnoreProjector" = "True"
            "RenderType" = "Overlay"
            "RenderPipeline" = "HDRenderPipeline"
        }

        Pass
        {
            ZWrite off
            ZTest Always
            Cull off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            float4 ObjectToHClip(float3 pos)
            {
                return mul(GetObjectToWorldMatrix(), float4(pos, 1.0));
            }

            struct line_data {
                float4 begin;
                float4 end;
                float4 color;
                float4 padding;
            };

            StructuredBuffer<line_data> position_buffer;
            int _BaseVertex;

            struct v2f {
                float4 pos : SV_POSITION;
                float4 color : TEXCOORD0;
            };

            v2f vert(uint vid : SV_VertexID)
            {
                const line_data id = position_buffer[(vid >> 1) + _BaseVertex];
                const float4 pos = (vid & 1) ? id.end : id.begin;
                const float4 world_pos = float4(pos.xyz, 1);

                v2f o;
                float4 worldPos = ObjectToHClip(pos.xyz);
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.color = id.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDHLSL
        }
    }
}

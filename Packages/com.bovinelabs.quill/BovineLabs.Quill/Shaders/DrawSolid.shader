Shader "hidden/BovineLabs/Solid"
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
            "RenderType" = "Transparent"
            "Queue" = "Transparent+1"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZWrite on
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Offset -0.2, -1
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct mesh_data {
                float4 vertex0;
                float4 vertex1;
                float4 vertex2;
                float4 color;
            };

            StructuredBuffer<mesh_data> mesh_buffer;
            int _BaseVertex;

            struct v2f {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert(uint vid : SV_VertexID)
            {
                float4 pos;
                const uint offset = vid / 3;
                const uint vertex = vid % 3;

                if (vertex == 0) pos = mesh_buffer[offset + _BaseVertex].vertex0;
                else if (vertex == 1) pos = mesh_buffer[offset + _BaseVertex].vertex1;
                else // if (vertex==2)
                    pos = mesh_buffer[offset + _BaseVertex].vertex2;

                v2f o;
                o.pos = TransformObjectToHClip(pos.xyz);
                o.color = mesh_buffer[offset + _BaseVertex].color;
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
            "RenderType" = "Transparent"
            "Queue" = "Transparent+1"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "HDRenderPipeline"
        }

        Pass
        {
            ZWrite on
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Offset -0.2, -1
            Cull Off

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

            struct mesh_data {
                float4 vertex0;
                float4 vertex1;
                float4 vertex2;
                float4 color;
            };

            StructuredBuffer<mesh_data> mesh_buffer;
            int _BaseVertex;

            struct v2f {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert(uint vid : SV_VertexID)
            {
                float4 pos;
                const uint offset = vid / 3;
                const uint vertex = vid % 3;

                if (vertex == 0) pos = mesh_buffer[offset + _BaseVertex].vertex0;
                else if (vertex == 1) pos = mesh_buffer[offset + _BaseVertex].vertex1;
                else // if (vertex==2)
                    pos = mesh_buffer[offset + _BaseVertex].vertex2;

                v2f o;
                float4 worldPos = ObjectToHClip(pos.xyz);
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.color = mesh_buffer[offset + _BaseVertex].color;
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

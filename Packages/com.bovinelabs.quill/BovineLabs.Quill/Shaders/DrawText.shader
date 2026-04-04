Shader "hidden/BovineLabs/Text"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FallbackTex ("Texture", 2D) = "white" {}
        _TextureWidth ("Texture Width", Integer) = 1024
        _Smoothness ("Edge Smoothness", Range(0.0, 0.5)) = 0.1
        _Thickness ("Text Thickness", Range(-0.5, 0.5)) = 0.0
    }

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
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Offset -0.2, -1
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_FallbackTex);
            SAMPLER(sampler_FallbackTex);

            int _TextureWidth;
            float _Smoothness;
            float _Thickness;

            // Improved signed distance field rendering with enhanced smoothness effect
            float sdf_alpha(float sdf_value)
            {
                // Apply thickness adjustment to the threshold
                float adjusted_threshold = 0.5 - _Thickness;

                // Direct control over edge smoothness
                float edge_width = _Smoothness;

                // Apply smoother step function with adjusted threshold and edge width
                return smoothstep(
                    adjusted_threshold - edge_width,
                    adjusted_threshold + edge_width,
                    sdf_value);
            }

            float get_main(float2 uv)
            {
                // Sample the signed distance field
                float sdf_value = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                return sdf_alpha(sdf_value);
            }

            float get_alpha(float2 uv)
            {
                // Calculate the pixel size for mip selection
                float pixel_size = length(float2(ddx(uv.x), ddy(uv.x)));

                // Get high-quality SDF result for close-up viewing
                float high_quality = get_main(uv);

                // Only fall back to the lower quality texture when necessary
                if (pixel_size * _TextureWidth < 1.5)
                {
                    return high_quality;
                }

                // Get low-quality fallback for distant viewing
                float fallback = SAMPLE_TEXTURE2D_BIAS(_FallbackTex, sampler_FallbackTex, uv, -1).a;

                // Scale the fallback slightly to match the main texture better
                fallback *= 1.15;

                // Create smooth transition between the two textures
                float blend_factor = smoothstep(1.0, 2.0, pixel_size * _TextureWidth);
                return lerp(high_quality, fallback, blend_factor);
            }

            struct text_data {
                float4 vertex0;
                float4 vertex1;
                float4 vertex2;
                float4 vertex3;
                float4 color;
                float4 uv01;
                float4 uv23;
                float4 padding;
            };

            StructuredBuffer<text_data> text_buffer;
            int _BaseVertex;

            struct v2f {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(uint vid : SV_VertexID)
            {
                float4 pos;
                float2 uv;
                const uint offset = (vid / 4) + _BaseVertex;
                const uint vertex = vid % 4;

                const text_data td = text_buffer[offset];

                if (vertex == 0) pos = td.vertex0;
                else if (vertex == 1) pos = td.vertex1;
                else if (vertex == 2) pos = td.vertex2;
                else // vertex==3
                    pos = td.vertex3;

                if (vertex == 0) uv = td.uv01.xy;
                else if (vertex == 1) uv = td.uv01.zw;
                else if (vertex == 2) uv = td.uv23.xy;
                else // vertex==3
                    uv = td.uv23.zw;

                v2f o;
                o.pos = TransformObjectToHClip(pos.xyz);
                o.uv = uv;
                o.color = td.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float alpha = get_alpha(i.uv);

                // Fix for the warning: ensure alpha is non-negative before applying pow
                alpha = max(0, alpha);

                // Apply slight gamma correction to make text more readable
                alpha = pow(alpha, 0.8);

                return i.color * float4(1, 1, 1, alpha);
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
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Offset -0.2, -1
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_FallbackTex);
            SAMPLER(sampler_FallbackTex);

            int _TextureWidth;
            float _Smoothness;
            float _Thickness;

            float4 ObjectToHClip(float3 pos)
            {
                return mul(GetObjectToWorldMatrix(), float4(pos, 1.0));
            }

            // Improved signed distance field rendering with enhanced smoothness effect
            float sdf_alpha(float sdf_value)
            {
                // Apply thickness adjustment to the threshold
                float adjusted_threshold = 0.5 - _Thickness;

                // Direct control over edge smoothness
                float edge_width = _Smoothness;

                // Apply smoother step function with adjusted threshold and edge width
                return smoothstep(
                    adjusted_threshold - edge_width,
                    adjusted_threshold + edge_width,
                    sdf_value);
            }

            float get_main(float2 uv)
            {
                // Sample the signed distance field
                float sdf_value = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                return sdf_alpha(sdf_value);
            }

            float get_alpha(float2 uv)
            {
                // Calculate the pixel size for mip selection
                float pixel_size = length(float2(ddx(uv.x), ddy(uv.x)));

                // Get high-quality SDF result for close-up viewing
                float high_quality = get_main(uv);

                // Only fall back to the lower quality texture when necessary
                if (pixel_size * _TextureWidth < 1.5)
                {
                    return high_quality;
                }

                // Get low-quality fallback for distant viewing
                float fallback = SAMPLE_TEXTURE2D_BIAS(_FallbackTex, sampler_FallbackTex, uv, -1).a;

                // Scale the fallback slightly to match the main texture better
                fallback *= 1.15;

                // Create smooth transition between the two textures
                float blend_factor = smoothstep(1.0, 2.0, pixel_size * _TextureWidth);
                return lerp(high_quality, fallback, blend_factor);
            }

            struct text_data {
                float4 vertex0;
                float4 vertex1;
                float4 vertex2;
                float4 vertex3;
                float4 color;
                float4 uv01;
                float4 uv23;
                float4 padding;
            };

            StructuredBuffer<text_data> text_buffer;
            int _BaseVertex;

            struct v2f {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(uint vid : SV_VertexID)
            {
                float4 pos;
                float2 uv;
                const uint offset = (vid / 4) + _BaseVertex;
                const uint vertex = vid % 4;

                const text_data td = text_buffer[offset];

                if (vertex == 0) pos = td.vertex0;
                else if (vertex == 1) pos = td.vertex1;
                else if (vertex == 2) pos = td.vertex2;
                else // vertex==3
                    pos = td.vertex3;

                if (vertex == 0) uv = td.uv01.xy;
                else if (vertex == 1) uv = td.uv01.zw;
                else if (vertex == 2) uv = td.uv23.xy;
                else // vertex==3
                    uv = td.uv23.zw;

                v2f o;
                float4 worldPos = ObjectToHClip(pos.xyz);
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.uv = uv;
                o.color = td.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float alpha = get_alpha(i.uv);

                // Fix for the warning: ensure alpha is non-negative before applying pow
                alpha = max(0, alpha);

                // Apply slight gamma correction to make text more readable
                alpha = pow(alpha, 0.8);

                return i.color * float4(1, 1, 1, alpha);
            }
            ENDHLSL
        }
    }
}

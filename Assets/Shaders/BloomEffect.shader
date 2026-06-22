Shader "Custom/BloomEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BloomIntensity ("Bloom Intensity", Range(0, 3)) = 1.0
        _BloomThreshold ("Bloom Threshold", Range(0, 2)) = 0.8
        _BloomBlurSize ("Bloom Blur Size", Range(1, 10)) = 4.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "BloomExtract"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float _BloomThreshold;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half brightness = dot(color.rgb, half3(0.2126, 0.7152, 0.0722));
                half bloomAmount = max(0, brightness - _BloomThreshold);
                half3 bloomColor = color.rgb * bloomAmount;
                return half4(bloomColor, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "GaussianBlur"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float _BloomBlurSize;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy * _BloomBlurSize;
                half4 color = half4(0, 0, 0, 0);
                float totalWeight = 0;
                
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        float2 offset = float2(x, y) * texelSize;
                        float weight = exp(-(x * x + y * y) / 2.0);
                        color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset) * weight;
                        totalWeight += weight;
                    }
                }
                
                return color / totalWeight;
            }
            ENDHLSL
        }

        Pass
        {
            Name "BloomCombine"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BloomTex);
            SAMPLER(sampler_BloomTex);
            float _BloomIntensity;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 originalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 bloomColor = SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, input.uv);
                half3 finalColor = originalColor.rgb + bloomColor.rgb * _BloomIntensity;
                finalColor = finalColor / (1 + finalColor);
                return half4(finalColor, originalColor.a);
            }
            ENDHLSL
        }
    }
}

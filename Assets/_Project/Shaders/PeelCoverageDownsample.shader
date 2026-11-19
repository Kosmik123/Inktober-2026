// Shrinks the peel mask to a small texture where every pixel is the average of the mask over its part of the area.
// The average over the small texture is the share of the apple that is peeled. Used with Graphics.Blit.
Shader "Hidden/Inktober/PeelCoverageDownsample"
{
    Properties
    {
        _MainTex ("Mask (R)", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Downsample"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Size of the output texture in pixels, and the number of samples per axis inside one output pixel.
            // With the samples two source pixels apart, every bilinear sample covers 2x2 pixels, so the average is exact.
            float _OutputSize;
            float _Taps;

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

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = float4(input.uv * 2.0 - 1.0, 0.0, 1.0);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 cell = floor(input.uv * _OutputSize) / _OutputSize;
                int taps = (int)_Taps;
                float sum = 0.0;
                for (int y = 0; y < taps; y++)
                {
                    for (int x = 0; x < taps; x++)
                    {
                        float2 offset = (float2(x, y) + 0.5) / _Taps / _OutputSize;
                        sum += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, cell + offset, 0).r;
                    }
                }

                float value = sum / (_Taps * _Taps);
                return half4(value, value, value, 1.0);
            }
            ENDHLSL
        }
    }
}

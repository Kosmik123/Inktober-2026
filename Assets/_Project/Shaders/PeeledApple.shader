// Draws the apple in two colours: skin where the mask is 0, flesh where it is 1.
// At the edge of the mask the normal is tilted as if the flesh lay lower than the skin (a dent).
Shader "Inktober/PeeledApple"
{
    Properties
    {
        _SkinColor ("Skin Color", Color) = (0.72, 0.07, 0.08, 1)
        _FleshColor ("Flesh Color", Color) = (1, 0.85, 0.25, 1)
        _SkinSmoothness ("Skin Smoothness", Range(0, 1)) = 0.55
        _FleshSmoothness ("Flesh Smoothness", Range(0, 1)) = 0.8
        [NoScaleOffset] _Mask ("Peel Mask (R)", 2D) = "black" {}
        _MaskResolution ("Mask Resolution", Float) = 2048
        _Threshold ("Color Threshold", Range(0.05, 0.95)) = 0.5
        _Depth ("Dent Depth (world units)", Range(0, 0.05)) = 0.006
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_Mask);
        SAMPLER(sampler_Mask);

        CBUFFER_START(UnityPerMaterial)
            half4 _SkinColor;
            half4 _FleshColor;
            half _SkinSmoothness;
            half _FleshSmoothness;
            float _MaskResolution;
            float _Threshold;
            float _Depth;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            float SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).r;
            }

            // Tilts the normal against the gradient of the mask, so the flesh (mask = 1) looks lower than the skin.
            // The gradient is converted to world units, so the dent has the same depth on every part of the apple.
            float3 DentNormal(float3 normalWS, float3 positionWS, float2 uv, float mask)
            {
                // At the UV seam the coordinates jump within one pixel quad, so the derivatives of the mask are meaningless there.
                float2 duv1 = ddx(uv);
                float2 duv2 = ddy(uv);
                if (max(max(abs(duv1.x), abs(duv2.x)), max(abs(duv1.y), abs(duv2.y))) > 0.25)
                    return normalWS;

                // Surface gradient from screen-space derivatives (Mikkelsen): find the vector g in the surface plane
                // with g . dp1 = change of the mask per pixel in x, and g . dp2 = change per pixel in y.
                float3 dp1 = ddx(positionWS);
                float3 dp2 = ddy(positionWS);
                float3 dp2perp = cross(dp2, normalWS);
                float3 dp1perp = cross(normalWS, dp1);
                float determinant = dot(dp1, dp2perp);
                if (abs(determinant) < 1e-12)
                    return normalWS;

                float3 gradient = (ddx(mask) * dp2perp + ddy(mask) * dp1perp) / determinant;
                return normalize(normalWS + _Depth * gradient);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float mask = SampleMask(input.uv);
                float flesh = smoothstep(_Threshold - 0.1, _Threshold + 0.1, mask);

                normalWS = DentNormal(normalWS, input.positionWS, input.uv, mask);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = lerp(_SkinColor.rgb, _FleshColor.rgb, flesh);
                surfaceData.smoothness = lerp(_SkinSmoothness, _FleshSmoothness, flesh);
                surfaceData.occlusion = 1;
                surfaceData.alpha = 1;
                surfaceData.normalTS = half3(0, 0, 1);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE * positionCS.w);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE * positionCS.w);
            #endif
                output.positionCS = positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}

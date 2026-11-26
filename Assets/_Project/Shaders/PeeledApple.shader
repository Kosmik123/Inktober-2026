// Draws the apple in two colours: skin where the mask is 0, flesh where it is 1.
// At the edge of the mask the normal is tilted as if the flesh lay lower than the skin (a dent).
Shader "Inktober/PeeledApple"
{
    Properties
    {
        _SkinColor ("Skin Color", Color) = (0.72, 0.07, 0.08, 1)
        _SkinMap ("Skin Texture (multiplied by the color)", 2D) = "white" {}
        [Normal] _SkinNormalMap ("Skin Normal Map", 2D) = "bump" {}
        _SkinNormalScale ("Skin Normal Scale", Float) = 1
        _SkinSmoothness ("Skin Smoothness", Range(0, 1)) = 0.55

        _FleshColor ("Flesh Color", Color) = (1, 0.85, 0.25, 1)
        _FleshMap ("Flesh Texture (multiplied by the color)", 2D) = "white" {}
        [Normal] _FleshNormalMap ("Flesh Normal Map", 2D) = "bump" {}
        _FleshNormalScale ("Flesh Normal Scale", Float) = 1
        _FleshSmoothness ("Flesh Smoothness", Range(0, 1)) = 0.8

        [Header(Uneven wetness of the flesh)]
        _FleshDryness ("Dry Patches (smoothness taken away)", Range(0, 1)) = 0.4
        _FleshPatchScale ("Patch Scale", Float) = 8
        _FleshSpotSmoothness ("Wet Spot Smoothness", Range(0, 1)) = 0.97
        _FleshSpotAmount ("Wet Spot Amount", Range(0, 1)) = 0.3
        _FleshSpotScale ("Wet Spot Scale", Float) = 80

        [Header(Fake subsurface scattering)]
        _ScatterColor ("Scatter Color", Color) = (1, 0.72, 0.42, 1)
        _ScatterWrap ("Light Wrap Width (flesh)", Range(0, 1)) = 0.6
        _ScatterStrength ("Light Wrap Strength (flesh)", Range(0, 2)) = 0.7
        _Translucency ("Translucency (thin things only)", Range(0, 2)) = 0
        _TranslucencyPower ("Translucency Focus", Range(1, 16)) = 4
        _TranslucencyDistortion ("Translucency Distortion", Range(0, 1)) = 0.3

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

        TEXTURE2D(_SkinMap);
        SAMPLER(sampler_SkinMap);
        TEXTURE2D(_SkinNormalMap);
        SAMPLER(sampler_SkinNormalMap);
        TEXTURE2D(_FleshMap);
        SAMPLER(sampler_FleshMap);
        TEXTURE2D(_FleshNormalMap);
        SAMPLER(sampler_FleshNormalMap);

        CBUFFER_START(UnityPerMaterial)
            half4 _SkinColor;
            half4 _FleshColor;
            float4 _SkinMap_ST;
            float4 _SkinNormalMap_ST;
            float4 _FleshMap_ST;
            float4 _FleshNormalMap_ST;
            half _SkinNormalScale;
            half _FleshNormalScale;
            half _SkinSmoothness;
            half _FleshSmoothness;
            float _MaskResolution;
            float _Threshold;
            float _Depth;
            half4 _ScatterColor;
            half _FleshDryness;
            half _FleshSpotSmoothness;
            half _FleshSpotAmount;
            float _FleshPatchScale;
            float _FleshSpotScale;
            half _ScatterWrap;
            half _ScatterStrength;
            half _Translucency;
            half _TranslucencyPower;
            half _TranslucencyDistortion;
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
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD4;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            float SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).r;
            }

            // Tilts the normal against the gradient of the mask, so the flesh (mask = 1) looks lower than the skin.
            // The gradient is converted to world units, so the dent has the same depth on every part of the apple.
            // normalWS is the normal to tilt (it may come from a normal map), surfaceNormalWS is the normal of the surface itself.
            //
            // The gradient of the mask is taken from samples of the texture, not from screen-space derivatives of the mask:
            // those are wrong for the pixels next to the border of a UV island (the seam, the poles), where the coordinates
            // of the neighbour pixels in a quad belong to another part of the texture. Only the screen-space derivatives of
            // the UV and of the position are used, to learn how the texture lies on the surface.
            float3 DentNormal(float3 normalWS, float3 surfaceNormalWS, float3 positionWS, float2 uv)
            {
                // At the UV seam the coordinates jump within one pixel quad, so their derivatives are meaningless there.
                float2 duv1 = ddx(uv);
                float2 duv2 = ddy(uv);
                if (max(max(abs(duv1.x), abs(duv2.x)), max(abs(duv1.y), abs(duv2.y))) > 0.25)
                    return normalWS;

                // Surface gradient from screen-space derivatives (Mikkelsen): for a value f that changes by ddx(f) and ddy(f)
                // per pixel, the gradient along the surface is (ddx(f) * dp2perp + ddy(f) * dp1perp) / determinant.
                float3 dp1 = ddx(positionWS);
                float3 dp2 = ddy(positionWS);
                float3 dp2perp = cross(dp2, surfaceNormalWS);
                float3 dp1perp = cross(surfaceNormalWS, dp1);
                float determinant = dot(dp1, dp2perp);
                if (abs(determinant) < 1e-12)
                    return normalWS;

                float3 gradientU = (duv1.x * dp2perp + duv2.x * dp1perp) / determinant;
                float3 gradientV = (duv1.y * dp2perp + duv2.y * dp1perp) / determinant;

                // Change of the mask per unit of U and V, from the texture itself.
                float texel = 1.0 / _MaskResolution;
                float dMaskDu = (SampleMask(uv + float2(texel, 0)) - SampleMask(uv - float2(texel, 0))) / (2.0 * texel);
                float dMaskDv = (SampleMask(uv + float2(0, texel)) - SampleMask(uv - float2(0, texel))) / (2.0 * texel);

                float3 gradient = dMaskDu * gradientU + dMaskDv * gradientV;
                return normalize(normalWS + _Depth * gradient);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w * GetOddNegativeScale());
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            float Hash(float2 cell)
            {
                float3 p = frac(float3(cell.xyx) * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            // Smooth noise from 0 to 1 that repeats along U, so it has no line at the UV seam (the U of the strip also goes on past 1).
            // The apple texture is twice as wide as high, so there are twice as many cells along U.
            // "fade" goes to 1 when the cells get smaller than a pixel, where the noise would only flicker.
            float SeamlessNoise(float2 uv, float scale, out float fade)
            {
                float cells = max(1.0, round(scale));
                float2 p = uv * float2(cells * 2.0, cells);
                float2 size = fwidth(p);
                fade = saturate(max(size.x, size.y) - 0.5);

                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float period = cells * 2.0;
                float x0 = cell.x - period * floor(cell.x / period);
                float x1 = cell.x + 1.0 - period * floor((cell.x + 1.0) / period);
                float bottom = lerp(Hash(float2(x0, cell.y)), Hash(float2(x1, cell.y)), f.x);
                float top = lerp(Hash(float2(x0, cell.y + 1.0)), Hash(float2(x1, cell.y + 1.0)), f.x);
                return lerp(bottom, top, f.y);
            }

            // The flesh is not wet everywhere alike: large drier patches and small wet spots that glint.
            half FleshSmoothness(float2 uv)
            {
                float patchFade;
                float patches = SeamlessNoise(uv, _FleshPatchScale, patchFade);
                half smoothness = saturate(_FleshSmoothness - _FleshDryness * (1.0 - smoothstep(0.25, 0.75, patches)));

                float spotFade;
                float spots = SeamlessNoise(uv + 0.37, _FleshSpotScale, spotFade);
                float threshold = 1.0 - _FleshSpotAmount;
                float wet = smoothstep(threshold - 0.08, threshold + 0.08, spots);
                wet = lerp(wet, _FleshSpotAmount * 0.5, spotFade);
                return lerp(smoothness, _FleshSpotSmoothness, wet);
            }

            // A cheap stand-in for the light that goes under the surface. It uses only the main light and no extra pass.
            half3 Scatter(float3 normalWS, float3 viewWS, Light light, half3 albedo, half flesh)
            {
                float facing = dot(normalWS, light.direction);

                // Flesh: the light reaches past the edge of the shadow side, in a warm colour.
                float wrapped = saturate((facing + _ScatterWrap) / (1.0 + _ScatterWrap));
                float past = max(0.0, wrapped * wrapped - saturate(facing) * saturate(facing));
                float amount = past * _ScatterStrength * flesh;

                // Thin things: the light from behind comes through, the most when looking towards it.
                float3 through = -normalize(light.direction + normalWS * _TranslucencyDistortion);
                float towards = pow(saturate(dot(viewWS, through)), _TranslucencyPower);
                amount += (saturate(-facing) * 0.5 + towards * 0.5) * _Translucency;

                // No shadow here: the side turned from the light is always in its own shadow, and that is the side that scatters.
                return albedo * _ScatterColor.rgb * light.color * (light.distanceAttenuation * amount);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 surfaceNormalWS = normalize(input.normalWS);
                float3 normalWS = surfaceNormalWS;
                float mask = SampleMask(input.uv);
                float flesh = smoothstep(_Threshold - 0.1, _Threshold + 0.1, mask);

                // Both layers are sampled and blended by the same weight as the colors.
                float2 skinUV = TRANSFORM_TEX(input.uv, _SkinMap);
                float2 fleshUV = TRANSFORM_TEX(input.uv, _FleshMap);
                half3 skinTexture = SAMPLE_TEXTURE2D(_SkinMap, sampler_SkinMap, skinUV).rgb;
                half3 fleshTexture = SAMPLE_TEXTURE2D(_FleshMap, sampler_FleshMap, fleshUV).rgb;
                half3 skinNormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_SkinNormalMap, sampler_SkinNormalMap, TRANSFORM_TEX(input.uv, _SkinNormalMap)), _SkinNormalScale);
                half3 fleshNormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_FleshNormalMap, sampler_FleshNormalMap, TRANSFORM_TEX(input.uv, _FleshNormalMap)), _FleshNormalScale);
                half3 normalTS = normalize(lerp(skinNormalTS, fleshNormalTS, flesh));

                // Normal map first, then the dent at the edge of the peel is added on top of it.
                float3 bitangentWS = cross(normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 tangentToWorld = float3x3(normalize(input.tangentWS.xyz), normalize(bitangentWS), normalWS);
                normalWS = normalize(mul(normalTS, tangentToWorld));

                normalWS = DentNormal(normalWS, surfaceNormalWS, input.positionWS, input.uv);

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
                surfaceData.albedo = lerp(_SkinColor.rgb * skinTexture, _FleshColor.rgb * fleshTexture, flesh);
                surfaceData.smoothness = lerp(_SkinSmoothness, FleshSmoothness(input.uv), flesh);
                surfaceData.occlusion = 1;
                surfaceData.alpha = 1;
                surfaceData.normalTS = half3(0, 0, 1);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb += Scatter(normalWS, inputData.viewDirectionWS, GetMainLight(), surfaceData.albedo, flesh);
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

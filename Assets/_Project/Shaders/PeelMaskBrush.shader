// Paints the peel mask. The mesh is drawn "unwrapped": every vertex lands where its UV is, so the
// pass fills the mask texture. For each texel the distance to the knife segment is measured in the
// object space of the mesh (in 3D), so the stroke has the real width everywhere, also at the poles and the UV seam.
Shader "Hidden/Inktober/PeelMaskBrush"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend One One
        BlendOp Max

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _SegmentStart;   // object space
            float4 _SegmentEnd;     // object space
            float _HalfWidth;       // object space units
            float _Softness;        // object space units

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float2 clip = input.uv * 2.0 - 1.0;
            #if UNITY_UV_STARTS_AT_TOP
                clip.y = -clip.y;
            #endif
                output.positionCS = float4(clip, 0, 1);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 start = _SegmentStart.xyz;
                float3 direction = _SegmentEnd.xyz - start;
                float lengthSquared = dot(direction, direction);
                float t = lengthSquared > 1e-12 ? saturate(dot(input.positionOS - start, direction) / lengthSquared) : 0;
                float distanceToSegment = distance(input.positionOS, start + direction * t);

                float value = 1.0 - smoothstep(_HalfWidth - _Softness, _HalfWidth, distanceToSegment);
                return half4(value, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}

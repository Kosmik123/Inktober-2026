// Paints the peel mask. The mesh is drawn "unwrapped": every vertex lands where its UV is, so the
// pass fills the mask texture. The brush is a rectangle laid along the knife segment: it is as wide as the strip
// and as long as the step of the knife, and it has no round ends, so nothing is peeled ahead of the knife.
// Every texel is placed in the object space of the mesh (in 3D), so the stroke has the real width everywhere,
// also at the poles and the UV seam.
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

            float4 _SegmentStart;       // object space, the beginning of the rectangle
            float4 _SegmentDirection;   // unit vector along the stroke, tangent to the surface
            float4 _SegmentSide;        // unit vector across the stroke, tangent to the surface
            float4 _SegmentNormal;      // unit surface normal
            float _SegmentLength;       // object space units
            float _HalfWidth;           // object space units
            float _Softness;            // soft edge of the sides, object space units
            float _Overlap;             // how far the rectangle reaches back over the previous one, object space units

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
                float3 offset = input.positionOS - _SegmentStart.xyz;
                float along = dot(offset, _SegmentDirection.xyz);
                float across = dot(offset, _SegmentSide.xyz);
                float height = dot(offset, _SegmentNormal.xyz);

                // The far side of the object is as close in the plane of the stroke, but far along the normal.
                float sameSurface = step(abs(height), _HalfWidth);

                // The sides are soft (they are the edge of the peel). The beginning reaches back over the previous
                // rectangle, and the front fades out over the same distance, which the next rectangle covers again,
                // so the rectangles join without a seam.
                float sides = 1.0 - smoothstep(_HalfWidth - _Softness, _HalfWidth, abs(across));
                float overlap = max(_Overlap, 1e-6);
                float back = step(-overlap, along);
                float front = saturate((_SegmentLength - along) / overlap);

                float value = sides * back * front * sameSurface;
                return half4(value, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}

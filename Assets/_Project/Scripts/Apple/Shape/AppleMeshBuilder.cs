using UnityEngine;
using UnityEngine.Rendering;

namespace Inktober.Apple.Shape
{
    /// <summary>
    /// Builds the mesh of the apple: a UV sphere deformed by <see cref="AppleShapeSettings"/> and a seed.
    /// The UV layout is the one of the default Unity sphere: U goes around the Y axis (the seam is at the -X side),
    /// V goes from the south pole (0) to the north pole (1), so the skin texture and the peel mask work as before.
    /// Every pole has one vertex per segment, so the triangles of the poles have a proper UV area.
    /// </summary>
    public static class AppleMeshBuilder
    {
        private const int MinSegments = 3;
        private const int MinRings = 3;
        private const float MinSpread = 1f;
        private const float NoiseOffsetRange = 100f;
        private const float NoiseBaseOffset = 20f;
        private const float SecondOctaveScale = 2.3f;
        private const float SecondOctaveWeight = 0.4f;

        /// <summary>Returns a new mesh. Destroy it when it is not needed.</summary>
        /// <param name="settings">The shape.</param>
        /// <param name="seed">The same seed always gives the same apple.</param>
        /// <param name="segments">Number of vertical slices around the apple.</param>
        /// <param name="rings">Number of bands from the north pole to the south pole.</param>
        public static Mesh Build(AppleShapeSettings settings, int seed, int segments, int rings)
        {
            segments = Mathf.Max(MinSegments, segments);
            rings = Mathf.Max(MinRings, rings);
            var offset = NoiseOffset(seed);

            // Row 0 and the last row are the poles with one vertex per segment, the rows between have one more vertex to close the seam.
            int vertexCount = segments * 2 + (rings - 1) * (segments + 1);
            var positions = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            int index = 0;

            for (int row = 0; row <= rings; row++)
            {
                bool isPole = row == 0 || row == rings;
                int columns = isPole ? segments : segments + 1;
                float theta = Mathf.PI * row / rings;
                var poleNormal = row == 0 ? Vector3.up : Vector3.down;
                for (int column = 0; column < columns; column++)
                {
                    float phi = 2f * Mathf.PI * column / segments;
                    float u = isPole ? (column + 0.5f) / segments : (float)column / segments;
                    positions[index] = Point(theta, phi, settings, offset);
                    normals[index] = isPole ? poleNormal : Normal(theta, phi, rings, segments, settings, offset);
                    uvs[index] = new Vector2(u, 1f - (float)row / rings);
                    index++;
                }
            }

            var mesh = new Mesh
            {
                name = "Apple",
                indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };

            mesh.vertices = positions;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = BuildTriangles(segments, rings);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        /// <summary>The radius in the given direction as a multiple of the base radius.</summary>
        /// <param name="direction">Unit vector from the center.</param>
        public static float RadiusFactor(Vector3 direction, AppleShapeSettings settings, Vector3 noiseOffset)
        {
            float theta = Mathf.Acos(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;

            float shoulder = settings.ShoulderWidening * Bump((theta - settings.ShoulderAngle) / Mathf.Max(MinSpread, settings.ShoulderSpread));
            float top = settings.TopDimpleDepth * Bump(theta / Mathf.Max(MinSpread, settings.TopDimpleSpread));
            float bottom = settings.BottomDimpleDepth * Bump((180f - theta) / Mathf.Max(MinSpread, settings.BottomDimpleSpread));
            float uneven = settings.Unevenness * Noise(direction, settings.UnevennessScale, noiseOffset);
            return 1f + shoulder - top - bottom + uneven;
        }

        /// <summary>Where the noise is read from; every seed gives another place.</summary>
        public static Vector3 NoiseOffset(int seed)
        {
            var random = new System.Random(seed);
            return new Vector3(NextOffset(random), NextOffset(random), NextOffset(random));
        }

        private static float NextOffset(System.Random random)
        {
            return NoiseBaseOffset + (float)random.NextDouble() * NoiseOffsetRange;
        }

        private static float Bump(float x)
        {
            return Mathf.Exp(-x * x);
        }

        // Smooth noise from -1 to 1 in 3D, made from three planes of Perlin noise, so it has no seam and no pole.
        private static float Noise(Vector3 direction, float scale, Vector3 offset)
        {
            float first = Noise3(direction * scale + offset);
            float second = Noise3(direction * (scale * SecondOctaveScale) + offset * 1.7f);
            return (first + second * SecondOctaveWeight) / (1f + SecondOctaveWeight);
        }

        private static float Noise3(Vector3 p)
        {
            float xy = Mathf.PerlinNoise(p.x, p.y);
            float yz = Mathf.PerlinNoise(p.y, p.z);
            float zx = Mathf.PerlinNoise(p.z, p.x);
            return ((xy + yz + zx) / 3f) * 2f - 1f;
        }

        // theta is the angle from the north pole, phi goes around the Y axis from the -X side towards -Z (the direction of increasing U).
        private static Vector3 Direction(float theta, float phi)
        {
            float ring = Mathf.Sin(theta);
            return new Vector3(-Mathf.Cos(phi) * ring, Mathf.Cos(theta), -Mathf.Sin(phi) * ring);
        }

        private static Vector3 Point(float theta, float phi, AppleShapeSettings settings, Vector3 offset)
        {
            var direction = Direction(theta, phi);
            return direction * (settings.Radius * RadiusFactor(direction, settings, offset));
        }

        // The normal of the deformed surface from the slopes along the two angles. The same point always gives the same normal, also across the seam.
        private static Vector3 Normal(float theta, float phi, int rings, int segments, AppleShapeSettings settings, Vector3 offset)
        {
            float stepTheta = 0.25f * Mathf.PI / rings;
            float stepPhi = 0.25f * 2f * Mathf.PI / segments;
            var alongTheta = Point(theta + stepTheta, phi, settings, offset) - Point(theta - stepTheta, phi, settings, offset);
            var alongPhi = Point(theta, phi + stepPhi, settings, offset) - Point(theta, phi - stepPhi, settings, offset);

            var normal = Vector3.Cross(alongPhi, alongTheta).normalized;
            var outward = Direction(theta, phi);
            return Vector3.Dot(normal, outward) < 0f ? -normal : normal;
        }

        // Front faces are clockwise seen from outside (the Unity convention).
        private static int[] BuildTriangles(int segments, int rings)
        {
            var triangles = new int[(segments * 2 + (rings - 2) * segments * 2) * 3];
            int next = 0;

            int RowStart(int row)
            {
                return row == 0 ? 0 : segments + (row - 1) * (segments + 1);
            }

            void Add(int a, int b, int c)
            {
                triangles[next++] = a;
                triangles[next++] = b;
                triangles[next++] = c;
            }

            for (int column = 0; column < segments; column++)
            {
                // North cap: the pole vertex, then the right and the left corner of the first ring.
                Add(RowStart(0) + column, RowStart(1) + column + 1, RowStart(1) + column);

                // South cap: the left and the right corner of the last ring, then the pole vertex.
                Add(RowStart(rings - 1) + column, RowStart(rings - 1) + column + 1, RowStart(rings) + column);
            }

            for (int row = 1; row < rings - 1; row++)
            {
                for (int column = 0; column < segments; column++)
                {
                    int topLeft = RowStart(row) + column;
                    int topRight = topLeft + 1;
                    int bottomLeft = RowStart(row + 1) + column;
                    int bottomRight = bottomLeft + 1;
                    Add(topLeft, topRight, bottomLeft);
                    Add(topRight, bottomRight, bottomLeft);
                }
            }

            return triangles;
        }
    }
}

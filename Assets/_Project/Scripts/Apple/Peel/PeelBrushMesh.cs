using UnityEngine;
using UnityEngine.Rendering;

namespace Inktober.Apple.Peel
{
    /// <summary>
    /// Builds the mesh that paints the peel mask: the apple mesh drawn "unwrapped" (every vertex lands where its UV is), with every
    /// triangle grown by a few texels in UV space. Without the growth the texels along the borders of UV islands (the UV seam,
    /// the poles) are never painted, and the bilinear filter blends the peeled area with them, which leaves a hairline there.
    /// The 3D position of a grown corner continues the plane of its triangle, so the distance to the knife stays correct.
    /// </summary>
    public static class PeelBrushMesh
    {
        // Sharp corners of a grown triangle would reach far, so the growth of one corner is limited to this many times the padding.
        private const float MaxCornerGrowth = 3f;
        private const float MinArea = 1e-12f;

        /// <summary>Returns a new mesh with three own vertices per triangle (positions and UV). Destroy it when it is not needed.</summary>
        /// <param name="source">The mesh of the peeled object, with UV in the first channel.</param>
        /// <param name="resolution">Size of the mask texture in pixels.</param>
        /// <param name="paddingTexels">How far to grow every triangle, in texels.</param>
        public static Mesh CreatePadded(Mesh source, int resolution, float paddingTexels)
        {
            var sourcePositions = source.vertices;
            var sourceUv = source.uv;
            var triangles = source.triangles;
            float padding = paddingTexels / Mathf.Max(1, resolution);

            int count = triangles.Length;
            var positions = new Vector3[count];
            var uvs = new Vector2[count];
            var indices = new int[count];

            for (int i = 0; i < count; i += 3)
            {
                var a = sourceUv[triangles[i]];
                var b = sourceUv[triangles[i + 1]];
                var c = sourceUv[triangles[i + 2]];
                var pa = sourcePositions[triangles[i]];
                var pb = sourcePositions[triangles[i + 1]];
                var pc = sourcePositions[triangles[i + 2]];

                Grow(a, b, c, padding, out var ga, out var gb, out var gc);
                SetCorner(i, ga, a, b, c, pa, pb, pc, positions, uvs);
                SetCorner(i + 1, gb, a, b, c, pa, pb, pc, positions, uvs);
                SetCorner(i + 2, gc, a, b, c, pa, pb, pc, positions, uvs);
                indices[i] = i;
                indices[i + 1] = i + 1;
                indices[i + 2] = i + 2;
            }

            var mesh = new Mesh
            {
                name = source.name + " Brush",
                indexFormat = count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                hideFlags = HideFlags.HideAndDontSave
            };

            mesh.vertices = positions;
            mesh.uv = uvs;
            mesh.triangles = indices;
            mesh.UploadMeshData(false);
            return mesh;
        }

        /// <summary>Moves the three UV corners outwards so that every edge of the triangle moves away by the padding (limited at sharp corners).</summary>
        public static void Grow(Vector2 a, Vector2 b, Vector2 c, float padding, out Vector2 grownA, out Vector2 grownB, out Vector2 grownC)
        {
            grownA = a;
            grownB = b;
            grownC = c;

            float twiceArea = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
            float area = Mathf.Abs(twiceArea) * 0.5f;
            if (area < MinArea || padding <= 0f)
                return;

            // Moving every edge outwards by the padding scales the triangle around its incenter by (inradius + padding) / inradius.
            float sideA = (b - c).magnitude;
            float sideB = (a - c).magnitude;
            float sideC = (a - b).magnitude;
            float perimeter = sideA + sideB + sideC;
            var incenter = (a * sideA + b * sideB + c * sideC) / perimeter;
            float inradius = 2f * area / perimeter;
            float scale = (inradius + padding) / inradius;

            grownA = GrowCorner(a, incenter, scale, padding);
            grownB = GrowCorner(b, incenter, scale, padding);
            grownC = GrowCorner(c, incenter, scale, padding);
        }

        private static Vector2 GrowCorner(Vector2 corner, Vector2 incenter, float scale, float padding)
        {
            var growth = (corner - incenter) * (scale - 1f);
            float limit = padding * MaxCornerGrowth;
            if (growth.magnitude > limit)
                growth = growth.normalized * limit;

            return corner + growth;
        }

        // The grown corner gets the 3D position that continues the plane of the original triangle (barycentric coordinates may be negative).
        private static void SetCorner(int index, Vector2 grown, Vector2 a, Vector2 b, Vector2 c, Vector3 pa, Vector3 pb, Vector3 pc, Vector3[] positions, Vector2[] uvs)
        {
            float twiceArea = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
            uvs[index] = grown;
            if (Mathf.Abs(twiceArea) < MinArea * 2f)
            {
                positions[index] = pa;
                return;
            }

            float wb = ((grown.x - a.x) * (c.y - a.y) - (c.x - a.x) * (grown.y - a.y)) / twiceArea;
            float wc = ((b.x - a.x) * (grown.y - a.y) - (grown.x - a.x) * (b.y - a.y)) / twiceArea;
            float wa = 1f - wb - wc;
            positions[index] = pa * wa + pb * wb + pc * wc;
        }
    }
}

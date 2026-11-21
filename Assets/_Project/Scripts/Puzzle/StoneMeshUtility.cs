using UnityEngine;

namespace Inktober2026.Puzzle
{
    /// <summary>
    /// Builds the simple meshes of the stones. Both are centered on the origin and flat-shaded:
    /// a cube with chamfered edges, and the head, a pyramid with a thick base that is cut short at the tip and points along +Z.
    /// </summary>
    public static class StoneMeshUtility
    {
        /// <param name="size">Edge of the cube, in cells.</param>
        /// <param name="bevel">How far the chamfer cuts into every edge, in cells.</param>
        public static Mesh BuildBlock(float size, float bevel)
        {
            var builder = new FlatMeshBuilder();
            float half = size * 0.5f;
            float inner = half - bevel;

            // Faces, one per axis direction.
            for (int axis = 0; axis < 3; axis++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    var a = Point(axis, sign * half, (axis + 1) % 3, -inner, (axis + 2) % 3, -inner);
                    var b = Point(axis, sign * half, (axis + 1) % 3, inner, (axis + 2) % 3, -inner);
                    var c = Point(axis, sign * half, (axis + 1) % 3, inner, (axis + 2) % 3, inner);
                    var d = Point(axis, sign * half, (axis + 1) % 3, -inner, (axis + 2) % 3, inner);
                    builder.AddQuad(a, b, c, d, (a + c) * 0.5f);
                }
            }

            // Chamfers of the edges, one along every edge of the cube.
            for (int edgeAxis = 0; edgeAxis < 3; edgeAxis++)
            {
                int axisA = (edgeAxis + 1) % 3;
                int axisB = (edgeAxis + 2) % 3;
                for (int signA = -1; signA <= 1; signA += 2)
                {
                    for (int signB = -1; signB <= 1; signB += 2)
                    {
                        var a0 = Point(axisA, signA * half, axisB, signB * inner, edgeAxis, -inner);
                        var a1 = Point(axisA, signA * half, axisB, signB * inner, edgeAxis, inner);
                        var b1 = Point(axisA, signA * inner, axisB, signB * half, edgeAxis, inner);
                        var b0 = Point(axisA, signA * inner, axisB, signB * half, edgeAxis, -inner);
                        builder.AddQuad(a0, a1, b1, b0, (a0 + b1) * 0.5f);
                    }
                }
            }

            // Chamfers of the corners.
            for (int signX = -1; signX <= 1; signX += 2)
            {
                for (int signY = -1; signY <= 1; signY += 2)
                {
                    for (int signZ = -1; signZ <= 1; signZ += 2)
                    {
                        var a = new Vector3(signX * half, signY * inner, signZ * inner);
                        var b = new Vector3(signX * inner, signY * half, signZ * inner);
                        var c = new Vector3(signX * inner, signY * inner, signZ * half);
                        builder.AddTriangle(a, b, c, (a + b + c) / 3f);
                    }
                }
            }

            return builder.ToMesh("Stone Block");
        }

        /// <param name="length">Length along Z, in cells.</param>
        /// <param name="baseSize">Edge of the square base at the back, in cells.</param>
        /// <param name="tipSize">Edge of the small square at the front, in cells.</param>
        public static Mesh BuildHead(float length, float baseSize, float tipSize)
        {
            var builder = new FlatMeshBuilder();
            float z0 = -length * 0.5f;
            float z1 = length * 0.5f;
            float baseHalf = baseSize * 0.5f;
            float tipHalf = tipSize * 0.5f;

            var b0 = new Vector3(-baseHalf, -baseHalf, z0);
            var b1 = new Vector3(baseHalf, -baseHalf, z0);
            var b2 = new Vector3(baseHalf, baseHalf, z0);
            var b3 = new Vector3(-baseHalf, baseHalf, z0);
            var t0 = new Vector3(-tipHalf, -tipHalf, z1);
            var t1 = new Vector3(tipHalf, -tipHalf, z1);
            var t2 = new Vector3(tipHalf, tipHalf, z1);
            var t3 = new Vector3(-tipHalf, tipHalf, z1);

            builder.AddQuad(b0, b1, b2, b3, Vector3.back);
            builder.AddQuad(t0, t1, t2, t3, Vector3.forward);
            AddSide(builder, b0, b1, t1, t0);
            AddSide(builder, b1, b2, t2, t1);
            AddSide(builder, b2, b3, t3, t2);
            AddSide(builder, b3, b0, t0, t3);

            return builder.ToMesh("Stone Head");
        }

        private static void AddSide(FlatMeshBuilder builder, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var center = (a + b + c + d) * 0.25f;
            builder.AddQuad(a, b, c, d, new Vector3(center.x, center.y, 0f));
        }

        private static Vector3 Point(int axisA, float valueA, int axisB, float valueB, int axisC, float valueC)
        {
            var point = Vector3.zero;
            point[axisA] = valueA;
            point[axisB] = valueB;
            point[axisC] = valueC;
            return point;
        }
    }
}

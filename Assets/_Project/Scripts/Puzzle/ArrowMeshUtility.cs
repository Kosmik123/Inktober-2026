using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Puzzle
{
    /// <summary>
    /// Builds the mesh of an arrow piece, in the grid space where a cell is one unit and its center is the integer position.
    /// Every cell but the head is a cube, neighbors are joined by bars and the head cell holds a cone that points along the direction.
    /// All the faces are flat-shaded.
    /// </summary>
    public static class ArrowMeshUtility
    {
        private const int ConeSegments = 12;

        /// <param name="thickness">Edge of the body cubes and bars, in cells (0 to 1).</param>
        /// <param name="headRadius">Radius of the base of the cone, in cells.</param>
        public static Mesh Build(ArrowPiece piece, float thickness, float headRadius)
        {
            var buffers = new MeshBuffers();
            var cells = piece.Cells;
            float half = thickness * 0.5f;
            for (int i = 0; i < cells.Count - 1; i++)
            {
                var center = (Vector3)cells[i];
                buffers.AddBox(center, Vector3.right * half, Vector3.up * half, Vector3.forward * half);

                var step = (Vector3)(cells[i + 1] - cells[i]);
                bool intoHead = i + 1 == cells.Count - 1;
                var start = center + step * half;
                var end = (Vector3)cells[i + 1] - step * (intoHead ? 0.5f : half);
                float length = Vector3.Distance(start, end);
                if (length < 0.0001f)
                    continue;

                GetPerpendiculars(step, out var first, out var second);
                buffers.AddBox((start + end) * 0.5f, step * (length * 0.5f), first * half, second * half);
            }

            AddCone(buffers, (Vector3)piece.Head, (Vector3)piece.Direction, headRadius);
            return buffers.ToMesh("Arrow");
        }

        private static void AddCone(MeshBuffers buffers, Vector3 center, Vector3 direction, float radius)
        {
            var baseCenter = center - direction * 0.5f;
            var tip = center + direction * 0.5f;
            GetPerpendiculars(direction, out var first, out var second);

            for (int i = 0; i < ConeSegments; i++)
            {
                float angle0 = Mathf.PI * 2f * i / ConeSegments;
                float angle1 = Mathf.PI * 2f * (i + 1) / ConeSegments;
                var a = baseCenter + (first * Mathf.Cos(angle0) + second * Mathf.Sin(angle0)) * radius;
                var b = baseCenter + (first * Mathf.Cos(angle1) + second * Mathf.Sin(angle1)) * radius;
                buffers.AddTriangle(a, b, tip, (a + b) * 0.5f - baseCenter);
                buffers.AddTriangle(baseCenter, a, b, -direction);
            }
        }

        /// <summary>Two unit vectors that are perpendicular to the axis direction and to each other.</summary>
        private static void GetPerpendiculars(Vector3 direction, out Vector3 first, out Vector3 second)
        {
            first = Mathf.Abs(direction.y) < 0.5f ? Vector3.up : Vector3.right;
            second = Vector3.Cross(direction, first).normalized;
        }

        private class MeshBuffers
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<int> triangles = new List<int>();

            /// <summary>A box given by its center and three half-edge vectors that are perpendicular to each other.</summary>
            public void AddBox(Vector3 center, Vector3 halfA, Vector3 halfB, Vector3 halfC)
            {
                var halves = new[] { halfA, halfB, halfC };
                for (int axis = 0; axis < 3; axis++)
                {
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        var normal = halves[axis] * sign;
                        var u = halves[(axis + 1) % 3];
                        var v = halves[(axis + 2) % 3];
                        var faceCenter = center + normal;
                        var p0 = faceCenter - u - v;
                        var p1 = faceCenter + u - v;
                        var p2 = faceCenter + u + v;
                        var p3 = faceCenter - u + v;
                        AddTriangle(p0, p1, p2, normal);
                        AddTriangle(p0, p2, p3, normal);
                    }
                }
            }

            /// <summary>Adds a flat triangle facing the side the outward vector points to.</summary>
            public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                var normal = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(normal, outward) < 0f)
                {
                    (b, c) = (c, b);
                    normal = -normal;
                }

                normal.Normalize();
                int index = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                triangles.Add(index);
                triangles.Add(index + 1);
                triangles.Add(index + 2);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}

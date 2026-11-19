using Inktober.Apple.Peel;
using NUnit.Framework;
using UnityEngine;

namespace Inktober.Tests.EditMode
{
    public class PeelBrushMeshTests
    {
        private static readonly Vector2 A = new Vector2(0.2f, 0.2f);
        private static readonly Vector2 B = new Vector2(0.6f, 0.2f);
        private static readonly Vector2 C = new Vector2(0.2f, 0.6f);

        private static float DistanceToLine(Vector2 point, Vector2 from, Vector2 to)
        {
            var direction = (to - from).normalized;
            var offset = point - from;
            return Mathf.Abs(offset.x * direction.y - offset.y * direction.x);
        }

        [Test]
        public void Grow_MovesEveryEdgeOutwardsByThePadding()
        {
            const float padding = 0.01f;

            PeelBrushMesh.Grow(A, B, C, padding, out var grownA, out var grownB, out var grownC);

            // The triangle is a right triangle, so its corners are not sharp enough to be limited. Every edge of the grown triangle is
            // parallel to the original one and as far from it as the padding: both corners of a grown edge are that far from the old edge.
            Assert.AreEqual(padding, DistanceToLine(grownB, B, C), 1e-4f, "edge BC, corner B");
            Assert.AreEqual(padding, DistanceToLine(grownC, B, C), 1e-4f, "edge BC, corner C");
            Assert.AreEqual(padding, DistanceToLine(grownA, A, C), 1e-4f, "edge AC, corner A");
            Assert.AreEqual(padding, DistanceToLine(grownC, A, C), 1e-4f, "edge AC, corner C");
            Assert.AreEqual(padding, DistanceToLine(grownA, A, B), 1e-4f, "edge AB, corner A");
            Assert.AreEqual(padding, DistanceToLine(grownB, A, B), 1e-4f, "edge AB, corner B");
        }

        [Test]
        public void Grow_KeepsTheOriginalCornersInsideTheGrownTriangle()
        {
            PeelBrushMesh.Grow(A, B, C, 0.01f, out var grownA, out var grownB, out var grownC);

            foreach (var corner in new[] { A, B, C })
            {
                float d1 = Cross(grownB - grownA, corner - grownA);
                float d2 = Cross(grownC - grownB, corner - grownB);
                float d3 = Cross(grownA - grownC, corner - grownC);
                bool allPositive = d1 > 0f && d2 > 0f && d3 > 0f;
                bool allNegative = d1 < 0f && d2 < 0f && d3 < 0f;
                Assert.IsTrue(allPositive || allNegative, $"{corner} is not inside the grown triangle");
            }
        }

        [Test]
        public void Grow_WithoutPadding_ChangesNothing()
        {
            PeelBrushMesh.Grow(A, B, C, 0f, out var grownA, out var grownB, out var grownC);

            Assert.AreEqual(A, grownA);
            Assert.AreEqual(B, grownB);
            Assert.AreEqual(C, grownC);
        }

        [Test]
        public void Grow_OfADegenerateTriangle_ChangesNothing()
        {
            var middle = (A + B) * 0.5f;

            PeelBrushMesh.Grow(A, middle, B, 0.01f, out var grownA, out var grownMiddle, out var grownB);

            Assert.AreEqual(A, grownA);
            Assert.AreEqual(middle, grownMiddle);
            Assert.AreEqual(B, grownB);
        }

        [Test]
        public void Grow_LimitsTheGrowthOfSharpCorners()
        {
            // A needle: a very sharp corner would grow far without the limit.
            var tip = new Vector2(0.9f, 0.5f);
            var baseA = new Vector2(0.1f, 0.5f);
            var baseB = new Vector2(0.1f, 0.52f);
            const float padding = 0.01f;

            PeelBrushMesh.Grow(baseA, baseB, tip, padding, out _, out _, out var grownTip);

            Assert.LessOrEqual((grownTip - tip).magnitude, padding * 3f + 1e-5f);
        }

        [Test]
        public void CreatePadded_KeepsThePlaneOfEveryTriangle()
        {
            // One triangle lying in the plane z = 0 whose UV equals its XY, so a grown corner must have XY equal to its UV and z = 0.
            var source = new Mesh
            {
                vertices = new[] { new Vector3(A.x, A.y, 0f), new Vector3(B.x, B.y, 0f), new Vector3(C.x, C.y, 0f) },
                uv = new[] { A, B, C },
                triangles = new[] { 0, 1, 2 }
            };

            var padded = PeelBrushMesh.CreatePadded(source, 1024, 3f);

            Assert.AreEqual(3, padded.vertexCount);
            var positions = padded.vertices;
            var uvs = padded.uv;
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(uvs[i].x, positions[i].x, 1e-5f);
                Assert.AreEqual(uvs[i].y, positions[i].y, 1e-5f);
                Assert.AreEqual(0f, positions[i].z, 1e-5f);
            }

            Assert.Greater((uvs[0] - A).magnitude, 0f, "the triangle is not grown");

            Object.DestroyImmediate(padded);
            Object.DestroyImmediate(source);
        }

        [Test]
        public void CreatePadded_GivesEveryTriangleItsOwnVertices()
        {
            var sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

            var padded = PeelBrushMesh.CreatePadded(sphere, 1024, 3f);

            Assert.AreEqual(sphere.triangles.Length, padded.vertexCount);
            Assert.AreEqual(sphere.triangles.Length, padded.triangles.Length);
            Object.DestroyImmediate(padded);
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
    }
}

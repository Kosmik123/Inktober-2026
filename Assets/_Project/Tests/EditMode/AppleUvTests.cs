using Inktober.Apple.Shape;
using NUnit.Framework;
using UnityEngine;

namespace Inktober.Tests.EditMode
{
    public class AppleUvTests
    {
        private const int Segments = 64;
        private const int Rings = 48;
        private const float Tolerance = 1e-3f;

        private Mesh mesh;

        [TearDown]
        public void TearDown()
        {
            if (mesh)
                Object.DestroyImmediate(mesh);
        }

        [Test]
        public void TheSeamSide_IsAtZeroU()
        {
            Assert.That(AppleUv.FromDirection(Vector3.left).x, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void TheOppositeSideOfTheSeam_IsAtHalfU()
        {
            Assert.That(AppleUv.FromDirection(Vector3.right).x, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void UGrowsTowardsMinusZ()
        {
            Assert.That(AppleUv.FromDirection(Vector3.back).x, Is.EqualTo(0.25f).Within(Tolerance));
            Assert.That(AppleUv.FromDirection(Vector3.forward).x, Is.EqualTo(0.75f).Within(Tolerance));
        }

        [Test]
        public void VGoesFromTheNorthPoleToTheSouthPole()
        {
            Assert.That(AppleUv.FromDirection(Vector3.up).y, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(AppleUv.FromDirection(Vector3.right).y, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(AppleUv.FromDirection(Vector3.down).y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void TheLengthOfTheDirection_DoesNotMatter()
        {
            var direction = new Vector3(0.3f, 0.2f, -0.7f);

            Assert.That(Vector2.Distance(AppleUv.FromDirection(direction), AppleUv.FromDirection(direction * 5f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void TheVerticesOfTheAppleMesh_HaveTheCoordinatesOfTheirDirection()
        {
            mesh = AppleMeshBuilder.Build(new AppleShapeSettings(), 3, Segments, Rings);
            var vertices = mesh.vertices;
            var uv = mesh.uv;
            int checkedVertices = 0;

            for (int i = 0; i < vertices.Length; i++)
            {
                // The poles have one vertex per segment, their U is only the middle of their triangle.
                if (uv[i].y < 0.001f || uv[i].y > 0.999f)
                    continue;

                var expected = AppleUv.FromDirection(vertices[i]);
                float du = Mathf.Abs(expected.x - uv[i].x);
                du = Mathf.Min(du, 1f - du);

                Assert.That(du, Is.LessThan(Tolerance), $"U of vertex {i}.");
                Assert.That(expected.y, Is.EqualTo(uv[i].y).Within(Tolerance), $"V of vertex {i}.");
                checkedVertices++;
            }

            Assert.That(checkedVertices, Is.GreaterThan(Segments * (Rings - 2)));
        }
    }
}

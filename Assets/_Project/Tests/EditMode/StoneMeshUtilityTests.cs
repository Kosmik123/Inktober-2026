using Inktober2026.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class StoneMeshUtilityTests
    {
        private const float Tolerance = 0.001f;

        private Mesh mesh;

        [TearDown]
        public void TearDown()
        {
            if (mesh)
                Object.DestroyImmediate(mesh);
        }

        private static void AssertFacesPointOutward(Mesh mesh, Vector3 inside)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var centroid = (vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3f - inside;
                var geometric = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]);

                Assert.Greater(Vector3.Dot(normals[triangles[i]], centroid), 0f, $"normal of triangle {i / 3}");
                Assert.Greater(Vector3.Dot(geometric, centroid), 0f, $"winding of triangle {i / 3}");
            }
        }

        [Test]
        public void BlockIsACenteredCubeWithChamferedEdges()
        {
            mesh = StoneMeshUtility.BuildBlock(0.9f, 0.1f);

            Assert.AreEqual(6 * 2 + 12 * 2 + 8, mesh.triangles.Length / 3);
            Assert.AreEqual(0.9f, mesh.bounds.size.x, Tolerance);
            Assert.AreEqual(0.9f, mesh.bounds.size.y, Tolerance);
            Assert.AreEqual(0.9f, mesh.bounds.size.z, Tolerance);
            Assert.AreEqual(0f, mesh.bounds.center.magnitude, Tolerance);
        }

        [Test]
        public void BlockFacesPointOutward()
        {
            mesh = StoneMeshUtility.BuildBlock(0.9f, 0.1f);

            AssertFacesPointOutward(mesh, Vector3.zero);
        }

        [Test]
        public void HeadPointsAlongZAndNarrowsToTheTip()
        {
            mesh = StoneMeshUtility.BuildHead(0.9f, 0.9f, 0.2f);

            Assert.AreEqual(12, mesh.triangles.Length / 3);
            Assert.AreEqual(0.9f, mesh.bounds.size.x, Tolerance);
            Assert.AreEqual(0.9f, mesh.bounds.size.z, Tolerance);
            foreach (var vertex in mesh.vertices)
            {
                if (vertex.z > 0f)
                    Assert.AreEqual(0.1f, Mathf.Abs(vertex.x), Tolerance, "tip");
            }
        }

        [Test]
        public void HeadFacesPointOutward()
        {
            mesh = StoneMeshUtility.BuildHead(0.9f, 0.9f, 0.2f);

            AssertFacesPointOutward(mesh, Vector3.zero);
        }
    }
}

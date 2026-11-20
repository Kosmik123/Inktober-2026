using Inktober2026.Apple.Shape;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class AppleMeshBuilderTests
    {
        private const int Segments = 16;
        private const int Rings = 8;
        private const float Radius = 0.5f;

        private Mesh mesh;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(mesh);
        }

        private static AppleShapeSettings Sphere()
        {
            return new AppleShapeSettings(Radius, 0f, 40f, 35f, 0f, 22f, 0f, 18f, 0f, 1.3f);
        }

        private static AppleShapeSettings Apple()
        {
            return new AppleShapeSettings(Radius, 0.07f, 55f, 45f, 0.14f, 22f, 0.08f, 18f, 0.025f, 1.3f);
        }

        // Index of a vertex in a ring between the poles; column Segments is the duplicate that closes the seam.
        private static int Vertex(int row, int column)
        {
            return Segments + (row - 1) * (Segments + 1) + column;
        }

        [Test]
        public void Counts_MatchTheGrid()
        {
            mesh = AppleMeshBuilder.Build(Apple(), 1, Segments, Rings);

            Assert.AreEqual(Segments * 2 + (Rings - 1) * (Segments + 1), mesh.vertexCount);
            Assert.AreEqual(Segments * 2 + (Rings - 2) * Segments * 2, mesh.triangles.Length / 3);
            Assert.AreEqual(mesh.vertexCount, mesh.normals.Length);
            Assert.AreEqual(mesh.vertexCount, mesh.uv.Length);
            Assert.AreEqual(mesh.vertexCount, mesh.tangents.Length);
        }

        [Test]
        public void WithoutDeformation_EveryVertexIsOnTheSphere()
        {
            mesh = AppleMeshBuilder.Build(Sphere(), 1, Segments, Rings);

            foreach (var position in mesh.vertices)
                Assert.AreEqual(Radius, position.magnitude, 1e-4f);
        }

        [Test]
        public void EveryTriangle_FacesOutwards()
        {
            mesh = AppleMeshBuilder.Build(Apple(), 7, Segments, Rings);
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                var normal = Vector3.Cross(b - a, c - a);
                Assert.Greater(Vector3.Dot(normal, (a + b + c) / 3f), 0f, $"Triangle {i / 3} faces inwards.");
            }
        }

        [Test]
        public void Normals_AreUnitAndPointOutwards()
        {
            mesh = AppleMeshBuilder.Build(Apple(), 7, Segments, Rings);
            var vertices = mesh.vertices;
            var normals = mesh.normals;

            for (int i = 0; i < normals.Length; i++)
            {
                Assert.AreEqual(1f, normals[i].magnitude, 1e-3f);
                Assert.Greater(Vector3.Dot(normals[i], vertices[i]), 0f);
            }
        }

        [Test]
        public void UvLayout_IsTheOneOfTheUnitySphere()
        {
            mesh = AppleMeshBuilder.Build(Sphere(), 1, Segments, Rings);
            var vertices = mesh.vertices;
            var uvs = mesh.uv;

            foreach (var uv in uvs)
            {
                Assert.GreaterOrEqual(uv.x, 0f);
                Assert.LessOrEqual(uv.x, 1f);
                Assert.GreaterOrEqual(uv.y, 0f);
                Assert.LessOrEqual(uv.y, 1f);
            }

            int equator = Vertex(Rings / 2, 0);
            Assert.AreEqual(0f, uvs[equator].x, 1e-5f, "The seam starts at U = 0.");
            Assert.Less(vertices[equator].x, -Radius * 0.99f, "The seam is on the -X side.");

            int quarter = Vertex(Rings / 2, Segments / 4);
            Assert.AreEqual(0.25f, uvs[quarter].x, 1e-5f);
            Assert.Less(vertices[quarter].z, -Radius * 0.99f, "U grows towards -Z, so the texture is not mirrored.");

            Assert.AreEqual(1f, uvs[0].y, 1e-5f, "The north pole is V = 1.");
            Assert.Greater(vertices[0].y, 0f);
            Assert.AreEqual(0f, uvs[Vertex(Rings, 0)].y, 1e-5f, "The south pole is V = 0.");
        }

        [Test]
        public void TheSeam_HasTheSamePositionAndNormalOnBothSides()
        {
            mesh = AppleMeshBuilder.Build(Apple(), 3, Segments, Rings);
            var vertices = mesh.vertices;
            var normals = mesh.normals;

            for (int row = 1; row < Rings; row++)
            {
                int first = Vertex(row, 0);
                int last = Vertex(row, Segments);
                Assert.AreEqual(0f, Vector3.Distance(vertices[first], vertices[last]), 1e-4f);
                Assert.AreEqual(0f, Vector3.Distance(normals[first], normals[last]), 1e-3f);
            }
        }

        [Test]
        public void Poles_AreDimpled()
        {
            mesh = AppleMeshBuilder.Build(new AppleShapeSettings(Radius, 0.06f, 40f, 35f, 0.14f, 22f, 0.08f, 18f, 0f, 1.3f), 1, 64, 48);
            var vertices = mesh.vertices;

            float north = vertices[0].magnitude;
            float south = vertices[mesh.vertexCount - 1].magnitude;
            Assert.Less(north, Radius * 0.9f, "The north dimple is deeper than 10% of the radius.");
            Assert.Less(south, Radius * 0.95f);
            Assert.Greater(north, 0f);
        }

        [Test]
        public void Shoulder_MakesTheUpperHalfWiderThanTheLowerHalf()
        {
            const int segments = 64;
            const int rings = 48;
            var settings = new AppleShapeSettings(Radius, 0.1f, 60f, 30f, 0f, 22f, 0f, 18f, 0f, 1.3f);
            mesh = AppleMeshBuilder.Build(settings, 1, segments, rings);
            var vertices = mesh.vertices;

            // The rings at 60 degrees from the north pole and at 60 degrees from the south pole have the same radius on a sphere.
            int upper = segments + (rings / 3 - 1) * (segments + 1);
            int lower = segments + (rings * 2 / 3 - 1) * (segments + 1);
            float upperWidth = new Vector2(vertices[upper].x, vertices[upper].z).magnitude;
            float lowerWidth = new Vector2(vertices[lower].x, vertices[lower].z).magnitude;

            Assert.Greater(upperWidth, lowerWidth * 1.05f);
        }

        [Test]
        public void TheSameSeed_GivesTheSameApple()
        {
            mesh = AppleMeshBuilder.Build(Apple(), 42, Segments, Rings);
            var other = AppleMeshBuilder.Build(Apple(), 42, Segments, Rings);

            CollectionAssert.AreEqual(mesh.vertices, other.vertices);
            Object.DestroyImmediate(other);
        }

        [Test]
        public void AnotherSeed_GivesAnotherApple()
        {
            mesh = AppleMeshBuilder.Build(Apple(), 42, Segments, Rings);
            var other = AppleMeshBuilder.Build(Apple(), 43, Segments, Rings);

            float largestDifference = 0f;
            var first = mesh.vertices;
            var second = other.vertices;
            for (int i = 0; i < first.Length; i++)
                largestDifference = Mathf.Max(largestDifference, Vector3.Distance(first[i], second[i]));

            Assert.Greater(largestDifference, 0.002f);
            Object.DestroyImmediate(other);
        }

        [Test]
        public void Unevenness_StaysWithinItsStrength()
        {
            var settings = new AppleShapeSettings(Radius, 0f, 40f, 35f, 0f, 22f, 0f, 18f, 0.05f, 1.3f);
            for (int seed = 0; seed < 5; seed++)
            {
                mesh = AppleMeshBuilder.Build(settings, seed, Segments, Rings);
                foreach (var position in mesh.vertices)
                    Assert.AreEqual(Radius, position.magnitude, Radius * 0.05f + 1e-4f);

                Object.DestroyImmediate(mesh);
            }

            mesh = null;
        }
    }
}

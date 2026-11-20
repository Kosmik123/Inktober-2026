using Inktober2026.Apple.Peel;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class PeelStripGeometryTests
    {
        private const float Tolerance = 1e-5f;

        private GameObject pathObject;
        private Mesh mesh;

        [TearDown]
        public void TearDown()
        {
            if (pathObject)
                Object.DestroyImmediate(pathObject);

            if (mesh)
                Object.DestroyImmediate(mesh);
        }

        private static PeelStripNode NodeAt(float z)
        {
            return new PeelStripNode(new Vector3(0f, 0f, z), Vector3.right, Vector3.up);
        }

        private Mesh Build(PeelStripGeometry geometry)
        {
            mesh = new Mesh();
            geometry.ApplyTo(mesh);
            return mesh;
        }

        // Sections with texture coordinates that cover an area, so the tangents can be worked out.
        private static PeelSection SectionAt(float distance)
        {
            return new PeelSection(distance, -0.03f, 0.03f, 0f, new Vector2(0.5f, 0.5f + distance), new Vector2(0.52f, 0.5f + distance));
        }

        [Test]
        public void ASingleSection_HasTwoVerticesPerSideAndNoTriangles()
        {
            var geometry = new PeelStripGeometry();

            geometry.Append(SectionAt(0f), NodeAt(0f));
            var built = Build(geometry);

            Assert.That(geometry.SectionCount, Is.EqualTo(1));
            Assert.That(built.vertexCount, Is.EqualTo(4));
            Assert.That(built.subMeshCount, Is.EqualTo(2));
            Assert.That(built.triangles.Length, Is.EqualTo(0));
        }

        [Test]
        public void EverySectionAfterTheFirst_AddsAQuadToBothSides()
        {
            var geometry = new PeelStripGeometry();

            geometry.Append(SectionAt(0f), NodeAt(0f));
            geometry.Append(SectionAt(0.03f), NodeAt(0.03f));
            geometry.Append(SectionAt(0.06f), NodeAt(0.06f));
            var built = Build(geometry);

            Assert.That(built.vertexCount, Is.EqualTo(12));
            Assert.That(built.GetTriangles(PeelStripGeometry.SkinSubmesh).Length, Is.EqualTo(12));
            Assert.That(built.GetTriangles(PeelStripGeometry.FleshSubmesh).Length, Is.EqualTo(12));
        }

        [Test]
        public void TheFleshSide_UsesItsOwnVertices()
        {
            var geometry = new PeelStripGeometry();
            geometry.Append(SectionAt(0f), NodeAt(0f));
            geometry.Append(SectionAt(0.03f), NodeAt(0.03f));
            var built = Build(geometry);

            Assert.That(built.GetTriangles(PeelStripGeometry.SkinSubmesh), Has.All.LessThan(4));
            Assert.That(built.GetTriangles(PeelStripGeometry.FleshSubmesh), Has.All.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void TheEdges_AreOffsetFromTheCentreLineAcrossTheStrip()
        {
            var geometry = new PeelStripGeometry();

            geometry.Append(new PeelSection(1f, -0.01f, 0.04f), NodeAt(1f));
            var built = Build(geometry);

            Assert.That(Vector3.Distance(built.vertices[0], new Vector3(-0.01f, 0f, 1f)), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(built.vertices[1], new Vector3(0.04f, 0f, 1f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void TheSkinTriangles_FaceTheUpDirectionOfTheNodes()
        {
            var geometry = new PeelStripGeometry();
            geometry.Append(SectionAt(0f), NodeAt(0f));
            geometry.Append(SectionAt(0.03f), NodeAt(0.03f));
            var built = Build(geometry);

            AssertAllTrianglesFace(built, PeelStripGeometry.SkinSubmesh, Vector3.up);
        }

        [Test]
        public void TheFleshTriangles_FaceAwayFromTheUpDirectionOfTheNodes()
        {
            var geometry = new PeelStripGeometry();
            geometry.Append(SectionAt(0f), NodeAt(0f));
            geometry.Append(SectionAt(0.03f), NodeAt(0.03f));
            var built = Build(geometry);

            AssertAllTrianglesFace(built, PeelStripGeometry.FleshSubmesh, Vector3.down);
        }

        [Test]
        public void TheNormals_PointOutOfTheirOwnSide()
        {
            var geometry = new PeelStripGeometry();
            geometry.Append(SectionAt(0f), NodeAt(0f));
            geometry.Append(SectionAt(0.03f), NodeAt(0.03f));
            var built = Build(geometry);

            var normals = built.normals;
            Assert.That(normals[0], Is.EqualTo(Vector3.up));
            Assert.That(normals[3], Is.EqualTo(Vector3.up));
            Assert.That(normals[4], Is.EqualTo(Vector3.down));
            Assert.That(normals[7], Is.EqualTo(Vector3.down));
        }

        [Test]
        public void TheTextureCoordinates_AreTheOnesOfTheSectionsOnBothSides()
        {
            var geometry = new PeelStripGeometry();
            geometry.Append(new PeelSection(0.5f, -0.03f, 0.03f, 0f, new Vector2(0.25f, 0.6f), new Vector2(0.27f, 0.6f)), NodeAt(0.5f));
            var built = Build(geometry);

            var uv = built.uv;
            Assert.That(uv[0], Is.EqualTo(new Vector2(0.25f, 0.6f)));
            Assert.That(uv[1], Is.EqualTo(new Vector2(0.27f, 0.6f)));
            Assert.That(uv[2], Is.EqualTo(uv[0]));
            Assert.That(uv[3], Is.EqualTo(uv[1]));
        }

        [Test]
        public void TheTangents_AreUnitVectorsForTheNormalMaps()
        {
            var geometry = new PeelStripGeometry();
            geometry.Append(SectionAt(0f), NodeAt(0f));
            geometry.Append(SectionAt(0.03f), NodeAt(0.03f));
            var built = Build(geometry);

            var tangents = built.tangents;
            Assert.That(tangents.Length, Is.EqualTo(built.vertexCount));
            foreach (var tangent in tangents)
                Assert.That(new Vector3(tangent.x, tangent.y, tangent.z).magnitude, Is.EqualTo(1f).Within(1e-3f));
        }

        private static void AssertAllTrianglesFace(Mesh built, int submesh, Vector3 expected)
        {
            var vertices = built.vertices;
            var triangles = built.GetTriangles(submesh);
            Assert.That(triangles.Length, Is.GreaterThan(0));

            for (int i = 0; i < triangles.Length; i += 3)
            {
                var normal = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]).normalized;
                Assert.That(Vector3.Dot(normal, expected), Is.GreaterThan(0.99f), $"Triangle {i / 3} of submesh {submesh} faces away.");
            }
        }

        [Test]
        public void Clear_RemovesEverything()
        {
            var geometry = new PeelStripGeometry();
            geometry.Append(new PeelSection(0f, -0.03f, 0.03f), NodeAt(0f));
            geometry.Append(new PeelSection(0.03f, -0.03f, 0.03f), NodeAt(0.03f));

            geometry.Clear();
            var built = Build(geometry);

            Assert.That(geometry.SectionCount, Is.EqualTo(0));
            Assert.That(built.vertexCount, Is.EqualTo(0));
        }

        [Test]
        public void StraightPath_PlacesTheNodesAlongTheForwardAxisAtTheDistanceOfTheSections()
        {
            pathObject = new GameObject("Path");
            var path = pathObject.AddComponent<StraightPeelStripPath>();

            path.Append(new PeelSection(0f, -0.03f, 0.03f));
            path.Append(new PeelSection(0.75f, -0.03f, 0.03f));

            Assert.That(path.IsDynamic, Is.False);
            Assert.That(path.NodeCount, Is.EqualTo(2));
            Assert.That(path.GetNode(1).Position, Is.EqualTo(new Vector3(0f, 0f, 0.75f)));
            Assert.That(path.GetNode(1).Right, Is.EqualTo(Vector3.right));
            Assert.That(path.GetNode(1).Up, Is.EqualTo(Vector3.up));
        }

        [Test]
        public void StraightPath_Clear_RemovesTheNodes()
        {
            pathObject = new GameObject("Path");
            var path = pathObject.AddComponent<StraightPeelStripPath>();
            path.Append(new PeelSection(0f, -0.03f, 0.03f));

            path.Clear();

            Assert.That(path.NodeCount, Is.EqualTo(0));
        }
    }
}

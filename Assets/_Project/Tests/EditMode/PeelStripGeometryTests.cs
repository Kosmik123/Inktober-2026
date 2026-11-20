using Inktober.Apple.Peel;
using NUnit.Framework;
using UnityEngine;

namespace Inktober.Tests.EditMode
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

        [Test]
        public void ASingleSection_HasTwoVerticesAndNoTriangles()
        {
            var geometry = new PeelStripGeometry();

            geometry.Append(new PeelSection(0f, -0.03f, 0.03f), NodeAt(0f));
            var built = Build(geometry);

            Assert.That(geometry.SectionCount, Is.EqualTo(1));
            Assert.That(built.vertexCount, Is.EqualTo(2));
            Assert.That(built.triangles.Length, Is.EqualTo(0));
        }

        [Test]
        public void EverySectionAfterTheFirst_AddsAQuad()
        {
            var geometry = new PeelStripGeometry();

            geometry.Append(new PeelSection(0f, -0.03f, 0.03f), NodeAt(0f));
            geometry.Append(new PeelSection(0.03f, -0.03f, 0.03f), NodeAt(0.03f));
            geometry.Append(new PeelSection(0.06f, -0.03f, 0.03f), NodeAt(0.06f));
            var built = Build(geometry);

            Assert.That(built.vertexCount, Is.EqualTo(6));
            Assert.That(built.triangles.Length, Is.EqualTo(12));
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
        public void TheTriangles_FaceTheUpDirectionOfTheNodes()
        {
            var geometry = new PeelStripGeometry();
            geometry.Append(new PeelSection(0f, -0.03f, 0.03f), NodeAt(0f));
            geometry.Append(new PeelSection(0.03f, -0.03f, 0.03f), NodeAt(0.03f));
            var built = Build(geometry);

            var vertices = built.vertices;
            var triangles = built.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var normal = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]).normalized;
                Assert.That(Vector3.Dot(normal, Vector3.up), Is.GreaterThan(0.99f), $"Triangle {i / 3} faces away.");
            }
        }

        [Test]
        public void TheTextureCoordinates_RunAcrossAndAlongTheStrip()
        {
            var geometry = new PeelStripGeometry();
            geometry.Append(new PeelSection(0.5f, -0.03f, 0.03f), NodeAt(0.5f));
            var built = Build(geometry);

            var uv = built.uv;
            Assert.That(uv[0], Is.EqualTo(new Vector2(0f, 0.5f)));
            Assert.That(uv[1], Is.EqualTo(new Vector2(1f, 0.5f)));
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

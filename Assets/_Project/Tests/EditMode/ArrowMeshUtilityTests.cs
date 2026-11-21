using Inktober2026.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class ArrowMeshUtilityTests
    {
        private const float Thickness = 0.5f;
        private const float HeadRadius = 0.4f;
        private const float Tolerance = 0.001f;

        private Mesh mesh;

        [TearDown]
        public void TearDown()
        {
            if (mesh)
                Object.DestroyImmediate(mesh);
        }

        private static ArrowPiece Piece(Vector3Int direction, params Vector3Int[] cells)
        {
            return new ArrowPiece(cells, direction);
        }

        [Test]
        public void OneCellPieceIsACone()
        {
            mesh = ArrowMeshUtility.Build(Piece(Vector3Int.right, Vector3Int.zero), Thickness, HeadRadius);

            Assert.AreEqual(24, mesh.triangles.Length / 3);
            Assert.AreEqual(1f, mesh.bounds.size.x, Tolerance);
            Assert.AreEqual(2f * HeadRadius, mesh.bounds.size.y, 0.05f);
            Assert.AreEqual(2f * HeadRadius, mesh.bounds.size.z, 0.05f);
            Assert.AreEqual(0f, mesh.bounds.center.x, Tolerance);
        }

        [Test]
        public void ConeFacesPointAwayFromItsCenter()
        {
            mesh = ArrowMeshUtility.Build(Piece(Vector3Int.up, Vector3Int.zero), Thickness, HeadRadius);

            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var centroid = (vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3f;
                var geometric = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]);

                Assert.Greater(Vector3.Dot(normals[triangles[i]], centroid), 0f, $"normal of triangle {i / 3}");
                Assert.Greater(Vector3.Dot(geometric, centroid), 0f, $"winding of triangle {i / 3}");
            }
        }

        [Test]
        public void TwoCellPieceHasACubeABarAndACone()
        {
            mesh = ArrowMeshUtility.Build(Piece(Vector3Int.right, Vector3Int.zero, Vector3Int.right), Thickness, HeadRadius);

            Assert.AreEqual(12 + 12 + 24, mesh.triangles.Length / 3);
        }

        [Test]
        public void BentPieceReachesTheTipAndTheTail()
        {
            var cells = new[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(1, 1, 0) };
            mesh = ArrowMeshUtility.Build(new ArrowPiece(cells, Vector3Int.up), Thickness, HeadRadius);

            var bounds = mesh.bounds;
            Assert.AreEqual(-0.25f, bounds.min.x, Tolerance, "tail cube");
            Assert.AreEqual(1.4f, bounds.max.x, 0.05f, "cone radius on the head cell");
            Assert.AreEqual(-0.25f, bounds.min.y, Tolerance, "tail cube");
            Assert.AreEqual(1.5f, bounds.max.y, Tolerance, "tip of the cone");
        }

        [Test]
        public void ViewShowsAnObjectForEveryPieceAndClearsThem()
        {
            var host = new GameObject("Puzzle View");
            try
            {
                var view = host.AddComponent<PuzzleView>();
                var grid = ArrowPuzzleGenerator.Generate(PuzzleGenerationSettings.Default, 3);

                view.Show(grid);

                Assert.AreEqual(grid.Pieces.Count, host.transform.childCount);
                Assert.IsNotNull(view.GetPieceObject(grid.Pieces[0]));

                var first = grid.Pieces[0];
                view.Remove(first);

                Assert.IsNull(view.GetPieceObject(first));
                Assert.AreEqual(grid.Pieces.Count - 1, view.transform.childCount);

                view.Clear();

                Assert.AreEqual(0, host.transform.childCount);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}

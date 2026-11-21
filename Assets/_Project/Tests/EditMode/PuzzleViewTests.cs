using Inktober2026.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class PuzzleViewTests
    {
        private GameObject host;
        private PuzzleView view;
        private ArrowGrid grid;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("Puzzle View");
            view = host.AddComponent<PuzzleView>();
            grid = ArrowPuzzleGenerator.Generate(PuzzleGenerationSettings.Default, 3);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        [Test]
        public void ShowMakesAStoneForEveryCellAndTheHeadPointsAlongTheDirection()
        {
            view.Show(grid);

            Assert.AreEqual(grid.Pieces.Count, host.transform.childCount);
            foreach (var piece in grid.Pieces)
            {
                var pieceObject = view.GetPieceObject(piece);
                Assert.AreEqual(piece.Cells.Count, pieceObject.transform.childCount);

                var head = pieceObject.transform.GetChild(piece.Cells.Count - 1);
                Assert.AreEqual(0f, Vector3.Distance((Vector3)piece.Direction, head.localRotation * Vector3.forward), 0.001f);
            }
        }

        [Test]
        public void RemoveTakesTheObjectAwayAtOnce()
        {
            view.Show(grid);
            var first = grid.Pieces[0];

            view.Remove(first);

            Assert.IsNull(view.GetPieceObject(first));
            Assert.AreEqual(grid.Pieces.Count - 1, host.transform.childCount);
        }

        [Test]
        public void SlideOutKeepsTheObjectUntilItsStonesAreOut()
        {
            view.Show(grid);
            var first = grid.Pieces[0];

            view.SlideOut(first);

            Assert.IsNull(view.GetPieceObject(first), "no longer a piece that can be bumped");
            Assert.AreEqual(grid.Pieces.Count, host.transform.childCount, "still sliding");
        }

        [Test]
        public void ClearTakesEverythingAwayIncludingSlidingPieces()
        {
            view.Show(grid);
            view.SlideOut(grid.Pieces[0]);

            view.Clear();

            Assert.AreEqual(0, host.transform.childCount);
        }
    }
}

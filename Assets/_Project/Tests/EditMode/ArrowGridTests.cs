using Inktober2026.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class ArrowGridTests
    {
        private ArrowGrid grid;

        [SetUp]
        public void SetUp()
        {
            grid = new ArrowGrid(new Vector3Int(5, 5, 5), new Vector3Int(2, 2, 2));
        }

        private static ArrowPiece Piece(Vector3Int direction, params Vector3Int[] cells)
        {
            return new ArrowPiece(cells, direction);
        }

        [Test]
        public void EmptyGridHasEveryCellEmptyExceptTheRelic()
        {
            Assert.AreEqual(124, grid.EmptyCellCount);
            Assert.IsFalse(grid.IsEmpty(new Vector3Int(2, 2, 2)));
        }

        [Test]
        public void PieceCannotTakeTheRelicCellOrTheSameCellTwice()
        {
            var overRelic = Piece(Vector3Int.right, new Vector3Int(1, 2, 2), new Vector3Int(2, 2, 2));
            var first = Piece(Vector3Int.right, new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0));
            var overlapping = Piece(Vector3Int.up, new Vector3Int(1, 0, 0));

            Assert.IsFalse(grid.TryAdd(overRelic));
            Assert.IsTrue(grid.TryAdd(first));
            Assert.IsFalse(grid.TryAdd(overlapping));
            Assert.AreEqual(1, grid.Pieces.Count);
        }

        [Test]
        public void PieceWithClearLineCanBeRemoved()
        {
            var piece = Piece(Vector3Int.right, new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0));
            grid.TryAdd(piece);

            Assert.IsTrue(grid.CanRemove(piece));
        }

        [Test]
        public void PieceInFrontBlocksUntilItIsRemoved()
        {
            var piece = Piece(Vector3Int.right, new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0));
            var blocker = Piece(Vector3Int.up, new Vector3Int(4, 0, 0));
            grid.TryAdd(piece);
            grid.TryAdd(blocker);

            Assert.IsFalse(grid.CanRemove(piece));

            grid.Remove(blocker);

            Assert.IsTrue(grid.CanRemove(piece));
            Assert.IsTrue(grid.IsEmpty(new Vector3Int(4, 0, 0)));
        }

        [Test]
        public void PieceAtTheEdgePointingOutsideCanBeRemoved()
        {
            var piece = Piece(Vector3Int.right, new Vector3Int(3, 0, 0), new Vector3Int(4, 0, 0));
            grid.TryAdd(piece);

            Assert.IsTrue(grid.CanRemove(piece));
        }

        [Test]
        public void RelicBlocksTheLine()
        {
            var piece = Piece(Vector3Int.right, new Vector3Int(0, 2, 2), new Vector3Int(1, 2, 2));
            grid.TryAdd(piece);

            Assert.IsFalse(grid.CanRemove(piece));
        }

        [Test]
        public void PieceIsNotBlockedByItsOwnBody()
        {
            // The head at (0,1,0) points down the Y axis and its line meets the tail at (0,0,0).
            var piece = Piece(
                Vector3Int.down,
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(1, 1, 0),
                new Vector3Int(1, 2, 0),
                new Vector3Int(0, 2, 0),
                new Vector3Int(0, 1, 0));
            grid.TryAdd(piece);

            Assert.IsTrue(grid.CanRemove(piece));
        }

        [Test]
        public void RaycastFindsTheFirstPieceOnTheLine()
        {
            var near = Piece(Vector3Int.up, new Vector3Int(1, 0, 0));
            var far = Piece(Vector3Int.up, new Vector3Int(3, 0, 0));
            grid.TryAdd(near);
            grid.TryAdd(far);

            Assert.IsTrue(grid.TryRaycast(new Vector3(-5f, 0f, 0f), Vector3.right, out var hit));
            Assert.AreSame(near, hit);
            Assert.IsTrue(grid.TryRaycast(new Vector3(9f, 0.2f, -0.3f), Vector3.left, out hit));
            Assert.AreSame(far, hit);
        }

        [Test]
        public void RaycastMissesWhenTheLineIsEmptyOrOutsideOfTheGrid()
        {
            grid.TryAdd(Piece(Vector3Int.up, new Vector3Int(1, 0, 0)));

            Assert.IsFalse(grid.TryRaycast(new Vector3(-5f, 1f, 0f), Vector3.right, out _));
            Assert.IsFalse(grid.TryRaycast(new Vector3(-5f, 10f, 0f), Vector3.right, out _));
            Assert.IsFalse(grid.TryRaycast(new Vector3(-5f, 0f, 0f), Vector3.left, out _));
            Assert.IsFalse(grid.TryRaycast(Vector3.zero, Vector3.zero, out _));
        }

        [Test]
        public void RaycastStopsAtTheRelic()
        {
            var front = Piece(Vector3Int.up, new Vector3Int(0, 2, 2));
            var behind = Piece(Vector3Int.up, new Vector3Int(4, 2, 2));
            grid.TryAdd(behind);

            Assert.IsFalse(grid.TryRaycast(new Vector3(-5f, 2f, 2f), Vector3.right, out _));

            grid.TryAdd(front);

            Assert.IsTrue(grid.TryRaycast(new Vector3(-5f, 2f, 2f), Vector3.right, out var hit));
            Assert.AreSame(front, hit);
        }

        [Test]
        public void RaycastWorksFromInsideAndAlongADiagonal()
        {
            var corner = Piece(Vector3Int.up, new Vector3Int(0, 0, 0));
            var inside = Piece(Vector3Int.up, new Vector3Int(3, 3, 3));
            grid.TryAdd(corner);
            grid.TryAdd(inside);

            Assert.IsTrue(grid.TryRaycast(new Vector3(-3f, -3f, -3f), Vector3.one, out var hit));
            Assert.AreSame(corner, hit);
            Assert.IsTrue(grid.TryRaycast(new Vector3(1f, 3f, 3f), Vector3.right, out hit));
            Assert.AreSame(inside, hit);
        }

        [Test]
        public void RemovingAPieceRaisesTheEventOnce()
        {
            var piece = Piece(Vector3Int.right, new Vector3Int(0, 0, 0));
            grid.TryAdd(piece);
            var removed = new System.Collections.Generic.List<ArrowPiece>();
            grid.OnPieceRemoved += removed.Add;

            grid.Remove(piece);
            grid.Remove(piece);

            CollectionAssert.AreEqual(new[] { piece }, removed);
        }

        [Test]
        public void SolvingDoesNotRaiseTheEventOfTheGrid()
        {
            grid.TryAdd(Piece(Vector3Int.right, new Vector3Int(0, 0, 0)));
            int raised = 0;
            grid.OnPieceRemoved += _ => raised++;

            grid.IsSolvable();

            Assert.AreEqual(0, raised);
        }

        [Test]
        public void TwoPiecesPointingAtEachOtherCannotBeSolved()
        {
            grid.TryAdd(Piece(Vector3Int.right, new Vector3Int(0, 0, 0)));
            grid.TryAdd(Piece(Vector3Int.left, new Vector3Int(1, 0, 0)));

            Assert.IsFalse(grid.IsSolvable());
        }

        [Test]
        public void PiecesInAChainCanBeSolved()
        {
            grid.TryAdd(Piece(Vector3Int.right, new Vector3Int(0, 0, 0)));
            grid.TryAdd(Piece(Vector3Int.right, new Vector3Int(1, 0, 0)));
            grid.TryAdd(Piece(Vector3Int.right, new Vector3Int(2, 0, 0)));

            Assert.IsTrue(grid.IsSolvable());
            Assert.AreEqual(3, grid.Pieces.Count, "Solving works on a copy.");
        }
    }
}

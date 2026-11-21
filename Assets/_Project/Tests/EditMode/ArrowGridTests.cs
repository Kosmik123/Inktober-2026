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

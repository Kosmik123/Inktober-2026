using System;
using Inktober2026.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class ArrowPieceTests
    {
        [Test]
        public void HeadIsTheLastCell()
        {
            var piece = new ArrowPiece(new[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0) }, Vector3Int.right);

            Assert.AreEqual(new Vector3Int(1, 0, 0), piece.Head);
            Assert.AreEqual(Vector3Int.right, piece.Direction);
        }

        [Test]
        public void ThrowsWhenCellsAreNotNeighbors()
        {
            Assert.Throws<ArgumentException>(() =>
                new ArrowPiece(new[] { new Vector3Int(0, 0, 0), new Vector3Int(2, 0, 0) }, Vector3Int.right));
        }

        [Test]
        public void ThrowsWhenACellRepeats()
        {
            var cells = new[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, 0, 0) };

            Assert.Throws<ArgumentException>(() => new ArrowPiece(cells, Vector3Int.left));
        }

        [Test]
        public void ThrowsWhenDirectionDoesNotFollowTheLastSegment()
        {
            Assert.Throws<ArgumentException>(() =>
                new ArrowPiece(new[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0) }, Vector3Int.up));
        }

        [Test]
        public void ThrowsWhenDirectionIsNotAnAxis()
        {
            Assert.Throws<ArgumentException>(() =>
                new ArrowPiece(new[] { new Vector3Int(0, 0, 0) }, new Vector3Int(1, 1, 0)));
        }

        [Test]
        public void OneCellPieceAcceptsEveryAxisDirection()
        {
            foreach (var direction in DirectionUtility.All)
                Assert.DoesNotThrow(() => new ArrowPiece(new[] { Vector3Int.zero }, direction));
        }
    }
}

using Inktober2026.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class PuzzleBoardTests
    {
        private GameObject host;
        private PuzzleBoard board;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("Puzzle Board");
            board = host.AddComponent<PuzzleBoard>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        [Test]
        public void SizeStartsAsFiveByFiveByFive()
        {
            Assert.AreEqual(new Vector3Int(5, 5, 5), board.Size);
        }

        [Test]
        public void SetSizeChangesTheSizeOfTheNextGrid()
        {
            board.SetSize(new Vector3Int(3, 7, 4));

            Assert.AreEqual(new Vector3Int(3, 7, 4), board.Size);
        }

        [Test]
        public void SetSizeKeepsEveryAxisBetweenOneAndTheMaximum()
        {
            board.SetSize(new Vector3Int(0, -3, 500));

            Assert.AreEqual(1, board.Size.x);
            Assert.AreEqual(1, board.Size.y);
            Assert.AreEqual(12, board.Size.z);
        }
    }
}

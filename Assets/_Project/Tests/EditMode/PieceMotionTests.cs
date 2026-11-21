using Inktober2026.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class PieceMotionTests
    {
        private const float Tolerance = 0.001f;

        private GameObject host;

        [TearDown]
        public void TearDown()
        {
            if (host)
                Object.DestroyImmediate(host);
        }

        private static void AssertPosition(Vector3 expected, Vector3 actual, string message = null)
        {
            Assert.AreEqual(0f, Vector3.Distance(expected, actual), Tolerance, $"{message} expected {expected}, got {actual}");
        }

        private static ArrowPiece Straight()
        {
            return new ArrowPiece(new[] { new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0) }, Vector3Int.right);
        }

        private static ArrowPiece Bent()
        {
            return new ArrowPiece(new[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(1, 1, 0) }, Vector3Int.up);
        }

        private Transform[] CreateStones(ArrowPiece piece)
        {
            host = new GameObject("Piece");
            var stones = new Transform[piece.Cells.Count];
            for (int i = 0; i < stones.Length; i++)
            {
                stones[i] = new GameObject($"Stone {i}").transform;
                stones[i].SetParent(host.transform, false);
                stones[i].localPosition = (Vector3)piece.Cells[i];
            }

            return stones;
        }

        [Test]
        public void PathContinuesStraightFromTheHeadUpToTheFirstCellOutside()
        {
            var path = new PiecePath(Straight(), new Vector3Int(5, 5, 5));

            Assert.AreEqual(4, path.ExitIndex);
            AssertPosition(new Vector3(1, 0, 0), path.Evaluate(0f));
            AssertPosition(new Vector3(2, 0, 0), path.Evaluate(1f));
            AssertPosition(new Vector3(3.5f, 0, 0), path.Evaluate(2.5f));
            AssertPosition(new Vector3(5, 0, 0), path.Evaluate(100f), "clamped at the end");
            AssertPosition(new Vector3(1, 0, 0), path.Evaluate(-3f), "clamped at the start");
        }

        [Test]
        public void PathOfABentPieceFollowsItsCells()
        {
            var path = new PiecePath(Bent(), new Vector3Int(3, 3, 3));

            Assert.AreEqual(4, path.ExitIndex);
            AssertPosition(new Vector3(0.5f, 0, 0), path.Evaluate(0.5f));
            AssertPosition(new Vector3(1, 0.5f, 0), path.Evaluate(1.5f));
            AssertPosition(new Vector3(1, 3, 0), path.Evaluate(4f));
        }

        [Test]
        public void SlideMovesEveryStoneAlongThePathOneBehindTheOther()
        {
            var piece = Bent();
            var stones = CreateStones(piece);
            var motion = PieceMotion.CreateSlide(new PiecePath(piece, new Vector3Int(3, 3, 3)), stones, 1f);

            motion.Advance(1f);

            AssertPosition(new Vector3(1, 0, 0), stones[0].localPosition, "tail takes the place of the second stone");
            AssertPosition(new Vector3(1, 1, 0), stones[1].localPosition, "second takes the place of the head");
            AssertPosition(new Vector3(1, 2, 0), stones[2].localPosition, "head goes on in its direction");
            Assert.IsFalse(motion.IsFinished);
        }

        [Test]
        public void SlideSwitchesStonesOffOnceOutsideAndFinishesWhenTheTailIsOut()
        {
            var piece = Straight();
            var stones = CreateStones(piece);
            var motion = PieceMotion.CreateSlide(new PiecePath(piece, new Vector3Int(5, 5, 5)), stones, 1f);

            motion.Advance(3f);

            Assert.IsTrue(stones[0].gameObject.activeSelf, "tail still inside");
            Assert.IsFalse(stones[1].gameObject.activeSelf, "head is outside");
            Assert.IsFalse(motion.IsFinished);

            motion.Advance(1f);

            Assert.IsFalse(stones[0].gameObject.activeSelf);
            Assert.IsTrue(motion.IsFinished);
        }

        [Test]
        public void StonesDoNotTurnWhileSliding()
        {
            var piece = Bent();
            var stones = CreateStones(piece);
            var motion = PieceMotion.CreateSlide(new PiecePath(piece, new Vector3Int(3, 3, 3)), stones, 1f);

            motion.Advance(1.5f);

            foreach (var stone in stones)
                Assert.AreEqual(Quaternion.identity, stone.localRotation);
        }

        [Test]
        public void BumpPushesForwardAndEndsInTheStartingCells()
        {
            var piece = Straight();
            var stones = CreateStones(piece);
            var motion = PieceMotion.CreateBump(new PiecePath(piece, new Vector3Int(5, 5, 5)), stones, 0.3f, 1f);

            motion.Advance(0.5f);

            AssertPosition(new Vector3(2.3f, 0, 0), stones[1].localPosition, "head at the farthest point");
            AssertPosition(new Vector3(1.3f, 0, 0), stones[0].localPosition);
            Assert.IsFalse(motion.IsFinished);

            motion.Advance(0.5f);

            AssertPosition(new Vector3(1, 0, 0), stones[0].localPosition);
            AssertPosition(new Vector3(2, 0, 0), stones[1].localPosition);
            Assert.IsTrue(motion.IsFinished);
        }
    }
}

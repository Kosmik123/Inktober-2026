using System.Collections.Generic;
using Inktober2026.Apple.Peel;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class PeelEnclosureTests
    {
        // The surface is the plane y = 0 with the normal up, scale 1. The knife of the tests travels along +Z, so its right side is +X.
        private const float Width = 0.06f;
        private const float IgnoredTail = 0.2f;
        private const float HeightTolerance = 0.03f;

        private static PeelPoint PointAt(float x, float z, float y = 0f, float width = Width)
        {
            return new PeelPoint(new Vector3(x, y, z), Vector3.up, Vector3.forward, width);
        }

        private static void AddLine(List<PeelPoint> points, float x, float height, bool upwards, float width = Width)
        {
            for (int i = 0; i <= 10; i++)
                points.Add(PointAt(x, (upwards ? i : 10 - i) * 0.1f, height, width));
        }

        // Like TrailWithKnifeBetween, with a width of the older lines and a width of the knife path of their own.
        private static List<PeelPoint> TrailWithWidths(float leftX, float rightX, float lineWidth, float knifeWidth)
        {
            var points = new List<PeelPoint>();
            AddLine(points, leftX, 0f, upwards: true, lineWidth);
            AddLine(points, rightX, 0f, upwards: false, lineWidth);
            for (int i = 0; i <= 10; i++)
                points.Add(PointAt(0f, -0.5f + i * 0.1f, 0f, knifeWidth));

            return points;
        }

        // Older lines parallel to the knife path (z from 0 to 1) and then the knife, which goes from z = -0.5 to z = 0.5 along x = 0.
        // The older lines are joined so that no connecting segment passes near the end of the knife path.
        // A NaN position leaves the line out.
        private static List<PeelPoint> TrailWithKnifeBetween(float leftX, float rightX, float lineHeight = 0f)
        {
            var points = new List<PeelPoint>();
            bool hasLeft = float.IsNaN(leftX) == false;
            bool hasRight = float.IsNaN(rightX) == false;
            if (hasLeft)
                AddLine(points, leftX, lineHeight, upwards: hasRight);

            if (hasRight)
                AddLine(points, rightX, lineHeight, upwards: false);

            for (int i = 0; i <= 10; i++)
                points.Add(PointAt(0f, -0.5f + i * 0.1f));

            return points;
        }

        [Test]
        public void FewerThanThreePoints_AreNeverEnclosed()
        {
            var points = new List<PeelPoint> { PointAt(0f, 0f), PointAt(0f, 0.01f) };

            Assert.IsFalse(PeelEnclosure.IsEnclosed(points, 1f, IgnoredTail, HeightTolerance));
        }

        [Test]
        public void LinesOnBothSidesAtHalfTheWidth_AreEnclosed()
        {
            var points = TrailWithKnifeBetween(-Width * 0.5f, Width * 0.5f);

            Assert.IsTrue(PeelEnclosure.IsEnclosed(points, 1f, IgnoredTail, HeightTolerance));
        }

        [Test]
        public void LinesOnBothSidesFartherThanHalfTheWidth_AreNotEnclosed()
        {
            var points = TrailWithKnifeBetween(-Width * 0.6f, Width * 0.6f);

            Assert.IsFalse(PeelEnclosure.IsEnclosed(points, 1f, IgnoredTail, HeightTolerance));
        }

        [Test]
        public void UnevenDistances_WithTheSumWithinTheWidth_AreEnclosed()
        {
            // 0.3 + 0.65 of the width: neither side is within half the width on its own.
            var points = TrailWithKnifeBetween(-Width * 0.3f, Width * 0.65f);

            Assert.IsTrue(PeelEnclosure.IsEnclosed(points, 1f, IgnoredTail, HeightTolerance));
        }

        [Test]
        public void UnevenDistances_WithTheSumOverTheWidth_AreNotEnclosed()
        {
            var points = TrailWithKnifeBetween(-Width * 0.3f, Width * 0.8f);

            Assert.IsFalse(PeelEnclosure.IsEnclosed(points, 1f, IgnoredTail, HeightTolerance));
        }

        [Test]
        public void NarrowOldLines_UnderAWideKnife_LeaveAGapAndDoNotEnclose()
        {
            // The sum of the distances (0.27) is within the width of the knife (0.28), but strips 0.16 wide leave a gap in the middle.
            var points = TrailWithWidths(-0.135f, 0.135f, lineWidth: 0.16f, knifeWidth: 0.28f);

            Assert.IsFalse(PeelEnclosure.IsEnclosed(points, 1f, IgnoredTail, HeightTolerance));
        }

        [Test]
        public void WideOldLines_CoveringANarrowKnife_Enclose()
        {
            // The sum of the distances (0.2) is over the width of the knife (0.16), but strips 0.28 wide cover all of it.
            var points = TrailWithWidths(-0.1f, 0.1f, lineWidth: 0.28f, knifeWidth: 0.16f);

            Assert.IsTrue(PeelEnclosure.IsEnclosed(points, 1f, IgnoredTail, HeightTolerance));
        }

        [Test]
        public void NarrowOldLinesInsideAWideKnife_DoNotEncloseItsEdges()
        {
            // The lines meet in the middle, but the outer edges of the knife strip stay unpeeled.
            var points = TrailWithWidths(-0.03f, 0.03f, lineWidth: 0.1f, knifeWidth: 0.28f);

            Assert.IsFalse(PeelEnclosure.IsEnclosed(points, 1f, IgnoredTail, HeightTolerance));
        }

        [Test]
        public void EveryOldLine_IsMeasuredWithItsOwnWidth()
        {
            var points = new List<PeelPoint>();
            AddLine(points, -0.1f, 0f, upwards: true, 0.2f);
            AddLine(points, 0.05f, 0f, upwards: false, 0.1f);
            for (int i = 0; i <= 10; i++)
                points.Add(PointAt(0f, -0.5f + i * 0.1f, 0f, 0.18f));

            Assert.IsTrue(PeelEnclosure.IsEnclosed(points, 1f, IgnoredTail, HeightTolerance));
        }

        [Test]
        public void LineOnOneSideOnly_IsNeverEnclosed()
        {
            var left = TrailWithKnifeBetween(-Width * 0.1f, float.NaN);
            var right = TrailWithKnifeBetween(float.NaN, Width * 0.1f);

            Assert.IsFalse(PeelEnclosure.IsEnclosed(left, 1f, IgnoredTail, HeightTolerance));
            Assert.IsFalse(PeelEnclosure.IsEnclosed(right, 1f, IgnoredTail, HeightTolerance));
        }

        [Test]
        public void TryMeasure_ReportsTheNearestLineOnEachSide()
        {
            var points = TrailWithKnifeBetween(-0.05f, 0.08f);

            bool measured = PeelEnclosure.TryMeasure(points, 1f, IgnoredTail, HeightTolerance, out float left, out float right);

            Assert.IsTrue(measured);
            Assert.AreEqual(0.05f, left, 1e-4f);
            Assert.AreEqual(0.08f, right, 1e-4f);
        }

        [Test]
        public void OwnTailInsideTheIgnoredLength_IsNotMeasured()
        {
            // The only older points are the knife's own last 0.1 of path, which is inside the ignored tail of 0.2.
            var points = new List<PeelPoint>
            {
                PointAt(-0.02f, 0.40f),
                PointAt(-0.02f, 0.45f),
                PointAt(0f, 0.5f),
                PointAt(0f, 0.51f),
            };

            bool measured = PeelEnclosure.TryMeasure(points, 1f, IgnoredTail, HeightTolerance, out float left, out float right);

            Assert.IsTrue(measured);
            Assert.IsTrue(float.IsPositiveInfinity(left));
            Assert.IsTrue(float.IsPositiveInfinity(right));
        }

        [Test]
        public void LinesOnTheFarSideOfTheObject_AreNotMeasured()
        {
            var near = TrailWithKnifeBetween(-0.02f, 0.02f);
            // The same lines, but 0.5 below the surface (along the normal).
            var far = TrailWithKnifeBetween(-0.02f, 0.02f, lineHeight: -0.5f);

            Assert.IsTrue(PeelEnclosure.IsEnclosed(near, 1f, IgnoredTail, HeightTolerance));
            Assert.IsFalse(PeelEnclosure.IsEnclosed(far, 1f, IgnoredTail, HeightTolerance));
        }

        [Test]
        public void ScaleOfTheObject_ScalesTheDistances()
        {
            // At scale 2 the same lines are twice as far apart in the world, so a width that enclosed them at scale 1 does not at scale 2.
            var points = TrailWithKnifeBetween(-Width * 0.5f, Width * 0.5f);

            Assert.IsTrue(PeelEnclosure.IsEnclosed(points, 1f, IgnoredTail, HeightTolerance));
            Assert.IsFalse(PeelEnclosure.IsEnclosed(points, 2f, IgnoredTail, HeightTolerance));
        }
    }
}

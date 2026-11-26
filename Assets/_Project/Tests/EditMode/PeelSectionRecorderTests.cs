using System.Collections.Generic;
using System.Linq;
using Inktober2026.Apple.Peel;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class PeelSectionRecorderTests
    {
        // The surface is the plane y = 0 with the normal up, scale 1. The knife of the tests travels along +Z, so its right side is +X.
        private const float Width = 0.06f;
        private const float Spacing = 0.03f;
        private const float IgnoredTail = 0.2f;
        private const float HeightTolerance = 0.03f;
        private const float Tolerance = 1e-5f;

        private static PeelPoint PointAt(float x, float z)
        {
            return new PeelPoint(new Vector3(x, 0f, z), Vector3.up, Vector3.forward);
        }

        // An older line along x from z = 0 to z = 1, joined to the knife path (z from -0.5 to 0.5 along x = 0) at the end of the line,
        // far from the end of the knife path. A NaN position leaves the line out.
        private static List<PeelPoint> TrailWithKnifeBetween(float leftX, float rightX)
        {
            var points = new List<PeelPoint>();
            bool hasLeft = float.IsNaN(leftX) == false;
            bool hasRight = float.IsNaN(rightX) == false;

            if (hasLeft)
                for (int i = 0; i <= 10; i++)
                    points.Add(PointAt(leftX, (hasRight ? i : 10 - i) * 0.1f));

            if (hasRight)
                for (int i = 0; i <= 10; i++)
                    points.Add(PointAt(rightX, (10 - i) * 0.1f));

            for (int i = 0; i <= 10; i++)
                points.Add(PointAt(0f, -0.5f + i * 0.1f));

            return points;
        }

        private static PeelSectionRecorder NewRecorder()
        {
            return new PeelSectionRecorder(Spacing, IgnoredTail, HeightTolerance);
        }

        // A recorder with one section at the start and the knife gone on along a line bent to the right.
        private PeelSectionRecorder RecorderPastTheFirstSections(List<PeelPoint> points)
        {
            var recorder = new PeelSectionRecorder(Spacing, IgnoredTail, HeightTolerance);
            for (int i = 0; i <= 2; i++)
            {
                points.Add(new PeelPoint(new Vector3(0f, 0.5f, i * Spacing), Vector3.up, Vector3.forward, Width));
                recorder.TryRecord(points, i * Spacing, Width, 1f, out _);
            }

            return recorder;
        }

        private static PeelPoint BentHead(float grown)
        {
            var step = new Vector3(0.5f, 0f, 1f).normalized * (Spacing * grown);
            return new PeelPoint(new Vector3(0f, 0.5f, 2f * Spacing) + step, Vector3.up, Vector3.forward, Width);
        }

        [Test]
        public void Preview_BeforeTheFirstSectionOrWithoutGrowth_IsNothing()
        {
            var empty = new PeelSectionRecorder(Spacing, IgnoredTail, HeightTolerance);
            Assert.IsFalse(empty.TryPreview(BentHead(0.5f), Spacing, Width, 1f, out _));

            var recorder = RecorderPastTheFirstSections(new List<PeelPoint>());
            Assert.IsFalse(recorder.TryPreview(BentHead(0f), 2f * Spacing, Width, 1f, out _));
        }

        [Test]
        public void Preview_GrownToTheSpacing_IsTheSectionThatGetsRecorded()
        {
            var points = new List<PeelPoint>();
            var recorder = RecorderPastTheFirstSections(points);

            Assert.IsTrue(recorder.TryPreview(BentHead(1f), 3.01f * Spacing, Width, 1f, out var preview));
            points.Add(BentHead(1f));
            Assert.IsTrue(recorder.TryRecord(points, 3.01f * Spacing, Width, 1f, out var recorded));

            Assert.AreEqual(recorded.Turn, preview.Turn, 1e-3f);
            Assert.AreEqual(recorded.LeftEdge, preview.LeftEdge, 1e-5f);
            Assert.AreEqual(recorded.RightEdge, preview.RightEdge, 1e-5f);
            Assert.Less(Vector2.Distance(recorded.LeftUv, preview.LeftUv), 1e-5f);
        }

        [Test]
        public void Preview_BringsTheTurnInWithItsLength()
        {
            var recorder = RecorderPastTheFirstSections(new List<PeelPoint>());

            recorder.TryPreview(BentHead(0.25f), 2.25f * Spacing, Width, 1f, out var quarter);
            recorder.TryPreview(BentHead(1f), 3f * Spacing, Width, 1f, out var whole);

            Assert.Greater(whole.Turn, 1f);
            Assert.AreEqual(whole.Turn * 0.25f, quarter.Turn, 0.05f);
        }

        [Test]
        public void Preview_ChangesNothing()
        {
            var plainPoints = new List<PeelPoint>();
            var plain = RecorderPastTheFirstSections(plainPoints);
            var previewedPoints = new List<PeelPoint>();
            var previewed = RecorderPastTheFirstSections(previewedPoints);

            previewed.TryPreview(BentHead(0.3f), 2.3f * Spacing, Width, 1f, out _);
            previewed.TryPreview(BentHead(0.9f), 2.9f * Spacing, Width, 1f, out _);
            plainPoints.Add(BentHead(1f));
            previewedPoints.Add(BentHead(1f));
            plain.TryRecord(plainPoints, 3.01f * Spacing, Width, 1f, out var expected);
            previewed.TryRecord(previewedPoints, 3.01f * Spacing, Width, 1f, out var actual);

            Assert.AreEqual(expected.Turn, actual.Turn, 1e-5f);
            Assert.AreEqual(expected.LeftEdge, actual.LeftEdge, 1e-6f);
            Assert.AreEqual(expected.LeftUv.x, actual.LeftUv.x, 1e-6f);
        }

        [Test]
        public void TheFirstPoint_StartsASectionWithTheFullWidth()
        {
            var recorder = NewRecorder();
            var points = new List<PeelPoint> { PointAt(0f, 0f) };

            bool recorded = recorder.TryRecord(points, 0f, Width, 1f, out var section);

            Assert.That(recorded, Is.True);
            Assert.That(section.Distance, Is.EqualTo(0f));
            Assert.That(section.Width, Is.EqualTo(Width).Within(Tolerance));
            Assert.That(section.LeftEdge, Is.EqualTo(-Width * 0.5f).Within(Tolerance));
        }

        [Test]
        public void PointsCloserThanTheSpacing_RecordNothing()
        {
            var recorder = NewRecorder();
            var points = new List<PeelPoint> { PointAt(0f, 0f) };
            recorder.TryRecord(points, 0f, Width, 1f, out _);

            bool recorded = recorder.TryRecord(points, Spacing * 0.5f, Width, 1f, out _);

            Assert.That(recorded, Is.False);
        }

        [Test]
        public void ThePointAtTheSpacing_ClosesASectionAtItsDistance()
        {
            var recorder = NewRecorder();
            var points = new List<PeelPoint> { PointAt(0f, 0f) };
            recorder.TryRecord(points, 0f, Width, 1f, out _);

            bool recorded = recorder.TryRecord(points, Spacing, Width, 1f, out var section);

            Assert.That(recorded, Is.True);
            Assert.That(section.Distance, Is.EqualTo(Spacing));
        }

        [Test]
        public void AnOlderLineOnTheLeftAtHalfTheWidth_NarrowsTheStripFromTheLeft()
        {
            var recorder = NewRecorder();
            var points = TrailWithKnifeBetween(-Width * 0.5f, float.NaN);

            recorder.TryRecord(points, 1f, Width, 1f, out var section);

            Assert.That(section.LeftEdge, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(section.RightEdge, Is.EqualTo(Width * 0.5f).Within(Tolerance));
        }

        [Test]
        public void AnOlderLineOnTheRightAtHalfTheWidth_NarrowsTheStripFromTheRight()
        {
            var recorder = NewRecorder();
            var points = TrailWithKnifeBetween(float.NaN, Width * 0.5f);

            recorder.TryRecord(points, 1f, Width, 1f, out var section);

            Assert.That(section.LeftEdge, Is.EqualTo(-Width * 0.5f).Within(Tolerance));
            Assert.That(section.RightEdge, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void LinesOnBothSidesAtHalfTheWidth_LeaveNoStrip()
        {
            var recorder = NewRecorder();
            var points = TrailWithKnifeBetween(-Width * 0.5f, Width * 0.5f);

            recorder.TryRecord(points, 1f, Width, 1f, out var section);

            Assert.That(section.Width, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void TheWidthOfASection_IsTheAverageSinceThePreviousSection()
        {
            var recorder = NewRecorder();
            var free = new List<PeelPoint> { PointAt(0f, 0f) };
            var covered = TrailWithKnifeBetween(-Width * 0.5f, float.NaN);
            recorder.TryRecord(free, 0f, Width, 1f, out _);

            recorder.TryRecord(covered, Spacing * 0.4f, Width, 1f, out _);
            recorder.TryRecord(free, Spacing * 1.1f, Width, 1f, out var section);

            // Half of the samples cover 0.03 on the left, the other half cover nothing.
            Assert.That(section.LeftEdge, Is.EqualTo(-Width * 0.5f + Width * 0.25f).Within(Tolerance));
        }

        // The knife goes 0.3 along +Z and then 0.3 along the given direction, in steps of 0.01, and the turns of all sections are collected.
        private static List<float> TurnsAlongBendTowards(Vector3 secondDirection)
        {
            var recorder = NewRecorder();
            var points = new List<PeelPoint>();
            var turns = new List<float>();
            var position = Vector3.zero;
            float length = 0f;

            for (int i = 0; i < 60; i++)
            {
                var direction = i < 30 ? Vector3.forward : secondDirection;
                if (i > 0)
                {
                    position += direction * 0.01f;
                    length += 0.01f;
                }

                points.Add(new PeelPoint(position, Vector3.up, direction));
                if (recorder.TryRecord(points, length, Width, 1f, out var section))
                    turns.Add(section.Turn);
            }

            return turns;
        }

        [Test]
        public void ASectionOnAStraightPath_HasNoTurn()
        {
            var turns = TurnsAlongBendTowards(Vector3.forward);

            Assert.That(turns.Count, Is.GreaterThan(5));
            Assert.That(turns.Sum(), Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void ABendToTheRight_TurnsRightByTheAngleOfTheBend()
        {
            var turns = TurnsAlongBendTowards(Vector3.right);

            Assert.That(turns.Sum(), Is.EqualTo(90f).Within(0.5f));
            Assert.That(turns.Min(), Is.GreaterThan(-0.01f));
        }

        [Test]
        public void ABendToTheLeft_TurnsLeftByTheAngleOfTheBend()
        {
            var turns = TurnsAlongBendTowards(Vector3.left);

            Assert.That(turns.Sum(), Is.EqualTo(-90f).Within(0.5f));
            Assert.That(turns.Max(), Is.LessThan(0.01f));
        }

        // A point on the equator at the given angle around the apple, as the knife would leave it (travelling towards growing U).
        private static PeelPoint PointOnTheEquator(float phi)
        {
            var direction = new Vector3(-Mathf.Cos(phi), 0f, -Mathf.Sin(phi));
            var forward = new Vector3(Mathf.Sin(phi), 0f, -Mathf.Cos(phi));
            return new PeelPoint(direction * 0.5f, Vector3.up, forward);
        }

        [Test]
        public void TheUv_OfTheEdges_IsWhereTheEdgesLieOnTheApple()
        {
            var recorder = NewRecorder();
            var points = new List<PeelPoint> { PointOnTheEquator(Mathf.PI) };

            recorder.TryRecord(points, 0f, Width, 1f, out var section);

            // At the angle of 180 degrees the apple is at +X and U is a half. V is a half on the equator.
            Assert.That(section.LeftUv.x, Is.EqualTo(0.5f).Within(0.02f));
            Assert.That(section.RightUv.x, Is.EqualTo(0.5f).Within(0.02f));
            Assert.That(section.LeftUv.y, Is.EqualTo(0.5f).Within(1e-3f));
        }

        [Test]
        public void TheEdges_AreOnTheSameSideOfTheSeam()
        {
            var recorder = NewRecorder();

            // On the seam, travelling towards the north pole, so the left edge is on one side of the seam and the right edge on the other.
            var points = new List<PeelPoint> { new PeelPoint(new Vector3(-0.5f, 0f, 0f), Vector3.left, Vector3.up) };

            recorder.TryRecord(points, 0f, Width, 1f, out var section);

            Assert.That(Mathf.Abs(section.LeftUv.x - section.RightUv.x), Is.LessThan(0.1f));
            Assert.That(section.RightUv.x, Is.GreaterThan(section.LeftUv.x));
        }

        [Test]
        public void TheUv_KeepsGrowingAcrossTheSeam()
        {
            var recorder = NewRecorder();
            var leftU = new List<float>();
            float length = 0f;

            // Goes around the apple from just before the seam to just after it, in steps of one section.
            for (int i = 0; i < 12; i++)
            {
                var points = new List<PeelPoint> { PointOnTheEquator(2f * Mathf.PI - 0.1f + i * 0.02f) };
                if (recorder.TryRecord(points, length, Width, 1f, out var section))
                    leftU.Add(section.LeftUv.x);

                length += Spacing * 1.01f;
            }

            Assert.That(leftU.Count, Is.EqualTo(12));
            for (int i = 1; i < leftU.Count; i++)
                Assert.That(leftU[i] - leftU[i - 1], Is.InRange(0f, 0.1f), $"U jumped between the sections {i - 1} and {i}.");

            Assert.That(leftU[leftU.Count - 1], Is.GreaterThan(1f));
        }

        [Test]
        public void Clear_StartsANewStrip()
        {
            var recorder = NewRecorder();
            var points = new List<PeelPoint> { PointAt(0f, 0f) };
            recorder.TryRecord(points, 0f, Width, 1f, out _);
            recorder.Clear();

            bool recorded = recorder.TryRecord(points, 0f, Width, 1f, out var section);

            Assert.That(recorded, Is.True);
            Assert.That(section.Width, Is.EqualTo(Width).Within(Tolerance));
        }
    }
}

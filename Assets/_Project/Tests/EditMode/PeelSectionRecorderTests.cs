using System.Collections.Generic;
using Inktober.Apple.Peel;
using NUnit.Framework;
using UnityEngine;

namespace Inktober.Tests.EditMode
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

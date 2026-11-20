using System.Collections.Generic;
using Inktober2026.Apple.Peel;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class PeelSelfIntersectionTests
    {
        // Peeled object: a sphere of this radius at the origin, scale 1.
        private const float Radius = 0.5f;
        private const float Step = 0.01f;
        private const float IgnoredTail = 0.2f;
        private const float HeightTolerance = 0.03f;

        private static PeelPoint PointAt(Vector3 position)
        {
            var normal = position.normalized;
            return new PeelPoint(position, normal, Vector3.Cross(Vector3.up, normal).normalized);
        }

        private static Vector3 Direction(float latitudeDegrees, float longitudeDegrees)
        {
            float latitude = latitudeDegrees * Mathf.Deg2Rad;
            float longitude = longitudeDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(latitude) * Mathf.Cos(longitude), Mathf.Sin(latitude), Mathf.Cos(latitude) * Mathf.Sin(longitude)).normalized;
        }

        private static bool Crosses(IReadOnlyList<PeelPoint> points, float ignoredTail = IgnoredTail)
        {
            return PeelSelfIntersection.TryFind(points, 1f, ignoredTail, HeightTolerance, out _);
        }

        // Adds points along the great circle arc between two directions and reports at which point count the trail first crossed itself (or -1).
        private static int Walk(List<PeelPoint> points, Vector3 from, Vector3 to)
        {
            float angle = Vector3.Angle(from, to) * Mathf.Deg2Rad;
            int steps = Mathf.Max(1, Mathf.CeilToInt(angle * Radius / Step));
            int firstHit = -1;
            for (int i = points.Count == 0 ? 0 : 1; i <= steps; i++)
            {
                points.Add(PointAt(Vector3.Slerp(from, to, (float)i / steps) * Radius));
                if (firstHit < 0 && Crosses(points))
                    firstHit = points.Count;
            }

            return firstHit;
        }

        [Test]
        public void FewerThanThreePoints_NeverCross()
        {
            var points = new List<PeelPoint> { PointAt(new Vector3(0, 0.5f, 0)), PointAt(new Vector3(0.1f, 0.49f, 0)) };

            Assert.IsFalse(Crosses(points));
        }

        [Test]
        public void StraightPath_NeverCrosses()
        {
            var points = new List<PeelPoint>();

            int hit = Walk(points, Direction(0, 0), Direction(0, 90));

            Assert.AreEqual(-1, hit);
        }

        [Test]
        public void PathCrossingItself_IsDetectedOnTheCrossingLeg_AndNotBefore()
        {
            // Along the equator, up a meridian, back west at latitude 60, then down through the equator: the last leg cuts the first one.
            var points = new List<PeelPoint>();
            int hitBefore = Walk(points, Direction(0, 0), Direction(0, 90));
            hitBefore = Mathf.Max(hitBefore, Walk(points, Direction(0, 90), Direction(60, 90)));
            hitBefore = Mathf.Max(hitBefore, Walk(points, Direction(60, 90), Direction(60, 45)));
            int pointsBeforeLastLeg = points.Count;

            int hit = Walk(points, Direction(60, 45), Direction(-30, 45));

            Assert.AreEqual(-1, hitBefore, "No crossing is expected before the last leg.");
            Assert.Greater(hit, pointsBeforeLastLeg, "The last leg crosses the first one.");
        }

        [Test]
        public void SegmentCrossingOnTheNearSide_IsACrossing()
        {
            var points = new List<PeelPoint>
            {
                PointAt(new Vector3(0, 0.4975f, -0.05f)),
                PointAt(new Vector3(0, 0.4975f, 0.05f)),
                PointAt(new Vector3(0.45f, 0.1f, 0.2f)),
                PointAt(new Vector3(-0.05f, 0.4975f, 0f)),
                PointAt(new Vector3(0.05f, 0.4975f, 0f)),
            };

            bool crossed = PeelSelfIntersection.TryFind(points, 1f, IgnoredTail, HeightTolerance, out var crossing);

            Assert.IsTrue(crossed);
            Assert.AreEqual(0f, crossing.x, 1e-3f);
            Assert.AreEqual(0.4975f, crossing.y, 1e-3f);
            Assert.AreEqual(0f, crossing.z, 1e-3f);
        }

        [Test]
        public void SegmentsWithTheSameProjectionOnTheFarSide_AreNotACrossing()
        {
            var points = new List<PeelPoint>
            {
                PointAt(new Vector3(0, -0.4975f, -0.05f)),
                PointAt(new Vector3(0, -0.4975f, 0.05f)),
                PointAt(new Vector3(0.45f, 0.1f, 0.2f)),
                PointAt(new Vector3(-0.05f, 0.4975f, 0f)),
                PointAt(new Vector3(0.05f, 0.4975f, 0f)),
            };

            Assert.IsFalse(Crosses(points));
        }

        [Test]
        public void CrossingInsideTheIgnoredTail_IsNotTested()
        {
            var points = new List<PeelPoint>
            {
                PointAt(new Vector3(0, 0.4975f, -0.05f)),
                PointAt(new Vector3(0, 0.4975f, 0.05f)),
                PointAt(new Vector3(-0.05f, 0.4975f, 0f)),
                PointAt(new Vector3(0.05f, 0.4975f, 0f)),
            };

            Assert.IsFalse(Crosses(points, ignoredTail: 0.3f), "The old segment is within 0.3 along the trail.");
            Assert.IsTrue(Crosses(points, ignoredTail: 0f), "Without an ignored tail it is a crossing.");
        }

        [Test]
        public void SharpTurn_TouchingTheAdjacentSegment_IsNotACrossing()
        {
            // The newest segment shares its start point with the previous one, so they only touch.
            var points = new List<PeelPoint>
            {
                PointAt(new Vector3(0, 0.4975f, -0.2f)),
                PointAt(new Vector3(0, 0.4975f, 0f)),
                PointAt(new Vector3(0.02f, 0.4975f, -0.2f)),
            };

            Assert.IsFalse(Crosses(points, ignoredTail: 0f));
        }

        [Test]
        public void ScaleOfTheObject_ChangesTheIgnoredTail()
        {
            var points = new List<PeelPoint>
            {
                PointAt(new Vector3(0, 0.4975f, -0.05f)),
                PointAt(new Vector3(0, 0.4975f, 0.05f)),
                PointAt(new Vector3(-0.05f, 0.4975f, 0f)),
                PointAt(new Vector3(0.05f, 0.4975f, 0f)),
            };

            // At scale 2 the same trail is twice as long in the world (0.54), so it is longer than the ignored tail of 0.3.
            bool crossed = PeelSelfIntersection.TryFind(points, 2f, 0.3f, HeightTolerance, out _);

            Assert.IsTrue(crossed);
        }
    }
}

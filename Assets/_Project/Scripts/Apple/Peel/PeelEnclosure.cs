using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// Finds out whether the knife is cutting between two older lines so closely that no new peel is made.
    /// Every point has the width it was peeled with. The newest point peels the area up to half its width on both sides,
    /// and it peels nothing new when the strips of older lines, each with its own width, already cover that whole span.
    /// The points are in the local space of the peeled object.
    /// </summary>
    public static class PeelEnclosure
    {
        /// <summary>At how many places across the newest strip the cover is tested, both edges included. A gap narrower than the distance between them can be missed.</summary>
        public const int SpanSamples = 33;

        // Local units. Lets strips that only touch count as touching despite the rounding.
        private const float CoverTolerance = 1e-5f;

        /// <summary>
        /// Measures the distance from the newest point to the nearest older segment on each side of the knife path.
        /// A side without any older segment in range reports <see cref="float.PositiveInfinity"/>.
        /// </summary>
        /// <param name="points">The trail, oldest first.</param>
        /// <param name="scale">World size of one local unit (uniform scale of the peeled object).</param>
        /// <param name="ignoredTailLength">Segments closer than this (in world units, along the trail) to the newest point are not measured, so the knife does not measure its own tail.</param>
        /// <param name="heightTolerance">Largest distance along the normal between the newest point and an older segment that still counts as the same surface, in world units (rejects the far side of the object).</param>
        /// <param name="left">Distance to the nearest older line on the left of the knife, in world units.</param>
        /// <param name="right">Distance to the nearest older line on the right of the knife, in world units.</param>
        /// <returns>False when there are too few points to measure.</returns>
        public static bool TryMeasure(IReadOnlyList<PeelPoint> points, float scale, float ignoredTailLength, float heightTolerance, out float left, out float right)
        {
            left = float.PositiveInfinity;
            right = float.PositiveInfinity;
            int count = points.Count;
            if (count < 3)
                return false;

            var newest = points[count - 1];
            var along = Vector3.ProjectOnPlane(newest.Position - points[count - 2].Position, newest.Normal);
            if (along.sqrMagnitude < 1e-12f)
                return false;

            // Positive side is to the right of the direction of travel (the same convention as PeelPoint.Right).
            var axisY = Vector3.Cross(newest.Normal, along.normalized);
            float safeScale = Mathf.Max(scale, 1e-6f);
            float localTolerance = heightTolerance / safeScale;

            // The newest segment itself is not measured, but its length counts as the beginning of the ignored tail.
            float tail = (newest.Position - points[count - 2].Position).magnitude * safeScale;
            for (int i = count - 3; i >= 0; i--)
            {
                var from = points[i].Position;
                var to = points[i + 1].Position;
                tail += (to - from).magnitude * safeScale;
                if (tail < ignoredTailLength)
                    continue;

                var offset = ClosestPoint(newest.Position, from, to) - newest.Position;
                if (Mathf.Abs(Vector3.Dot(offset, newest.Normal)) > localTolerance)
                    continue;

                float distance = offset.magnitude * safeScale;
                if (Vector3.Dot(offset, axisY) < 0f)
                {
                    left = Mathf.Min(left, distance);
                    continue;
                }

                right = Mathf.Min(right, distance);
            }

            return true;
        }

        /// <summary>
        /// True when the newest point peels nothing new: the strips of older lines cover its whole width, and there is such a line on both sides
        /// of the knife (a knife that goes along a single wider strip is not between two lines).
        /// The span across the newest point is tested at <see cref="SpanSamples"/> places in the plane tangent to the surface.
        /// Every older segment covers what is closer to it than half the width it was peeled with. See <see cref="TryMeasure"/> for the arguments.
        /// </summary>
        public static bool IsEnclosed(IReadOnlyList<PeelPoint> points, float scale, float ignoredTailLength, float heightTolerance)
        {
            int count = points.Count;
            if (count < 3)
                return false;

            var newest = points[count - 1];
            var along = Vector3.ProjectOnPlane(newest.Position - points[count - 2].Position, newest.Normal);
            if (along.sqrMagnitude < 1e-12f)
                return false;

            var axisX = along.normalized;
            var axisY = Vector3.Cross(newest.Normal, axisX);
            float safeScale = Mathf.Max(scale, 1e-6f);
            float localTolerance = heightTolerance / safeScale;
            float halfSpan = newest.Width * 0.5f / safeScale;
            if (halfSpan <= 0f)
                return false;

            long allCovered = (1L << SpanSamples) - 1;
            long covered = 0;
            bool hasLeft = false;
            bool hasRight = false;

            // The newest segment itself is not measured, but its length counts as the beginning of the ignored tail.
            float tail = (newest.Position - points[count - 2].Position).magnitude * safeScale;
            for (int i = count - 3; i >= 0; i--)
            {
                var from = points[i].Position;
                var to = points[i + 1].Position;
                tail += (to - from).magnitude * safeScale;
                if (tail < ignoredTailLength)
                    continue;

                var offset = ClosestPoint(newest.Position, from, to) - newest.Position;
                if (Mathf.Abs(Vector3.Dot(offset, newest.Normal)) > localTolerance)
                    continue;

                // A segment is peeled with the width of the point that ends it.
                float halfWidth = points[i + 1].Width * 0.5f / safeScale + CoverTolerance;
                var start = ToPlane(from - newest.Position, axisX, axisY);
                var end = ToPlane(to - newest.Position, axisX, axisY);
                if (((Vector2)ClosestPoint(Vector3.zero, start, end)).magnitude > halfSpan + halfWidth)
                    continue;

                long segmentCovers = 0;
                for (int sample = 0; sample < SpanSamples; sample++)
                {
                    var place = new Vector3(0f, Mathf.Lerp(-halfSpan, halfSpan, sample / (SpanSamples - 1f)));
                    if ((ClosestPoint(place, start, end) - place).sqrMagnitude <= halfWidth * halfWidth)
                        segmentCovers |= 1L << sample;
                }

                if (segmentCovers == 0)
                    continue;

                covered |= segmentCovers;
                // Positive side is to the right of the direction of travel, like in TryMeasure.
                if (Vector3.Dot(offset, axisY) < 0f)
                    hasLeft = true;
                else
                    hasRight = true;

                if (hasLeft && hasRight && covered == allCovered)
                    return true;
            }

            return false;
        }

        private static Vector3 ToPlane(Vector3 offset, Vector3 axisX, Vector3 axisY)
        {
            return new Vector3(Vector3.Dot(offset, axisX), Vector3.Dot(offset, axisY));
        }

        private static Vector3 ClosestPoint(Vector3 point, Vector3 from, Vector3 to)
        {
            var direction = to - from;
            float lengthSquared = direction.sqrMagnitude;
            if (lengthSquared < 1e-12f)
                return from;

            float t = Mathf.Clamp01(Vector3.Dot(point - from, direction) / lengthSquared);
            return from + direction * t;
        }
    }
}

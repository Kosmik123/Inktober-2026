using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// Finds out whether the knife is cutting between two older lines so closely that no new peel is made.
    /// The peeled strip is <c>width</c> wide, so the newest point peels the area up to half the width on both sides.
    /// With the nearest older line at <c>left</c> and <c>right</c> (distances between the centres of the strips),
    /// the strips of those lines cover the newest one completely when <c>left + right &lt;= width</c>.
    /// The points are in the local space of the peeled object.
    /// </summary>
    public static class PeelEnclosure
    {
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
        /// True when the strips of the nearest older lines on both sides cover the strip of the newest point,
        /// so the knife peeled nothing new. See <see cref="TryMeasure"/> for the arguments.
        /// </summary>
        /// <param name="width">Width of the peeled strip, in world units.</param>
        public static bool IsEnclosed(IReadOnlyList<PeelPoint> points, float width, float scale, float ignoredTailLength, float heightTolerance)
        {
            if (TryMeasure(points, scale, ignoredTailLength, heightTolerance, out float left, out float right) == false)
                return false;

            // Infinity on one side keeps the sum infinite, so a side without a line never encloses.
            return left + right <= width;
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

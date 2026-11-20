using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// Finds out whether the newest segment of the peel trail crosses an older one. The trail has no thickness: it is a line,
    /// and the game is lost when the line crosses itself. The points are in the local space of the peeled object.
    /// </summary>
    public static class PeelSelfIntersection
    {
        /// <summary>
        /// Tests the newest segment (the last two points) against all older segments.
        /// Segments lie on a curved surface, so they never meet exactly in 3D. The test is done in the plane tangent to the surface
        /// at the end of the newest segment, and a crossing counts only if the two segments are within <paramref name="heightTolerance"/>
        /// of each other along the normal (this rejects the far side of the object).
        /// </summary>
        /// <param name="points">The trail, oldest first.</param>
        /// <param name="scale">World size of one local unit (uniform scale of the peeled object).</param>
        /// <param name="ignoredTailLength">Segments closer than this (in world units, along the trail) to the newest one are not tested.</param>
        /// <param name="heightTolerance">Largest distance along the normal between two segments that still counts as crossing, in world units.</param>
        /// <param name="crossing">Where the segments cross, in local space.</param>
        public static bool TryFind(IReadOnlyList<PeelPoint> points, float scale, float ignoredTailLength, float heightTolerance, out Vector3 crossing)
        {
            crossing = default;
            int count = points.Count;
            if (count < 3)
                return false;

            var newStart = points[count - 2].Position;
            var newEnd = points[count - 1].Position;
            var normal = points[count - 1].Normal;

            var along = Vector3.ProjectOnPlane(newEnd - newStart, normal);
            float newLength = along.magnitude;
            if (newLength < 1e-6f)
                return false;

            var axisX = along / newLength;
            var axisY = Vector3.Cross(normal, axisX);
            float localTolerance = heightTolerance / Mathf.Max(scale, 1e-6f);

            // The segment right before the newest one shares its start point, so it only touches the newest one and is never tested.
            float tail = ((newEnd - newStart).magnitude + (newStart - points[count - 3].Position).magnitude) * scale;
            for (int i = count - 4; i >= 0; i--)
            {
                var from = points[i].Position;
                var to = points[i + 1].Position;
                tail += (to - from).magnitude * scale;
                if (tail < ignoredTailLength)
                    continue;

                if (TrySegmentCrossing(newStart, axisX, axisY, normal, newLength, localTolerance, from, to, out crossing))
                    return true;
            }

            return false;
        }

        // The newest segment is the section 0..length of the X axis. The old segment is crossed when its ends are on different sides of that axis.
        private static bool TrySegmentCrossing(Vector3 origin, Vector3 axisX, Vector3 axisY, Vector3 normal, float length, float tolerance, Vector3 from, Vector3 to, out Vector3 crossing)
        {
            crossing = default;
            var fromOffset = from - origin;
            var toOffset = to - origin;

            float y0 = Vector3.Dot(fromOffset, axisY);
            float y1 = Vector3.Dot(toOffset, axisY);
            if ((y0 > 0f && y1 > 0f) || (y0 < 0f && y1 < 0f))
                return false;

            float dy = y0 - y1;
            if (Mathf.Abs(dy) < 1e-9f)
                return false;

            float t = y0 / dy;
            float x0 = Vector3.Dot(fromOffset, axisX);
            float x1 = Vector3.Dot(toOffset, axisX);
            float x = x0 + t * (x1 - x0);
            if (x < 0f || x > length)
                return false;

            float h0 = Vector3.Dot(fromOffset, normal);
            float h1 = Vector3.Dot(toOffset, normal);
            float h = h0 + t * (h1 - h0);
            if (Mathf.Abs(h) > tolerance)
                return false;

            crossing = Vector3.Lerp(from, to, t);
            return true;
        }
    }
}

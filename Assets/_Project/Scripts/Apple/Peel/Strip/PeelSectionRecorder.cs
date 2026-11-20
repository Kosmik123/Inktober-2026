using System.Collections.Generic;
using Inktober.Apple.Shape;
using UnityEngine;

namespace Inktober.Apple.Peel
{
    /// <summary>
    /// Turns the stream of trail points into cross sections of the peeled strip, one for every <c>spacing</c> of peel length.
    /// The width of a section is the average over the points since the previous section,
    /// so a single odd measurement does not jag the edge and a narrow place is not missed between two sections.
    /// </summary>
    public class PeelSectionRecorder
    {
        private readonly float spacing;
        private readonly float ignoredTailLength;
        private readonly float heightTolerance;

        private float coverLeftSum;
        private float coverRightSum;
        private int samples;
        private float lastDistance;
        private bool hasSection;
        private Vector3 lastPosition;
        private Vector3 lastChord;
        private bool hasLastChord;
        private float lastLeftU;
        private float lastRightU;
        private bool hasUv;

        /// <param name="spacing">Peel length between two sections, in world units.</param>
        /// <param name="ignoredTailLength">See <see cref="PeelEnclosure.TryMeasure"/>.</param>
        /// <param name="heightTolerance">See <see cref="PeelEnclosure.TryMeasure"/>.</param>
        public PeelSectionRecorder(float spacing, float ignoredTailLength, float heightTolerance)
        {
            this.spacing = spacing;
            this.ignoredTailLength = ignoredTailLength;
            this.heightTolerance = heightTolerance;
        }

        /// <summary>
        /// Measures the newest point and closes a section when the peel is long enough since the previous one.
        /// The first point always starts a section.
        /// </summary>
        /// <param name="points">The trail with the newest point added, oldest first.</param>
        /// <param name="length">Length of the peel up to the newest point, in world units.</param>
        /// <param name="width">Width of the brush, in world units.</param>
        /// <param name="scale">World size of one local unit of the peeled object.</param>
        public bool TryRecord(IReadOnlyList<PeelPoint> points, float length, float width, float scale, out PeelSection section)
        {
            section = default;

            if (PeelEnclosure.TryMeasure(points, scale, ignoredTailLength, heightTolerance, out float left, out float right))
            {
                coverLeftSum += PeelStripWidth.Cover(left, width);
                coverRightSum += PeelStripWidth.Cover(right, width);
            }

            samples++;

            if (hasSection && length - lastDistance < spacing)
                return false;

            PeelStripWidth.Edges(coverLeftSum / samples, coverRightSum / samples, width, out float leftEdge, out float rightEdge);

            var newest = points[points.Count - 1];
            float turn = MeasureTurn(newest);
            MeasureUv(newest, leftEdge, rightEdge, scale, out var leftUv, out var rightUv);
            section = new PeelSection(length, leftEdge, rightEdge, turn, leftUv, rightUv);

            coverLeftSum = 0f;
            coverRightSum = 0f;
            samples = 0;
            lastDistance = length;
            hasSection = true;
            return true;
        }

        /// <summary>Forgets everything, so the next point starts a new strip.</summary>
        public void Clear()
        {
            coverLeftSum = 0f;
            coverRightSum = 0f;
            samples = 0;
            lastDistance = 0f;
            hasSection = false;
            hasLastChord = false;
            hasUv = false;
        }

        /// <summary>
        /// The turn of the path since the previous section: the angle between the chords (the straight lines between the points
        /// of two neighbouring sections), measured along the surface at the newest point. The chord is a few centimetres long,
        /// so the small zigzag of single points does not count as turning.
        /// </summary>
        private float MeasureTurn(PeelPoint newest)
        {
            var chord = Vector3.ProjectOnPlane(newest.Position - lastPosition, newest.Normal);
            bool hasChord = hasSection && chord.sqrMagnitude > 1e-12f;
            float turn = 0f;

            if (hasChord && hasLastChord)
                turn = Vector3.SignedAngle(lastChord, chord, newest.Normal);

            if (hasChord)
            {
                lastChord = chord;
                hasLastChord = true;
            }

            lastPosition = newest.Position;
            return turn;
        }

        /// <summary>
        /// Where the two edges of the strip lay on the textures of the apple. The points are in the local space of the apple,
        /// so the direction from its center is enough. U is shifted by whole numbers so that it does not jump at the seam
        /// between two sections (the textures repeat), and both edges of a section stay on the same side of the seam.
        /// </summary>
        private void MeasureUv(PeelPoint newest, float leftEdge, float rightEdge, float scale, out Vector2 leftUv, out Vector2 rightUv)
        {
            float safeScale = Mathf.Max(scale, 1e-6f);
            var right = newest.Right;
            leftUv = AppleUv.FromDirection(newest.Position + right * (leftEdge / safeScale));
            rightUv = AppleUv.FromDirection(newest.Position + right * (rightEdge / safeScale));

            leftUv.x += Mathf.Round((hasUv ? lastLeftU : leftUv.x) - leftUv.x);
            rightUv.x += Mathf.Round((hasUv ? lastRightU : leftUv.x) - rightUv.x);

            lastLeftU = leftUv.x;
            lastRightU = rightUv.x;
            hasUv = true;
        }
    }
}

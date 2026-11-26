using System.Collections.Generic;
using Inktober2026.Apple.Shape;
using UnityEngine;

namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// Turns the stream of trail points into cross sections of the peeled strip, one for every <c>spacing</c> of peel length.
    /// The width of a section is the average over the points since the previous section,
    /// so a single odd measurement does not jag the edge and a narrow place is not missed between two sections.
    /// </summary>
    public class PeelSectionRecorder
    {
        private const float MinChordSquared = 1e-12f;
        private const float MinPreviewLength = 1e-5f;

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
        private float lastLeftEdge;
        private float lastRightEdge;

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
                coverLeftSum += PeelStripWidthUtility.Cover(left, width);
                coverRightSum += PeelStripWidthUtility.Cover(right, width);
            }

            samples++;

            if (hasSection && length - lastDistance < spacing)
                return false;

            PeelStripWidthUtility.Edges(coverLeftSum / samples, coverRightSum / samples, width, out float leftEdge, out float rightEdge);

            var newest = points[points.Count - 1];
            var chord = Chord(newest);
            float turn = MeasureTurn(chord, newest.Normal);
            MeasureUv(newest, leftEdge, rightEdge, scale, out var leftUv, out var rightUv);
            section = new PeelSection(length, leftEdge, rightEdge, turn, leftUv, rightUv);

            if (hasSection && chord.sqrMagnitude > MinChordSquared)
            {
                lastChord = chord;
                hasLastChord = true;
            }

            lastPosition = newest.Position;
            lastLeftEdge = leftEdge;
            lastRightEdge = rightEdge;
            lastLeftU = leftUv.x;
            lastRightU = rightUv.x;
            hasUv = true;
            coverLeftSum = 0f;
            coverRightSum = 0f;
            samples = 0;
            lastDistance = length;
            hasSection = true;
            return true;
        }

        /// <summary>
        /// The section still growing at the knife: from the last recorded one to where the knife is now. Nothing is recorded or changed.
        /// Its turn and the change of its edges come in as it grows to the length of the spacing, so it starts as a copy of the last section
        /// and ends as the section that is going to be recorded. Without that the direction of a very short piece, which is mostly noise,
        /// would swing the whole strip.
        /// </summary>
        /// <param name="head">Where the knife is now.</param>
        /// <param name="length">Length of the peel up to the knife, in world units.</param>
        /// <param name="width">Width of the brush, in world units.</param>
        /// <param name="scale">World size of one local unit of the peeled object.</param>
        /// <returns>False before the first section and when the peel has not grown since the last one.</returns>
        public bool TryPreview(PeelPoint head, float length, float width, float scale, out PeelSection section)
        {
            section = default;
            if (hasSection == false || length - lastDistance <= MinPreviewLength)
                return false;

            float grown = Mathf.Clamp01((length - lastDistance) / Mathf.Max(spacing, MinPreviewLength));
            float targetLeft = lastLeftEdge;
            float targetRight = lastRightEdge;
            if (samples > 0)
                PeelStripWidthUtility.Edges(coverLeftSum / samples, coverRightSum / samples, width, out targetLeft, out targetRight);

            float leftEdge = Mathf.Lerp(lastLeftEdge, targetLeft, grown);
            float rightEdge = Mathf.Lerp(lastRightEdge, targetRight, grown);
            float turn = MeasureTurn(Chord(head), head.Normal) * grown;
            MeasureUv(head, leftEdge, rightEdge, scale, out var leftUv, out var rightUv);
            section = new PeelSection(length, leftEdge, rightEdge, turn, leftUv, rightUv);
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

        // The straight line from the last section to the point, along the surface at the point.
        private Vector3 Chord(PeelPoint point)
        {
            return Vector3.ProjectOnPlane(point.Position - lastPosition, point.Normal);
        }

        /// <summary>
        /// The turn of the path since the previous section: the angle between the chords (the straight lines between the points
        /// of two neighbouring sections), measured along the surface at the newest point. The chord is a few centimetres long,
        /// so the small zigzag of single points does not count as turning.
        /// </summary>
        private float MeasureTurn(Vector3 chord, Vector3 normal)
        {
            if (hasSection == false || hasLastChord == false || chord.sqrMagnitude <= MinChordSquared)
                return 0f;

            return Vector3.SignedAngle(lastChord, chord, normal);
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
        }
    }
}

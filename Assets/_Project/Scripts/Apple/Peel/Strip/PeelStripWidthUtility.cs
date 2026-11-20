using UnityEngine;

namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// How wide the strip peeled by the newest point is. The brush paints a band of <c>width</c> around the knife path,
    /// so an older line at distance <c>d</c> (between the centres of the two bands) has already peeled <c>width - d</c> of it.
    /// What is left of the band is the new strip. The distances come from <see cref="PeelEnclosure.TryMeasure"/>.
    /// </summary>
    public static class PeelStripWidthUtility
    {
        /// <summary>
        /// How much of the band of the newest point the nearest older line on one side has peeled before, from 0 to <paramref name="width"/>.
        /// No line on that side (<see cref="float.PositiveInfinity"/>) covers nothing.
        /// </summary>
        public static float Cover(float distanceToOlderLine, float width)
        {
            return Mathf.Clamp(width - distanceToOlderLine, 0f, width);
        }

        /// <summary>
        /// Edges of the new strip across the knife path, given what the older lines cover on each side.
        /// When they cover everything the strip has no width and both edges meet in the middle of what is left.
        /// </summary>
        public static void Edges(float coverLeft, float coverRight, float width, out float leftEdge, out float rightEdge)
        {
            float half = width * 0.5f;
            leftEdge = -half + coverLeft;
            rightEdge = half - coverRight;

            if (leftEdge <= rightEdge)
                return;

            float middle = (leftEdge + rightEdge) * 0.5f;
            leftEdge = middle;
            rightEdge = middle;
        }
    }
}

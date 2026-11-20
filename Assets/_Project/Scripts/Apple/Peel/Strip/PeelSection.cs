using UnityEngine;

namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// One cross section of the peeled strip: how far along the strip it is and where its edges are across the knife path.
    /// The edges are offsets from the centre line of the knife path, negative to the left, positive to the right.
    /// </summary>
    public readonly struct PeelSection
    {
        public PeelSection(float distance, float leftEdge, float rightEdge, float turn = 0f, Vector2 leftUv = default, Vector2 rightUv = default)
        {
            Distance = distance;
            LeftEdge = leftEdge;
            RightEdge = rightEdge;
            Turn = turn;
            LeftUv = leftUv;
            RightUv = rightUv;
        }

        /// <summary>Length of the peel at the section, in world units.</summary>
        public float Distance { get; }

        /// <summary>Offset of the left edge of the strip from the centre line, in world units.</summary>
        public float LeftEdge { get; }

        /// <summary>Offset of the right edge of the strip from the centre line, in world units.</summary>
        public float RightEdge { get; }

        /// <summary>How far the knife path turned since the previous section, in degrees along the surface. Positive turns right.</summary>
        public float Turn { get; }

        /// <summary>Where the left edge lay on the textures of the apple. U goes on past 1 instead of jumping back at the seam.</summary>
        public Vector2 LeftUv { get; }

        /// <summary>Where the right edge lay on the textures of the apple. U goes on past 1 instead of jumping back at the seam.</summary>
        public Vector2 RightUv { get; }

        /// <summary>Width of the strip at the section, in world units.</summary>
        public float Width => RightEdge - LeftEdge;
    }
}

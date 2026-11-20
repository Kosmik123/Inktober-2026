namespace Inktober.Apple.Peel
{
    /// <summary>
    /// One cross section of the peeled strip: how far along the strip it is and where its edges are across the knife path.
    /// The edges are offsets from the centre line of the knife path, negative to the left, positive to the right.
    /// </summary>
    public readonly struct PeelSection
    {
        public PeelSection(float distance, float leftEdge, float rightEdge)
        {
            Distance = distance;
            LeftEdge = leftEdge;
            RightEdge = rightEdge;
        }

        /// <summary>Length of the peel at the section, in world units.</summary>
        public float Distance { get; }

        /// <summary>Offset of the left edge of the strip from the centre line, in world units.</summary>
        public float LeftEdge { get; }

        /// <summary>Offset of the right edge of the strip from the centre line, in world units.</summary>
        public float RightEdge { get; }

        /// <summary>Width of the strip at the section, in world units.</summary>
        public float Width => RightEdge - LeftEdge;
    }
}

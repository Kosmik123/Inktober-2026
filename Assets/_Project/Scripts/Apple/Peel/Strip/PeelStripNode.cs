using UnityEngine;

namespace Inktober.Apple.Peel
{
    /// <summary>Where the centre line of the strip is at one section and how the strip is turned there, in the local space of the strip object.</summary>
    public readonly struct PeelStripNode
    {
        public PeelStripNode(Vector3 position, Vector3 right, Vector3 up)
        {
            Position = position;
            Right = right;
            Up = up;
        }

        /// <summary>Point of the centre line.</summary>
        public Vector3 Position { get; }

        /// <summary>Unit direction across the strip, towards its right edge.</summary>
        public Vector3 Right { get; }

        /// <summary>Unit direction out of the face of the strip (the skin side).</summary>
        public Vector3 Up { get; }
    }
}

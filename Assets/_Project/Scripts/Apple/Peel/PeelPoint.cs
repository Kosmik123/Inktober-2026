using UnityEngine;

namespace Inktober2026.Apple.Peel
{
    /// <summary>One sample of the knife path, in the local space of the peeled object (it moves and turns with the apple).</summary>
    public readonly struct PeelPoint
    {
        public PeelPoint(Vector3 position, Vector3 normal, Vector3 forward)
        {
            Position = position;
            Normal = normal;
            Forward = forward;
        }

        /// <summary>Centre of the peeled strip.</summary>
        public Vector3 Position { get; }

        /// <summary>Surface normal at the point.</summary>
        public Vector3 Normal { get; }

        /// <summary>Direction the knife was moving, tangent to the surface.</summary>
        public Vector3 Forward { get; }

        /// <summary>Direction across the strip, tangent to the surface.</summary>
        public Vector3 Right => Vector3.Cross(Normal, Forward).normalized;
    }
}

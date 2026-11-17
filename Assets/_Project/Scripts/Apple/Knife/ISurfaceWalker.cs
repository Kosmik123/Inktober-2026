using UnityEngine;

namespace Inktober.Apple.Knife
{
    /// <summary>
    /// Strategy that moves a point across the surface of an object.
    /// It only knows geometry: it does not read input and does not move any transform.
    /// </summary>
    public interface ISurfaceWalker
    {
        /// <summary>False before <see cref="Place"/> succeeds and after the walker leaves the surface.</summary>
        bool IsOnSurface { get; }

        /// <summary>Current point on the surface.</summary>
        Vector3 Position { get; }

        /// <summary>Surface normal at <see cref="Position"/>, smoothed so it is safe to drive a camera with.</summary>
        Vector3 Normal { get; }

        /// <summary>Unit vector tangent to the surface, the direction of travel.</summary>
        Vector3 Forward { get; }

        /// <summary>Puts the walker on the first surface hit from <paramref name="origin"/> along <paramref name="direction"/>.</summary>
        bool Place(Vector3 origin, Vector3 direction, Vector3 forwardHint);

        /// <summary>Rotates the direction of travel around the surface normal. Positive turns right.</summary>
        void Turn(float degrees);

        /// <summary>Moves forward along the surface by the given distance.</summary>
        void Advance(float distance);
    }
}

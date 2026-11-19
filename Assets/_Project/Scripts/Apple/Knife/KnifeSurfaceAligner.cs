using UnityEngine;

namespace Inktober.Apple.Knife
{
    /// <summary>
    /// Keeps the surface in place and lets it only rotate around its center, so the knife stays almost still.
    /// The walker finds where the knife would be next. The surface is rotated so that point comes back under the knife,
    /// onto the line from the center through the knife's home direction. The difference in distance from the center is taken
    /// by the knife, which slides along that line, and it is tilted to match the surface at the new point.
    /// </summary>
    /// <remarks>
    /// The knife does not slide along its own tilted up axis: the surface slopes make that axis lean against the direction
    /// of travel, so every step would push the knife a little backwards and it would creep around the apple.
    /// </remarks>
    public static class KnifeSurfaceAligner
    {
        private const float Epsilon = 1e-6f;

        /// <summary>
        /// Rotates <paramref name="surface"/> around its position and moves <paramref name="knife"/> to the walker point.
        /// <paramref name="homeDirection"/> is the world direction from the center to the knife, <paramref name="heading"/>
        /// is the world direction the knife keeps facing. The surface turns under both.
        /// </summary>
        public static void Apply(ISurfaceWalker walker, Transform knife, Transform surface, Vector3 homeDirection, Vector3 heading)
        {
            if (walker.IsOnSurface == false)
                return;

            var center = surface.position;
            var rotation = SolveRotation(center, homeDirection, heading, walker.Position, walker.Forward);
            var delta = Matrix4x4.Translate(center) * Matrix4x4.Rotate(rotation) * Matrix4x4.Translate(-center);

            surface.rotation = rotation * surface.rotation;
            Physics.SyncTransforms();
            walker.Reanchor(delta);

            knife.SetPositionAndRotation(walker.Position, Quaternion.LookRotation(walker.Forward, walker.Normal));
        }

        /// <summary>
        /// The rotation around <paramref name="center"/> that moves <paramref name="surfacePoint"/> onto the ray from the center
        /// along <paramref name="homeDirection"/> and turns <paramref name="surfaceForward"/> towards <paramref name="heading"/>
        /// (both measured along the surface there).
        /// </summary>
        public static Quaternion SolveRotation(
            Vector3 center,
            Vector3 homeDirection,
            Vector3 heading,
            Vector3 surfacePoint,
            Vector3 surfaceForward)
        {
            var fromOffset = surfacePoint - center;
            if (fromOffset.sqrMagnitude < Epsilon)
                return Quaternion.identity;

            var from = fromOffset.normalized;
            var to = homeDirection.normalized;

            return Frame(to, heading) * Quaternion.Inverse(Frame(from, surfaceForward));
        }

        /// <summary>Orientation with the given up and the forward as close to <paramref name="forward"/> as the plane allows.</summary>
        private static Quaternion Frame(Vector3 up, Vector3 forward)
        {
            var tangent = Vector3.ProjectOnPlane(forward, up);
            if (tangent.sqrMagnitude < Epsilon)
                tangent = Vector3.ProjectOnPlane(Vector3.forward, up);

            if (tangent.sqrMagnitude < Epsilon)
                tangent = Vector3.ProjectOnPlane(Vector3.right, up);

            return Quaternion.LookRotation(tangent.normalized, up);
        }
    }
}

using UnityEngine;

namespace Inktober2026.Apple.Knife
{
    /// <summary>
    /// Spins the surface under the knife like an apple on a lathe: by a fixed angle around the axis across the knife
    /// (the pitch, it moves the skin under the blade) and by the steering angle around the axis from the center to the knife
    /// (the yaw, it turns the path). Then the point under the knife is read with a ray from the knife towards the center.
    /// The knife stays above <c>homeDirection</c> and keeps facing <c>heading</c>, only the surface moves.
    /// </summary>
    public static class KnifeSpin
    {
        private const float Epsilon = 1e-6f;

        /// <summary>
        /// The rotation around the center of the surface for one frame.
        /// The pitch moves the surface under the knife backwards, a positive yaw makes the path turn right.
        /// </summary>
        public static Quaternion Rotation(Vector3 homeDirection, Vector3 heading, float pitchDegrees, float yawDegrees)
        {
            var yaw = Quaternion.AngleAxis(-yawDegrees, homeDirection.normalized);

            var pitchAxis = Vector3.Cross(heading, homeDirection);
            if (pitchAxis.sqrMagnitude < Epsilon)
                return yaw;

            return yaw * Quaternion.AngleAxis(pitchDegrees, pitchAxis.normalized);
        }

        /// <summary>
        /// Spins <paramref name="surface"/> around its position and puts the walker on the point that is now under the knife.
        /// <paramref name="reach"/> is how far from the center the ray starts, it has to be outside of the surface.
        /// False when the ray hits nothing.
        /// </summary>
        public static bool Apply(ISurfaceWalker walker, Transform surface, Vector3 homeDirection, Vector3 heading, float pitchDegrees, float yawDegrees, float reach)
        {
            var center = surface.position;
            float radius = (walker.Position - center).magnitude;

            surface.rotation = Rotation(homeDirection, heading, pitchDegrees, yawDegrees) * surface.rotation;
            Physics.SyncTransforms();

            float travelled = Mathf.Abs(pitchDegrees) * Mathf.Deg2Rad * radius;
            return walker.Follow(center + homeDirection.normalized * reach, -homeDirection, heading, travelled);
        }
    }
}

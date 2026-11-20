using UnityEngine;

namespace Inktober2026.Apple.Knife
{
    /// <summary>Frame-rate independent exponential follow of a pose, so the knife eases towards where the surface wants it.</summary>
    public static class KnifePoseSmoother
    {
        /// <summary>Moves <paramref name="current"/> towards <paramref name="target"/>. With no smooth time the target is returned.</summary>
        /// <param name="smoothTime">Time in seconds to cover about two thirds of the way.</param>
        public static Pose Step(Pose current, Pose target, float deltaTime, float smoothTime)
        {
            if (smoothTime <= 0f)
                return target;

            float blend = 1f - Mathf.Exp(-deltaTime / smoothTime);
            return new Pose(
                Vector3.Lerp(current.position, target.position, blend),
                Quaternion.Slerp(current.rotation, target.rotation, blend));
        }
    }
}

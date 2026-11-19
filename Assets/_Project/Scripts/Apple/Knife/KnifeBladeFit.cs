using UnityEngine;

namespace Inktober.Apple.Knife
{
    /// <summary>
    /// Lets the knife rest on its whole blade instead of on a single point: the surface is sampled on a grid
    /// under the blade and the knife is laid on the plane through those points. Bumps smaller than the blade
    /// and the facets of the mesh do not tilt the knife.
    /// </summary>
    public static class KnifeBladeFit
    {
        private const int Samples = 3;
        private const float CastHeight = 0.15f;
        private const float Epsilon = 1e-6f;

        /// <summary>
        /// Fits the blade to <paramref name="surface"/> around the walker point. False when a sample misses the surface
        /// (the blade hangs over its edge), then <paramref name="pose"/> is meaningless.
        /// </summary>
        /// <param name="length">Size of the blade along <paramref name="forward"/>.</param>
        /// <param name="width">Size of the blade across it.</param>
        public static bool TryFit(Collider surface, Vector3 position, Vector3 normal, Vector3 forward, float length, float width, out Pose pose)
        {
            pose = default;

            var across = Vector3.Cross(normal, forward);
            if (across.sqrMagnitude < Epsilon)
                return false;

            across.Normalize();

            var hits = new Vector3[Samples, Samples];
            var mean = Vector3.zero;

            for (int i = 0; i < Samples; i++)
            {
                for (int j = 0; j < Samples; j++)
                {
                    float alongOffset = (i - 1) * 0.5f * length;
                    float acrossOffset = (j - 1) * 0.5f * width;
                    var origin = position + forward * alongOffset + across * acrossOffset + normal * CastHeight;

                    if (surface.Raycast(new Ray(origin, -normal), out var hit, CastHeight * 2f) == false)
                        return false;

                    hits[i, j] = hit.point;
                    mean += hit.point;
                }
            }

            mean /= Samples * Samples;

            var forwardSpan = Vector3.zero;
            var acrossSpan = Vector3.zero;
            for (int k = 0; k < Samples; k++)
            {
                forwardSpan += hits[Samples - 1, k] - hits[0, k];
                acrossSpan += hits[k, Samples - 1] - hits[k, 0];
            }

            var fitted = Vector3.Cross(forwardSpan, acrossSpan);
            if (fitted.sqrMagnitude < Epsilon)
                return false;

            fitted.Normalize();
            if (Vector3.Dot(fitted, normal) < 0f)
                fitted = -fitted;

            var tangent = Vector3.ProjectOnPlane(forward, fitted);
            if (tangent.sqrMagnitude < Epsilon)
                return false;

            pose = new Pose(mean, Quaternion.LookRotation(tangent.normalized, fitted));
            return true;
        }
    }
}

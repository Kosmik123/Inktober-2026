using UnityEngine;

namespace Inktober2026.Apple.Knife
{
    /// <summary>
    /// Surface walker that steps along the tangent and snaps back to the surface with a ray,
    /// reading a smoothed surface normal with a SphereCast.
    /// Works with any collider, including concave meshes, as long as the surface features are
    /// larger than <see cref="castRadius"/> and shallower than <see cref="castHeight"/>.
    /// </summary>
    public class SpherecastSurfaceWalker : MonoBehaviour, ISurfaceWalker
    {
        private const float MinDirectionSqrMagnitude = 1e-6f;

        [Tooltip("Layers that make up the surface the walker sticks to.")]
        [SerializeField]
        private LayerMask surfaceMask = ~0;

        [Tooltip("Radius of the sphere used to read the surface normal. Larger values smooth out small bumps.")]
        [SerializeField]
        private float castRadius = 0.03f;

        [Tooltip("How far above the surface the cast starts. Must be larger than the cast radius.")]
        [SerializeField]
        private float castHeight = 0.2f;

        [Tooltip("Longest single step. Longer moves are split so small features are not skipped.")]
        [SerializeField]
        private float maxStep = 0.02f;

        [Tooltip("How fast Normal follows the real surface normal, per unit of distance travelled.")]
        [SerializeField]
        private float normalSmoothing = 30f;

        [Header("States")]
        [SerializeField]
        private bool isOnSurface;
        public bool IsOnSurface => isOnSurface;

        [SerializeField]
        private Vector3 position;
        public Vector3 Position => position;

        [SerializeField]
        private Vector3 smoothedNormal = Vector3.up;
        public Vector3 Normal => smoothedNormal;

        [SerializeField]
        private Vector3 forward = Vector3.forward;
        public Vector3 Forward => forward;

        private Vector3 surfaceNormal = Vector3.up;

        public bool Place(Vector3 origin, Vector3 direction, Vector3 forwardHint)
        {
            isOnSurface = false;

            if (TrySample(origin, direction.normalized, Mathf.Infinity, out var point, out var normal) == false)
                return false;

            position = point;
            surfaceNormal = normal;
            smoothedNormal = normal;
            forward = ProjectOnSurface(forwardHint, normal);
            isOnSurface = true;
            return true;
        }

        public bool Follow(Vector3 origin, Vector3 direction, Vector3 forwardHint, float travelled)
        {
            if (TrySample(origin, direction.normalized, Mathf.Infinity, out var point, out var normal) == false)
            {
                isOnSurface = false;
                return false;
            }

            float blend = 1f - Mathf.Exp(-normalSmoothing * travelled);

            position = point;
            surfaceNormal = normal;
            smoothedNormal = isOnSurface ? Vector3.Slerp(smoothedNormal, normal, blend).normalized : normal;
            forward = ProjectOnSurface(forwardHint, normal);
            isOnSurface = true;
            return true;
        }

        public void Turn(float degrees)
        {
            if (isOnSurface == false)
                return;

            forward = Quaternion.AngleAxis(degrees, surfaceNormal) * forward;
        }

        public void Advance(float distance)
        {
            if (isOnSurface == false)
                return;

            float remaining = distance;
            while (remaining > 0f)
            {
                float step = Mathf.Min(remaining, maxStep);
                remaining -= step;

                if (Step(step) == false)
                    return;
            }
        }

        public void Reanchor(Matrix4x4 delta)
        {
            position = delta.MultiplyPoint3x4(position);
            surfaceNormal = delta.MultiplyVector(surfaceNormal).normalized;
            smoothedNormal = delta.MultiplyVector(smoothedNormal).normalized;
            forward = delta.MultiplyVector(forward).normalized;
        }

        private bool Step(float step)
        {
            var target = position + forward * step;
            var origin = target + surfaceNormal * castHeight;

            if (TrySample(origin, -surfaceNormal, castHeight * 2f, out var point, out var normal) == false)
            {
                isOnSurface = false;
                return false;
            }

            position = point;
            surfaceNormal = normal;
            forward = ProjectOnSurface(forward, surfaceNormal);

            float blend = 1f - Mathf.Exp(-normalSmoothing * step);
            smoothedNormal = Vector3.Slerp(smoothedNormal, surfaceNormal, blend).normalized;
            return true;
        }

        /// <summary>
        /// The point comes from a thin ray, so travel speed along the surface stays constant.
        /// The normal comes from a SphereCast, which smooths out bumps smaller than the cast radius.
        /// </summary>
        private bool TrySample(Vector3 origin, Vector3 direction, float maxDistance, out Vector3 point, out Vector3 normal)
        {
            point = default;
            normal = default;

            if (Physics.Raycast(origin, direction, out var rayHit, maxDistance, surfaceMask, QueryTriggerInteraction.Ignore) == false)
                return false;

            point = rayHit.point;
            normal = rayHit.normal;

            if (Physics.SphereCast(origin, castRadius, direction, out var sphereHit, maxDistance, surfaceMask, QueryTriggerInteraction.Ignore))
                normal = sphereHit.normal;

            return true;
        }

        private static Vector3 ProjectOnSurface(Vector3 direction, Vector3 normal)
        {
            var projected = Vector3.ProjectOnPlane(direction, normal);
            if (projected.sqrMagnitude > MinDirectionSqrMagnitude)
                return projected.normalized;

            projected = Vector3.ProjectOnPlane(Vector3.forward, normal);
            if (projected.sqrMagnitude > MinDirectionSqrMagnitude)
                return projected.normalized;

            return Vector3.ProjectOnPlane(Vector3.right, normal).normalized;
        }

        private void OnValidate()
        {
            castRadius = Mathf.Max(0.001f, castRadius);
            castHeight = Mathf.Max(castRadius * 2f, castHeight);
            maxStep = Mathf.Max(0.001f, maxStep);
            normalSmoothing = Mathf.Max(0f, normalSmoothing);
        }
    }
}

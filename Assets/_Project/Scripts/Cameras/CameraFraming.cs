using UnityEngine;

namespace Inktober2026.Cameras
{
    /// <summary>
    /// Keeps a camera's field of view wide enough to satisfy both a minimum horizontal
    /// and a minimum vertical FOV, whatever the screen aspect ratio is.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CameraFraming : MonoBehaviour
    {
        private const float MinAllowedFov = 1f;
        private const float MaxAllowedFov = 179f;

        [Tooltip("Minimum vertical field of view in degrees that must always be visible.")]
        [SerializeField, Range(0, 179)]
        private float minVerticalFov = 35f;
        public float MinVerticalFov
        {
            get => minVerticalFov;
            set
            {
                minVerticalFov = ClampFov(value);
                Apply();
            }
        }

        [Tooltip("Minimum horizontal field of view in degrees that must always be visible.")]
        [SerializeField, Range(0, 179)]
        private float minHorizontalFov = 35f;
        public float MinHorizontalFov
        {
            get => minHorizontalFov;
            set
            {
                minHorizontalFov = ClampFov(value);
                Apply();
            }
        }

        [Header("States")]
        [SerializeField]
        private float appliedVerticalFov;

        [SerializeField]
        private float appliedHorizontalFov;

        private Camera cam;
        private float lastAspect;

        private Camera Camera
        {
            get
            {
                if (cam == null)
                    cam = GetComponent<Camera>();

                return cam;
            }
        }

        /// <summary>Vertical FOV (the value Unity uses) that satisfies both minimum FOVs at the given aspect.</summary>
        public static float CalculateVerticalFov(float minVertical, float minHorizontal, float aspect)
        {
            if (aspect <= 0f)
                return ClampFov(minVertical);

            float halfHorizontal = ClampFov(minHorizontal) * 0.5f * Mathf.Deg2Rad;
            float verticalFromHorizontal = 2f * Mathf.Atan(Mathf.Tan(halfHorizontal) / aspect) * Mathf.Rad2Deg;
            return ClampFov(Mathf.Max(minVertical, verticalFromHorizontal));
        }

        /// <summary>Recalculates and applies the camera FOV. Called automatically when the aspect ratio changes.</summary>
        public void Apply()
        {
            var targetCamera = Camera;
            lastAspect = targetCamera.aspect;

            float vertical = CalculateVerticalFov(minVerticalFov, minHorizontalFov, lastAspect);
            targetCamera.fieldOfView = vertical;

            appliedVerticalFov = vertical;
            appliedHorizontalFov = Camera.VerticalToHorizontalFieldOfView(vertical, lastAspect);
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (Mathf.Approximately(Camera.aspect, lastAspect) == false)
                Apply();
        }

        private void OnValidate()
        {
            minVerticalFov = ClampFov(minVerticalFov);
            minHorizontalFov = ClampFov(minHorizontalFov);
            Apply();
        }

        private static float ClampFov(float fov)
        {
            return Mathf.Clamp(fov, MinAllowedFov, MaxAllowedFov);
        }
    }
}

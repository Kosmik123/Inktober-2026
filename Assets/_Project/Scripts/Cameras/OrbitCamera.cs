using UnityEngine;

namespace Inktober2026.Cameras
{
    /// <summary>
    /// Keeps the camera on a sphere around the target and looks at it. The view is turned by the same amount
    /// as the pointer moved: <see cref="Rotate"/> takes the movement in pixels.
    /// </summary>
    public class OrbitCamera : MonoBehaviour
    {
        [SerializeField]
        private Transform target;

        [Tooltip("Degrees the view turns for every pixel the pointer moves.")]
        [SerializeField]
        private float degreesPerPixel = 0.3f;

        [Tooltip("The camera cannot look steeper than this, up or down, in degrees.")]
        [SerializeField, Range(1f, 89f)]
        private float maxPitch = 85f;

        [Header("States")]
        [SerializeField]
        private float yaw;

        [Tooltip("Positive when the camera looks down.")]
        [SerializeField]
        private float pitch;

        [SerializeField]
        private float distance;

        private void Awake()
        {
            ReadCurrentPose();
        }

        /// <summary>Turns the view by the pointer movement in pixels. Dragging to the right turns the scene to the right.</summary>
        public void Rotate(Vector2 pixels)
        {
            yaw += pixels.x * degreesPerPixel;
            pitch = Mathf.Clamp(pitch - pixels.y * degreesPerPixel, -maxPitch, maxPitch);
            Apply();
        }

        private void ReadCurrentPose()
        {
            var toTarget = target.position - transform.position;
            distance = toTarget.magnitude;
            var forward = toTarget / distance;
            yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            pitch = Mathf.Asin(-forward.y) * Mathf.Rad2Deg;
        }

        private void Apply()
        {
            var rotation = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, Vector3.right);
            transform.SetPositionAndRotation(target.position - rotation * Vector3.forward * distance, rotation);
        }
    }
}

using Inktober.Apple.Knife;
using UnityEngine;

namespace Inktober.Apple.Cameras
{
    /// <summary>
    /// Keeps the camera behind the target, in the target's local space.
    /// Up is the target's up, so on a curved surface the camera rolls with it.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField]
        private Transform target;
        public Transform Target
        {
            get => target;
            set => target = value;
        }

        [Tooltip("Camera position in the target's local space: x right, y along the surface normal, z forward.")]
        [SerializeField]
        private Vector3 offset = new Vector3(0f, 0.8f, -0.6f);

        [Tooltip("Point the camera looks at, in the target's local space.")]
        [SerializeField]
        private Vector3 lookAtOffset = new Vector3(0f, 0f, 0.2f);

        [Tooltip("How quickly the camera catches up with the target, per second. 0 attaches it rigidly.")]
        [SerializeField]
        private float followSharpness;

        private void Reset()
        {
            var knife = FindFirstObjectByType<KnifeController>();
            if (knife)
                target = knife.transform;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            var desiredPosition = target.TransformPoint(offset);
            var lookTarget = target.TransformPoint(lookAtOffset);
            var desiredRotation = Quaternion.LookRotation(lookTarget - desiredPosition, target.up);

            if (followSharpness <= 0f)
            {
                transform.SetPositionAndRotation(desiredPosition, desiredRotation);
                return;
            }

            float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, desiredPosition, blend),
                Quaternion.Slerp(transform.rotation, desiredRotation, blend));
        }

        private void OnValidate()
        {
            followSharpness = Mathf.Max(0f, followSharpness);
        }
    }
}

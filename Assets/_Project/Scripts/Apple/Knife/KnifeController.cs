using UnityEngine;
using UnityEngine.InputSystem;

namespace Inktober.Apple.Knife
{
    /// <summary>
    /// Keeps the surface in place and turns it under the knife: the walker finds where the knife would be
    /// next, then the surface is rotated around its center so that point comes back under the knife
    /// (see <see cref="KnifeSurfaceAligner"/>). The knife only slides away from or towards the center and tilts to the surface.
    /// Constant speed, the Steer action turns.
    /// The way the knife sticks to the surface is a strategy, any <see cref="ISurfaceWalker"/> on the same object.
    /// </summary>
    public class KnifeController : MonoBehaviour
    {
        private const float MaxDeltaTime = 0.05f;

        [SerializeField]
        private InputActionReference steerAction;

        [Tooltip("The object the knife walks on. It stays where it is and only rotates around its pivot.")]
        [SerializeField]
        private Transform surface;

        [Header("Movement")]
        [Tooltip("Travel speed along the surface, in units per second.")]
        [SerializeField]
        private float speed = 0.8f;
        public float Speed
        {
            get => speed;
            set => speed = Mathf.Max(0f, value);
        }

        [Tooltip("Upper limit of the turn rate, in degrees per second.")]
        [SerializeField]
        private float maxTurnRate = 180f;

        [Header("States")]
        [SerializeField]
        private float turnRate;

        private ISurfaceWalker walker;
        private Vector3 homeDirection;
        private Vector3 heading;

        private void Awake()
        {
            walker = GetComponent<ISurfaceWalker>();

            if (walker == null)
            {
                Debug.LogError($"{nameof(KnifeController)} needs a component implementing {nameof(ISurfaceWalker)} on the same object.", this);
                enabled = false;
                return;
            }

            if (steerAction == null || surface == null)
            {
                Debug.LogError($"{nameof(KnifeController)} needs the Steer action and the surface assigned.", this);
                enabled = false;
            }
        }

        private void Start()
        {
            if (walker.Place(transform.position, -transform.up, transform.forward) == false)
            {
                Debug.LogError($"{nameof(KnifeController)} could not find a surface below the knife.", this);
                enabled = false;
                return;
            }

            transform.SetPositionAndRotation(walker.Position, Quaternion.LookRotation(walker.Forward, walker.Normal));

            // The knife stays above this point and keeps facing this way, the surface turns under it.
            homeDirection = (transform.position - surface.position).normalized;
            heading = transform.forward;
        }

        private void Update()
        {
            float deltaTime = Mathf.Min(Time.deltaTime, MaxDeltaTime);
            if (deltaTime <= 0f)
                return;

            float limit = maxTurnRate * deltaTime;
            float degrees = Mathf.Clamp(ReadTurnDegrees(), -limit, limit);
            turnRate = degrees / deltaTime;

            walker.Turn(degrees);
            walker.Advance(speed * deltaTime);
            KnifeSurfaceAligner.Apply(walker, transform, surface, homeDirection, heading);
        }

        /// <summary>Turn for this frame in degrees. The scaling per device (and delta time for sticks) is done by the binding processors.</summary>
        private float ReadTurnDegrees()
        {
            return steerAction.action.ReadValue<Vector2>().x;
        }
    }
}

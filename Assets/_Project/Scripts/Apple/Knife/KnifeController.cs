using UnityEngine;
using UnityEngine.InputSystem;

namespace Inktober.Apple.Knife
{
    /// <summary>
    /// Drives the knife across the apple: moves forward at constant speed and turns with the Steer action.
    /// The way the knife sticks to the surface is a strategy, any <see cref="ISurfaceWalker"/> on the same object.
    /// </summary>
    public class KnifeController : MonoBehaviour
    {
        private const float MaxDeltaTime = 0.05f;

        [SerializeField]
        private InputActionReference steerAction;

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

        [Header("Steering")]
        [Tooltip("Degrees of turn per pixel of pointer movement (mouse, touch).")]
        [SerializeField]
        private float pointerDegreesPerPixel = 0.2f;

        [Tooltip("Degrees per second at full stick deflection (gamepad, joystick).")]
        [SerializeField]
        private float stickDegreesPerSecond = 180f;

        [Header("States")]
        [SerializeField]
        private float turnRate;

        private ISurfaceWalker walker;

        private void Awake()
        {
            walker = GetComponent<ISurfaceWalker>();

            if (walker == null)
            {
                Debug.LogError($"{nameof(KnifeController)} needs a component implementing {nameof(ISurfaceWalker)} on the same object.", this);
                enabled = false;
                return;
            }

            if (steerAction == null)
            {
                Debug.LogError($"{nameof(KnifeController)} has no Steer action assigned.", this);
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

            ApplyPose();
        }

        private void Update()
        {
            float deltaTime = Mathf.Min(Time.deltaTime, MaxDeltaTime);
            if (deltaTime <= 0f)
                return;

            float limit = maxTurnRate * deltaTime;
            float degrees = Mathf.Clamp(ReadTurnDegrees(deltaTime), -limit, limit);
            turnRate = degrees / deltaTime;

            walker.Turn(degrees);
            walker.Advance(speed * deltaTime);
            ApplyPose();
        }

        private float ReadTurnDegrees(float deltaTime)
        {
            var action = steerAction.action;
            var control = action.activeControl;
            if (control == null)
                return 0f;

            float x = action.ReadValue<Vector2>().x;
            return control.device is Pointer ? x * pointerDegreesPerPixel : x * stickDegreesPerSecond * deltaTime;
        }

        private void ApplyPose()
        {
            transform.SetPositionAndRotation(walker.Position, Quaternion.LookRotation(walker.Forward, walker.Normal));
        }
    }
}

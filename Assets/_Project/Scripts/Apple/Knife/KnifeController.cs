using UnityEngine;
using UnityEngine.InputSystem;

namespace Inktober2026.Apple.Knife
{
    /// <summary>
    /// Keeps the surface in place and spins it under the knife at a constant angular speed (see <see cref="KnifeSpin"/>).
    /// The first axis of the Steer action turns the path. The second axis moves the throttle (<see cref="KnifeThrottle"/>):
    /// forward is faster and the peel is wider, back is slower and narrower (the peel reads <see cref="WidthScale"/>). The knife only slides away from or towards the center and tilts to the surface.
    /// The way the contact point is read is a strategy, any <see cref="ISurfaceWalker"/> on the same object.
    /// </summary>
    public class KnifeController : MonoBehaviour
    {
        private const float MaxDeltaTime = 0.05f;
        private const float ReachMargin = 0.2f;

        [SerializeField]
        private InputActionReference steerAction;

        [Tooltip("The object the knife walks on. It stays where it is and only rotates around its pivot.")]
        [SerializeField]
        private Transform surface;

        [Header("Movement")]
        [Tooltip("Travel speed along the surface at the average radius of the surface, in units per second. The surface spins at a constant angular speed, so the real speed follows the radius.")]
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

        [Header("Throttle")]
        [Tooltip("How much of the second axis (in the same units as the turn, degrees) moves the throttle from the middle to the end. Smaller is more sensitive.")]
        [SerializeField]
        private float throttleRange = 60f;

        [Tooltip("How fast the throttle falls back to the middle when the second axis rests, as part of its range per second. 0 keeps it where it was left.")]
        [SerializeField]
        private float throttleReturn;

        [Tooltip("Multiplier of the speed with the throttle pulled all the way back.")]
        [SerializeField]
        private float minSpeedScale = 0.4f;

        [Tooltip("Multiplier of the speed with the throttle pushed all the way forward.")]
        [SerializeField]
        private float maxSpeedScale = 1.8f;

        [Tooltip("Multiplier of the width of the peel with the throttle pulled all the way back.")]
        [SerializeField]
        private float minWidthScale = 0.5f;

        [Tooltip("Multiplier of the width of the peel with the throttle pushed all the way forward.")]
        [SerializeField]
        private float maxWidthScale = 1.6f;

        [Header("Blade")]
        [Tooltip("Size of the blade along the direction of travel. The knife rests on the surface over this length, so smaller bumps do not tilt it.")]
        [SerializeField]
        private float bladeLength = 0.12f;

        [Tooltip("Size of the blade across the direction of travel.")]
        [SerializeField]
        private float bladeWidth = 0.06f;

        [Tooltip("How long the knife takes to ease towards the surface, in seconds. 0 follows the surface rigidly.")]
        [SerializeField]
        private float smoothTime = 0.08f;

        [Header("States")]
        [SerializeField]
        private float turnRate;

        [SerializeField]
        private float throttleValue;

        private readonly KnifeThrottle throttle = new KnifeThrottle();
        private ISurfaceWalker walker;
        private Collider surfaceCollider;
        private Vector3 homeDirection;
        private Vector3 heading;
        private bool initialized;
        private Quaternion surfaceStartRotation;
        private float referenceRadius;
        private float reach;
        private Pose pose;

        /// <summary>The object the knife walks on.</summary>
        public Transform Surface => surface;

        /// <summary>Current turn rate in degrees per second, negative to one side and positive to the other.</summary>
        public float TurnRate => turnRate;

        /// <summary>Upper limit of the turn rate, in degrees per second.</summary>
        public float MaxTurnRate => maxTurnRate;

        /// <summary>Position of the throttle, from -1 (pulled back) to 1 (pushed forward).</summary>
        public float Throttle => throttle.Value;

        /// <summary>Multiplier of the speed that the throttle gives.</summary>
        public float SpeedScale => KnifeThrottle.Scale(throttle.Value, minSpeedScale, maxSpeedScale);

        /// <summary>Multiplier of the width of the peel that the throttle gives.</summary>
        public float WidthScale => KnifeThrottle.Scale(throttle.Value, minWidthScale, maxWidthScale);

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
                return;
            }

            surfaceStartRotation = surface.rotation;
        }

        /// <summary>
        /// Lays the knife on the surface under it and remembers the place the knife keeps above the surface. Runs on the first update, that is when the round starts:
        /// before it the knife stays where the scene put it, and the apple may turn under it. The look of the knife eases to the surface from there.
        /// </summary>
        private void Initialize()
        {
            initialized = true;

            // The size of the apple is measured in the pose it had in the scene, so the speed does not depend on how it was turned before the round.
            var turnedRotation = surface.rotation;
            surface.rotation = surfaceStartRotation;
            Physics.SyncTransforms();
            surfaceCollider = surface.GetComponentInChildren<Collider>();
            var extents = surfaceCollider ? surfaceCollider.bounds.extents : Vector3.one * (transform.position - surface.position).magnitude;
            surface.rotation = turnedRotation;
            Physics.SyncTransforms();

            referenceRadius = Mathf.Max(0.01f, (extents.x + extents.y + extents.z) / 3f);
            reach = extents.magnitude + ReachMargin;

            var scenePose = new Pose(transform.position, transform.rotation);
            var outward = (transform.position - surface.position).normalized;
            if (walker.Place(surface.position + outward * reach, -outward, transform.forward) == false)
            {
                Debug.LogError($"{nameof(KnifeController)} could not find a surface below the knife.", this);
                enabled = false;
                return;
            }

            transform.SetPositionAndRotation(walker.Position, Quaternion.LookRotation(walker.Forward, walker.Normal));

            // The knife stays above this point and keeps facing this way, the surface turns under it.
            homeDirection = (transform.position - surface.position).normalized;
            heading = transform.forward;

            pose = scenePose;
            transform.SetPositionAndRotation(scenePose.position, scenePose.rotation);
        }

        private void Update()
        {
            if (initialized == false)
            {
                Initialize();
                if (enabled == false)
                    return;
            }

            float deltaTime = Mathf.Min(Time.deltaTime, MaxDeltaTime);
            if (deltaTime <= 0f)
                return;

            var steer = steerAction.action.ReadValue<Vector2>();
            throttle.Add(steer.y / Mathf.Max(0.01f, throttleRange));
            throttle.Relax(throttleReturn, deltaTime);
            throttleValue = throttle.Value;

            float limit = maxTurnRate * deltaTime;
            float yaw = Mathf.Clamp(steer.x, -limit, limit);
            turnRate = yaw / deltaTime;

            float pitch = speed * SpeedScale / referenceRadius * Mathf.Rad2Deg * deltaTime;
            KnifeSpin.Apply(walker, surface, homeDirection, heading, pitch, yaw, reach);
            PlaceKnife(deltaTime);
        }

        /// <summary>
        /// The walker point is exact, but the knife lies on its whole blade and eases towards that pose,
        /// so facets and small bumps do not shake it. Only the look of the knife is smoothed, the peel follows the walker.
        /// </summary>
        private void PlaceKnife(float deltaTime)
        {
            if (walker.IsOnSurface == false)
                return;

            var target = new Pose(walker.Position, Quaternion.LookRotation(walker.Forward, walker.Normal));
            if (surfaceCollider && KnifeBladeFit.TryFit(surfaceCollider, walker.Position, walker.Normal, walker.Forward, bladeLength, bladeWidth, out var fitted))
                target = fitted;

            pose = KnifePoseSmoother.Step(pose, target, deltaTime, smoothTime);
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private void OnValidate()
        {
            throttleRange = Mathf.Max(0.01f, throttleRange);
            throttleReturn = Mathf.Max(0f, throttleReturn);
            minSpeedScale = Mathf.Max(0f, minSpeedScale);
            maxSpeedScale = Mathf.Max(0f, maxSpeedScale);
            minWidthScale = Mathf.Max(0.01f, minWidthScale);
            maxWidthScale = Mathf.Max(0.01f, maxWidthScale);
            bladeLength = Mathf.Max(0.01f, bladeLength);
            bladeWidth = Mathf.Max(0.01f, bladeWidth);
            smoothTime = Mathf.Max(0f, smoothTime);
        }
    }
}

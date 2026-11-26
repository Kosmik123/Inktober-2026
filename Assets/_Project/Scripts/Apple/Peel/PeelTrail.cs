using System;
using System.Collections.Generic;
using Inktober2026.Apple.Knife;
using UnityEngine;

namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// Records where the knife has been as points in the local space of the peeled object.
    /// The surface moves under a still knife, so the points move with it. Put it on the knife object.
    /// This is only data (path, length); drawing the peel is a separate concern.
    /// </summary>
    public class PeelTrail : MonoBehaviour
    {
        // Lifts gizmos off the surface so they are not hidden inside the apple.
        private const float GizmoLift = 0.003f;

        /// <summary>Raised after a point is added to the trail.</summary>
        public event Action<PeelPoint> OnPointAdded;

        /// <summary>Raised after the trail is cleared (for example on restart).</summary>
        public event Action OnCleared;

        [Tooltip("The peeled object. Points are stored in its local space.")]
        [SerializeField]
        private Transform surface;

        [Header("Peel")]
        [Tooltip("Width of the peeled strip at the middle position of the throttle, in world units.")]
        [SerializeField]
        private float width = 0.06f;

        /// <summary>
        /// The width of the peeled strip right now, in world units: the set width times the throttle of the knife (if it has one).
        /// Every recorded <see cref="PeelPoint"/> keeps the width it was peeled with, so older lines are measured with their own width.
        /// </summary>
        public float Width
        {
            get => width * (knife ? knife.WidthScale : 1f);
            set => width = Mathf.Max(0f, value);
        }

        [Tooltip("Colour of the flesh uncovered by the knife.")]
        [SerializeField]
        private Color fleshColor = new Color(1f, 0.85f, 0.25f);
        public Color FleshColor
        {
            get => fleshColor;
            set => fleshColor = value;
        }

        [Tooltip("Minimum distance between recorded points, in world units.")]
        [SerializeField]
        private float spacing = 0.01f;

        [Header("Debug")]
        [SerializeField]
        private bool drawGizmos = true;

        [Header("States")]
        [SerializeField]
        private float length;

        private readonly List<PeelPoint> points = new List<PeelPoint>();
        private ISurfaceWalker walker;
        private KnifeController knife;

        /// <summary>Recorded points, oldest first.</summary>
        public IReadOnlyList<PeelPoint> Points => points;

        /// <summary>Length of the peel so far, in world units (the score).</summary>
        public float Length => length;

        /// <summary>The object the points are local to.</summary>
        public Transform Surface => surface;

        /// <summary>
        /// Where the knife is right now, which is usually a little past the newest recorded point, and the length of the peel up to there.
        /// False before the first point and when the knife is off the surface. Nothing is recorded.
        /// </summary>
        public bool TryGetHead(out PeelPoint head, out float headLength)
        {
            head = default;
            headLength = length;
            if (points.Count == 0 || walker == null || walker.IsOnSurface == false)
                return false;

            var position = surface.InverseTransformPoint(walker.Position);
            var normal = surface.InverseTransformDirection(walker.Normal).normalized;
            var forward = surface.InverseTransformDirection(walker.Forward).normalized;
            head = new PeelPoint(position, normal, forward, Width);
            headLength += Vector3.Distance(surface.TransformPoint(points[points.Count - 1].Position), walker.Position);
            return true;
        }

        /// <summary>Removes all points and resets the length.</summary>
        public void Clear()
        {
            points.Clear();
            length = 0f;
            OnCleared?.Invoke();
        }

        private void Awake()
        {
            walker = GetComponent<ISurfaceWalker>();
            TryGetComponent(out knife);
            if (walker == null || surface == null)
            {
                Debug.LogError($"{nameof(PeelTrail)} needs a component implementing {nameof(ISurfaceWalker)} on the same object and the surface assigned.", this);
                enabled = false;
            }
        }

        // After KnifeController.Update, so the walker is already reanchored to the moved surface.
        private void LateUpdate()
        {
            if (walker.IsOnSurface == false)
                return;

            var position = surface.InverseTransformPoint(walker.Position);
            if (points.Count > 0 && IsTooClose(position))
                return;

            var normal = surface.InverseTransformDirection(walker.Normal).normalized;
            var forward = surface.InverseTransformDirection(walker.Forward).normalized;
            Add(new PeelPoint(position, normal, forward, Width));
        }

        private bool IsTooClose(Vector3 localPosition)
        {
            // The distance is measured in world units, whatever the scale of the surface is.
            var last = points[points.Count - 1].Position;
            var distance = Vector3.Distance(surface.TransformPoint(last), surface.TransformPoint(localPosition));
            return distance < spacing;
        }

        private void Add(PeelPoint point)
        {
            if (points.Count > 0)
            {
                var last = points[points.Count - 1].Position;
                length += Vector3.Distance(surface.TransformPoint(last), surface.TransformPoint(point.Position));
            }

            points.Add(point);
            OnPointAdded?.Invoke(point);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (drawGizmos == false || surface == null || points.Count == 0)
                return;

            Gizmos.matrix = surface.localToWorldMatrix;
            Gizmos.color = fleshColor;
            float half = Width * 0.5f;
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                var lifted = point.Position + point.Normal * GizmoLift;
                if (i > 0)
                    Gizmos.DrawLine(points[i - 1].Position + points[i - 1].Normal * GizmoLift, lifted);

                // A bar across the strip shows its width at every point.
                Gizmos.DrawLine(lifted - point.Right * half, lifted + point.Right * half);
            }
        }
#endif
    }
}

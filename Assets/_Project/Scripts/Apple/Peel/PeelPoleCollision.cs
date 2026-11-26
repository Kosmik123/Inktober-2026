using System;
using UnityEngine;

namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// Raises <see cref="OnPoleHit"/> when the knife reaches the stalk or the calyx of the apple (see <see cref="PeelPole"/>).
    /// It only detects: what happens next (game over) is up to the game flow.
    /// </summary>
    public class PeelPoleCollision : MonoBehaviour
    {
        /// <summary>Raised once, with the position of the knife in the local space of the peeled object, until the trail is cleared.</summary>
        public event Action<Vector3> OnPoleHit;

        [Tooltip("The trail of the knife. Empty: the one of the scene is looked for.")]
        [SerializeField]
        private PeelTrail trail;

        [Tooltip("Angle of the forbidden cap around the stalk (the north pole), in degrees from the axis.")]
        [Range(0f, 45f)]
        [SerializeField]
        private float stalkAngle = 6f;

        [Tooltip("Angle of the forbidden cap around the calyx (the south pole), in degrees from the axis.")]
        [Range(0f, 45f)]
        [SerializeField]
        private float calyxAngle = 6f;

        [Tooltip("How much of half the width of the peel is added to the caps. 0: only the centre of the knife counts. 1: the edge of the peel counts, which on a wide peel is more than 10 degrees on its own.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float widthShare = 0.3f;

        [Header("Debug")]
        [Tooltip("Draws the places where the knife loses, when this object is selected: red for the centre of the knife with the peel as wide as it is now.")]
        [SerializeField]
        private bool drawGizmos = true;

        [Header("States")]
        [SerializeField]
        private bool hasHit;

        private void Reset()
        {
            trail = FindAnyObjectByType<PeelTrail>();
        }

        private void Awake()
        {
            if (trail == null)
                trail = FindAnyObjectByType<PeelTrail>();
        }

        private void OnEnable()
        {
            if (trail == null)
            {
                Debug.LogError($"{nameof(PeelPoleCollision)} needs the trail assigned.", this);
                enabled = false;
                return;
            }

            trail.OnPointAdded += PeelTrail_OnPointAdded;
            trail.OnCleared += PeelTrail_OnCleared;
        }

        private void PeelTrail_OnPointAdded(PeelPoint point)
        {
            if (hasHit)
                return;

            float scale = Mathf.Abs(trail.Surface.lossyScale.x);
            if (PeelPole.Touches(point.Position, point.Width, scale, stalkAngle, calyxAngle, widthShare) == false)
                return;

            hasHit = true;
            OnPoleHit?.Invoke(point.Position);
        }

        private void PeelTrail_OnCleared()
        {
            hasHit = false;
        }

        private void OnDisable()
        {
            if (trail == null)
                return;

            trail.OnPointAdded -= PeelTrail_OnPointAdded;
            trail.OnCleared -= PeelTrail_OnCleared;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var drawn = trail ? trail : FindAnyObjectByType<PeelTrail>();
            if (drawGizmos == false || drawn == null || drawn.Surface == null)
                return;

            DrawReach(drawn, Vector3.up, stalkAngle);
            DrawReach(drawn, Vector3.down, calyxAngle);
        }

        // A circle around a pole, on a sphere about the size of the apple at its dimples.
        private void DrawReach(PeelTrail drawn, Vector3 pole, float capAngle)
        {
            const float DimpleRadius = 0.44f;

            var surface = drawn.Surface;
            float scale = Mathf.Abs(surface.lossyScale.x);
            float angle = PeelPole.Reach(capAngle, drawn.Width, DimpleRadius * scale, widthShare) * Mathf.Deg2Rad;

            UnityEditor.Handles.color = Color.red;
            var centre = surface.TransformPoint(pole * (DimpleRadius * Mathf.Cos(angle)));
            UnityEditor.Handles.DrawWireDisc(centre, surface.TransformDirection(pole), DimpleRadius * Mathf.Sin(angle) * scale);
        }
#endif
    }
}

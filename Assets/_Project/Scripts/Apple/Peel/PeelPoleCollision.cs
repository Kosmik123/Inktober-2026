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

        [Tooltip("Angle of the forbidden cap around the stalk (the north pole), in degrees from the axis. The knife is lost when the edge of the peel gets into it.")]
        [Range(0f, 45f)]
        [SerializeField]
        private float stalkAngle = 6f;

        [Tooltip("Angle of the forbidden cap around the calyx (the south pole), in degrees from the axis.")]
        [Range(0f, 45f)]
        [SerializeField]
        private float calyxAngle = 6f;

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
            if (PeelPole.Touches(point.Position, point.Width, scale, stalkAngle, calyxAngle) == false)
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
    }
}

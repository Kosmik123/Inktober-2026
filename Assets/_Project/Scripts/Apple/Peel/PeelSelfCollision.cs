using System;
using UnityEngine;

namespace Inktober.Apple.Peel
{
    /// <summary>
    /// Raises <see cref="OnSelfIntersection"/> when the newest segment of the <see cref="PeelTrail"/> crosses an older one.
    /// The trail is a line without thickness. It only detects: what happens next (game over) is up to the game flow.
    /// </summary>
    public class PeelSelfCollision : MonoBehaviour
    {
        /// <summary>Raised once, with the crossing point in the local space of the peeled object, until the trail is cleared.</summary>
        public event Action<Vector3> OnSelfIntersection;

        [SerializeField]
        private PeelTrail trail;

        [Tooltip("Trail closer to the knife than this (in world units, measured along the trail) is not tested, so the knife does not hit its own tail.")]
        [SerializeField]
        private float ignoredTailLength = 0.2f;

        [Tooltip("Largest distance between the two lines along the surface normal that still counts as a crossing, in world units.")]
        [SerializeField]
        private float heightTolerance = 0.03f;

        [Header("Debug")]
        [SerializeField]
        private bool drawGizmos = true;

        [Header("States")]
        [SerializeField]
        private bool hasCrossed;

        private Vector3 crossingPoint;

        /// <summary>True after the trail crossed itself, until it is cleared.</summary>
        public bool HasCrossed => hasCrossed;

        private void OnEnable()
        {
            if (trail == null)
            {
                Debug.LogError($"{nameof(PeelSelfCollision)} needs the trail assigned.", this);
                enabled = false;
                return;
            }

            trail.OnPointAdded += HandlePointAdded;
            trail.OnCleared += HandleCleared;
        }

        private void OnDisable()
        {
            if (trail == null)
                return;

            trail.OnPointAdded -= HandlePointAdded;
            trail.OnCleared -= HandleCleared;
        }

        private void HandlePointAdded(PeelPoint point)
        {
            if (hasCrossed)
                return;

            float scale = Mathf.Abs(trail.Surface.lossyScale.x);
            if (PeelSelfIntersection.TryFind(trail.Points, scale, ignoredTailLength, heightTolerance, out crossingPoint) == false)
                return;

            hasCrossed = true;
            OnSelfIntersection?.Invoke(crossingPoint);
        }

        private void HandleCleared()
        {
            hasCrossed = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (drawGizmos == false || hasCrossed == false || trail == null || trail.Surface == null)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(trail.Surface.TransformPoint(crossingPoint), 0.02f);
        }
#endif
    }
}

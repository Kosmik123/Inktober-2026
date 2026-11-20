using System;
using UnityEngine;

namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// Raises <see cref="OnEnclosed"/> when the knife has been cutting between two older lines for a while, so no new peel is made.
    /// A single point in such a spot is not enough (a turn or a graze still peels something). It only detects:
    /// what happens next (game over) is up to the game flow. See <see cref="PeelEnclosure"/> for the test.
    /// </summary>
    public class PeelEnclosureDetector : MonoBehaviour
    {
        /// <summary>Raised once, with the position of the knife in the local space of the peeled object, until the trail is cleared.</summary>
        public event Action<Vector3> OnEnclosed;

        [SerializeField]
        private PeelTrail trail;

        [Tooltip("Trail closer to the knife than this (in world units, measured along the trail) is not measured, so the knife does not hit its own tail.")]
        [SerializeField]
        private float ignoredTailLength = 0.2f;

        [Tooltip("Largest distance between the two lines along the surface normal that still counts as the same surface, in world units.")]
        [SerializeField]
        private float heightTolerance = 0.03f;

        [Tooltip("How long the knife has to peel nothing new before it counts, in seconds.")]
        [SerializeField]
        private float duration = 0.15f;

        [Header("States")]
        [SerializeField]
        private bool hasEnclosed;

        [SerializeField]
        private bool isEnclosed;

        private float enclosedSince;

        /// <summary>True after the knife was enclosed long enough, until the trail is cleared.</summary>
        public bool HasEnclosed => hasEnclosed;

        private void Reset()
        {
            trail = FindAnyObjectByType<PeelTrail>();
        }

        private void OnEnable()
        {
            if (trail == null)
            {
                Debug.LogError($"{nameof(PeelEnclosureDetector)} needs the trail assigned.", this);
                enabled = false;
                return;
            }

            trail.OnPointAdded += PeelTrail_OnPointAdded;
            trail.OnCleared += PeelTrail_OnCleared;
        }

        private void OnDisable()
        {
            if (trail == null)
                return;

            trail.OnPointAdded -= PeelTrail_OnPointAdded;
            trail.OnCleared -= PeelTrail_OnCleared;
        }

        private void PeelTrail_OnPointAdded(PeelPoint point)
        {
            if (hasEnclosed)
                return;

            float scale = Mathf.Abs(trail.Surface.lossyScale.x);
            bool enclosed = PeelEnclosure.IsEnclosed(trail.Points, trail.Width, scale, ignoredTailLength, heightTolerance);
            if (enclosed && isEnclosed == false)
                enclosedSince = Time.time;

            isEnclosed = enclosed;
            if (isEnclosed == false || Time.time - enclosedSince < duration)
                return;

            hasEnclosed = true;
            OnEnclosed?.Invoke(point.Position);
        }

        private void PeelTrail_OnCleared()
        {
            hasEnclosed = false;
            isEnclosed = false;
        }
    }
}

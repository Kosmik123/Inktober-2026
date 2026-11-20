using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// Follows the trail and keeps the width of the peeled strip as a list of cross sections, one every few centimetres of peel.
    /// The strip is not the knife path: the knife peels only what is not peeled yet, so the strip is narrower where the path
    /// runs along an older line. This is only data, drawing and moving the strip is a separate concern.
    /// See <see cref="PeelStripWidthUtility"/> for the width.
    /// </summary>
    public class PeelWidthProfile : MonoBehaviour
    {
        /// <summary>Raised after a section is added to the end of the strip.</summary>
        public event Action<PeelSection> OnSectionAdded;

        /// <summary>Raised after the sections are cleared (for example on restart).</summary>
        public event Action OnCleared;

        [SerializeField]
        private PeelTrail trail;

        [Tooltip("Peel length between two sections, in world units. Longer means fewer sections.")]
        [SerializeField]
        private float sectionSpacing = 0.03f;

        [Tooltip("Trail closer to the knife than this (in world units, measured along the trail) is not measured, so the knife does not hit its own tail.")]
        [SerializeField]
        private float ignoredTailLength = 0.2f;

        [Tooltip("Largest distance between the two lines along the surface normal that still counts as the same surface, in world units.")]
        [SerializeField]
        private float heightTolerance = 0.03f;

        private readonly List<PeelSection> sections = new List<PeelSection>();
        private PeelSectionRecorder recorder;

        /// <summary>The sections of the strip, from the beginning of the peel.</summary>
        public IReadOnlyList<PeelSection> Sections => sections;

        private void Reset()
        {
            trail = FindAnyObjectByType<PeelTrail>();
        }

        private void OnEnable()
        {
            if (trail == null)
            {
                Debug.LogError($"{nameof(PeelWidthProfile)} needs the trail assigned.", this);
                enabled = false;
                return;
            }

            recorder = new PeelSectionRecorder(sectionSpacing, ignoredTailLength, heightTolerance);
            trail.OnPointAdded += PeelTrail_OnPointAdded;
            trail.OnCleared += PeelTrail_OnCleared;
        }

		private void PeelTrail_OnPointAdded(PeelPoint point)
		{
			float scale = Mathf.Abs(trail.Surface.lossyScale.x);
			if (recorder.TryRecord(trail.Points, trail.Length, trail.Width, scale, out var section) == false)
				return;

			sections.Add(section);
			OnSectionAdded?.Invoke(section);
		}

		private void PeelTrail_OnCleared()
		{
			sections.Clear();
			recorder.Clear();
			OnCleared?.Invoke();
		}

		private void OnDisable()
        {
            if (trail == null)
                return;

            trail.OnPointAdded -= PeelTrail_OnPointAdded;
            trail.OnCleared -= PeelTrail_OnCleared;
        }


        private void OnValidate()
        {
            sectionSpacing = Mathf.Max(0.001f, sectionSpacing);
            ignoredTailLength = Mathf.Max(0f, ignoredTailLength);
            heightTolerance = Mathf.Max(0f, heightTolerance);
        }
    }
}

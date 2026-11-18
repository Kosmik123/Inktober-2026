using Inktober.Apple.Peel;
using TMPro;
using UnityEngine;

namespace Inktober.UI
{
    /// <summary>
    /// Shows the current peel length (the score) in a text label.
    /// The text is refreshed only when the trail changes.
    /// </summary>
    public class PeelLengthDisplay : MonoBehaviour
    {
        [SerializeField]
        private PeelTrail trail;

        [SerializeField]
        private TMP_Text label;

        [Tooltip("World units are multiplied by this before showing (100 turns units into centimetres).")]
        [SerializeField]
        private float displayScale = 100f;

        [Tooltip("Text format. {0} is the scaled length.")]
        [SerializeField]
        private string format = "{0:0} cm";

		private void Reset()
		{
			trail = FindAnyObjectByType<PeelTrail>();
            label = GetComponentInChildren<TMP_Text>();
		}

		private void OnEnable()
        {
            trail.OnPointAdded += HandlePointAdded;
            trail.OnCleared += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            trail.OnPointAdded -= HandlePointAdded;
            trail.OnCleared -= Refresh;
        }

        private void HandlePointAdded(PeelPoint point) => Refresh();

        private void Refresh()
        {
            label.text = string.Format(format, trail.Length * displayScale);
        }
    }
}

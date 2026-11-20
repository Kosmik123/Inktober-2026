using Inktober2026.Apple.Peel;
using TMPro;
using UnityEngine;

namespace Inktober2026.UI
{
    /// <summary>
    /// Shows how much of the apple is peeled, as a percentage, in the text label on the same object.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class PeelCoverageDisplay : MonoBehaviour
    {
        [SerializeField]
        private PeelCoverage coverage;

        [Tooltip("Text format. {0} is the percentage (0 to 100).")]
        [SerializeField]
        private string format = "Peeled: {0:0}%";

        private TMP_Text label;

        private void Reset()
        {
            coverage = FindAnyObjectByType<PeelCoverage>();
        }

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            coverage.OnCoverageChanged += PeelCoverage_OnCoverageChanged;
            Refresh(coverage.Coverage);
        }

        private void Refresh(float value)
        {
            label.text = string.Format(format, value * 100f);
        }

        private void PeelCoverage_OnCoverageChanged(float value)
        {
            Refresh(value);
        }

        private void OnDisable()
        {
            coverage.OnCoverageChanged -= PeelCoverage_OnCoverageChanged;
        }
    }
}

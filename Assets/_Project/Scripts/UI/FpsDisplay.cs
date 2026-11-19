using TMPro;
using UnityEngine;

namespace Inktober.UI
{
    /// <summary>
    /// Shows the frame rate in the text label on the same object, for checking performance on devices.
    /// The value is the average over the update interval, so it does not flicker.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class FpsDisplay : MonoBehaviour
    {
        [Tooltip("Seconds between refreshes of the text.")]
        [SerializeField]
        private float updateInterval = 0.5f;

        [Tooltip("Text format. {0} is frames per second, {1} is the average frame time in milliseconds.")]
        [SerializeField]
        private string format = "{0:0} FPS ({1:0.0} ms)";

        private TMP_Text label;
        private float elapsed;
        private int frames;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            frames++;
            if (elapsed < updateInterval)
                return;

            float averageFrameTime = elapsed / frames;
            label.text = string.Format(format, 1f / averageFrameTime, averageFrameTime * 1000f);
            elapsed = 0f;
            frames = 0;
        }
    }
}

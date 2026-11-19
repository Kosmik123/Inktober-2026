using System;
using UnityEngine;

namespace Inktober.Apple.Peel
{
    /// <summary>
    /// Measures how much of the apple is peeled: the average value of the <see cref="PeelMask"/> texture (0 to 1).
    /// The mask is shrunk on the GPU to a small texture, which is the only thing read back, a few times per second.
    /// The share is measured in texture space, so parts of the apple that take more room in the UV map count more.
    /// </summary>
    public class PeelCoverage : MonoBehaviour
    {
        private static readonly int OutputSizeId = Shader.PropertyToID("_OutputSize");
        private static readonly int TapsId = Shader.PropertyToID("_Taps");

        /// <summary>Raised after the measured coverage changed; the argument is the new coverage (0 to 1).</summary>
        public event Action<float> OnCoverageChanged;

        [SerializeField]
        private PeelMask peelMask;

        [SerializeField]
        private PeelTrail trail;

        [Tooltip("Hidden/Inktober/PeelCoverageDownsample. Assigned here so that it is included in builds.")]
        [SerializeField]
        private Shader downsampleShader;

        [Tooltip("Seconds between measurements while the peel grows.")]
        [SerializeField]
        private float updateInterval = 0.25f;

        [Tooltip("Size of the shrunk texture in pixels. It is read back, so keep it small.")]
        [SerializeField]
        private int sampleSize = 64;

        [Header("States")]
        [SerializeField]
        private float coverage;

        private Material material;
        private RenderTexture target;
        private Texture2D readTexture;
        private float nextRefreshTime;
        private bool isDirty;

        /// <summary>Share of the apple that is peeled, from 0 to 1.</summary>
        public float Coverage => coverage;

        private void Reset()
        {
            peelMask = FindAnyObjectByType<PeelMask>();
            trail = FindAnyObjectByType<PeelTrail>();
        }

        private void Awake()
        {
            if (peelMask == null || trail == null || downsampleShader == null)
            {
                Debug.LogError($"{nameof(PeelCoverage)} needs the mask, the trail and the downsample shader assigned.", this);
                enabled = false;
                return;
            }

            sampleSize = Mathf.Max(1, sampleSize);
            material = new Material(downsampleShader) { hideFlags = HideFlags.HideAndDontSave };
            target = new RenderTexture(sampleSize, sampleSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                name = "Peel Coverage",
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            target.Create();
            readTexture = new Texture2D(sampleSize, sampleSize, TextureFormat.RGBA32, false, true)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void OnEnable()
        {
            if (trail == null)
                return;

            trail.OnPointAdded += PeelTrail_OnPointAdded;
            trail.OnCleared += PeelTrail_OnCleared;
        }

        private void Update()
        {
            if (isDirty && Time.unscaledTime >= nextRefreshTime)
                Refresh();
        }

        /// <summary>Measures the coverage now, without waiting for the next scheduled measurement.</summary>
        public void Refresh()
        {
            isDirty = false;
            nextRefreshTime = Time.unscaledTime + updateInterval;
            if (peelMask.Mask == null)
                return;

            SetCoverage(Measure());
        }

        private float Measure()
        {
            // With the samples two source pixels apart, every bilinear sample covers 2x2 pixels, so the average is exact.
            material.SetFloat(OutputSizeId, sampleSize);
            material.SetFloat(TapsId, Mathf.Max(1, peelMask.Mask.width / sampleSize / 2));
            Graphics.Blit(peelMask.Mask, target, material);

            var previous = RenderTexture.active;
            RenderTexture.active = target;
            readTexture.ReadPixels(new Rect(0, 0, sampleSize, sampleSize), 0, 0, false);
            RenderTexture.active = previous;

            var pixels = readTexture.GetRawTextureData<Color32>();
            long sum = 0;
            for (int i = 0; i < pixels.Length; i++)
                sum += pixels[i].r;

            return sum / (255f * pixels.Length);
        }

        private void SetCoverage(float value)
        {
            if (Mathf.Approximately(value, coverage))
                return;

            coverage = value;
            OnCoverageChanged?.Invoke(coverage);
        }

        private void PeelTrail_OnPointAdded(PeelPoint point)
        {
            isDirty = true;
        }

        private void PeelTrail_OnCleared()
        {
            isDirty = false;
            SetCoverage(0f);
        }

        private void OnDisable()
        {
            if (trail == null)
                return;

            trail.OnPointAdded -= PeelTrail_OnPointAdded;
            trail.OnCleared -= PeelTrail_OnCleared;
        }

        private void OnDestroy()
        {
            if (target != null)
                target.Release();

            if (material != null)
                Destroy(material);

            if (readTexture != null)
                Destroy(readTexture);
        }
    }
}

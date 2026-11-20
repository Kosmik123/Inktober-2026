using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Inktober2026.Tests.EditMode
{
    /// <summary>
    /// Checks the PeelCoverageDownsample shader on masks with a known peeled share. Needs a graphics device.
    /// </summary>
    public class PeelCoverageShaderTests
    {
        private const string ShaderPath = "Assets/_Project/Shaders/PeelCoverageDownsample.shader";
        private const int MaskSize = 2048;
        private const int SampleSize = 64;
        private const float Tolerance = 0.005f;

        private Material material;

        [SetUp]
        public void SetUp()
        {
            Assume.That(SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null, "A graphics device is needed.");

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, "The shader is missing.");
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "The shader does not compile.");

            material = new Material(shader);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(material);
        }

        private static RenderTexture CreateMask()
        {
            var mask = new RenderTexture(MaskSize, MaskSize, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            mask.Create();
            return mask;
        }

        private static void Paint(RenderTexture mask, Color background, params Rect[] whiteRects)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = mask;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, MaskSize, 0, MaskSize);
            GL.Clear(false, true, background);
            foreach (var rect in whiteRects)
                Graphics.DrawTexture(rect, Texture2D.whiteTexture);

            GL.PopMatrix();
            RenderTexture.active = previous;
        }

        private float Measure(RenderTexture mask)
        {
            material.SetFloat("_OutputSize", SampleSize);
            material.SetFloat("_Taps", MaskSize / SampleSize / 2);
            var target = new RenderTexture(SampleSize, SampleSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            target.Create();
            Graphics.Blit(mask, target, material);

            var read = new Texture2D(SampleSize, SampleSize, TextureFormat.RGBA32, false, true);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            read.ReadPixels(new Rect(0, 0, SampleSize, SampleSize), 0, 0, false);
            RenderTexture.active = previous;

            var pixels = read.GetRawTextureData<Color32>();
            long sum = 0;
            for (int i = 0; i < pixels.Length; i++)
                sum += pixels[i].r;

            Object.DestroyImmediate(read);
            target.Release();
            return sum / (255f * pixels.Length);
        }

        [Test]
        public void EmptyMask_IsZero()
        {
            var mask = CreateMask();
            Paint(mask, Color.black);

            Assert.AreEqual(0f, Measure(mask), Tolerance);
            mask.Release();
        }

        [Test]
        public void FullMask_IsOne()
        {
            var mask = CreateMask();
            Paint(mask, Color.white);

            Assert.AreEqual(1f, Measure(mask), Tolerance);
            mask.Release();
        }

        [Test]
        public void LeftQuarter_IsAQuarter()
        {
            var mask = CreateMask();
            Paint(mask, Color.black, new Rect(0, 0, MaskSize * 0.25f, MaskSize));

            Assert.AreEqual(0.25f, Measure(mask), Tolerance);
            mask.Release();
        }

        [Test]
        public void SmallSquare_HasItsArea()
        {
            var mask = CreateMask();
            float side = MaskSize * 0.3f;
            Paint(mask, Color.black, new Rect(300, 700, side, side));

            Assert.AreEqual(0.09f, Measure(mask), Tolerance);
            mask.Release();
        }
    }
}

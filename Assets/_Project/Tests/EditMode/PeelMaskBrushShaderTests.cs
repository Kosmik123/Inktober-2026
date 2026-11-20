using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Inktober2026.Tests.EditMode
{
    /// <summary>
    /// Checks the shape of the PeelMaskBrush shader. The "apple" is a flat square: positions (x, y, height) and UV (x, y) with x, y in 0..1,
    /// so a texel of the mask is at the same x, y as its object space position. Needs a graphics device.
    /// </summary>
    public class PeelMaskBrushShaderTests
    {
        private const string ShaderPath = "Assets/_Project/Shaders/PeelMaskBrush.shader";
        private const int Size = 256;
        private const float HalfWidth = 0.05f;
        private const float Softness = 0.01f;
        private const float Extension = 0.03f;

        // The stroke runs along +X at y = 0.5, from x = 0.2 to x = 0.6. The surface normal is +Z, so the side axis is +Y.
        private const float Start = 0.2f;
        private const float End = 0.6f;
        private const float Row = 0.5f;

        private Material material;
        private Mesh mesh;
        private RenderTexture mask;
        private CommandBuffer commands;

        [SetUp]
        public void SetUp()
        {
            Assume.That(SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null, "A graphics device is needed.");

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, "The shader is missing.");
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "The shader does not compile.");

            material = new Material(shader);
            commands = new CommandBuffer();
            mask = new RenderTexture(Size, Size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            mask.Create();
            Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (mask)
                mask.Release();

            commands?.Release();
            Object.DestroyImmediate(mask);
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(material);
        }

        private void Clear()
        {
            var previous = RenderTexture.active;
            RenderTexture.active = mask;
            GL.Clear(false, true, Color.black);
            RenderTexture.active = previous;
        }

        // A square at the given height above the plane of the stroke.
        private void CreateSurface(float height)
        {
            mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, height),
                new Vector3(1f, 0f, height),
                new Vector3(1f, 1f, height),
                new Vector3(0f, 1f, height),
            };

            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        }

        private void Paint(float length = End - Start, float extension = Extension)
        {
            material.SetVector("_SegmentStart", new Vector3(Start, Row, 0f));
            material.SetVector("_SegmentDirection", Vector3.right);
            material.SetVector("_SegmentSide", Vector3.up);
            material.SetVector("_SegmentNormal", Vector3.forward);
            material.SetFloat("_SegmentLength", length);
            material.SetFloat("_HalfWidth", HalfWidth);
            material.SetFloat("_Softness", Softness);
            material.SetFloat("_Extension", extension);

            commands.Clear();
            commands.SetRenderTarget(mask);
            commands.DrawMesh(mesh, Matrix4x4.identity, material, 0, 0);
            Graphics.ExecuteCommandBuffer(commands);
        }

        private float ValueAt(float x, float y)
        {
            var read = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            var previous = RenderTexture.active;
            RenderTexture.active = mask;
            read.ReadPixels(new Rect(0, 0, Size, Size), 0, 0, false);
            RenderTexture.active = previous;

            float value = read.GetPixel(Mathf.FloorToInt(x * Size), Mathf.FloorToInt(y * Size)).r;
            Object.DestroyImmediate(read);
            return value;
        }

        [Test]
        public void InsideTheRectangle_IsPeeled()
        {
            CreateSurface(0f);
            Paint();

            Assert.AreEqual(1f, ValueAt(0.4f, Row), 0.02f);
            Assert.AreEqual(1f, ValueAt(0.4f, Row + HalfWidth * 0.6f), 0.02f);
            Assert.AreEqual(1f, ValueAt(0.4f, Row - HalfWidth * 0.6f), 0.02f);
        }

        [Test]
        public void BesideTheRectangle_IsNotPeeled()
        {
            CreateSurface(0f);
            Paint();

            Assert.AreEqual(0f, ValueAt(0.4f, Row + HalfWidth * 1.4f), 0.02f);
            Assert.AreEqual(0f, ValueAt(0.4f, Row - HalfWidth * 1.4f), 0.02f);
        }

        [Test]
        public void AheadOfTheEnd_IsNotPeeled()
        {
            // The brush has square ends: there is no round cap that reaches past the end of the step.
            CreateSurface(0f);
            Paint();

            Assert.AreEqual(0f, ValueAt(End + Softness * 2f, Row), 0.02f);
            Assert.AreEqual(0f, ValueAt(End + HalfWidth * 0.5f, Row), 0.02f);
        }

        [Test]
        public void BehindTheStart_IsPeeledOnlyWithinTheExtension()
        {
            CreateSurface(0f);
            Paint();

            Assert.AreEqual(1f, ValueAt(Start - Extension * 0.5f, Row), 0.02f, "The rectangle reaches back behind the start of the step.");
            Assert.AreEqual(1f, ValueAt(Start - Extension * 0.9f, Row), 0.02f);
            Assert.AreEqual(0f, ValueAt(Start - Extension * 1.5f, Row), 0.02f, "It does not reach farther back.");
        }

        [Test]
        public void TheFrontEdge_IsSquare()
        {
            CreateSurface(0f);
            Paint();

            Assert.AreEqual(1f, ValueAt(End - 0.01f, Row), 0.02f);
            Assert.AreEqual(0f, ValueAt(End + 0.01f, Row), 0.02f);
        }

        [Test]
        public void WithoutExtension_ThereIsNothingBehindTheStart()
        {
            CreateSurface(0f);
            Paint(extension: 0f);

            Assert.AreEqual(1f, ValueAt(Start + 0.01f, Row), 0.02f);
            Assert.AreEqual(0f, ValueAt(Start - 0.01f, Row), 0.02f);
        }

        [Test]
        public void TwoStepsOneAfterAnother_LeaveNoGapBetweenThem()
        {
            // Steps of 0.1: 0.2 to 0.3, then 0.3 to 0.4. The join at x = 0.3 has to be fully peeled.
            CreateSurface(0f);
            Paint(0.1f);
            material.SetVector("_SegmentStart", new Vector3(0.3f, Row, 0f));
            commands.Clear();
            commands.SetRenderTarget(mask);
            commands.DrawMesh(mesh, Matrix4x4.identity, material, 0, 0);
            Graphics.ExecuteCommandBuffer(commands);

            Assert.AreEqual(1f, ValueAt(0.3f, Row), 0.02f);
            Assert.AreEqual(1f, ValueAt(0.3f - 1f / Size * 2f, Row), 0.02f);
            Assert.AreEqual(1f, ValueAt(0.3f + 1f / Size * 2f, Row), 0.02f);
        }

        [Test]
        public void TheOtherSideOfTheObject_IsNotPeeled()
        {
            // The same position in the plane of the stroke, but far along the normal: the back of the apple.
            CreateSurface(0.5f);
            Paint();

            Assert.AreEqual(0f, ValueAt(0.4f, Row), 0.02f);
        }
    }
}

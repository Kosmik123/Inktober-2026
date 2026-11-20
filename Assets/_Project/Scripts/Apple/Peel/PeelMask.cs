using UnityEngine;
using UnityEngine.Rendering;

namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// Keeps the peel mask: a texture that is 1 where the apple is peeled and 0 where the skin is left.
    /// Every new step of the <see cref="PeelTrail"/> is drawn into it as a rectangle (square ends, no dot at the start),
    /// and the apple material (PeeledApple shader) reads it.
    /// Put it on the apple (the object the trail points are local to). The mask is only visual, the game logic uses the trail points.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class PeelMask : MonoBehaviour
    {
        private const string MaskProperty = "_Mask";
        private const string MaskResolutionProperty = "_MaskResolution";
        private const string FleshColorProperty = "_FleshColor";

        // A step shorter than this (in local units) has no direction, so it paints nothing.
        private const float MinStrokeLength = 1e-6f;

        private static readonly int SegmentStartId = Shader.PropertyToID("_SegmentStart");
        private static readonly int SegmentDirectionId = Shader.PropertyToID("_SegmentDirection");
        private static readonly int SegmentSideId = Shader.PropertyToID("_SegmentSide");
        private static readonly int SegmentNormalId = Shader.PropertyToID("_SegmentNormal");
        private static readonly int SegmentLengthId = Shader.PropertyToID("_SegmentLength");
        private static readonly int HalfWidthId = Shader.PropertyToID("_HalfWidth");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
        private static readonly int ExtensionId = Shader.PropertyToID("_Extension");

        [SerializeField]
        private PeelTrail trail;

        [Tooltip("Renderer with a material that uses the PeeledApple shader. Its material is instanced.")]
        [SerializeField]
        private Renderer targetRenderer;

        [Tooltip("Hidden/Inktober/PeelMaskBrush. Assigned here so that it is included in builds.")]
        [SerializeField]
        private Shader brushShader;

        [Header("Mask")]
        [Tooltip("Size of the square mask texture in pixels (one 8-bit channel, 2048 is 4 MB). Applied at start.")]
        [SerializeField]
        private int resolution = 2048;

        [Tooltip("Width of the soft edge of the peeled area, in world units. It is also the width of the dent.")]
        [SerializeField]
        private float edgeSoftness = 0.006f;

        [Tooltip("How far the painted triangles are grown beyond the borders of the UV islands, in texels. Hides the hairline at the UV seam.")]
        [SerializeField]
        private float paddingTexels = 3f;

        [Tooltip("How far each rectangle of the brush reaches back behind the start of its step, in world units. The length of the rectangle is the step plus this. A longer rectangle closes the gap at sharp turns, but bulges a little on the outside of a turn.")]
        [SerializeField]
        private float extension = 0.02f;

        private RenderTexture mask;
        private Material brush;
        private Material appleMaterial;
        private CommandBuffer commands;
        private Mesh mesh;
        private Vector3 lastPoint;
        private bool hasLastPoint;

        /// <summary>The mask texture (R channel), or null before start.</summary>
        public RenderTexture Mask => mask;

        /// <summary>Makes the whole apple unpeeled again.</summary>
        public void Clear()
        {
            hasLastPoint = false;
            if (mask == null)
                return;

            var previous = RenderTexture.active;
            RenderTexture.active = mask;
            GL.Clear(false, true, Color.black);
            RenderTexture.active = previous;
        }

        /// <summary>
        /// Draws a rectangle of the current trail width from one point to the next, both given in the local space of this object.
        /// The rectangle lies in the plane tangent to the surface (given by <paramref name="normal"/>) and has square ends.
        /// It covers the step itself and reaches back behind it by the configured extension; nothing is painted ahead of the step.
        /// </summary>
        public void Paint(Vector3 from, Vector3 to, Vector3 normal)
        {
            if (mask == null || mesh == null)
                return;

            var direction = Vector3.ProjectOnPlane(to - from, normal);
            float length = direction.magnitude;
            if (length < MinStrokeLength)
                return;

            direction /= length;
            float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), 1e-5f);
            float halfWidth = trail.Width * 0.5f / scale;
            float softness = Mathf.Min(edgeSoftness / scale, halfWidth * 0.99f);

            brush.SetVector(SegmentStartId, from);
            brush.SetVector(SegmentDirectionId, direction);
            brush.SetVector(SegmentSideId, Vector3.Cross(normal, direction));
            brush.SetVector(SegmentNormalId, normal);
            brush.SetFloat(SegmentLengthId, length);
            brush.SetFloat(HalfWidthId, halfWidth);
            brush.SetFloat(SoftnessId, softness);
            brush.SetFloat(ExtensionId, Mathf.Max(0f, extension) / scale);

            commands.Clear();
            commands.SetRenderTarget(mask);
            commands.DrawMesh(mesh, Matrix4x4.identity, brush, 0, 0);
            Graphics.ExecuteCommandBuffer(commands);
        }

        private void Awake()
        {
            var filter = GetComponent<MeshFilter>();
            if (trail == null || targetRenderer == null || brushShader == null || filter == null || filter.sharedMesh == null)
            {
                Debug.LogError($"{nameof(PeelMask)} needs the trail, the renderer, the brush shader and a mesh on the same object.", this);
                enabled = false;
                return;
            }

            brush = new Material(brushShader) { hideFlags = HideFlags.HideAndDontSave };
            commands = new CommandBuffer { name = nameof(PeelMask) };
            mask = CreateMask(Mathf.Max(16, resolution));
            mesh = PeelBrushMesh.CreatePadded(filter.sharedMesh, mask.width, paddingTexels);

            appleMaterial = targetRenderer.material;
            appleMaterial.SetTexture(MaskProperty, mask);
            appleMaterial.SetFloat(MaskResolutionProperty, mask.width);
            appleMaterial.SetColor(FleshColorProperty, trail.FleshColor);
            Clear();
        }

        private void OnEnable()
        {
            if (trail == null)
                return;

            trail.OnPointAdded += HandlePointAdded;
            trail.OnCleared += Clear;
        }

        private void OnDisable()
        {
            if (trail == null)
                return;

            trail.OnPointAdded -= HandlePointAdded;
            trail.OnCleared -= Clear;
        }

        private void OnDestroy()
        {
            if (mask != null)
                mask.Release();

            commands?.Release();
            if (brush != null)
                Destroy(brush);

            if (mesh != null)
                Destroy(mesh);

            if (appleMaterial != null)
                Destroy(appleMaterial);
        }

        private void HandlePointAdded(PeelPoint point)
        {
            // The first point only marks where the knife is: nothing is peeled until it moves.
            if (hasLastPoint)
                Paint(lastPoint, point.Position, point.Normal);

            lastPoint = point.Position;
            hasLastPoint = true;
        }

        private static RenderTexture CreateMask(int size)
        {
            var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;
            var texture = new RenderTexture(size, size, 0, format, RenderTextureReadWrite.Linear)
            {
                name = "Peel Mask",
                useMipMap = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.Create();
            return texture;
        }
    }
}

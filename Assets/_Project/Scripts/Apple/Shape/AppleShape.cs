using UnityEngine;

namespace Inktober2026.Apple.Shape
{
    /// <summary>
    /// Gives the apple its shape: generates the mesh (a sphere with a wider shoulder, dimples at the poles and a random unevenness)
    /// and uses it for the renderer and for the collider, so the knife walks on the real surface.
    /// It runs before the other components of the apple (the peel mask reads the mesh in its Awake).
    /// Put it on the apple mesh object together with a MeshFilter and a MeshCollider.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
    public class AppleShape : MonoBehaviour
    {
        [Tooltip("Every round gets another random seed. Turn it off to use the seed below, for example to see the same apple again.")]
        [SerializeField]
        private bool randomSeed = true;

        [Tooltip("Used when Random Seed is off.")]
        [SerializeField]
        private int seed = 1;

        [Header("Mesh")]
        [Tooltip("Number of vertical slices around the apple.")]
        [SerializeField]
        private int segments = 64;

        [Tooltip("Number of bands from the north pole to the south pole.")]
        [SerializeField]
        private int rings = 48;

        [SerializeField]
        private AppleShapeSettings shape = new AppleShapeSettings();

        [Header("States")]
        [SerializeField]
        private int usedSeed;

        private Mesh mesh;

        /// <summary>The seed of the apple of this round.</summary>
        public int UsedSeed => usedSeed;

        /// <summary>
        /// Distance from the centre to the surface in a direction, in local units, on the apple of this round. For things that sit on the apple.
        /// </summary>
        public float SurfaceRadius(Vector3 direction)
        {
            return shape.Radius * AppleMeshBuilder.RadiusFactor(direction.normalized, shape, AppleMeshBuilder.NoiseOffset(usedSeed));
        }

        private void Awake()
        {
            usedSeed = randomSeed ? Random.Range(int.MinValue, int.MaxValue) : seed;
            shape.Randomize();
            mesh = AppleMeshBuilder.Build(shape, usedSeed, segments, rings);
            mesh.hideFlags = HideFlags.HideAndDontSave;

            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        private void OnDestroy()
        {
            if (mesh)
                Destroy(mesh);
        }
    }
}

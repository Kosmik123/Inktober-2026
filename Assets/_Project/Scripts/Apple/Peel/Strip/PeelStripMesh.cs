using UnityEngine;

namespace Inktober.Apple.Peel
{
    /// <summary>
    /// Builds the mesh of the peeled strip from the sections of a <see cref="PeelWidthProfile"/> and places them with the
    /// <see cref="IPeelStripPath"/> on the same object. The mesh grows with the strip. With a static path it only adds
    /// vertices, with a dynamic one (a simulation) it rewrites them every frame.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PeelStripMesh : MonoBehaviour
    {
        [SerializeField]
        private PeelWidthProfile profile;

        [Tooltip("The strip stops growing after this many sections, to keep the mesh small.")]
        [SerializeField]
        private int maxSections = 4000;

        private IPeelStripPath path;
        private PeelStripGeometry geometry;
        private Mesh mesh;
        private bool isDirty;

        private void Awake()
        {
            path = GetComponent<IPeelStripPath>();
            if (path == null)
            {
                Debug.LogError($"{nameof(PeelStripMesh)} needs a component implementing {nameof(IPeelStripPath)} on the same object.", this);
                enabled = false;
                return;
            }

            geometry = new PeelStripGeometry();
            mesh = new Mesh { name = "Peel Strip" };
            mesh.hideFlags = HideFlags.HideAndDontSave;
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        private void OnEnable()
        {
            if (profile == null)
            {
                Debug.LogError($"{nameof(PeelStripMesh)} needs the width profile assigned.", this);
                enabled = false;
                return;
            }

            profile.OnSectionAdded += PeelWidthProfile_OnSectionAdded;
            profile.OnCleared += PeelWidthProfile_OnCleared;

            // Sections created before this component was enabled.
            for (int i = path.NodeCount; i < profile.Sections.Count; i++)
                PeelWidthProfile_OnSectionAdded(profile.Sections[i]);
        }

        private void LateUpdate()
        {
            if (path.IsDynamic)
                Rebuild();

            if (isDirty == false)
                return;

            geometry.ApplyTo(mesh);
            isDirty = false;
        }

        private void OnDisable()
        {
            if (profile == null)
                return;

            profile.OnSectionAdded -= PeelWidthProfile_OnSectionAdded;
            profile.OnCleared -= PeelWidthProfile_OnCleared;
        }

        private void OnDestroy()
        {
            if (mesh)
                Destroy(mesh);
        }

        private void PeelWidthProfile_OnSectionAdded(PeelSection section)
        {
            if (path.NodeCount >= maxSections)
                return;

            path.Append(section);
            if (path.IsDynamic)
                return;

            geometry.Append(section, path.GetNode(path.NodeCount - 1));
            isDirty = true;
        }

        private void PeelWidthProfile_OnCleared()
        {
            path.Clear();
            geometry.Clear();
            isDirty = true;
        }

        private void Rebuild()
        {
            geometry.Clear();
            for (int i = 0; i < path.NodeCount; i++)
                geometry.Append(profile.Sections[i], path.GetNode(i));

            isDirty = true;
        }

        private void OnValidate()
        {
            maxSections = Mathf.Max(2, maxSections);
        }
    }
}

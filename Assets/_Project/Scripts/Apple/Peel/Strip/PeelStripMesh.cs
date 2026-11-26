using UnityEngine;
using UnityEngine.Profiling;

namespace Inktober2026.Apple.Peel
{
	/// <summary>
	/// Builds the mesh of the peeled strip from the sections of a <see cref="PeelWidthProfile"/> and places them with the
	/// <see cref="IPeelStripPath"/> on the same object. The mesh grows with the strip and is rebuilt when it does (or every
	/// frame for a dynamic path, a simulation). Between two sections the piece still growing at the knife is added too, so the strip comes out smoothly.
	/// The pivot of the object is always at the newest end: the vertices are
	/// shifted so that the last node is at the origin, and the object is moved the other way, so the strip does not move in the world.
	/// </summary>
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class PeelStripMesh : MonoBehaviour
	{
		private PeelWidthProfile profile;

		[Tooltip("The strip stops growing after this many sections, to keep the mesh small.")]
		[SerializeField]
		private int maxSections = 4000;

		private IPeelStripPath path;
		private PeelStripGeometry geometry;
		private Mesh mesh;
		private bool isDirty;
		private bool needsRebuild;
		// Distance of the growing section in the mesh, negative when the mesh has none.
		private float builtLiveDistance = -1f;

		private void Awake()
		{
            profile = FindAnyObjectByType<PeelWidthProfile>();
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

			profile.OnSectionAdded += AddPeelSection;
			profile.OnCleared += PeelWidthProfile_OnCleared;

			// Sections created before this component was enabled.
			for (int i = path.NodeCount; i < profile.Sections.Count; i++)
				AddPeelSection(profile.Sections[i]);
		}

		private void AddPeelSection(PeelSection section)
		{
			if (path.NodeCount >= maxSections)
				return;

			path.Append(section);
			needsRebuild = true;
		}

		private void PeelWidthProfile_OnCleared()
		{
			path.Clear();
			geometry.Clear();
			needsRebuild = false;
			builtLiveDistance = -1f;
			isDirty = true;
		}

		private void Rebuild(bool hasLive, PeelSection live)
		{
			needsRebuild = false;
			builtLiveDistance = hasLive ? live.Distance : -1f;
			geometry.Clear();
			isDirty = true;
			if (path.NodeCount == 0)
				return;

			var view = path.Lay(hasLive, live, out var liveNode);
			for (int i = 0; i < path.NodeCount; i++)
				geometry.Append(profile.Sections[i], view.ToView(path.GetNode(i)));

			if (hasLive)
				geometry.Append(live, view.ToView(liveNode));
		}

		private void LateUpdate()
		{
			PeelSection live = default;
			bool hasLive = path.NodeCount > 0 && path.NodeCount < maxSections && profile.TryGetLiveSection(out live);
			float liveDistance = hasLive ? live.Distance : -1f;

			// The knife standing still leaves the growing section as it was, so there is nothing to rebuild.
			if (path.IsDynamic || needsRebuild || Mathf.Approximately(liveDistance, builtLiveDistance) == false)
				Rebuild(hasLive, live);

			if (isDirty == false)
				return;

			geometry.ApplyTo(mesh);
			isDirty = false;
		}

		private void OnDisable()
		{
			if (profile == null)
				return;

			profile.OnSectionAdded -= AddPeelSection;
			profile.OnCleared -= PeelWidthProfile_OnCleared;
		}

		private void OnDestroy()
		{
			if (mesh)
				Destroy(mesh);
		}

		private void OnValidate()
		{
			maxSections = Mathf.Max(2, maxSections);
		}
	}
}

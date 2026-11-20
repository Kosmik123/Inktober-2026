using UnityEngine;

namespace Inktober2026.Apple.Peel
{
	/// <summary>
	/// Builds the mesh of the peeled strip from the sections of a <see cref="PeelWidthProfile"/> and places them with the
	/// <see cref="IPeelStripPath"/> on the same object. The mesh grows with the strip and is rebuilt when it does (or every
	/// frame for a dynamic path, a simulation). The pivot of the object is always at the newest section: the vertices are
	/// shifted so that the last node is at the origin, and the object is moved the other way, so the strip does not move in the world.
	/// </summary>
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(IPeelStripPath))]
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
		private bool needsRebuild;

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
			isDirty = true;
		}

		private void Rebuild()
		{
			needsRebuild = false;
			geometry.Clear();
			if (path.NodeCount == 0)
			{
				isDirty = true;
				return;
			}

			// The nodes are in the frame the object had at the start, the last one becomes the pivot.

			var lastNode = path.GetNode(path.NodeCount - 1);
			var pivot = lastNode.Position;
			float angle = Vector3.SignedAngle(Vector3.right, lastNode.Right, lastNode.Up);

			Debug.Log("Last section turn is " + profile.Sections[profile.Sections.Count - 1].Turn);
			Debug.Log("Right is " + lastNode.Right + ", up is " + lastNode.Up + ", angle is " + angle);
			
			for (int i = 0; i < path.NodeCount; i++)
			{
				var node = path.GetNode(i);
				var rotation = Quaternion.AngleAxis(-angle, lastNode.Up);
				var rotatedNode = new PeelStripNode(
					rotation * (node.Position - pivot), 
					rotation * node.Right,
					rotation * node.Up);
				Debug.Log($"Node {i}: position {node.Position}, right {node.Right}, up {node.Up}");
				geometry.Append(profile.Sections[i], rotatedNode);
			}

			isDirty = true;
		}

		private void LateUpdate()
		{
			if (path.IsDynamic || needsRebuild)
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

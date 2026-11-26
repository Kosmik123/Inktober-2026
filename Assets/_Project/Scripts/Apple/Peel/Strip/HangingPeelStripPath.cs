using System.Collections.Generic;
using Inktober2026.Apple.Knife;
using UnityEngine;

namespace Inktober2026.Apple.Peel
{
	/// <summary>
	/// The strip that leaves the knife along its up axis and bends down the farther it is from it (see <see cref="PeelHangingShape"/>).
	/// It keeps only the flat shape (the length and the heading of every section) and lays the whole strip out again from the knife
	/// every time the mesh is built. There is no physics, no collision and no lag.
	/// </summary>
	public class HangingPeelStripPath : MonoBehaviour, IPeelStripPath
	{
		[Tooltip("The strip leaves along the up axis of this transform. Empty: the knife of the scene is looked for.")]
		[SerializeField]
		private Transform knife;

		[Tooltip("The way the strip leaves when there is no knife, in world space.")]
		[SerializeField]
		private Vector3 defaultExitDirection = new Vector3(0f, 1f, -1f);

		[Tooltip("Distance from the knife at which the strip hangs straight down, in world units. Shorter drops faster.")]
		[Min(0.01f)]
		[SerializeField]
		private float bendLength = 0.8f;

		[Tooltip("How the turn down is spread over the bend length: the share of the turn (up) over the share of the length (right). A straight line is a round arc, a flat start keeps the strip straight at the knife for longer.")]
		[SerializeField]
		private AnimationCurve bend = AnimationCurve.Linear(0f, 0f, 1f, 1f);

		private readonly List<float> distances = new List<float>();
		private readonly List<float> headings = new List<float>();
		private readonly List<PeelStripNode> nodes = new List<PeelStripNode>();
		private bool isLaid;

		// The knife can tilt at any moment and the strip with it.
		public bool IsDynamic => true;

		public int NodeCount => distances.Count;

		private void Awake()
		{
			if (knife)
				return;

			var controller = FindAnyObjectByType<KnifeController>();
			if (controller)
				knife = controller.transform;
		}

		public void Append(PeelSection section)
		{
			// The first section starts the strip, there is nothing for its turn to turn from.
			headings.Add(headings.Count == 0 ? 0f : headings[headings.Count - 1] + section.Turn);
			distances.Add(section.Distance);
			isLaid = false;
		}

		public PeelStripView Lay(bool hasLive, PeelSection live, out PeelStripNode liveNode)
		{
			liveNode = default;
			nodes.Clear();
			int count = distances.Count;
			if (count == 0)
				return PeelStripView.Identity;

			float headDistance = hasLive ? live.Distance : distances[count - 1];
			float headHeading = headings[count - 1] + (hasLive ? live.Turn : 0f);

			var exit = knife ? knife.up : defaultExitDirection;
			var shape = new PeelHangingShape(exit, bendLength, bend);
			if (hasLive)
				liveNode = ToLocal(shape.Node(Vector3.zero, 0f, 0f));

			for (int i = 0; i < count; i++)
				nodes.Add(default);

			// From the knife to the old end, every piece is added to the one nearer the knife.
			var position = Vector3.zero;
			float newerDistance = headDistance;
			float newerHeading = headHeading;
			for (int i = count - 1; i >= 0; i--)
			{
				float length = newerDistance - distances[i];
				// A piece has the heading of its newer end, as in the flat shape, and the pitch of its middle.
				float middle = headDistance - (newerDistance + distances[i]) * 0.5f;
				position += shape.TailDirection(middle, newerHeading - headHeading) * length;
				nodes[i] = ToLocal(shape.Node(position, headDistance - distances[i], headings[i] - headHeading));

				newerDistance = distances[i];
				newerHeading = headings[i];
			}

			isLaid = true;
			// The nodes are laid with the newest end at the origin and gravity where it is, so they are drawn as they are.
			return PeelStripView.Identity;
		}

		public PeelStripNode GetNode(int index)
		{
			if (isLaid == false)
				Lay(false, default, out _);

			return nodes[index];
		}

		public void Clear()
		{
			distances.Clear();
			headings.Clear();
			nodes.Clear();
			isLaid = false;
		}

		// The shape is worked out in world directions, the nodes are in the space of the strip object.
		private PeelStripNode ToLocal(PeelStripNode node)
		{
			return new PeelStripNode(
				transform.InverseTransformDirection(node.Position),
				transform.InverseTransformDirection(node.Right),
				transform.InverseTransformDirection(node.Up));
		}

		private void OnValidate()
		{
			bendLength = Mathf.Max(0.01f, bendLength);
		}
	}
}

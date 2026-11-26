using UnityEngine;

namespace Inktober2026.Apple.Peel
{
	/// <summary>
	/// The strip is laid out by an <see cref="IPeelStripPath"/> in a frame of its own, where its newest end travels on. It is drawn in the frame of
	/// its object, which stands still at the knife: the newest node is moved to the origin and turned around its up axis, so that its right side is +X.
	/// This is the conversion between the two frames. Whatever the path has to relate to the world (gravity, the apple) goes through it.
	/// </summary>
	public readonly struct PeelStripView
	{
		private readonly Vector3 pivot;
		private readonly Quaternion rotation;

		/// <summary>For nodes that are already in the frame the strip is drawn in.</summary>
		public static PeelStripView Identity => new PeelStripView(new PeelStripNode(Vector3.zero, Vector3.right, Vector3.up));

		public PeelStripView(PeelStripNode newest)
		{
			pivot = newest.Position;
			float angle = Vector3.SignedAngle(Vector3.right, newest.Right, newest.Up);
			rotation = Quaternion.AngleAxis(-angle, newest.Up);
		}

		public PeelStripNode ToView(PeelStripNode node)
		{
			return new PeelStripNode(rotation * (node.Position - pivot), rotation * node.Right, rotation * node.Up);
		}

		public Vector3 PointFromView(Vector3 point)
		{
			return pivot + Quaternion.Inverse(rotation) * point;
		}

		public Vector3 DirectionFromView(Vector3 direction)
		{
			return Quaternion.Inverse(rotation) * direction;
		}
	}
}

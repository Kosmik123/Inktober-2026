using UnityEngine;

namespace Inktober2026.Apple.Peel
{
	/// <summary>
	/// The shape of a strip that leaves the knife and bends down the farther it is from it, worked out from the flat shape only:
	/// no physics and nothing kept between the frames. Every piece of the strip has a heading and a pitch. The heading comes from the turns
	/// of the flat shape. The pitch depends only on the distance from the knife: at the knife the strip goes the way it leaves it,
	/// and over the bend length it turns to hang straight down. The side axis of the strip stays horizontal, so the bending never rolls it,
	/// and the pieces keep their length.
	/// Everything is relative to the newest end of the strip, in world directions (Y is up).
	/// </summary>
	public readonly struct PeelHangingShape
	{
		private const float Down = -90f;

		private readonly Vector3 back;
		private readonly Vector3 side;
		private readonly float exitPitch;
		private readonly float bendLength;
		private readonly AnimationCurve bend;

		/// <param name="exitDirection">The way the strip leaves the knife. Straight up or down has no heading, then it goes towards -Z.</param>
		/// <param name="bendLength">Distance from the knife at which the strip hangs straight down, in world units.</param>
		/// <param name="bend">Share of the turn down (0 to 1) over the share of the bend length (0 to 1). Null is a steady turn, a round arc.</param>
		public PeelHangingShape(Vector3 exitDirection, float bendLength, AnimationCurve bend = null)
		{
			var exit = exitDirection.sqrMagnitude < 1e-12f ? Vector3.back : exitDirection.normalized;
			var horizontal = new Vector3(exit.x, 0f, exit.z);
			back = horizontal.sqrMagnitude < 1e-8f ? Vector3.back : horizontal.normalized;
			side = Vector3.Cross(Vector3.up, -back);
			exitPitch = Mathf.Asin(Mathf.Clamp(exit.y, -1f, 1f)) * Mathf.Rad2Deg;
			this.bendLength = Mathf.Max(bendLength, 1e-4f);
			this.bend = bend;
		}

		/// <summary>Angle of the strip over the horizon at a distance from the knife, in degrees. -90 hangs straight down.</summary>
		public float PitchAt(float distance)
		{
			float share = Mathf.Clamp01(distance / bendLength);
			if (bend != null && bend.length > 0)
				share = Mathf.Clamp01(bend.Evaluate(share));

			return Mathf.Lerp(exitPitch, Down, share);
		}

		/// <summary>Direction along the strip away from the knife.</summary>
		/// <param name="distance">Distance from the knife along the strip.</param>
		/// <param name="heading">Heading of the flat shape relative to the one at the knife, in degrees, positive to the right.</param>
		public Vector3 TailDirection(float distance, float heading)
		{
			float pitch = PitchAt(distance) * Mathf.Deg2Rad;
			var horizontal = Quaternion.AngleAxis(heading, Vector3.up) * back;
			return horizontal * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch);
		}

		/// <summary>The node at a distance from the knife, standing at the given position.</summary>
		public PeelStripNode Node(Vector3 position, float distance, float heading)
		{
			var right = Quaternion.AngleAxis(heading, Vector3.up) * side;
			var up = Vector3.Cross(-TailDirection(distance, heading), right).normalized;
			return new PeelStripNode(position, right, up);
		}
	}
}

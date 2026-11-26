using UnityEngine;

namespace Inktober2026.Apple.Peel
{
	/// <summary>
	/// The two ends of the apple (the stalk at the north pole, the calyx at the south pole) are places the knife must not reach.
	/// Each is a cap around the pole, given as an angle from the axis of the apple. The poles are on the local Y axis.
	/// </summary>
	public static class PeelPole
	{
		/// <summary>
		/// True when any part of the peeled strip is in one of the caps: the centre of the knife path is closer to the pole than the cap
		/// and half the width of the strip together. So the strip never covers a pole, where the textures cannot be laid on it.
		/// </summary>
		/// <param name="position">Centre of the knife path, in the local space of the apple.</param>
		/// <param name="width">Width of the strip, in world units.</param>
		/// <param name="scale">World size of one local unit of the apple.</param>
		/// <param name="northAngle">Angle of the cap at the north pole, in degrees.</param>
		/// <param name="southAngle">Angle of the cap at the south pole, in degrees.</param>
		public static bool Touches(Vector3 position, float width, float scale, float northAngle, float southAngle)
		{
			float radius = position.magnitude * Mathf.Max(scale, 1e-6f);
			if (radius < 1e-6f)
				return false;

			float halfWidthAngle = Mathf.Max(0f, width) * 0.5f / radius * Mathf.Rad2Deg;
			float fromNorth = Vector3.Angle(Vector3.up, position);
			return fromNorth < northAngle + halfWidthAngle || 180f - fromNorth < southAngle + halfWidthAngle;
		}

		/// <summary>
		/// The turn, in world space, that takes the poles away from a direction: after it the angle between the axis of the apple
		/// and the direction is at least <paramref name="minAngle"/>, at both ends. No turn when it already is.
		/// </summary>
		/// <param name="direction">Direction from the centre of the apple, for example towards the knife.</param>
		/// <param name="axis">The axis of the apple, from the south pole to the north pole.</param>
		/// <param name="minAngle">Smallest allowed angle between a pole and the direction, in degrees, below 90.</param>
		public static Quaternion KeepAway(Vector3 direction, Vector3 axis, float minAngle)
		{
			minAngle = Mathf.Clamp(minAngle, 0f, 89f);
			float angle = Vector3.Angle(direction, axis);
			float allowed = Mathf.Clamp(angle, minAngle, 180f - minAngle);
			if (Mathf.Approximately(allowed, angle))
				return Quaternion.identity;

			var around = Vector3.Cross(direction, axis);
			// A pole right under the direction can be moved away to any side.
			if (around.sqrMagnitude < 1e-10f)
				around = Vector3.Cross(direction, Mathf.Abs(direction.normalized.y) < 0.9f ? Vector3.up : Vector3.right);

			return Quaternion.AngleAxis(allowed - angle, around.normalized);
		}
	}
}

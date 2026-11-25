using UnityEngine;

namespace Inktober2026.Apple
{
	/// <summary>
	/// A slow, random rotation: the object turns around an axis that drifts to a new random one every few seconds,
	/// at a speed that drifts too. Gives a rotation for one frame, the caller applies it.
	/// </summary>
	public class Tumble
	{
		private readonly float minSpeed;
		private readonly float maxSpeed;
		private readonly float minChangeTime;
		private readonly float maxChangeTime;
		private readonly float sharpness;

		private Vector3 axis;
		private Vector3 targetAxis;
		private float speed;
		private float targetSpeed;
		private float timeToChange;

		public Vector3 Axis => axis;

		/// <summary>Current speed in degrees per second.</summary>
		public float Speed => speed;

		/// <param name="minSpeed">Slowest speed, degrees per second.</param>
		/// <param name="maxSpeed">Fastest speed, degrees per second.</param>
		/// <param name="minChangeTime">Shortest time before the axis and the speed head for new random ones, in seconds.</param>
		/// <param name="maxChangeTime">Longest such time.</param>
		/// <param name="sharpness">How fast the axis and the speed reach the new ones: 1 is about a second.</param>
		public Tumble(float minSpeed, float maxSpeed, float minChangeTime, float maxChangeTime, float sharpness = 1f)
		{
			this.minSpeed = Mathf.Max(0f, minSpeed);
			this.maxSpeed = Mathf.Max(this.minSpeed, maxSpeed);
			this.minChangeTime = Mathf.Max(0.01f, minChangeTime);
			this.maxChangeTime = Mathf.Max(this.minChangeTime, maxChangeTime);
			this.sharpness = Mathf.Max(0f, sharpness);

			axis = targetAxis = Random.onUnitSphere;
			speed = targetSpeed = Random.Range(this.minSpeed, this.maxSpeed);
			timeToChange = Random.Range(this.minChangeTime, this.maxChangeTime);
		}

		/// <summary>The rotation around the world axis for the given time.</summary>
		public Quaternion Step(float deltaTime)
		{
			timeToChange -= deltaTime;
			if (timeToChange <= 0f)
			{
				targetAxis = Random.onUnitSphere;
				targetSpeed = Random.Range(minSpeed, maxSpeed);
				timeToChange = Random.Range(minChangeTime, maxChangeTime);
			}

			float blend = 1f - Mathf.Exp(-sharpness * deltaTime);
			var blended = Vector3.Slerp(axis, targetAxis, blend);
			// Slerp between opposite axes has no direction of its own, so the old axis is kept then.
			if (blended.sqrMagnitude > 1e-6f)
				axis = blended.normalized;
			speed = Mathf.Lerp(speed, targetSpeed, blend);

			return Quaternion.AngleAxis(speed * deltaTime, axis);
		}
	}
}

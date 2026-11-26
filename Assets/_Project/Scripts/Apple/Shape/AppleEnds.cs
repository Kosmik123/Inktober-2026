using UnityEngine;

namespace Inktober2026.Apple.Shape
{
	/// <summary>
	/// Puts the stalk into the dimple at the north pole and the calyx into the one at the south pole. The depth of the dimples depends on
	/// the apple of the round, so they are placed at the start. They are only for the look: they have no colliders, as the knife walks
	/// on the colliders of the apple. Their meshes grow along their local Y axis from their origin.
	/// </summary>
	public class AppleEnds : MonoBehaviour
	{
		[SerializeField]
		private AppleShape shape;

		[SerializeField]
		private Transform stalk;

		[SerializeField]
		private Transform calyx;

		[Tooltip("How deep the stalk and the calyx are sunk under the surface, in local units, so their bases do not float.")]
		[SerializeField]
		private float sink = 0.01f;

		private void Reset()
		{
			shape = GetComponentInChildren<AppleShape>();
		}

		// The shape is built in Awake.
		private void Start()
		{
			Place(stalk, Vector3.up);
			Place(calyx, Vector3.down);
		}

		private void Place(Transform end, Vector3 pole)
		{
			if (end == null || shape == null)
				return;

			var surface = shape.transform;
			end.position = surface.TransformPoint(pole * (shape.SurfaceRadius(pole) - sink));
			end.rotation = surface.rotation * Quaternion.FromToRotation(Vector3.up, pole);
		}
	}
}

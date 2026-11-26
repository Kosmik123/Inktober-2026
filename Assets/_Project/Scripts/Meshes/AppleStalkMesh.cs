using Bipolar.ProceduralMeshes;
using UnityEngine;

namespace Inktober2026.Meshes
{
	/// <summary>
	/// The stalk of an apple: a thin tube that starts at the origin, goes up along Y, bends to +X and gets a little thicker again
	/// at its end, closed with a flat cap.
	/// </summary>
	[CreateAssetMenu(menuName = "Inktober/Procedural Meshes/Apple Stalk")]
	public class AppleStalkMesh : ProceduralMesh
	{
		[SerializeField]
		private float length = 0.16f;

		[Tooltip("How far the end is moved to the side.")]
		[SerializeField]
		private float bend = 0.04f;

		[SerializeField]
		private float baseRadius = 0.014f;

		[Tooltip("Radius in the middle of the stalk, the thinnest place.")]
		[SerializeField]
		private float middleRadius = 0.009f;

		[SerializeField]
		private float tipRadius = 0.015f;

		[SerializeField]
		private int sides = 8;

		[SerializeField]
		private int segments = 10;

		public override void BuildMesh(Mesh mesh)
		{
			int ringSize = sides + 1;
			int tubeCount = ringSize * (segments + 1);
			// The cap has vertices of its own, so its edge stays sharp.
			var vertices = new Vector3[tubeCount + ringSize + 1];
			var uvs = new Vector2[vertices.Length];
			var triangles = new int[(segments * sides * 2 + sides) * 3];
			int next = 0;

			for (int segment = 0; segment <= segments; segment++)
			{
				float t = (float)segment / segments;
				Frame(t, out var centre, out var across, out var depth);
				float radius = t < 0.5f ? Mathf.Lerp(baseRadius, middleRadius, t * 2f) : Mathf.Lerp(middleRadius, tipRadius, t * 2f - 1f);
				for (int side = 0; side <= sides; side++)
				{
					float angle = 2f * Mathf.PI * side / sides;
					int index = segment * ringSize + side;
					vertices[index] = centre + (across * Mathf.Cos(angle) + depth * Mathf.Sin(angle)) * radius;
					uvs[index] = new Vector2((float)side / sides, t);
					if (segment == segments)
					{
						vertices[tubeCount + side] = vertices[index];
						uvs[tubeCount + side] = new Vector2(0.5f + Mathf.Cos(angle) * 0.5f, 0.5f + Mathf.Sin(angle) * 0.5f);
					}
				}
			}

			for (int segment = 0; segment < segments; segment++)
			{
				for (int side = 0; side < sides; side++)
				{
					int low = segment * ringSize + side;
					int high = low + ringSize;
					Add(low, high, low + 1);
					Add(low + 1, high, high + 1);
				}
			}

			int capCentre = vertices.Length - 1;
			Frame(1f, out vertices[capCentre], out _, out _);
			uvs[capCentre] = new Vector2(0.5f, 0.5f);
			for (int side = 0; side < sides; side++)
				Add(capCentre, tubeCount + side + 1, tubeCount + side);

			mesh.Clear();
			mesh.vertices = vertices;
			mesh.uv = uvs;
			mesh.triangles = triangles;
			mesh.RecalculateNormals();
			mesh.RecalculateTangents();
			mesh.RecalculateBounds();

			void Add(int a, int b, int c)
			{
				triangles[next++] = a;
				triangles[next++] = b;
				triangles[next++] = c;
			}
		}

		// The middle line of the stalk and two directions across it.
		private void Frame(float t, out Vector3 centre, out Vector3 across, out Vector3 depth)
		{
			centre = new Vector3(bend * t * t, length * t, 0f);
			var along = new Vector3(2f * bend * t, length, 0f).normalized;
			across = Vector3.Cross(along, Vector3.forward).normalized;
			depth = Vector3.Cross(across, along);
		}

		public override void Validate()
		{
			length = Mathf.Max(0.001f, length);
			baseRadius = Mathf.Max(0f, baseRadius);
			middleRadius = Mathf.Max(0f, middleRadius);
			tipRadius = Mathf.Max(0f, tipRadius);
			sides = Mathf.Max(3, sides);
			segments = Mathf.Max(1, segments);
		}
	}
}

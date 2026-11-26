using Bipolar.ProceduralMeshes;
using UnityEngine;

namespace Inktober2026.Meshes
{
	/// <summary>
	/// The calyx at the bottom of an apple (the rest of the flower): a few small pointed leaves around the origin. Each leaf goes out
	/// from the centre and rises along Y towards its tip. The leaves are seen from both sides.
	/// </summary>
	[CreateAssetMenu(menuName = "Inktober/Procedural Meshes/Apple Calyx")]
	public class AppleCalyxMesh : ProceduralMesh
	{
		private const int LeafVertices = 5;
		private const int LeafTriangles = 3;

		[SerializeField]
		private int leaves = 5;

		[Tooltip("How far from the centre the tips reach.")]
		[SerializeField]
		private float length = 0.05f;

		[SerializeField]
		private float width = 0.028f;

		[Tooltip("How high over the origin the tips are.")]
		[SerializeField]
		private float rise = 0.035f;

		public override void BuildMesh(Mesh mesh)
		{
			// Every leaf twice, one copy for each side, with vertices of its own so the normals differ.
			var vertices = new Vector3[leaves * LeafVertices * 2];
			var uvs = new Vector2[vertices.Length];
			var triangles = new int[leaves * LeafTriangles * 3 * 2];
			int next = 0;

			for (int leaf = 0; leaf < leaves; leaf++)
			{
				var rotation = Quaternion.AngleAxis(360f * leaf / leaves, Vector3.up);
				var outward = rotation * Vector3.forward;
				var across = rotation * Vector3.right;

				for (int face = 0; face < 2; face++)
				{
					int first = (leaf * 2 + face) * LeafVertices;
					Set(first, outward * (length * 0.1f) - across * (width * 0.3f), 0f, 0f);
					Set(first + 1, outward * (length * 0.1f) + across * (width * 0.3f), 1f, 0f);
					Set(first + 2, outward * (length * 0.5f) - across * (width * 0.5f) + Vector3.up * (rise * 0.35f), 0f, 0.5f);
					Set(first + 3, outward * (length * 0.5f) + across * (width * 0.5f) + Vector3.up * (rise * 0.35f), 1f, 0.5f);
					Set(first + 4, outward * length + Vector3.up * rise, 0.5f, 1f);

					bool top = face == 0;
					Add(first, first + 2, first + 3, top);
					Add(first, first + 3, first + 1, top);
					Add(first + 2, first + 4, first + 3, top);
				}
			}

			mesh.Clear();
			mesh.vertices = vertices;
			mesh.uv = uvs;
			mesh.triangles = triangles;
			mesh.RecalculateNormals();
			mesh.RecalculateTangents();
			mesh.RecalculateBounds();

			void Set(int index, Vector3 position, float u, float v)
			{
				vertices[index] = position;
				uvs[index] = new Vector2(u, v);
			}

			void Add(int a, int b, int c, bool asGiven)
			{
				triangles[next++] = a;
				triangles[next++] = asGiven ? b : c;
				triangles[next++] = asGiven ? c : b;
			}
		}

		public override void Validate()
		{
			leaves = Mathf.Max(1, leaves);
			length = Mathf.Max(0.001f, length);
			width = Mathf.Max(0.001f, width);
		}
	}
}

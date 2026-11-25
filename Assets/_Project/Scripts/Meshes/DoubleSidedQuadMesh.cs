using Bipolar.ProceduralMeshes;
using UnityEngine;

namespace Inktober2026.Meshes
{
	/// <summary>
	/// A flat rectangle seen from both sides, in the XY plane and centred on the origin, like the built-in Quad.
	/// Submesh 0 is the front (it faces -Z, as the built-in Quad does), submesh 1 is the back, so each side takes its own material.
	/// The sides have vertices of their own, with opposite normals. A texture reads the right way round on both sides.
	/// </summary>
	[CreateAssetMenu(menuName = "Inktober/Procedural Meshes/Double Sided Quad")]
	public class DoubleSidedQuadMesh : ProceduralMesh
	{
		public const int FrontSubmesh = 0;
		public const int BackSubmesh = 1;

		private const int CornersPerSide = 4;

		[SerializeField]
		private Vector2 size = Vector2.one;
		public Vector2 Size
		{
			get => size;
			set
			{
				size = value;
				Validate();
			}
		}

		public override void BuildMesh(Mesh mesh)
		{
			var half = size * 0.5f;
			// Bottom left, bottom right, top left, top right, as seen from the front.
			var corners = new[]
			{
				new Vector3(-half.x, -half.y, 0f),
				new Vector3(half.x, -half.y, 0f),
				new Vector3(-half.x, half.y, 0f),
				new Vector3(half.x, half.y, 0f),
			};
			var frontUvs = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };

			var vertices = new Vector3[CornersPerSide * 2];
			var normals = new Vector3[vertices.Length];
			var uvs = new Vector2[vertices.Length];
			for (int i = 0; i < CornersPerSide; i++)
			{
				vertices[i] = corners[i];
				normals[i] = Vector3.back;
				uvs[i] = frontUvs[i];

				int back = CornersPerSide + i;
				vertices[back] = corners[i];
				normals[back] = Vector3.forward;
				// From behind left and right swap places, so U is mirrored.
				uvs[back] = new Vector2(1f - frontUvs[i].x, frontUvs[i].y);
			}

			// Clockwise as seen from the side the triangle faces.
			var frontTriangles = new[] { 0, 2, 3, 0, 3, 1 };
			var backTriangles = new[] { 4, 7, 6, 4, 5, 7 };

			mesh.Clear();
			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.uv = uvs;
			mesh.subMeshCount = 2;
			mesh.SetTriangles(frontTriangles, FrontSubmesh);
			mesh.SetTriangles(backTriangles, BackSubmesh);
			mesh.RecalculateTangents();
			mesh.RecalculateBounds();
		}

		public override void Validate()
		{
			size = Vector2.Max(size, Vector2.zero);
		}
	}
}

using Inktober2026.Meshes;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
	public class DoubleSidedQuadMeshTests
	{
		private DoubleSidedQuadMesh quad;
		private Mesh mesh;

		[SetUp]
		public void SetUp()
		{
			quad = ScriptableObject.CreateInstance<DoubleSidedQuadMesh>();
			quad.Size = new Vector2(2f, 3f);
			mesh = new Mesh();
			quad.BuildMesh(mesh);
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(mesh);
			Object.DestroyImmediate(quad);
		}

		[Test]
		public void HasASubmeshForEachSideAndTheSetSize()
		{
			Assert.AreEqual(2, mesh.subMeshCount);
			Assert.AreEqual(6, mesh.GetTriangles(DoubleSidedQuadMesh.FrontSubmesh).Length);
			Assert.AreEqual(6, mesh.GetTriangles(DoubleSidedQuadMesh.BackSubmesh).Length);
			Assert.AreEqual(new Vector3(2f, 3f, 0f), mesh.bounds.size);
		}

		[TestCase(DoubleSidedQuadMesh.FrontSubmesh, -1f)]
		[TestCase(DoubleSidedQuadMesh.BackSubmesh, 1f)]
		public void TrianglesAndNormalsFaceTheirSide(int submesh, float z)
		{
			var vertices = mesh.vertices;
			var normals = mesh.normals;
			var triangles = mesh.GetTriangles(submesh);
			for (int i = 0; i < triangles.Length; i += 3)
			{
				var a = vertices[triangles[i]];
				var facing = Vector3.Cross(vertices[triangles[i + 1]] - a, vertices[triangles[i + 2]] - a).normalized;

				Assert.AreEqual(z, facing.z, 1e-4f);
				for (int j = 0; j < 3; j++)
					Assert.AreEqual(z, normals[triangles[i + j]].z, 1e-4f);
			}
		}

		[TestCase(DoubleSidedQuadMesh.FrontSubmesh)]
		[TestCase(DoubleSidedQuadMesh.BackSubmesh)]
		public void BothTrianglesOfASideCoverTheWholeQuad(int submesh)
		{
			var vertices = mesh.vertices;
			var triangles = mesh.GetTriangles(submesh);
			float area = 0f;
			for (int i = 0; i < triangles.Length; i += 3)
			{
				var a = vertices[triangles[i]];
				area += Vector3.Cross(vertices[triangles[i + 1]] - a, vertices[triangles[i + 2]] - a).magnitude * 0.5f;
			}

			Assert.AreEqual(6f, area, 1e-4f);
		}
	}
}

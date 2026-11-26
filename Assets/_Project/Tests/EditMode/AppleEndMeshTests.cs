using Bipolar.ProceduralMeshes;
using Inktober2026.Meshes;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class AppleEndMeshTests
    {
        private static Mesh Build<T>() where T : ProceduralMesh
        {
            var asset = ScriptableObject.CreateInstance<T>();
            var mesh = new Mesh();
            asset.BuildMesh(mesh);
            Object.DestroyImmediate(asset);
            return mesh;
        }

        [Test]
        public void TheSidesOfTheStalk_FaceOutwards()
        {
            var mesh = Build<AppleStalkMesh>();

            // The first ring is around the origin, where the stalk goes straight up.
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            for (int i = 0; i < 8; i++)
                Assert.Greater(Vector3.Dot(normals[i], new Vector3(vertices[i].x, 0f, vertices[i].z)), 0f, $"Vertex {i}.");

            Assert.Greater(mesh.bounds.max.y, 0.15f);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void TheCapOfTheStalk_FacesUp()
        {
            var mesh = Build<AppleStalkMesh>();

            Assert.Greater(mesh.normals[mesh.vertexCount - 1].y, 0.7f);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void EveryLeafOfTheCalyx_HasBothSides()
        {
            var mesh = Build<AppleCalyxMesh>();

            var normals = mesh.normals;
            for (int leaf = 0; leaf < 5; leaf++)
                Assert.Less(Vector3.Dot(normals[leaf * 10 + 4], normals[leaf * 10 + 9]), -0.9f, $"Leaf {leaf}.");

            Object.DestroyImmediate(mesh);
        }
    }
}

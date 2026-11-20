using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Inktober.Apple.Peel
{
    /// <summary>
    /// The vertices of the strip: two for every section (the left and the right edge) and a quad between neighbouring sections.
    /// The strip has two sides in two submeshes, so each can have its own material: the skin side looks along
    /// <see cref="PeelStripNode.Up"/> (submesh 0), the flesh side looks the other way (submesh 1). Both sides use the texture
    /// coordinates of the sections, so the textures lie as they did on the apple. The winding is clockwise seen from the outside of each side.
    /// </summary>
    public class PeelStripGeometry
    {
        public const int SkinSubmesh = 0;
        public const int FleshSubmesh = 1;

        private readonly List<Vector3> skinVertices = new List<Vector3>();
        private readonly List<Vector3> skinNormals = new List<Vector3>();
        private readonly List<Vector2> uv = new List<Vector2>();
        private readonly List<Vector3> fleshVertices = new List<Vector3>();
        private readonly List<Vector3> fleshNormals = new List<Vector3>();
        private readonly List<int> skinTriangles = new List<int>();
        private readonly List<int> fleshTriangles = new List<int>();

        private readonly List<Vector3> allVertices = new List<Vector3>();
        private readonly List<Vector3> allNormals = new List<Vector3>();
        private readonly List<Vector2> allUv = new List<Vector2>();
        private readonly List<int> shiftedFleshTriangles = new List<int>();

        /// <summary>Number of sections added.</summary>
        public int SectionCount => skinVertices.Count / 2;

        /// <summary>Adds the next section at the end of the strip.</summary>
        public void Append(PeelSection section, PeelStripNode node)
        {
            int left = skinVertices.Count;
            int right = left + 1;

            var leftPoint = node.Position + node.Right * section.LeftEdge;
            var rightPoint = node.Position + node.Right * section.RightEdge;

            skinVertices.Add(leftPoint);
            skinVertices.Add(rightPoint);
            skinNormals.Add(node.Up);
            skinNormals.Add(node.Up);
            fleshVertices.Add(leftPoint);
            fleshVertices.Add(rightPoint);
            fleshNormals.Add(-node.Up);
            fleshNormals.Add(-node.Up);
            uv.Add(section.LeftUv);
            uv.Add(section.RightUv);

            if (left == 0)
                return;

            int previousLeft = left - 2;
            int previousRight = left - 1;

            skinTriangles.Add(previousLeft);
            skinTriangles.Add(left);
            skinTriangles.Add(right);

            skinTriangles.Add(previousLeft);
            skinTriangles.Add(right);
            skinTriangles.Add(previousRight);

            fleshTriangles.Add(previousLeft);
            fleshTriangles.Add(right);
            fleshTriangles.Add(left);

            fleshTriangles.Add(previousLeft);
            fleshTriangles.Add(previousRight);
            fleshTriangles.Add(right);
        }

        /// <summary>Removes all sections.</summary>
        public void Clear()
        {
            skinVertices.Clear();
            skinNormals.Clear();
            fleshVertices.Clear();
            fleshNormals.Clear();
            uv.Clear();
            skinTriangles.Clear();
            fleshTriangles.Clear();
        }

        /// <summary>Replaces the contents of <paramref name="mesh"/> with the strip: the skin side first, then the flesh side.</summary>
        public void ApplyTo(Mesh mesh)
        {
            int skinCount = skinVertices.Count;

            allVertices.Clear();
            allVertices.AddRange(skinVertices);
            allVertices.AddRange(fleshVertices);

            allNormals.Clear();
            allNormals.AddRange(skinNormals);
            allNormals.AddRange(fleshNormals);

            allUv.Clear();
            allUv.AddRange(uv);
            allUv.AddRange(uv);

            shiftedFleshTriangles.Clear();
            for (int i = 0; i < fleshTriangles.Count; i++)
                shiftedFleshTriangles.Add(fleshTriangles[i] + skinCount);

            mesh.Clear();
            mesh.indexFormat = allVertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(allVertices);
            mesh.SetNormals(allNormals);
            mesh.SetUVs(0, allUv);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(skinTriangles, SkinSubmesh);
            mesh.SetTriangles(shiftedFleshTriangles, FleshSubmesh);
            mesh.RecalculateBounds();

            // The shader of the apple needs tangents for the normal maps.
            mesh.RecalculateTangents();
        }
    }
}

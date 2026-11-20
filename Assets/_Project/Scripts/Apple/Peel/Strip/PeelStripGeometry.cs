using System.Collections.Generic;
using UnityEngine;

namespace Inktober.Apple.Peel
{
    /// <summary>
    /// The vertices of the strip: two for every section (the left and the right edge) and a quad between neighbouring sections.
    /// The face looks along <see cref="PeelStripNode.Up"/>, the winding is clockwise seen from there.
    /// Texture coordinates run from 0 on the left edge to 1 on the right, and along the strip by its length in world units.
    /// </summary>
    public class PeelStripGeometry
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uv = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();

        /// <summary>Number of sections added.</summary>
        public int SectionCount => vertices.Count / 2;

        /// <summary>Adds the next section at the end of the strip.</summary>
        public void Append(PeelSection section, PeelStripNode node)
        {
            int left = vertices.Count;
            int right = left + 1;

            vertices.Add(node.Position + node.Right * section.LeftEdge);
            vertices.Add(node.Position + node.Right * section.RightEdge);
            normals.Add(node.Up);
            normals.Add(node.Up);
            uv.Add(new Vector2(0f, section.Distance));
            uv.Add(new Vector2(1f, section.Distance));

            if (left == 0)
                return;

            int previousLeft = left - 2;
            int previousRight = left - 1;

            triangles.Add(previousLeft);
            triangles.Add(left);
            triangles.Add(right);

            triangles.Add(previousLeft);
            triangles.Add(right);
            triangles.Add(previousRight);
        }

        /// <summary>Removes all sections.</summary>
        public void Clear()
        {
            vertices.Clear();
            normals.Clear();
            uv.Clear();
            triangles.Clear();
        }

        /// <summary>Replaces the contents of <paramref name="mesh"/> with the strip.</summary>
        public void ApplyTo(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }
    }
}

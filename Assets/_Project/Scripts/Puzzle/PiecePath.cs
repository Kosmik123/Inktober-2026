using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Puzzle
{
    /// <summary>
    /// The track a piece rides on when it leaves the grid: its own cells from the tail to the head, then straight on
    /// along the direction of the head up to the first cell outside the grid. A position on the track is given as an index
    /// (3.5 is halfway between the points 3 and 4), in grid space where a cell is one unit.
    /// </summary>
    public sealed class PiecePath
    {
        private readonly Vector3[] points;

        public PiecePath(ArrowPiece piece, Vector3Int gridSize)
        {
            var list = new List<Vector3>();
            for (int i = 0; i < piece.Cells.Count; i++)
                list.Add((Vector3)piece.Cells[i]);

            var cell = piece.Head;
            bool inside;
            do
            {
                cell += piece.Direction;
                list.Add((Vector3)cell);
                inside = IsInside(cell, gridSize);
            }
            while (inside);

            points = list.ToArray();
            ExitIndex = points.Length - 1;
        }

        /// <summary>The index of the first point outside the grid. A stone that is there has left the grid completely.</summary>
        public int ExitIndex { get; }

        /// <summary>The position at the index; indexes outside of the track give its first or its last point.</summary>
        public Vector3 Evaluate(float index)
        {
            if (index <= 0f)
                return points[0];

            if (index >= points.Length - 1)
                return points[points.Length - 1];

            int lower = Mathf.FloorToInt(index);
            return Vector3.Lerp(points[lower], points[lower + 1], index - lower);
        }

        private static bool IsInside(Vector3Int cell, Vector3Int size)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.z >= 0 && cell.x < size.x && cell.y < size.y && cell.z < size.z;
        }
    }
}

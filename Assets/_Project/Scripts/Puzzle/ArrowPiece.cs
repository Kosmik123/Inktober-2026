using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Puzzle
{
    /// <summary>
    /// A bent arrow made of grid cells. The cells go from the tail to the head, the head moves along
    /// <see cref="Direction"/>, which follows the last segment (a one-cell piece can point anywhere).
    /// </summary>
    public class ArrowPiece
    {
        private readonly Vector3Int[] cells;

        public IReadOnlyList<Vector3Int> Cells => cells;

        public Vector3Int Head => cells[cells.Length - 1];

        public Vector3Int Direction { get; }

        public ArrowPiece(IEnumerable<Vector3Int> cells, Vector3Int direction)
        {
            this.cells = new List<Vector3Int>(cells).ToArray();
            Direction = direction;
            Validate();
        }

        private void Validate()
        {
            if (cells.Length == 0)
                throw new ArgumentException("A piece needs at least one cell.");

            if (DirectionUtility.IsAxisDirection(Direction) == false)
                throw new ArgumentException($"The direction {Direction} is not an axis direction.");

            var visited = new HashSet<Vector3Int>();
            for (int i = 0; i < cells.Length; i++)
            {
                if (visited.Add(cells[i]) == false)
                {
                    throw new ArgumentException($"The cell {cells[i]} is used twice.");
                }

                if (i > 0 && DirectionUtility.IsAxisDirection(cells[i] - cells[i - 1]) == false)
                {
                    throw new ArgumentException($"The cells {cells[i - 1]} and {cells[i]} are not neighbors.");
                }
            }

            if (cells.Length > 1 && Head - cells[cells.Length - 2] != Direction)
                throw new ArgumentException("The direction has to follow the last segment of the piece.");
        }
    }
}

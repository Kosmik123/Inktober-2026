using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Puzzle
{
    /// <summary>
    /// The voxel grid with arrow pieces and the relic cell, which no piece can occupy.
    /// A piece can be removed when the line from its head, along its direction, is clear up to the edge of the grid.
    /// The piece's own cells never block it, the relic does.
    /// </summary>
    public class ArrowGrid
    {
        private readonly ArrowPiece[,,] occupancy;
        private readonly List<ArrowPiece> pieces = new List<ArrowPiece>();

        public Vector3Int Size { get; }

        public Vector3Int RelicCell { get; }

        public IReadOnlyList<ArrowPiece> Pieces => pieces;

        /// <summary>Cells that are neither taken by a piece nor the relic.</summary>
        public int EmptyCellCount
        {
            get
            {
                int count = 0;
                for (int x = 0; x < Size.x; x++)
                    for (int y = 0; y < Size.y; y++)
                        for (int z = 0; z < Size.z; z++)
                            if (IsEmpty(new Vector3Int(x, y, z)))
                                count++;

                return count;
            }
        }

        public ArrowGrid(Vector3Int size, Vector3Int relicCell)
        {
            if (size.x < 1 || size.y < 1 || size.z < 1)
                throw new ArgumentException($"The size {size} has to be positive.");

            Size = size;
            RelicCell = relicCell;
            if (IsInside(relicCell) == false)
                throw new ArgumentException($"The relic cell {relicCell} is outside of the grid.");

            occupancy = new ArrowPiece[size.x, size.y, size.z];
        }

        public bool IsInside(Vector3Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.z >= 0
                && cell.x < Size.x && cell.y < Size.y && cell.z < Size.z;
        }

        /// <summary>The piece that takes the cell, or null.</summary>
        public ArrowPiece GetPiece(Vector3Int cell)
        {
            return IsInside(cell) ? occupancy[cell.x, cell.y, cell.z] : null;
        }

        /// <summary>The cell is inside of the grid, is not the relic and no piece takes it.</summary>
        public bool IsEmpty(Vector3Int cell)
        {
            return IsInside(cell) && cell != RelicCell && occupancy[cell.x, cell.y, cell.z] == null;
        }

        public bool CanPlace(ArrowPiece piece)
        {
            foreach (var cell in piece.Cells)
                if (IsEmpty(cell) == false)
                    return false;

            return true;
        }

        /// <summary>Adds the piece when all its cells are empty. The line of its head is not checked.</summary>
        public bool TryAdd(ArrowPiece piece)
        {
            if (CanPlace(piece) == false)
                return false;

            pieces.Add(piece);
            foreach (var cell in piece.Cells)
                occupancy[cell.x, cell.y, cell.z] = piece;

            return true;
        }

        public void Remove(ArrowPiece piece)
        {
            if (pieces.Remove(piece) == false)
                return;

            foreach (var cell in piece.Cells)
                occupancy[cell.x, cell.y, cell.z] = null;
        }

        /// <summary>
        /// Checks the cells from the origin along the direction up to the edge of the grid.
        /// The line is blocked by the relic and by every piece except the ignored one.
        /// </summary>
        public bool IsRayClear(Vector3Int origin, Vector3Int direction, ArrowPiece ignored)
        {
            for (var cell = origin; IsInside(cell); cell += direction)
            {
                if (cell == RelicCell)
                    return false;

                var other = occupancy[cell.x, cell.y, cell.z];
                if (other != null && other != ignored)
                    return false;
            }

            return true;
        }

        public bool CanRemove(ArrowPiece piece)
        {
            return IsRayClear(piece.Head + piece.Direction, piece.Direction, piece);
        }

        /// <summary>Removes pieces one by one, on a copy, as long as any can be removed. True when all of them could be.</summary>
        public bool IsSolvable()
        {
            var copy = Clone();
            bool progress = true;
            while (progress)
            {
                progress = false;
                for (int i = copy.pieces.Count - 1; i >= 0; i--)
                {
                    var piece = copy.pieces[i];
                    if (copy.CanRemove(piece) == false)
                        continue;

                    copy.Remove(piece);
                    progress = true;
                }
            }

            return copy.pieces.Count == 0;
        }

        public ArrowGrid Clone()
        {
            var copy = new ArrowGrid(Size, RelicCell);
            foreach (var piece in pieces)
                copy.TryAdd(piece);

            return copy;
        }
    }
}

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
        /// <summary>Raised after a piece was removed from the grid; the argument is that piece.</summary>
        public event Action<ArrowPiece> OnPieceRemoved;

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

            OnPieceRemoved?.Invoke(piece);
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

        /// <summary>
        /// Finds the first piece a line meets. The line is given in grid space, where the center of a cell is its integer position
        /// and a cell reaches half a unit to every side. The relic is solid: the line stops there.
        /// </summary>
        public bool TryRaycast(Vector3 origin, Vector3 direction, out ArrowPiece piece)
        {
            piece = null;
            if (direction == Vector3.zero || TryGetEntryDistance(origin, direction, out float entry) == false)
                return false;

            var position = origin + direction * entry;
            var cell = new Vector3Int(
                Mathf.Clamp(Mathf.RoundToInt(position.x), 0, Size.x - 1),
                Mathf.Clamp(Mathf.RoundToInt(position.y), 0, Size.y - 1),
                Mathf.Clamp(Mathf.RoundToInt(position.z), 0, Size.z - 1));

            var step = new Vector3Int();
            var nextCrossing = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var crossingInterval = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            for (int axis = 0; axis < 3; axis++)
            {
                if (Mathf.Approximately(direction[axis], 0f))
                    continue;

                step[axis] = direction[axis] > 0f ? 1 : -1;
                nextCrossing[axis] = (cell[axis] + 0.5f * step[axis] - position[axis]) / direction[axis];
                crossingInterval[axis] = Mathf.Abs(1f / direction[axis]);
            }

            while (IsInside(cell))
            {
                if (cell == RelicCell)
                    return false;

                piece = occupancy[cell.x, cell.y, cell.z];
                if (piece != null)
                    return true;

                int next = GetSmallestAxis(nextCrossing);
                cell[next] += step[next];
                nextCrossing[next] += crossingInterval[next];
            }

            return false;
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

        /// <summary>Distance along the line (from 0) at which it enters the box of the whole grid.</summary>
        private bool TryGetEntryDistance(Vector3 origin, Vector3 direction, out float entry)
        {
            float near = 0f;
            float far = float.PositiveInfinity;
            for (int axis = 0; axis < 3; axis++)
            {
                float min = -0.5f;
                float max = Size[axis] - 0.5f;
                if (Mathf.Approximately(direction[axis], 0f))
                {
                    if (origin[axis] < min || origin[axis] > max)
                    {
                        entry = 0f;
                        return false;
                    }

                    continue;
                }

                float a = (min - origin[axis]) / direction[axis];
                float b = (max - origin[axis]) / direction[axis];
                near = Mathf.Max(near, Mathf.Min(a, b));
                far = Mathf.Min(far, Mathf.Max(a, b));
            }

            entry = near;
            return near <= far;
        }

        private static int GetSmallestAxis(Vector3 values)
        {
            if (values.x <= values.y && values.x <= values.z)
                return 0;

            return values.y <= values.z ? 1 : 2;
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

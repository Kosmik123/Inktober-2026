using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Puzzle
{
    /// <summary>
    /// Builds a puzzle that can always be solved. First the volume (without the relic cell) is cut into bent paths.
    /// Then the arrows are "peeled" in a simulated solution: from the paths that have a clear line at their end,
    /// one is picked at random, gets that end as its head, and is taken out. What is taken out later
    /// is therefore never in the way of what was taken out earlier, so the order of picking is a solution.
    /// </summary>
    public static class ArrowPuzzleGenerator
    {
        /// <summary>
        /// Tries to fill the whole volume. After the given number of attempts it takes the one with the fewest holes
        /// (cells with no piece). The same seed gives the same puzzle.
        /// </summary>
        public static ArrowGrid Generate(PuzzleGenerationSettings settings, int seed)
        {
            var random = new System.Random(seed);
            ArrowGrid best = null;
            for (int attempt = 0; attempt < settings.attempts; attempt++)
            {
                var grid = GenerateAttempt(settings, random);
                if (best == null || grid.EmptyCellCount < best.EmptyCellCount)
                {
                    best = grid;
                }

                if (best.EmptyCellCount == 0)
                    break;
            }

            return best;
        }

        private static ArrowGrid GenerateAttempt(PuzzleGenerationSettings settings, System.Random random)
        {
            var paths = BuildPaths(settings, random);
            return Peel(paths, settings, random);
        }

        private static List<List<Vector3Int>> BuildPaths(PuzzleGenerationSettings settings, System.Random random)
        {
            var size = settings.size;
            var relic = settings.RelicCell;
            var assigned = new bool[size.x, size.y, size.z];
            assigned[relic.x, relic.y, relic.z] = true;

            var paths = new List<List<Vector3Int>>();
            foreach (var start in ShuffledCells(size, random))
            {
                if (assigned[start.x, start.y, start.z])
                    continue;

                int length = random.Next(settings.minLength, settings.maxLength + 1);
                paths.Add(GrowPath(start, length, assigned, size, settings.randomness, random));
            }

            return paths;
        }

        private static List<Vector3Int> ShuffledCells(Vector3Int size, System.Random random)
        {
            var cells = new List<Vector3Int>();
            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                    for (int z = 0; z < size.z; z++)
                        cells.Add(new Vector3Int(x, y, z));

            for (int i = cells.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (cells[i], cells[j]) = (cells[j], cells[i]);
            }

            return cells;
        }

        private static List<Vector3Int> GrowPath(Vector3Int start, int length, bool[,,] assigned, Vector3Int size, float randomness, System.Random random)
        {
            var path = new List<Vector3Int> { start };
            assigned[start.x, start.y, start.z] = true;
            while (path.Count < length)
            {
                var next = ChooseNext(path[path.Count - 1], assigned, size, randomness, random);
                if (next.HasValue == false)
                    break;

                path.Add(next.Value);
                assigned[next.Value.x, next.Value.y, next.Value.z] = true;
            }

            return path;
        }

        /// <summary>
        /// Picks a free neighbor. Unless chosen at random, it is the one with the fewest free neighbors of its own,
        /// so cells that would stay alone get used first.
        /// </summary>
        private static Vector3Int? ChooseNext(Vector3Int from, bool[,,] assigned, Vector3Int size, float randomness, System.Random random)
        {
            var candidates = new List<Vector3Int>();
            foreach (var direction in DirectionUtility.All)
                if (IsFree(from + direction, assigned, size))
                    candidates.Add(from + direction);

            if (candidates.Count == 0)
                return null;

            if (random.NextDouble() < randomness)
                return candidates[random.Next(candidates.Count)];

            int fewest = int.MaxValue;
            var best = new List<Vector3Int>();
            foreach (var candidate in candidates)
            {
                int free = CountFreeNeighbors(candidate, assigned, size);
                if (free < fewest)
                {
                    fewest = free;
                    best.Clear();
                }

                if (free == fewest)
                {
                    best.Add(candidate);
                }
            }

            return best[random.Next(best.Count)];
        }

        private static int CountFreeNeighbors(Vector3Int cell, bool[,,] assigned, Vector3Int size)
        {
            int count = 0;
            foreach (var direction in DirectionUtility.All)
                if (IsFree(cell + direction, assigned, size))
                    count++;

            return count;
        }

        private static bool IsFree(Vector3Int cell, bool[,,] assigned, Vector3Int size)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.z >= 0
                && cell.x < size.x && cell.y < size.y && cell.z < size.z
                && assigned[cell.x, cell.y, cell.z] == false;
        }

        /// <summary>
        /// Simulates the solution. The paths that cannot be taken out any more (when nothing has a clear line)
        /// are left out, so their cells become holes.
        /// </summary>
        private static ArrowGrid Peel(List<List<Vector3Int>> paths, PuzzleGenerationSettings settings, System.Random random)
        {
            var relic = settings.RelicCell;
            var work = new ArrowGrid(settings.size, relic);
            var remaining = new List<ArrowPiece>();
            foreach (var path in paths)
            {
                var piece = new ArrowPiece(path, GetEndDirection(path));
                work.TryAdd(piece);
                remaining.Add(piece);
            }

            var result = new ArrowGrid(settings.size, relic);
            while (remaining.Count > 0)
            {
                var candidates = new List<(ArrowPiece source, List<ArrowPiece> options)>();
                foreach (var source in remaining)
                {
                    var options = GetClearOrientations(work, source);
                    if (options.Count > 0)
                    {
                        candidates.Add((source, options));
                    }
                }

                if (candidates.Count == 0)
                    break;

                var chosen = candidates[random.Next(candidates.Count)];
                var oriented = chosen.options[random.Next(chosen.options.Count)];
                work.Remove(chosen.source);
                remaining.Remove(chosen.source);
                result.TryAdd(oriented);
            }

            return result;
        }

        /// <summary>The orientations (which end is the head) whose line is clear. A one-cell path can point in any direction.</summary>
        private static List<ArrowPiece> GetClearOrientations(ArrowGrid work, ArrowPiece source)
        {
            var options = new List<ArrowPiece>();
            var cells = source.Cells;
            if (cells.Count == 1)
            {
                foreach (var direction in DirectionUtility.All)
                    AddIfClear(work, source, cells, direction, options);

                return options;
            }

            AddIfClear(work, source, cells, GetEndDirection(cells), options);

            var reversed = new List<Vector3Int>(cells);
            reversed.Reverse();
            AddIfClear(work, source, reversed, GetEndDirection(reversed), options);
            return options;
        }

        private static void AddIfClear(ArrowGrid work, ArrowPiece source, IReadOnlyList<Vector3Int> cells, Vector3Int direction, List<ArrowPiece> options)
        {
            var piece = new ArrowPiece(cells, direction);
            if (work.IsRayClear(piece.Head + direction, direction, source))
                options.Add(piece);
        }

        /// <summary>The direction of the last segment. A one-cell path gets an arbitrary one.</summary>
        private static Vector3Int GetEndDirection(IReadOnlyList<Vector3Int> cells)
        {
            int last = cells.Count - 1;
            return last == 0 ? Vector3Int.right : cells[last] - cells[last - 1];
        }
    }
}

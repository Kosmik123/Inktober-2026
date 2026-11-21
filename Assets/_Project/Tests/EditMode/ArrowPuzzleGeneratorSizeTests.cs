using System.Diagnostics;
using Inktober2026.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class ArrowPuzzleGeneratorSizeTests
    {
        private const int SeedCount = 10;

        private static readonly Vector3Int[] Sizes =
        {
            new Vector3Int(1, 1, 1),
            new Vector3Int(2, 2, 2),
            new Vector3Int(3, 3, 3),
            new Vector3Int(4, 4, 4),
            new Vector3Int(6, 6, 6),
            new Vector3Int(7, 7, 7),
            new Vector3Int(1, 5, 5),
            new Vector3Int(2, 8, 3),
            new Vector3Int(3, 5, 4),
            new Vector3Int(4, 6, 5),
            new Vector3Int(9, 3, 6),
        };

        private static PuzzleGenerationSettings SettingsFor(Vector3Int size)
        {
            return new PuzzleGenerationSettings(size, 3, 7, 0.3f, 40);
        }

        [TestCaseSource(nameof(Sizes))]
        public void PuzzleIsSolvableAndTheRelicStaysFree(Vector3Int size)
        {
            var settings = SettingsFor(size);
            for (int seed = 0; seed < SeedCount; seed++)
            {
                var grid = ArrowPuzzleGenerator.Generate(settings, seed);

                Assert.AreEqual(size, grid.Size, $"seed {seed}");
                Assert.IsTrue(grid.IsSolvable(), $"seed {seed}");
                Assert.IsNull(grid.GetPiece(settings.RelicCell), $"seed {seed}");
            }
        }

        [TestCaseSource(nameof(Sizes))]
        public void EveryCellIsAPieceTheRelicOrAHole(Vector3Int size)
        {
            var settings = SettingsFor(size);
            int total = size.x * size.y * size.z;
            for (int seed = 0; seed < SeedCount; seed++)
            {
                var grid = ArrowPuzzleGenerator.Generate(settings, seed);
                int pieceCells = 0;
                foreach (var piece in grid.Pieces)
                {
                    pieceCells += piece.Cells.Count;
                    Assert.LessOrEqual(piece.Cells.Count, settings.maxLength, $"seed {seed}");
                }

                Assert.AreEqual(total, pieceCells + grid.EmptyCellCount + 1, $"seed {seed}");
            }
        }

        [TestCaseSource(nameof(Sizes))]
        public void WholeVolumeIsFilled(Vector3Int size)
        {
            var settings = SettingsFor(size);
            for (int seed = 0; seed < SeedCount; seed++)
                Assert.AreEqual(0, ArrowPuzzleGenerator.Generate(settings, seed).EmptyCellCount, $"seed {seed}");
        }

        [Test]
        public void DefaultSizeIsGeneratedQuickly()
        {
            // A loose limit: a generation takes a few dozen milliseconds, rarely a few hundred.
            for (int seed = 0; seed < SeedCount; seed++)
            {
                var stopwatch = Stopwatch.StartNew();
                ArrowPuzzleGenerator.Generate(PuzzleGenerationSettings.Default, seed);
                stopwatch.Stop();

                Assert.Less(stopwatch.ElapsedMilliseconds, 2000, $"seed {seed}");
            }
        }
    }
}

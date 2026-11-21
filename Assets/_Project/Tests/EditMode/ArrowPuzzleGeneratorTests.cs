using Inktober2026.Puzzle;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class ArrowPuzzleGeneratorTests
    {
        private const int SeedCount = 100;

        [Test]
        public void EveryPuzzleCanBeSolved()
        {
            for (int seed = 0; seed < SeedCount; seed++)
                Assert.IsTrue(ArrowPuzzleGenerator.Generate(PuzzleGenerationSettings.Default, seed).IsSolvable(), $"seed {seed}");
        }

        [Test]
        public void RelicCellStaysFree()
        {
            var settings = PuzzleGenerationSettings.Default;
            for (int seed = 0; seed < SeedCount; seed++)
            {
                var grid = ArrowPuzzleGenerator.Generate(settings, seed);
                Assert.IsNull(grid.GetPiece(settings.RelicCell), $"seed {seed}");
            }
        }

        [Test]
        public void EveryCellIsAPieceTheRelicOrAHole()
        {
            var settings = PuzzleGenerationSettings.Default;
            int total = settings.size.x * settings.size.y * settings.size.z;
            for (int seed = 0; seed < SeedCount; seed++)
            {
                var grid = ArrowPuzzleGenerator.Generate(settings, seed);
                int pieceCells = 0;
                foreach (var piece in grid.Pieces)
                    pieceCells += piece.Cells.Count;

                Assert.AreEqual(total, pieceCells + grid.EmptyCellCount + 1, $"seed {seed}");
            }
        }

        [Test]
        public void SameSeedGivesTheSamePuzzle()
        {
            var settings = PuzzleGenerationSettings.Default;
            var first = ArrowPuzzleGenerator.Generate(settings, 7);
            var second = ArrowPuzzleGenerator.Generate(settings, 7);

            Assert.AreEqual(first.Pieces.Count, second.Pieces.Count);
            for (int i = 0; i < first.Pieces.Count; i++)
            {
                CollectionAssert.AreEqual(first.Pieces[i].Cells, second.Pieces[i].Cells);
                Assert.AreEqual(first.Pieces[i].Direction, second.Pieces[i].Direction);
            }
        }

        [Test]
        public void DifferentSeedsGiveDifferentPuzzles()
        {
            var settings = PuzzleGenerationSettings.Default;
            Assert.AreNotEqual(Describe(ArrowPuzzleGenerator.Generate(settings, 1)), Describe(ArrowPuzzleGenerator.Generate(settings, 2)));
        }

        private static string Describe(ArrowGrid grid)
        {
            var builder = new System.Text.StringBuilder();
            foreach (var piece in grid.Pieces)
                builder.Append(piece.Head).Append(piece.Direction).Append(piece.Cells.Count).Append(';');

            return builder.ToString();
        }

        [Test]
        public void PieceLengthsStayWithinTheSettings()
        {
            var settings = PuzzleGenerationSettings.Default;
            for (int seed = 0; seed < SeedCount; seed++)
            {
                var grid = ArrowPuzzleGenerator.Generate(settings, seed);
                foreach (var piece in grid.Pieces)
                {
                    Assert.LessOrEqual(piece.Cells.Count, settings.maxLength, $"seed {seed}");
                }
            }
        }

        [Test]
        public void DefaultSettingsFillTheWholeVolume()
        {
            for (int seed = 0; seed < SeedCount; seed++)
                Assert.AreEqual(0, ArrowPuzzleGenerator.Generate(PuzzleGenerationSettings.Default, seed).EmptyCellCount, $"seed {seed}");
        }
    }
}

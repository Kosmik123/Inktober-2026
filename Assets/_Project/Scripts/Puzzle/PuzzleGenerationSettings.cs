using UnityEngine;

namespace Inktober2026.Puzzle
{
    public readonly struct PuzzleGenerationSettings
    {
        /// <summary>Number of cells along each axis.</summary>
        public readonly Vector3Int size;

        /// <summary>Wanted number of cells of a piece (a piece can be shorter when it has no room to grow).</summary>
        public readonly int minLength;
        public readonly int maxLength;

        /// <summary>0 grows pieces so they leave no lonely cells, 1 grows them completely at random.</summary>
        public readonly float randomness;

        /// <summary>How many times the generator tries to fill the whole grid before it accepts holes.</summary>
        public readonly int attempts;

        /// <summary>The relic stays in the middle of the grid.</summary>
        public Vector3Int RelicCell => new Vector3Int(size.x / 2, size.y / 2, size.z / 2);

        public static PuzzleGenerationSettings Default => new PuzzleGenerationSettings(new Vector3Int(5, 5, 5), 3, 7, 0.3f, 40);

        public PuzzleGenerationSettings(Vector3Int size, int minLength, int maxLength, float randomness, int attempts)
        {
            this.size = new Vector3Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y), Mathf.Max(1, size.z));
            this.minLength = Mathf.Max(1, minLength);
            this.maxLength = Mathf.Max(this.minLength, maxLength);
            this.randomness = Mathf.Clamp01(randomness);
            this.attempts = Mathf.Max(1, attempts);
        }
    }
}

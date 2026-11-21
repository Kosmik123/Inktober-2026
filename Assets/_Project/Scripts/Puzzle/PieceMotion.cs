using UnityEngine;

namespace Inktober2026.Puzzle
{
    /// <summary>
    /// Moves the stones of a piece along its <see cref="PiecePath"/>. The stones only slide, one behind another, and never turn.
    /// Stone number i stands at the index i of the path when the piece rests, and the head is the last one.
    /// A slide takes the piece out of the grid; a bump pushes it forward a little and back, as a piece that is blocked.
    /// </summary>
    public sealed class PieceMotion
    {
        private readonly PiecePath path;
        private readonly Transform[] stones;
        private readonly float offsetSpeed;
        private readonly float bumpDistance;
        private readonly float bumpDuration;
        private readonly bool isBump;
        private float time;

        private PieceMotion(PiecePath path, Transform[] stones, bool isBump, float offsetSpeed, float bumpDistance, float bumpDuration)
        {
            this.path = path;
            this.stones = stones;
            this.isBump = isBump;
            this.offsetSpeed = offsetSpeed;
            this.bumpDistance = bumpDistance;
            this.bumpDuration = bumpDuration;
        }

        public bool IsFinished { get; private set; }

        /// <param name="cellsPerSecond">How fast the stones travel along the path.</param>
        public static PieceMotion CreateSlide(PiecePath path, Transform[] stones, float cellsPerSecond)
        {
            return new PieceMotion(path, stones, false, cellsPerSecond, 0f, 0f);
        }

        /// <param name="distance">The farthest the stones get from their cells, in cells.</param>
        /// <param name="duration">Seconds from the start to the stones being back in their cells.</param>
        public static PieceMotion CreateBump(PiecePath path, Transform[] stones, float distance, float duration)
        {
            return new PieceMotion(path, stones, true, 0f, distance, duration);
        }

        /// <summary>Moves time forward and puts the stones where they belong. Stones that left the grid are switched off.</summary>
        public void Advance(float deltaTime)
        {
            time += deltaTime;
            float offset;
            if (isBump)
            {
                float progress = Mathf.Clamp01(time / bumpDuration);
                offset = bumpDistance * Mathf.Sin(progress * Mathf.PI);
                IsFinished = progress >= 1f;
            }
            else
            {
                offset = time * offsetSpeed;
                IsFinished = offset >= path.ExitIndex;
            }

            Apply(offset);
        }

        private void Apply(float offset)
        {
            for (int i = 0; i < stones.Length; i++)
            {
                float index = i + offset;
                bool isOut = index >= path.ExitIndex;
                if (stones[i].gameObject.activeSelf == isOut)
                    stones[i].gameObject.SetActive(isOut == false);

                stones[i].localPosition = path.Evaluate(index);
            }
        }
    }
}

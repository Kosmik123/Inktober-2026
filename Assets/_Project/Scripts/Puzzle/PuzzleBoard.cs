using System;
using Inktober2026.Game;
using UnityEngine;

namespace Inktober2026.Puzzle
{
    /// <summary>
    /// The component that holds the <see cref="ArrowGrid"/> of the round. The grid stays a plain C# class,
    /// this component creates a new one every time the round is set up (the Ready state of <see cref="GameFlow"/>),
    /// forwards its changes as events and tells <see cref="GameFlow"/> when the player starts and when the grid is empty.
    /// </summary>
    public class PuzzleBoard : MonoBehaviour
    {
        /// <summary>Raised after a new grid was created; the argument is that grid.</summary>
        public event Action<ArrowGrid> OnGridCreated;

        /// <summary>Raised after a piece was removed from the current grid; the argument is that piece.</summary>
        public event Action<ArrowPiece> OnPieceRemoved;

        [SerializeField]
        private GameFlow gameFlow;

        [Header("Generation")]
        [SerializeField]
        private Vector3Int size = new Vector3Int(5, 5, 5);

        [SerializeField]
        private int minLength = 3;

        [SerializeField]
        private int maxLength = 7;

        [Range(0f, 1f)]
        [SerializeField]
        private float randomness = 0.3f;

        [Tooltip("How many times the generator tries to fill the whole grid before it accepts holes.")]
        [SerializeField]
        private int attempts = 40;

        [Tooltip("Generate the same puzzle every round, with the seed below.")]
        [SerializeField]
        private bool useFixedSeed;

        [SerializeField]
        private int fixedSeed;

        [Header("States")]
        [SerializeField]
        private int seed;

        public ArrowGrid Grid { get; private set; }

        private void Reset()
        {
            gameFlow = FindFirstObjectByType<GameFlow>();
        }

        private void OnEnable()
        {
            gameFlow.OnStateChanged += GameFlow_OnStateChanged;
        }

        /// <summary>
        /// Removes the piece when its line is clear. The first removal starts the round.
        /// Nothing is removed once the round is won. Returns whether the piece was removed.
        /// </summary>
        public bool TryRemove(ArrowPiece piece)
        {
            if (Grid == null || gameFlow.State == GameState.Won || Grid.CanRemove(piece) == false)
                return false;

            gameFlow.BeginRound();
            Grid.Remove(piece);
            return true;
        }

        private void CreateGrid()
        {
            if (Grid != null)
                Grid.OnPieceRemoved -= Grid_OnPieceRemoved;

            seed = useFixedSeed ? fixedSeed : UnityEngine.Random.Range(0, int.MaxValue);
            var settings = new PuzzleGenerationSettings(size, minLength, maxLength, randomness, attempts);
            Grid = ArrowPuzzleGenerator.Generate(settings, seed);
            Grid.OnPieceRemoved += Grid_OnPieceRemoved;
            OnGridCreated?.Invoke(Grid);
        }

        private void GameFlow_OnStateChanged(GameState state)
        {
            if (state != GameState.Ready)
                return;

            CreateGrid();
        }

        private void Grid_OnPieceRemoved(ArrowPiece piece)
        {
            OnPieceRemoved?.Invoke(piece);
            if (Grid.Pieces.Count == 0)
                gameFlow.Win();
        }

        private void OnDisable()
        {
            gameFlow.OnStateChanged -= GameFlow_OnStateChanged;
        }
    }
}

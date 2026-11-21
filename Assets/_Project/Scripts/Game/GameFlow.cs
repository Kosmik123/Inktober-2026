using System;
using UnityEngine;

namespace Inktober2026.Game
{
    /// <summary>
    /// Keeps the state of the round: ready, playing, won. It knows nothing about the game itself.
    /// Other components change the state through <see cref="BeginRound"/>, <see cref="Win"/> and <see cref="Restart"/>,
    /// and react to <see cref="OnStateChanged"/>. The <see cref="GameState.Ready"/> state means a new round has to be set up.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        /// <summary>Raised after the state changed; the argument is the new state.</summary>
        public event Action<GameState> OnStateChanged;

        public GameState State { get; private set; } = GameState.Ready;

        private void Start()
        {
            // Listeners subscribe in OnEnable, so they all get the first Ready state.
            Restart();
        }

        /// <summary>Moves from Ready to Playing, on the player's first move.</summary>
        public void BeginRound()
        {
            if (State != GameState.Ready)
                return;

            SetState(GameState.Playing);
        }

        /// <summary>Ends the round that is being played as solved.</summary>
        public void Win()
        {
            if (State != GameState.Playing)
                return;

            SetState(GameState.Won);
        }

        /// <summary>Starts a new round, from any state. Always raises the event, so listeners set the round up again.</summary>
        public void Restart()
        {
            SetState(GameState.Ready);
        }

        private void SetState(GameState state)
        {
            State = state;
            OnStateChanged?.Invoke(state);
        }
    }
}

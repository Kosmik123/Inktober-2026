namespace Inktober2026.Game
{
    public enum GameState
    {
        /// <summary>A new round is set up and waits for the player's first move.</summary>
        Ready,
        Playing,
        /// <summary>The round is solved. A new round starts after <see cref="GameFlow.Restart"/>.</summary>
        Won,
    }
}

using System;
using Inktober.Apple.Knife;
using Inktober.Apple.Peel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Inktober.Game
{
    public enum GameState
    {
        /// <summary>Waiting for the player. The knife stands still.</summary>
        Ready,
        Playing,
        Paused,
        /// <summary>The peel crossed itself. Restarting is possible after a short delay.</summary>
        GameOver,
    }

    /// <summary>
    /// Runs the round: waits for the player, lets the knife move while playing, ends the round
    /// when the peel crosses itself and restarts it by reloading the scene.
    /// The knife is only enabled while playing. The cursor is locked and hidden then.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        /// <summary>Raised after the state changed; the argument is the new state.</summary>
        public event Action<GameState> OnStateChanged;

        [SerializeField]
        private KnifeController knife;

        [SerializeField]
        private PeelSelfCollision selfCollision;

        [Tooltip("Starts the round, resumes after a pause and restarts after game over (for example UI/Click).")]
        [SerializeField]
        private InputActionReference confirmAction;

        [Tooltip("Pauses and resumes the round (for example UI/Cancel).")]
        [SerializeField]
        private InputActionReference pauseAction;

        [Tooltip("Time after game over during which confirming does not restart, so the player does not skip the result by accident.")]
        [SerializeField]
        private float restartDelay = 0.7f;

        [Header("States")]
        [SerializeField]
        private GameState state;
        public GameState State => state;

        private float gameOverTime;

        private void Reset()
        {
            knife = FindFirstObjectByType<KnifeController>();
            selfCollision = FindFirstObjectByType<PeelSelfCollision>();
        }

        private void OnEnable()
        {
            selfCollision.OnSelfIntersection += PeelSelfCollision_OnSelfIntersection;
            confirmAction.action.performed += ConfirmAction_Performed;
            pauseAction.action.performed += PauseAction_Performed;
        }

        private void Start()
        {
            SetState(GameState.Ready);
        }

        private void SetState(GameState newState)
        {
            state = newState;
            knife.enabled = newState == GameState.Playing;
            ApplyCursor(newState == GameState.Playing);

            if (newState == GameState.GameOver)
                gameOverTime = Time.unscaledTime;

            OnStateChanged?.Invoke(newState);
        }

        private void ApplyCursor(bool playing)
        {
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = playing == false;
        }

        private void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void PeelSelfCollision_OnSelfIntersection(Vector3 crossingPoint)
        {
            if (state == GameState.Playing)
                SetState(GameState.GameOver);
        }

        private void ConfirmAction_Performed(InputAction.CallbackContext context)
        {
            if (state == GameState.Ready || state == GameState.Paused)
            {
                SetState(GameState.Playing);
                return;
            }

            if (state == GameState.GameOver && Time.unscaledTime >= gameOverTime + restartDelay)
                Restart();
        }

        private void PauseAction_Performed(InputAction.CallbackContext context)
        {
            if (state == GameState.Playing)
            {
                SetState(GameState.Paused);
                return;
            }

            if (state == GameState.Paused)
                SetState(GameState.Playing);
        }

        private void OnDisable()
        {
            selfCollision.OnSelfIntersection -= PeelSelfCollision_OnSelfIntersection;
            confirmAction.action.performed -= ConfirmAction_Performed;
            pauseAction.action.performed -= PauseAction_Performed;
        }
    }
}

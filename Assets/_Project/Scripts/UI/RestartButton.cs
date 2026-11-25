using Inktober2026.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Inktober2026.UI
{
    /// <summary>
    /// A button that restarts the round from the pause. It is shown only while the game is paused, next to the resume button
    /// of <see cref="GameFlowButton"/>. After game over the same button of <see cref="GameFlowButton"/> restarts.
    /// Put this component on another object than the button, because the button object is hidden when not shown.
    /// </summary>
    public class RestartButton : MonoBehaviour
    {
        [SerializeField]
        private GameFlow gameFlow;

        [SerializeField]
        private Button button;

        private void Reset()
        {
            gameFlow = FindAnyObjectByType<GameFlow>();
            button = GetComponentInChildren<Button>(true);
        }

        private void OnEnable()
        {
            gameFlow.OnStateChanged += Refresh;
            button.onClick.AddListener(gameFlow.Restart);
            Refresh(gameFlow.State);
        }

        private void Refresh(GameState state)
        {
            button.gameObject.SetActive(state == GameState.Paused);
        }

        private void OnDisable()
        {
            gameFlow.OnStateChanged -= Refresh;
            button.onClick.RemoveListener(gameFlow.Restart);
        }
    }
}

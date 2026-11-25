using Bipolar.TouchSticks;
using Inktober2026.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Inktober2026.UI
{
    /// <summary>
    /// A button that pauses the round on touch devices. It is shown only while the round is being played
    /// and only when touch controls are active (the same condition as for the touch sticks),
    /// because the mouse cursor is hidden during play and could not click it. Resuming is done by <see cref="GameFlowButton"/>.
    /// Put this component on another object than the button, because the button object is hidden when not shown.
    /// </summary>
    public class PauseButton : MonoBehaviour
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
            button.onClick.AddListener(gameFlow.TogglePause);
            Refresh(gameFlow.State);
        }

        private void Refresh(GameState state)
        {
            button.gameObject.SetActive(state == GameState.Playing && TouchPointers.Enabled);
        }

        private void OnDisable()
        {
            gameFlow.OnStateChanged -= Refresh;
            button.onClick.RemoveListener(gameFlow.TogglePause);
        }
    }
}

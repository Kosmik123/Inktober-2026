using Bipolar.TouchSticks;
using Inktober2026.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Inktober2026.UI
{
    /// <summary>
    /// A button that confirms in <see cref="GameFlow"/> (start, resume, restart), for devices without a keyboard.
    /// It is shown whenever the round is not being played, and its label follows the state.
    /// With touch controls it is not shown before the first round: holding the stick starts it (<see cref="TouchStartTrigger"/>).
    /// Put this component on another object than the button, because the button object is hidden while playing.
    /// </summary>
    public class GameFlowButton : MonoBehaviour
    {
        [SerializeField]
        private GameFlow gameFlow;

        [SerializeField]
        private Button button;

        [SerializeField]
        private TMP_Text label;

        [Header("Labels")]
        [SerializeField]
        private string readyText = "Start";

        [SerializeField]
        private string pausedText = "Resume";

        [SerializeField]
        private string gameOverText = "Restart";

        private void Reset()
        {
            gameFlow = FindAnyObjectByType<GameFlow>();
            button = GetComponentInChildren<Button>(true);
            label = GetComponentInChildren<TMP_Text>(true);
        }

        private void OnEnable()
        {
            gameFlow.OnStateChanged += Refresh;
            button.onClick.AddListener(gameFlow.Confirm);
            Refresh(gameFlow.State);
        }

        private void Refresh(GameState state)
        {
            // On touch devices the round is started by holding the stick (TouchStartTrigger), so there is no Start button.
            bool startsByTouch = state == GameState.Ready && TouchPointers.Enabled;
            button.gameObject.SetActive(state != GameState.Playing && startsByTouch == false);
            label.text = state switch
            {
                GameState.Paused => pausedText,
                GameState.GameOver => gameOverText,
                _ => readyText,
            };
        }

        private void OnDisable()
        {
            gameFlow.OnStateChanged -= Refresh;
            button.onClick.RemoveListener(gameFlow.Confirm);
        }
    }
}

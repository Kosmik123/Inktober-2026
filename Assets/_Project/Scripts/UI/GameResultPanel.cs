using System.Text;
using Inktober2026.Apple.Peel;
using Inktober2026.Game;
using TMPro;
using UnityEngine;

namespace Inktober2026.UI
{
    /// <summary>
    /// Shows the result of the round after game over: peel length, how much of the apple is peeled and the best length.
    /// Saves a new record. Put this component on another object than the panel, because the panel object is hidden outside of game over.
    /// </summary>
    public class GameResultPanel : MonoBehaviour
    {
        [SerializeField]
        private GameFlow gameFlow;

        [SerializeField]
        private PeelTrail trail;

        [SerializeField]
        private PeelCoverage coverage;

        [Tooltip("The object that is shown at game over.")]
        [SerializeField]
        private GameObject panel;

        [SerializeField]
        private TMP_Text label;

        [Tooltip("World units are multiplied by this before showing (100 turns units into centimeters).")]
        [SerializeField]
        private float displayScale = 100f;

        [Header("Texts")]
        [SerializeField]
        private string titleText = "Game Over";

        [Tooltip("{0} is the length.")]
        [SerializeField]
        private string lengthFormat = "Length: {0:0} cm";

        [Tooltip("{0} is the percentage of the apple that is peeled.")]
        [SerializeField]
        private string coverageFormat = "Peeled: {0:0}%";

        [Tooltip("{0} is the best length.")]
        [SerializeField]
        private string bestFormat = "Best: {0:0} cm";

        [SerializeField]
        private string recordText = "New record!";

        private void Reset()
        {
            gameFlow = FindAnyObjectByType<GameFlow>();
            trail = FindAnyObjectByType<PeelTrail>();
            coverage = FindAnyObjectByType<PeelCoverage>();
            label = GetComponentInChildren<TMP_Text>(true);
            panel = label ? label.gameObject : null;
        }

        private void OnEnable()
        {
            gameFlow.OnStateChanged += GameFlow_OnStateChanged;
            panel.SetActive(gameFlow.State == GameState.GameOver);
        }

        private void Show(bool show)
        {
            panel.SetActive(show);
            if (show == false)
                return;

            coverage.Refresh();
            float previousBest = BestScore.Length;
            bool isRecord = BestScore.TrySet(trail.Length);

            var text = new StringBuilder();
            text.AppendLine($"<size=130%>{titleText}</size>");
            text.AppendLine(string.Format(lengthFormat, trail.Length * displayScale));
            text.AppendLine(string.Format(coverageFormat, coverage.Coverage * 100f));
            text.Append(string.Format(bestFormat, previousBest * displayScale));
            if (isRecord)
                text.Append("\n<color=#FFD84A>").Append(recordText).Append("</color>");

            label.text = text.ToString();
            panel.SetActive(true);
        }

        private void GameFlow_OnStateChanged(GameState state)
        {
            Show(state == GameState.GameOver);
        }

        private void OnDisable()
        {
            gameFlow.OnStateChanged -= GameFlow_OnStateChanged;
        }
    }
}

using Inktober2026.Game;
using Inktober2026.Puzzle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Inktober2026.UI
{
    /// <summary>
    /// The buttons and messages of the round, for testing. Three fields hold the size of the grid; the restart button
    /// reads them, gives the size to the <see cref="PuzzleBoard"/> and starts a new round. A field that is empty
    /// or is not a number keeps the current size of its axis, and the fields show the size that was really used.
    /// The won panel is shown while the round is in the <see cref="GameState.Won"/> state.
    /// </summary>
    public class RoundHud : MonoBehaviour
    {
        [SerializeField]
        private GameFlow gameFlow;

        [SerializeField]
        private PuzzleBoard board;

        [SerializeField]
        private Button restartButton;

        [Header("Size fields")]
        [SerializeField]
        private TMP_InputField sizeXField;

        [SerializeField]
        private TMP_InputField sizeYField;

        [SerializeField]
        private TMP_InputField sizeZField;

        [Tooltip("Shown only when the round is won.")]
        [SerializeField]
        private GameObject wonPanel;

        private void Reset()
        {
            gameFlow = FindFirstObjectByType<GameFlow>();
            board = FindFirstObjectByType<PuzzleBoard>();
        }

        private void OnEnable()
        {
            gameFlow.OnStateChanged += GameFlow_OnStateChanged;
            restartButton.onClick.AddListener(RestartButton_OnClick);
            wonPanel.SetActive(gameFlow.State == GameState.Won);
            ShowSize(board.Size);
        }

        private void ShowSize(Vector3Int size)
        {
            sizeXField.text = size.x.ToString();
            sizeYField.text = size.y.ToString();
            sizeZField.text = size.z.ToString();
        }

        private Vector3Int ReadSize()
        {
            var current = board.Size;
            return new Vector3Int(
                ReadAxis(sizeXField, current.x),
                ReadAxis(sizeYField, current.y),
                ReadAxis(sizeZField, current.z));
        }

        private static int ReadAxis(TMP_InputField field, int fallback)
        {
            return int.TryParse(field.text, out int value) ? value : fallback;
        }

        private void GameFlow_OnStateChanged(GameState state)
        {
            wonPanel.SetActive(state == GameState.Won);
        }

        private void RestartButton_OnClick()
        {
            board.SetSize(ReadSize());
            ShowSize(board.Size);
            gameFlow.Restart();
        }

        private void OnDisable()
        {
            gameFlow.OnStateChanged -= GameFlow_OnStateChanged;
            restartButton.onClick.RemoveListener(RestartButton_OnClick);
        }
    }
}

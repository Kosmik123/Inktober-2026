using Inktober2026.Cameras;
using Inktober2026.Puzzle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Inktober2026.Controls
{
    /// <summary>
    /// The only handler of the player's input in the puzzle. A quick press and release on a piece (a tap, or a left click)
    /// removes it. Moving the pointer while the orbit action is held (the right mouse button, or a finger on the screen)
    /// turns the camera. A touch that lasts longer than <see cref="maxTapDuration"/>, or that moves, is not a tap.
    /// </summary>
    public class PuzzleInput : MonoBehaviour
    {
        [SerializeField]
        private PuzzleBoard board;

        [SerializeField]
        private PuzzleView view;

        [SerializeField]
        private OrbitCamera orbitCamera;

        [SerializeField]
        private Camera worldCamera;

        [Header("Actions")]
        [SerializeField]
        private InputActionReference selectAction;

        [SerializeField]
        private InputActionReference orbitAction;

        [SerializeField]
        private InputActionReference positionAction;

        [SerializeField]
        private InputActionReference deltaAction;

        [Header("Tap")]
        [Tooltip("A press that lasts longer than this many seconds is not a tap.")]
        [SerializeField]
        private float maxTapDuration = 0.5f;

        [Tooltip("A pointer that moved more than this many pixels since the press is dragging, and it is not a tap.")]
        [SerializeField]
        private float dragThreshold = 12f;

        [Header("States")]
        [SerializeField]
        private bool isSelecting;

        [SerializeField]
        private bool isDragging;

        [Tooltip("The current press began on the interface, so the puzzle ignores it.")]
        [SerializeField]
        private bool isStartedOverUi;

        private float selectStartTime;
        private Vector2 selectStartPosition;

        private void Reset()
        {
            board = FindFirstObjectByType<PuzzleBoard>();
            view = FindFirstObjectByType<PuzzleView>();
            orbitCamera = FindFirstObjectByType<OrbitCamera>();
            worldCamera = Camera.main;
        }

        private void OnEnable()
        {
            selectAction.action.started += SelectAction_Started;
            selectAction.action.canceled += SelectAction_Canceled;
        }

        private void Update()
        {
            if (isSelecting && isDragging == false)
                isDragging = (positionAction.action.ReadValue<Vector2>() - selectStartPosition).magnitude > dragThreshold;

            if (orbitAction.action.IsPressed() == false || isStartedOverUi)
                return;

            // A finger presses select and orbit together, so it turns the view only once it is dragging.
            if (isSelecting && isDragging == false)
                return;

            orbitCamera.Rotate(deltaAction.action.ReadValue<Vector2>());
        }

        private void SelectAction_Started(InputAction.CallbackContext context)
        {
            // A press on a button of the interface belongs to the button, not to the puzzle.
            isStartedOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (isStartedOverUi)
                return;

            isSelecting = true;
            isDragging = false;
            selectStartTime = Time.unscaledTime;
            selectStartPosition = positionAction.action.ReadValue<Vector2>();
        }

        private void SelectAction_Canceled(InputAction.CallbackContext context)
        {
            if (isStartedOverUi)
            {
                isStartedOverUi = false;
                return;
            }

            bool isTap = isSelecting && isDragging == false && Time.unscaledTime - selectStartTime <= maxTapDuration;
            isSelecting = false;
            isDragging = false;
            if (isTap)
                Tap(positionAction.action.ReadValue<Vector2>());
        }

        private void Tap(Vector2 screenPosition)
        {
            if (board.Grid == null)
                return;

            var ray = worldCamera.ScreenPointToRay(screenPosition);
            if (view.TryPick(ray, board.Grid, out var piece))
                board.TryRemove(piece);
        }

        private void OnDisable()
        {
            selectAction.action.started -= SelectAction_Started;
            selectAction.action.canceled -= SelectAction_Canceled;
            isSelecting = false;
            isDragging = false;
            isStartedOverUi = false;
        }
    }
}

using Bipolar.TouchSticks;
using UnityEngine;

namespace Inktober.Game
{
    /// <summary>
    /// Starts the round on touch devices when the player puts a finger on the steering stick and holds it for a moment,
    /// so there is no Start button and the finger that starts the round is already steering the knife.
    /// It only starts the round from the Ready state (resuming and restarting use the button).
    /// </summary>
    public class TouchStartTrigger : MonoBehaviour
    {
        [SerializeField]
        private GameFlow gameFlow;

        [Tooltip("The steering stick. The round starts when a finger holds it, so the same finger steers right away.")]
        [SerializeField]
        private VirtualJoystick stick;

        [Tooltip("How long the finger has to stay down before the round starts, in seconds. 0 starts at the first touch.")]
        [SerializeField]
        private float holdTime = 0.25f;

        [Header("States")]
        [SerializeField]
        private float heldTime;

        private void Reset()
        {
            gameFlow = FindAnyObjectByType<GameFlow>();
            stick = FindAnyObjectByType<VirtualJoystick>();
        }

        private void Awake()
        {
            if (gameFlow == null || stick == null)
            {
                Debug.LogError($"{nameof(TouchStartTrigger)} needs the game flow and the stick assigned.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (TouchPointers.Enabled == false || gameFlow.State != GameState.Ready || stick.IsPressed == false)
            {
                heldTime = 0f;
                return;
            }

            heldTime += Time.unscaledDeltaTime;
            if (heldTime >= holdTime)
                gameFlow.Confirm();
        }
    }
}

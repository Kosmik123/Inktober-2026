using Bipolar.TouchSticks;
using UnityEngine;

namespace Inktober.Game
{
    /// <summary>
    /// Starts the round on touch devices when the player moves the steering stick out of its dead zone,
    /// so there is no Start button and the finger that starts the round is already steering the knife.
    /// It only starts the round from the Ready state (resuming and restarting use the button).
    /// </summary>
    public class TouchStartTrigger : MonoBehaviour
    {
        [SerializeField]
        private GameFlow gameFlow;

        [Tooltip("The steering stick. The round starts when its value leaves the dead zone (a finger resting on it is not enough).")]
        [SerializeField]
        private VirtualJoystick stick;

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
            if (TouchPointers.Enabled == false || gameFlow.State != GameState.Ready)
                return;

            // The value of the stick is zero inside the dead zone.
            if (stick.IsPressed && stick.Value != Vector2.zero)
                gameFlow.Confirm();
        }
    }
}

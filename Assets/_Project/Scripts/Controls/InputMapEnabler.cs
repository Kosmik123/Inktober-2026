using UnityEngine;
using UnityEngine.InputSystem;

namespace Inktober2026.Controls
{
    /// <summary>
    /// The owner of an action map: it is enabled while this component is. Components that use the actions
    /// of the map only subscribe to them and never enable or disable them.
    /// </summary>
    public class InputMapEnabler : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset actions;

        [SerializeField]
        private string mapName = "Puzzle";

        private void OnEnable()
        {
            actions.FindActionMap(mapName, true).Enable();
        }

        private void OnDisable()
        {
            actions.FindActionMap(mapName, true).Disable();
        }
    }
}

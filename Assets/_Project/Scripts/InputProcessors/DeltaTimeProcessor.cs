using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Inktober2026.InputProcessors
{
    /// <summary>
    /// Multiplies a Vector2 by <see cref="Time.deltaTime"/>, turning a rate (units per second, for example a stick)
    /// into an amount for this frame. Add it to a binding as "DeltaTime", after any scale processor.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class DeltaTimeProcessor : InputProcessor<Vector2>
    {
        static DeltaTimeProcessor()
        {
            Register();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            InputSystem.RegisterProcessor<DeltaTimeProcessor>();
        }

        public override Vector2 Process(Vector2 value, InputControl control)
        {
            return value * Time.deltaTime;
        }
    }
}

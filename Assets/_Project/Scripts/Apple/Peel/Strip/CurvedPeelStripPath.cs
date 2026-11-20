using UnityEngine;

namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// Lays the strip out flat in the shape of <see cref="PeelRestCurve"/>: bent left and right the way the knife turned,
    /// the skin facing up. Only the turns count, the shape of the apple does not.
    /// </summary>
    public class CurvedPeelStripPath : MonoBehaviour, IPeelStripPath
    {
        [Tooltip("How much of the turning of the knife the strip follows. 1 follows it exactly, more exaggerates it.")]
        [SerializeField]
        private float turnScale = 1f;

        [Tooltip("How far the strip rises per unit of its length, in world units.")]
        [SerializeField]
        private float climb = 0.02f;

        private readonly PeelRestCurve curve = new PeelRestCurve();

        public bool IsDynamic => false;

        public int NodeCount => curve.Count;

        public void Append(PeelSection section)
        {
            curve.TurnScale = turnScale;
            curve.Climb = climb;
            curve.Append(section);
        }

        public PeelStripNode GetNode(int index)
        {
            return curve.GetNode(index);
        }

        public void Clear()
        {
            curve.Clear();
        }
    }
}

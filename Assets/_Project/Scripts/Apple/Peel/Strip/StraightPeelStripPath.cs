using System.Collections.Generic;
using UnityEngine;

namespace Inktober.Apple.Peel
{
    /// <summary>
    /// Lays the strip out flat and straight: along the local Z axis, across the local X axis, the skin facing up.
    /// A preview of the width of the strip, nothing moves.
    /// </summary>
    public class StraightPeelStripPath : MonoBehaviour, IPeelStripPath
    {
        private readonly List<PeelStripNode> nodes = new List<PeelStripNode>();

        public bool IsDynamic => false;

        public int NodeCount => nodes.Count;

        public void Append(PeelSection section)
        {
            nodes.Add(new PeelStripNode(Vector3.forward * section.Distance, Vector3.right, Vector3.up));
        }

        public PeelStripNode GetNode(int index)
        {
            return nodes[index];
        }

        public void Clear()
        {
            nodes.Clear();
        }
    }
}

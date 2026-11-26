using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Apple.Peel
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
            nodes.Add(Preview(section));
        }

        public PeelStripNode Preview(PeelSection section)
        {
            return new PeelStripNode(Vector3.forward * section.Distance, Vector3.right, Vector3.up);
        }

        public PeelStripView Lay(bool hasLive, PeelSection live, out PeelStripNode liveNode)
        {
            liveNode = hasLive ? Preview(live) : default;
            return new PeelStripView(hasLive ? liveNode : GetNode(NodeCount - 1));
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

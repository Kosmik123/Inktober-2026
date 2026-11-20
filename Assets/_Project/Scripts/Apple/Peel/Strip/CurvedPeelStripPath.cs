using System.Collections.Generic;
using UnityEngine;

namespace Inktober.Apple.Peel
{
    /// <summary>
    /// Lays the strip out flat and bends it the way the knife turned: when the knife turned left the strip goes left,
    /// when it turned right the strip goes right. Only the turns count, the shape of the apple does not.
    /// The strip starts along the local Z axis, the skin facing up, and rises a little along its length so that
    /// the loops of a long strip do not lie in the same plane.
    /// </summary>
    public class CurvedPeelStripPath : MonoBehaviour, IPeelStripPath
    {
        [Tooltip("How much of the turning of the knife the strip follows. 1 follows it exactly, more exaggerates it.")]
        [SerializeField]
        private float turnScale = 1f;

        [Tooltip("How far the strip rises per unit of its length, in world units.")]
        [SerializeField]
        private float climb = 0.02f;

        private readonly List<PeelStripNode> nodes = new List<PeelStripNode>();
        private Vector3 flatPosition;
        private float heading;
        private float lastDistance;

        public bool IsDynamic => false;

        public int NodeCount => nodes.Count;

        public void Append(PeelSection section)
        {
            if (nodes.Count > 0)
            {
                heading += section.Turn * turnScale;
                flatPosition += Direction(heading) * (section.Distance - lastDistance);
            }

            lastDistance = section.Distance;
            nodes.Add(new PeelStripNode(flatPosition + Vector3.up * (section.Distance * climb), Across(heading), Vector3.up));
        }

        public PeelStripNode GetNode(int index)
        {
            return nodes[index];
        }

        public void Clear()
        {
            nodes.Clear();
            flatPosition = Vector3.zero;
            heading = 0f;
            lastDistance = 0f;
        }

        // Heading is in degrees from the local Z axis, positive towards the local X axis (a turn to the right).
        private static Vector3 Direction(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
        }

        private static Vector3 Across(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians), 0f, -Mathf.Sin(radians));
        }
    }
}

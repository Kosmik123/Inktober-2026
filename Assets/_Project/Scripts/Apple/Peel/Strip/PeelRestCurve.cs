using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// The flat shape of the strip: it starts along the Z axis and bends the way the knife turned (a turn to the left bends it left,
    /// a turn to the right bends it right), rising a little along its length. A strip without physics lies in this shape,
    /// a simulated one is pulled towards it.
    /// </summary>
    public class PeelRestCurve
    {
        private readonly List<PeelStripNode> nodes = new List<PeelStripNode>();
        private Vector3 flatPosition;
        private float heading;
        private float lastDistance;

        public int Count => nodes.Count;

        /// <summary>How much of the turning of the knife the strip follows. 1 follows it exactly.</summary>
        public float TurnScale { get; set; } = 1f;

        /// <summary>How far the strip rises per unit of its length, in world units.</summary>
        public float Climb { get; set; } = 0.02f;

        public PeelStripNode Append(PeelSection section)
        {
            var node = Next(section, out heading, out flatPosition);
            lastDistance = section.Distance;
            nodes.Add(node);
            return node;
        }

        /// <summary>The node <see cref="Append"/> would make, without adding it.</summary>
        public PeelStripNode Preview(PeelSection section)
        {
            return Next(section, out _, out _);
        }

        private PeelStripNode Next(PeelSection section, out float nextHeading, out Vector3 nextPosition)
        {
            nextHeading = heading;
            nextPosition = flatPosition;
            if (nodes.Count > 0)
            {
                nextHeading += section.Turn * TurnScale;
                nextPosition += Direction(nextHeading) * (section.Distance - lastDistance);
            }

            return new PeelStripNode(nextPosition + Vector3.up * (section.Distance * Climb), Across(nextHeading), Vector3.up);
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

using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// A chain of points that behaves like a hanging strip: a Verlet simulation with fixed distances between the neighbours,
    /// gravity, an optional pull towards the rest shape and an optional sphere (the apple) the points cannot enter.
    /// The newest point, the head, is pinned to its rest position: that is where the strip leaves the knife.
    /// </summary>
    public class PeelChain
    {
        private const float MinDistance = 1e-6f;

        private readonly List<Vector3> positions = new List<Vector3>();
        private readonly List<Vector3> previous = new List<Vector3>();
        private readonly List<Vector3> rest = new List<Vector3>();
        private readonly List<float> linkLengths = new List<float>();
        // How far the links moved every point in the current step.
        private readonly List<Vector3> corrections = new List<Vector3>();

        public int Count => positions.Count;

        /// <summary>Acceleration of gravity, in units per second squared.</summary>
        public Vector3 Gravity { get; set; } = new Vector3(0f, -40f, 0f);

        /// <summary>Extra acceleration away from the centre of the sphere, so the strip leans away from the apple.</summary>
        public float OutwardAcceleration { get; set; }

        /// <summary>Share of the velocity lost per second.</summary>
        public float Damping { get; set; } = 2f;

        /// <summary>How fast the points are pulled back to the rest shape, per second. Zero lets the strip hang freely.</summary>
        public float RestStiffness { get; set; }

        /// <summary>
        /// How much of the speed that the links give to the points is taken back, from 0 to 1. The links move only the point farther from the head,
        /// so without this every pull travels down to the tail and builds up there like in a whip, and <see cref="Damping"/> hardly helps.
        /// With it the point nearer the head feels the weight of its neighbour (the velocity correction of Dynamic Follow The Leader). Near 1 is calm, 0 is off.
        /// </summary>
        public float VelocityCorrection { get; set; } = 0.9f;

        /// <summary>How many times the constraints are solved in one step.</summary>
        public int Iterations { get; set; } = 4;

        public bool HasSphere { get; set; }

        public Vector3 SphereCenter { get; set; }

        public float SphereRadius { get; set; }

        /// <summary>Adds a new head at its rest position. The previous head is let go.</summary>
        public void Add(Vector3 restPosition)
        {
            if (rest.Count > 0)
                linkLengths.Add(Vector3.Distance(rest[rest.Count - 1], restPosition));

            rest.Add(restPosition);
            positions.Add(restPosition);
            previous.Add(restPosition);
            corrections.Add(Vector3.zero);
        }

        public void Clear()
        {
            positions.Clear();
            previous.Clear();
            rest.Clear();
            linkLengths.Clear();
            corrections.Clear();
        }

        public Vector3 GetPosition(int index)
        {
            return positions[index];
        }

        public Vector3 GetRestPosition(int index)
        {
            return rest[index];
        }

        /// <summary>Moves the head to the rest position it has now and simulates the time <paramref name="deltaTime"/>.</summary>
        public void Step(float deltaTime)
        {
            int head = positions.Count - 1;
            if (head < 0 || deltaTime <= 0f)
                return;

            float keep = Mathf.Clamp01(1f - Damping * deltaTime);
            float pull = Mathf.Clamp01(RestStiffness * deltaTime);

            for (int i = 0; i < head; i++)
            {
                var position = positions[i];
                var velocity = (position - previous[i]) * keep;
                var acceleration = Gravity + OutwardDirection(position) * OutwardAcceleration;
                previous[i] = position;
                positions[i] = position + velocity + acceleration * (deltaTime * deltaTime) + (rest[i] - position) * pull;
            }

            positions[head] = rest[head];
            previous[head] = rest[head];

            for (int i = 0; i <= head; i++)
                corrections[i] = Vector3.zero;

            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                SolveLinks(head);
                PushOutOfSphere(head);
            }

            CorrectVelocities(head);
        }

        // In Verlet the velocity is the difference to the previous position, so moving the previous position changes the velocity.
        // A point loses the share of the move that the link made to its neighbour on the side of the tail.
        private void CorrectVelocities(int head)
        {
            float share = Mathf.Clamp01(VelocityCorrection);
            for (int i = 1; i < head; i++)
                previous[i] += corrections[i - 1] * share;
        }

        private Vector3 OutwardDirection(Vector3 position)
        {
            if (HasSphere == false)
                return Vector3.zero;

            var away = position - SphereCenter;
            return away.sqrMagnitude < MinDistance ? Vector3.zero : away.normalized;
        }

        // From the head to the tail: every point is put exactly one link away from the one before it, so the strip never
        // stretches, however long it is and however hard gravity pulls.
        private void SolveLinks(int head)
        {
            for (int a = head - 1; a >= 0; a--)
            {
                var fromNext = positions[a] - positions[a + 1];
                float distance = fromNext.magnitude;
                if (distance < MinDistance)
                    continue;

                var corrected = positions[a + 1] + fromNext * (linkLengths[a] / distance);
                corrections[a] += corrected - positions[a];
                positions[a] = corrected;
            }
        }

        private void PushOutOfSphere(int head)
        {
            if (HasSphere == false)
                return;

            for (int i = 0; i < head; i++)
            {
                var away = positions[i] - SphereCenter;
                float distance = away.magnitude;
                if (distance >= SphereRadius || distance < MinDistance)
                    continue;

                positions[i] = SphereCenter + away * (SphereRadius / distance);
            }
        }
    }
}

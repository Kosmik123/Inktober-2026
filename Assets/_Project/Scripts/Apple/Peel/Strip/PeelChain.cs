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

        public int Count => positions.Count;

        /// <summary>Acceleration of gravity, in units per second squared.</summary>
        public Vector3 Gravity { get; set; } = new Vector3(0f, -40f, 0f);

        /// <summary>Extra acceleration away from the centre of the sphere, so the strip leans away from the apple.</summary>
        public float OutwardAcceleration { get; set; }

        /// <summary>Share of the velocity lost per second.</summary>
        public float Damping { get; set; } = 2f;

        /// <summary>How fast the points are pulled back to the rest shape, per second. Zero lets the strip hang freely.</summary>
        public float RestStiffness { get; set; }

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
        }

        public void Clear()
        {
            positions.Clear();
            previous.Clear();
            rest.Clear();
            linkLengths.Clear();
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

            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                SolveLinks(head);
                PushOutOfSphere(head);
            }
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

                positions[a] = positions[a + 1] + fromNext * (linkLengths[a] / distance);
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

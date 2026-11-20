using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// The strip as a hanging ribbon. The newest point stays where the strip leaves the knife (it follows the flat shape of
    /// <see cref="PeelRestCurve"/>), the older points are simulated by <see cref="PeelChain"/>: they fall, lean away from the apple
    /// and cannot enter it. The ribbon is not twisted: its sides are carried along the chain from the head.
    /// </summary>
    public class PhysicalPeelStripPath : MonoBehaviour, IPeelStripPath
    {
        [Header("Rest shape")]
        [Tooltip("How much of the turning of the knife the strip follows. 1 follows it exactly, more exaggerates it.")]
        [SerializeField]
        private float turnScale = 1f;

        [Tooltip("How far the strip rises per unit of its length, in world units.")]
        [SerializeField]
        private float climb = 0.02f;

        [Tooltip("How fast the strip is pulled back to its flat shape, per second. Zero lets it hang freely.")]
        [SerializeField]
        private float restStiffness = 0.5f;

        [Header("Forces")]
        [Tooltip("Acceleration of gravity in world units per second squared (1 unit is about 10 cm).")]
        [SerializeField]
        private float gravity = 40f;

        [Tooltip("Extra acceleration away from the centre of the apple, so the strip does not fall onto it.")]
        [SerializeField]
        private float outwardAcceleration = 15f;

        [Tooltip("Share of the velocity lost per second.")]
        [SerializeField]
        private float damping = 2f;

        [Header("Apple")]
        [Tooltip("The apple the strip must not enter. Empty means no collision and no leaning away.")]
        [SerializeField]
        private Transform apple;

        [Tooltip("Radius of the apple in its own units.")]
        [SerializeField]
        private float appleRadius = 0.5f;

        [Tooltip("The strip stays this far from the apple, in world units.")]
        [SerializeField]
        private float clearance = 0.01f;

        [Header("Simulation")]
        [SerializeField]
        private int iterations = 4;

        [Tooltip("The simulation runs in steps of this length, so it does not depend on the frame rate.")]
        [SerializeField]
        private float stepTime = 1f / 120f;

        [SerializeField]
        private int maxStepsPerFrame = 8;

        private readonly PeelRestCurve curve = new PeelRestCurve();
        private readonly PeelChain chain = new PeelChain();
        private readonly List<PeelStripNode> nodes = new List<PeelStripNode>();
        private float timeLeft;

        public bool IsDynamic => true;

        public int NodeCount => nodes.Count;

        private void Update()
        {
            if (chain.Count == 0)
                return;

            ApplySettings();

            timeLeft += Time.deltaTime;
            int steps = 0;
            while (timeLeft >= stepTime && steps < maxStepsPerFrame)
            {
                chain.Step(stepTime);
                timeLeft -= stepTime;
                steps++;
            }

            // Time that does not fit in the allowed steps is dropped, so a long frame does not make the strip catch up.
            if (steps == maxStepsPerFrame)
                timeLeft = 0f;

            UpdateNodes();
        }

        public void Append(PeelSection section)
        {
            curve.TurnScale = turnScale;
            curve.Climb = climb;
            var node = curve.Append(section);

            chain.Add(node.Position);
            nodes.Add(node);
            UpdateNodes();
        }

        public PeelStripNode GetNode(int index)
        {
            return nodes[index];
        }

        public void Clear()
        {
            curve.Clear();
            chain.Clear();
            nodes.Clear();
            timeLeft = 0f;
        }

        private void ApplySettings()
        {
            chain.Gravity = transform.InverseTransformDirection(Vector3.down) * gravity;
            chain.OutwardAcceleration = outwardAcceleration;
            chain.Damping = damping;
            chain.RestStiffness = restStiffness;
            chain.Iterations = iterations;

            chain.HasSphere = apple;
            if (apple == null)
                return;

            float scale = Mathf.Max(transform.lossyScale.x, 1e-6f);
            chain.SphereCenter = transform.InverseTransformPoint(apple.position);
            chain.SphereRadius = (appleRadius * apple.lossyScale.x + clearance) / scale;
        }

        // The sides of the ribbon follow the chain without twisting: from the head, each side is the previous one
        // turned to stay across the next piece of the chain.
        private void UpdateNodes()
        {
            int count = chain.Count;
            var headNode = curve.GetNode(count - 1);
            var right = headNode.Right;
            var tangent = Vector3.forward;

            for (int i = count - 1; i >= 0; i--)
            {
                if (count > 1)
                    tangent = TangentAt(i, count, tangent);

                right = Vector3.ProjectOnPlane(right, tangent);
                right = right.sqrMagnitude < 1e-8f ? headNode.Right : right.normalized;
                nodes[i] = new PeelStripNode(chain.GetPosition(i), right, Vector3.Cross(tangent, right));
            }
        }

        // Direction of the chain at a point, towards the head. The head uses the piece before it.
        private Vector3 TangentAt(int index, int count, Vector3 fallback)
        {
            int from = Mathf.Min(index, count - 2);
            var delta = chain.GetPosition(from + 1) - chain.GetPosition(from);
            return delta.sqrMagnitude < 1e-12f ? fallback : delta.normalized;
        }

        private void OnValidate()
        {
            gravity = Mathf.Max(0f, gravity);
            outwardAcceleration = Mathf.Max(0f, outwardAcceleration);
            damping = Mathf.Max(0f, damping);
            restStiffness = Mathf.Max(0f, restStiffness);
            appleRadius = Mathf.Max(0f, appleRadius);
            clearance = Mathf.Max(0f, clearance);
            iterations = Mathf.Max(1, iterations);
            stepTime = Mathf.Max(1f / 1000f, stepTime);
            maxStepsPerFrame = Mathf.Max(1, maxStepsPerFrame);
        }
    }
}

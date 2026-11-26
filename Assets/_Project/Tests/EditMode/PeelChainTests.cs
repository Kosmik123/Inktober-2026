using Inktober2026.Apple.Peel;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class PeelChainTests
    {
        private const int NodeCount = 20;
        private const float Spacing = 0.03f;
        private const float Step = 1f / 120f;

        // A strip along +Z at height 1, the last node is the head.
        private static PeelChain CreateChain()
        {
            var chain = new PeelChain { RestStiffness = 0f };
            for (int i = 0; i < NodeCount; i++)
                chain.Add(new Vector3(0f, 1f, i * Spacing));

            return chain;
        }

        private static void Run(PeelChain chain, float seconds)
        {
            int steps = Mathf.RoundToInt(seconds / Step);
            for (int i = 0; i < steps; i++)
                chain.Step(Step);
        }

        [Test]
        public void TheHead_StaysWhereItWasAdded()
        {
            var chain = CreateChain();

            Run(chain, 2f);

            Assert.That(Vector3.Distance(chain.GetPosition(NodeCount - 1), chain.GetRestPosition(NodeCount - 1)), Is.LessThan(1e-5f));
        }

        [Test]
        public void TheTail_FallsUnderGravity()
        {
            var chain = CreateChain();

            // It swings like a pendulum and the damping settles it hanging below the head, about one length of the strip down.
            Run(chain, 6f);

            Assert.That(chain.GetPosition(0).y, Is.LessThan(1f - 0.4f));
        }

        [Test]
        public void TheLinks_KeepTheirLength()
        {
            var chain = CreateChain();

            Run(chain, 2f);

            for (int i = 0; i < NodeCount - 1; i++)
                Assert.That(Vector3.Distance(chain.GetPosition(i), chain.GetPosition(i + 1)), Is.EqualTo(Spacing).Within(Spacing * 0.05f), $"Link {i}.");
        }

        [Test]
        public void WithoutGravityOrStiffness_TheStripStaysWhereItIs()
        {
            var chain = CreateChain();
            chain.Gravity = Vector3.zero;

            Run(chain, 1f);

            Assert.That(Vector3.Distance(chain.GetPosition(0), chain.GetRestPosition(0)), Is.LessThan(1e-4f));
        }

        [Test]
        public void TheRestStiffness_HoldsTheStripNearItsFlatShape()
        {
            var hanging = CreateChain();
            var held = CreateChain();
            held.RestStiffness = 30f;

            Run(hanging, 2f);
            Run(held, 2f);

            Assert.That(held.GetPosition(0).y, Is.GreaterThan(hanging.GetPosition(0).y + 0.1f));
        }

        [Test]
        public void TheSphere_KeepsEveryPointOutside()
        {
            var chain = new PeelChain
            {
                RestStiffness = 0f,
                HasSphere = true,
                SphereCenter = new Vector3(0f, 0.5f, 0.3f),
                SphereRadius = 0.3f,
            };

            // The strip starts right above the sphere, so gravity pushes it into it.
            for (int i = 0; i < NodeCount; i++)
                chain.Add(new Vector3(0f, 0.9f, i * Spacing));

            Run(chain, 2f);

            for (int i = 0; i < NodeCount; i++)
                Assert.That(Vector3.Distance(chain.GetPosition(i), chain.SphereCenter), Is.GreaterThan(chain.SphereRadius - 0.005f), $"Node {i}.");
        }

        [Test]
        public void TheOutwardAcceleration_LeansTheStripAwayFromTheCentre()
        {
            var chain = CreateChain();
            chain.Gravity = Vector3.zero;
            chain.HasSphere = true;
            chain.SphereCenter = new Vector3(0f, 0f, 0f);
            chain.SphereRadius = 0.1f;
            chain.OutwardAcceleration = 20f;

            Run(chain, 0.5f);

            Assert.That(chain.GetPosition(0).y, Is.GreaterThan(1f + 0.05f));
        }

        [Test]
        public void Clear_RemovesAllPoints()
        {
            var chain = CreateChain();

            chain.Clear();

            Assert.That(chain.Count, Is.EqualTo(0));
        }
    }
}

using Inktober2026.Apple.Peel;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class PhysicalPeelStripPathTests
    {
        private GameObject pathObject;

        [TearDown]
        public void TearDown()
        {
            if (pathObject)
                Object.DestroyImmediate(pathObject);
        }

        private PhysicalPeelStripPath CreatePath()
        {
            pathObject = new GameObject("Path");
            return pathObject.AddComponent<PhysicalPeelStripPath>();
        }

        private static PeelSection SectionAt(float distance, float turn = 0f)
        {
            return new PeelSection(distance, -0.03f, 0.03f, turn);
        }

        [Test]
        public void ThePath_IsDynamic()
        {
            Assert.That(CreatePath().IsDynamic, Is.True);
        }

        [Test]
        public void EverySection_AddsANode()
        {
            var path = CreatePath();

            for (int i = 0; i < 5; i++)
                path.Append(SectionAt(i * 0.03f));

            Assert.That(path.NodeCount, Is.EqualTo(5));
        }

        [Test]
        public void TheNodes_HaveUnitSidesAcrossTheStrip()
        {
            var path = CreatePath();
            for (int i = 0; i < 10; i++)
                path.Append(SectionAt(i * 0.03f, i == 5 ? 20f : 0f));

            for (int i = 0; i < path.NodeCount; i++)
            {
                var node = path.GetNode(i);
                Assert.That(node.Right.magnitude, Is.EqualTo(1f).Within(1e-4f), $"Right of {i}.");
                Assert.That(node.Up.magnitude, Is.EqualTo(1f).Within(1e-4f), $"Up of {i}.");
                Assert.That(Vector3.Dot(node.Right, node.Up), Is.EqualTo(0f).Within(1e-4f), $"Sides of {i}.");
            }
        }

        [Test]
        public void ANewStrip_StartsFlatLikeTheRestShape()
        {
            var path = CreatePath();

            path.Append(SectionAt(0f));
            path.Append(SectionAt(0.03f));

            var head = path.GetNode(1);
            Assert.That(head.Up.y, Is.GreaterThan(0.99f));
            Assert.That(head.Right.x, Is.GreaterThan(0.99f));
        }

        [Test]
        public void Clear_RemovesTheNodes()
        {
            var path = CreatePath();
            path.Append(SectionAt(0f));

            path.Clear();

            Assert.That(path.NodeCount, Is.EqualTo(0));
        }
    }
}

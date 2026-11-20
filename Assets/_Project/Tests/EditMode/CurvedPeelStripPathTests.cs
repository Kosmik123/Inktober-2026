using Inktober2026.Apple.Peel;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class CurvedPeelStripPathTests
    {
        private const float Tolerance = 1e-4f;

        private GameObject pathObject;

        [TearDown]
        public void TearDown()
        {
            if (pathObject)
                Object.DestroyImmediate(pathObject);
        }

        private CurvedPeelStripPath CreatePath()
        {
            pathObject = new GameObject("Path");
            return pathObject.AddComponent<CurvedPeelStripPath>();
        }

        private static PeelSection SectionAt(float distance, float turn = 0f)
        {
            return new PeelSection(distance, -0.03f, 0.03f, turn);
        }

        private static Vector2 Flat(Vector3 position)
        {
            return new Vector2(position.x, position.z);
        }

        [Test]
        public void WithoutTurns_TheStripGoesStraightAlongZ()
        {
            var path = CreatePath();

            path.Append(SectionAt(0f));
            path.Append(SectionAt(0.03f));
            path.Append(SectionAt(0.06f));

            Assert.That(path.IsDynamic, Is.False);
            Assert.That(path.NodeCount, Is.EqualTo(3));
            Assert.That(Vector2.Distance(Flat(path.GetNode(2).Position), new Vector2(0f, 0.06f)), Is.LessThan(Tolerance));
            Assert.That(path.GetNode(2).Right.x, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void ATurnToTheRight_SendsTheStripToTheRight()
        {
            var path = CreatePath();

            path.Append(SectionAt(0f));
            path.Append(SectionAt(0.03f));
            path.Append(SectionAt(0.06f, 90f));

            Assert.That(Vector2.Distance(Flat(path.GetNode(2).Position), new Vector2(0.03f, 0.03f)), Is.LessThan(Tolerance));
            Assert.That(path.GetNode(2).Right.z, Is.EqualTo(-1f).Within(Tolerance));
        }

        [Test]
        public void ATurnToTheLeft_SendsTheStripToTheLeft()
        {
            var path = CreatePath();

            path.Append(SectionAt(0f));
            path.Append(SectionAt(0.03f));
            path.Append(SectionAt(0.06f, -90f));

            Assert.That(Vector2.Distance(Flat(path.GetNode(2).Position), new Vector2(-0.03f, 0.03f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void TheStrip_RisesAlongItsLength()
        {
            var path = CreatePath();

            path.Append(SectionAt(0f));
            path.Append(SectionAt(1f));

            Assert.That(path.GetNode(1).Position.y, Is.GreaterThan(path.GetNode(0).Position.y));
        }

        [Test]
        public void Clear_StartsAgainFromTheOrigin()
        {
            var path = CreatePath();
            path.Append(SectionAt(0f));
            path.Append(SectionAt(0.03f, 45f));
            path.Clear();

            path.Append(SectionAt(0f));
            path.Append(SectionAt(0.03f));

            Assert.That(path.NodeCount, Is.EqualTo(2));
            Assert.That(Vector2.Distance(Flat(path.GetNode(1).Position), new Vector2(0f, 0.03f)), Is.LessThan(Tolerance));
        }
    }
}

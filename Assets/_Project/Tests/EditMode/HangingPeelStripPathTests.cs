using Inktober2026.Apple.Peel;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class HangingPeelStripPathTests
    {
        private const float Tolerance = 1e-3f;
        private const float Spacing = 0.03f;
        private const int Count = 60;

        private GameObject pathObject;
        private GameObject otherObject;

        [TearDown]
        public void TearDown()
        {
            if (pathObject)
                Object.DestroyImmediate(pathObject);
            if (otherObject)
                Object.DestroyImmediate(otherObject);
        }

        // Without a knife the strip leaves along the default direction, 45 degrees up and back.
        private HangingPeelStripPath CreatePath(float turnPerSection = 0f)
        {
            pathObject = new GameObject("Path");
            var path = pathObject.AddComponent<HangingPeelStripPath>();
            for (int i = 0; i < Count; i++)
                path.Append(new PeelSection(i * Spacing, -0.03f, 0.03f, turnPerSection));

            path.Lay(false, default, out _);
            return path;
        }

        [Test]
        public void AtTheKnife_TheStripLeavesUpAndBack()
        {
            var path = CreatePath();

            var step = (path.GetNode(Count - 2).Position - path.GetNode(Count - 1).Position).normalized;

            Assert.That(path.GetNode(Count - 1).Position.magnitude, Is.LessThan(Tolerance));
            Assert.That(Vector3.Angle(step, new Vector3(0f, 1f, -1f)), Is.LessThan(4f));
        }

        [Test]
        public void FarFromTheKnife_TheStripHangsStraightDown()
        {
            var path = CreatePath();

            var step = (path.GetNode(0).Position - path.GetNode(1).Position).normalized;

            Assert.That(Vector3.Angle(step, Vector3.down), Is.LessThan(0.1f));
        }

        [Test]
        public void TheBending_KeepsTheSideHorizontal_AndTheLengthOfThePieces()
        {
            var path = CreatePath(turnPerSection: 12f);

            for (int i = 0; i < Count; i++)
            {
                var node = path.GetNode(i);
                Assert.That(node.Right.y, Is.EqualTo(0f).Within(Tolerance), $"Right of {i}.");
                Assert.That(node.Up.magnitude, Is.EqualTo(1f).Within(Tolerance), $"Up of {i}.");
                Assert.That(Vector3.Dot(node.Right, node.Up), Is.EqualTo(0f).Within(Tolerance), $"Sides of {i}.");
                if (i > 0)
                    Assert.That(Vector3.Distance(node.Position, path.GetNode(i - 1).Position), Is.EqualTo(Spacing).Within(Tolerance), $"Piece {i}.");
            }
        }

        // With no bending at all the shape has to be the flat strip as it is drawn, so the turns go to the same sides.
        [Test]
        public void WithoutBending_ItIsTheFlatShapeAsItIsDrawn()
        {
            pathObject = new GameObject("Path");
            var flat = pathObject.AddComponent<CurvedPeelStripPath>();
            var shape = new PeelHangingShape(Vector3.back, 1000f);
            var rest = new PeelRestCurve { Climb = 0f };
            var turns = new[] { 0f, 10f, 25f, -5f, 40f, 90f, -30f };
            var headings = new float[turns.Length];
            for (int i = 0; i < turns.Length; i++)
            {
                rest.Append(new PeelSection(i * Spacing, -0.03f, 0.03f, turns[i]));
                headings[i] = i == 0 ? 0f : headings[i - 1] + turns[i];
            }

            int head = turns.Length - 1;
            var view = new PeelStripView(rest.GetNode(head));
            var position = Vector3.zero;
            for (int i = head; i >= 0; i--)
            {
                if (i < head)
                    position += shape.TailDirection(0f, headings[i + 1] - headings[head]) * Spacing;

                var expected = view.ToView(rest.GetNode(i));
                var actual = shape.Node(position, 0f, headings[i] - headings[head]);
                Assert.That(Vector3.Distance(actual.Position, expected.Position), Is.LessThan(0.002f), $"Position {i}.");
                Assert.That(Vector3.Distance(actual.Right, expected.Right), Is.LessThan(0.002f), $"Right {i}.");
                Assert.That(Vector3.Distance(actual.Up, expected.Up), Is.LessThan(0.002f), $"Up {i}.");
            }
        }

        [Test]
        public void TheShape_FollowsTheWorld_HoweverTheStripObjectIsTurned()
        {
            var straight = CreatePath(turnPerSection: 5f);
            otherObject = new GameObject("Turned");
            otherObject.transform.rotation = Quaternion.Euler(-30f, 20f, 10f);
            var turned = otherObject.AddComponent<HangingPeelStripPath>();
            for (int i = 0; i < Count; i++)
                turned.Append(new PeelSection(i * Spacing, -0.03f, 0.03f, 5f));

            var view = turned.Lay(false, default, out _);

            var seen = otherObject.transform.TransformDirection(view.ToView(turned.GetNode(0)).Position);
            Assert.That(Vector3.Distance(seen, straight.GetNode(0).Position), Is.LessThan(Tolerance));
        }

        [Test]
        public void TheGrowingSection_BecomesTheNewestEnd_AndIsNotKept()
        {
            var path = CreatePath();

            path.Lay(true, new PeelSection(Count * Spacing - 0.01f, -0.03f, 0.03f), out var live);

            Assert.That(live.Position.magnitude, Is.LessThan(Tolerance));
            Assert.That(path.GetNode(Count - 1).Position.magnitude, Is.EqualTo(0.02f).Within(Tolerance));
            Assert.That(path.NodeCount, Is.EqualTo(Count));
        }
    }
}

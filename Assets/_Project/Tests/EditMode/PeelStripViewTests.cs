using Inktober2026.Apple.Peel;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class PeelStripViewTests
    {
        // A head that travelled away and turned, with a tilted up axis like on a rising strip.
        private static PeelStripNode Head()
        {
            var rotation = Quaternion.Euler(10f, 70f, 0f);
            return new PeelStripNode(new Vector3(2f, 0.3f, 5f), rotation * Vector3.right, rotation * Vector3.up);
        }

        [Test]
        public void TheNewestNode_IsAtTheOrigin()
        {
            var view = new PeelStripView(Head());

            Assert.That(view.ToView(Head()).Position.magnitude, Is.LessThan(1e-5f));
        }

        [Test]
        public void PointFromView_UndoesToView()
        {
            var view = new PeelStripView(Head());
            var point = new Vector3(1f, -0.4f, 3f);

            var seen = view.ToView(new PeelStripNode(point, Vector3.right, Vector3.up)).Position;

            Assert.That(Vector3.Distance(view.PointFromView(seen), point), Is.LessThan(1e-4f));
        }

        [Test]
        public void DirectionFromView_UndoesToView()
        {
            var view = new PeelStripView(Head());

            var seen = view.ToView(new PeelStripNode(Vector3.zero, Vector3.forward, Vector3.up)).Right;

            Assert.That(Vector3.Distance(view.DirectionFromView(seen), Vector3.forward), Is.LessThan(1e-4f));
        }
    }
}

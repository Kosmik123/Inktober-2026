using Inktober2026.Apple.Peel;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class PeelPoleTests
    {
        private const float Cap = 6f;

        // A point of a sphere of radius 0.5 at the given angle from the north pole.
        private static Vector3 At(float degrees)
        {
            return Quaternion.AngleAxis(degrees, Vector3.forward) * Vector3.up * 0.5f;
        }

        [TestCase(0f, true)]
        [TestCase(5f, true)]
        [TestCase(7f, false)]
        [TestCase(90f, false)]
        [TestCase(173f, false)]
        [TestCase(175f, true)]
        [TestCase(180f, true)]
        public void ALineWithoutWidth_TouchesOnlyInsideTheCaps(float angle, bool touches)
        {
            Assert.AreEqual(touches, PeelPole.Touches(At(angle), 0f, 1f, Cap, Cap));
        }

        [Test]
        public void TheEdgeOfTheStrip_CountsToo()
        {
            // Half of the width 0.2 is 0.1, which on a radius of 0.5 is about 11.5 degrees.
            Assert.IsTrue(PeelPole.Touches(At(17f), 0.2f, 1f, Cap, Cap));
            Assert.IsFalse(PeelPole.Touches(At(18f), 0.2f, 1f, Cap, Cap));
            Assert.IsTrue(PeelPole.Touches(At(163f), 0.2f, 1f, Cap, Cap));
        }

        [Test]
        public void TheShareOfTheWidth_SaysHowMuchTheEdgeCounts()
        {
            // The edge of a strip 0.2 wide is about 11.5 degrees from its centre.
            Assert.IsFalse(PeelPole.Touches(At(7f), 0.2f, 1f, Cap, Cap, 0f));
            Assert.IsTrue(PeelPole.Touches(At(5f), 0.2f, 1f, Cap, Cap, 0f));
            Assert.IsTrue(PeelPole.Touches(At(11f), 0.2f, 1f, Cap, Cap, 0.5f));
            Assert.IsFalse(PeelPole.Touches(At(12.5f), 0.2f, 1f, Cap, Cap, 0.5f));
        }

        [TestCase(0f)]
        [TestCase(10f)]
        [TestCase(39f)]
        [TestCase(150f)]
        [TestCase(180f)]
        public void KeepAway_MovesAPoleThatIsTooCloseToTheSmallestAllowedAngle(float angle)
        {
            var direction = new Vector3(0f, 1f, -1f).normalized;
            var axis = Quaternion.AngleAxis(angle, Vector3.right) * direction;

            var turned = PeelPole.KeepAway(direction, axis, 40f) * axis;

            Assert.AreEqual(angle < 90f ? 40f : 140f, Vector3.Angle(direction, turned), 0.1f);
        }

        [Test]
        public void KeepAway_LeavesAFarPoleWhereItIs()
        {
            var turn = PeelPole.KeepAway(Vector3.back, Vector3.up, 40f);

            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, turn), 1e-3f);
        }
    }
}

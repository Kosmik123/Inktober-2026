using Inktober2026.Apple.Peel;
using NUnit.Framework;

namespace Inktober2026.Tests.EditMode
{
    public class PeelStripWidthTests
    {
        private const float Width = 0.06f;
        private const float Tolerance = 1e-5f;

        [Test]
        public void Cover_WithNoOlderLine_IsZero()
        {
            Assert.That(PeelStripWidthUtility.Cover(float.PositiveInfinity, Width), Is.EqualTo(0f));
        }

        [Test]
        public void Cover_OfALineOnTheSamePath_IsTheWholeWidth()
        {
            Assert.That(PeelStripWidthUtility.Cover(0f, Width), Is.EqualTo(Width).Within(Tolerance));
        }

        [Test]
        public void Cover_OfALineAtHalfTheWidth_IsHalfTheWidth()
        {
            Assert.That(PeelStripWidthUtility.Cover(Width * 0.5f, Width), Is.EqualTo(Width * 0.5f).Within(Tolerance));
        }

        [Test]
        public void Cover_OfALineAtTheWidth_IsZero()
        {
            Assert.That(PeelStripWidthUtility.Cover(Width, Width), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Cover_OfALineFartherThanTheWidth_IsZero()
        {
            Assert.That(PeelStripWidthUtility.Cover(Width * 3f, Width), Is.EqualTo(0f));
        }

        [Test]
        public void Edges_WithNothingCovered_AreTheWholeBand()
        {
            PeelStripWidthUtility.Edges(0f, 0f, Width, out float left, out float right);

            Assert.That(left, Is.EqualTo(-Width * 0.5f).Within(Tolerance));
            Assert.That(right, Is.EqualTo(Width * 0.5f).Within(Tolerance));
        }

        [Test]
        public void Edges_WithTheLeftCovered_MoveTheLeftEdgeInwards()
        {
            PeelStripWidthUtility.Edges(0.02f, 0f, Width, out float left, out float right);

            Assert.That(left, Is.EqualTo(-0.01f).Within(Tolerance));
            Assert.That(right, Is.EqualTo(Width * 0.5f).Within(Tolerance));
        }

        [Test]
        public void Edges_WithBothSidesCovered_LeaveTheMiddle()
        {
            PeelStripWidthUtility.Edges(0.02f, 0.01f, Width, out float left, out float right);

            Assert.That(left, Is.EqualTo(-0.01f).Within(Tolerance));
            Assert.That(right, Is.EqualTo(0.02f).Within(Tolerance));
        }

        [Test]
        public void Edges_WithEverythingCovered_MeetInAPoint()
        {
            PeelStripWidthUtility.Edges(Width * 0.8f, Width * 0.6f, Width, out float left, out float right);

            Assert.That(right - left, Is.EqualTo(0f).Within(Tolerance));
        }
    }
}

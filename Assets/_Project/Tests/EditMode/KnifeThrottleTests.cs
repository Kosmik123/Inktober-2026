using Inktober2026.Apple.Knife;
using NUnit.Framework;

namespace Inktober2026.Tests.EditMode
{
    public class KnifeThrottleTests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void ANewThrottle_IsInTheMiddle()
        {
            Assert.That(new KnifeThrottle().Value, Is.EqualTo(0f));
        }

        [Test]
        public void Add_MovesTheLeverByThePartOfItsRange()
        {
            var throttle = new KnifeThrottle();

            throttle.Add(0.25f);
            throttle.Add(-0.5f);

            Assert.That(throttle.Value, Is.EqualTo(-0.25f).Within(Tolerance));
        }

        [Test]
        public void Add_StopsAtBothEnds()
        {
            var throttle = new KnifeThrottle();

            throttle.Add(5f);
            Assert.That(throttle.Value, Is.EqualTo(1f));

            throttle.Add(-9f);
            Assert.That(throttle.Value, Is.EqualTo(-1f));
        }

        [Test]
        public void Relax_WithoutARate_KeepsTheLever()
        {
            var throttle = new KnifeThrottle();
            throttle.Add(0.6f);

            throttle.Relax(0f, 10f);

            Assert.That(throttle.Value, Is.EqualTo(0.6f).Within(Tolerance));
        }

        [Test]
        public void Relax_PullsTheLeverToTheMiddleFromBothSides()
        {
            var up = new KnifeThrottle();
            up.Add(0.6f);
            var down = new KnifeThrottle();
            down.Add(-0.6f);

            up.Relax(1f, 0.5f);
            down.Relax(1f, 0.5f);

            Assert.That(up.Value, Is.EqualTo(0.1f).Within(Tolerance));
            Assert.That(down.Value, Is.EqualTo(-0.1f).Within(Tolerance));
        }

        [Test]
        public void Relax_DoesNotPassTheMiddle()
        {
            var throttle = new KnifeThrottle();
            throttle.Add(0.2f);

            throttle.Relax(1f, 5f);

            Assert.That(throttle.Value, Is.EqualTo(0f));
        }

        [Test]
        public void Reset_PutsTheLeverInTheMiddle()
        {
            var throttle = new KnifeThrottle();
            throttle.Add(0.7f);

            throttle.Reset();

            Assert.That(throttle.Value, Is.EqualTo(0f));
        }

        [Test]
        public void Scale_IsOneInTheMiddle()
        {
            Assert.That(KnifeThrottle.Scale(0f, 0.4f, 1.8f), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Scale_ReachesTheEnds()
        {
            Assert.That(KnifeThrottle.Scale(1f, 0.4f, 1.8f), Is.EqualTo(1.8f).Within(Tolerance));
            Assert.That(KnifeThrottle.Scale(-1f, 0.4f, 1.8f), Is.EqualTo(0.4f).Within(Tolerance));
        }

        [Test]
        public void Scale_IsAStraightLineOnEachSideOfTheMiddle()
        {
            Assert.That(KnifeThrottle.Scale(0.5f, 0.4f, 1.8f), Is.EqualTo(1.4f).Within(Tolerance));
            Assert.That(KnifeThrottle.Scale(-0.5f, 0.4f, 1.8f), Is.EqualTo(0.7f).Within(Tolerance));
        }

        [Test]
        public void Scale_OutsideTheRange_StaysAtTheEnds()
        {
            Assert.That(KnifeThrottle.Scale(3f, 0.4f, 1.8f), Is.EqualTo(1.8f).Within(Tolerance));
            Assert.That(KnifeThrottle.Scale(-3f, 0.4f, 1.8f), Is.EqualTo(0.4f).Within(Tolerance));
        }
    }
}

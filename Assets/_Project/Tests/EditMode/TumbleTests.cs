using Inktober2026.Apple;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
	public class TumbleTests
	{
		[Test]
		public void TurnsByTheSpeedTimesTheTimeAtFirst()
		{
			var tumble = new Tumble(10f, 10f, 100f, 100f);

			var rotation = tumble.Step(0.5f);

			Assert.AreEqual(5f, Quaternion.Angle(Quaternion.identity, rotation), 1e-3f);
		}

		[Test]
		public void KeepsTheSpeedInTheRangeAndTheAxisAUnitVector()
		{
			var tumble = new Tumble(5f, 20f, 0.5f, 1f, 2f);

			for (int i = 0; i < 2000; i++)
			{
				tumble.Step(0.02f);
				Assert.GreaterOrEqual(tumble.Speed, 5f - 1e-3f);
				Assert.LessOrEqual(tumble.Speed, 20f + 1e-3f);
				Assert.AreEqual(1f, tumble.Axis.magnitude, 1e-3f);
			}
		}

		[Test]
		public void ChangesTheAxisOverTime()
		{
			var tumble = new Tumble(10f, 10f, 0.5f, 0.5f, 5f);
			var first = tumble.Axis;

			for (int i = 0; i < 500; i++)
				tumble.Step(0.02f);

			Assert.Greater(Vector3.Angle(first, tumble.Axis), 1f);
		}
	}
}

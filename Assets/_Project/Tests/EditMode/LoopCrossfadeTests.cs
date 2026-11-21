using Inktober2026.Audio;
using NUnit.Framework;

namespace Inktober.Tests.EditMode
{
	public class LoopCrossfadeTests
	{
		[Test]
		public void IsSilentAtTheStartOfAFadeIn()
		{
			Assert.AreEqual(0f, LoopCrossfade.Gain(0f, 10f, 2f, 2f), 1e-4f);
		}

		[Test]
		public void IsFullInTheMiddle()
		{
			Assert.AreEqual(1f, LoopCrossfade.Gain(5f, 5f, 2f, 2f), 1e-4f);
		}

		[Test]
		public void IsSilentAtTheEnd()
		{
			Assert.AreEqual(0f, LoopCrossfade.Gain(8f, 0f, 2f, 2f), 1e-4f);
		}

		[Test]
		public void KeepsTheLoudnessEvenWhenTwoVoicesCrossfade()
		{
			// The outgoing voice has `remaining` left, the incoming one has played for the same time of the overlap.
			for (float t = 0f; t <= 2f; t += 0.25f)
			{
				float outgoing = LoopCrossfade.Gain(8f, 2f - t, 0f, 2f);
				float incoming = LoopCrossfade.Gain(t, 8f, 2f, 0f);
				Assert.AreEqual(1f, outgoing * outgoing + incoming * incoming, 1e-4f);
			}
		}

		[Test]
		public void ZeroFadeDurationsMeanNoFade()
		{
			Assert.AreEqual(1f, LoopCrossfade.Gain(0f, 0f, 0f, 0f), 1e-4f);
		}
	}
}

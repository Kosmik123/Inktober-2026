using UnityEngine;

namespace Inktober2026.Audio
{
	/// <summary>
	/// Volume of one voice of a clip that is played again and again with the voices overlapping.
	/// A voice fades in while the previous one fades out, so the join of two clips is not audible.
	/// The curve is equal power (square root), so the loudness stays even in the middle of the crossfade.
	/// </summary>
	public static class LoopCrossfade
	{
		/// <param name="played">Seconds since the voice started.</param>
		/// <param name="remaining">Seconds left until the voice ends.</param>
		/// <param name="fadeInDuration">Length of the fade in; 0 means no fade in.</param>
		/// <param name="fadeOutDuration">Length of the fade out; 0 means no fade out.</param>
		public static float Gain(float played, float remaining, float fadeInDuration, float fadeOutDuration)
		{
			float fadeIn = fadeInDuration > 0f ? Mathf.Clamp01(played / fadeInDuration) : 1f;
			float fadeOut = fadeOutDuration > 0f ? Mathf.Clamp01(remaining / fadeOutDuration) : 1f;
			return Mathf.Sqrt(Mathf.Min(fadeIn, fadeOut));
		}
	}
}

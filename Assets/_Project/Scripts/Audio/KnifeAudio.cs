using Inktober2026.Apple.Knife;
using UnityEngine;

namespace Inktober2026.Audio
{
	/// <summary>
	/// Connects the knife to a <see cref="ContinuousAudio"/>, so the player hears the moves: the pitch follows the speed of the knife
	/// (the throttle), and turning the knife sideways shifts the pitch noise (<see cref="ContinuousAudio.Randomness"/>), which changes the way the pitch wanders.
	/// </summary>
	[RequireComponent(typeof(ContinuousAudio))]
	public class KnifeAudio : MonoBehaviour
	{
		[SerializeField]
		private KnifeController knife;

		[SerializeField]
		private ContinuousAudio audioSource;

		[Tooltip("The pitch multiplier is the speed multiplier of the knife to this power. 1 follows the speed exactly, 0 ignores it, 0.5 makes the change gentler.")]
		[SerializeField]
		private float speedToPitch = 0.5f;

		[Tooltip("Randomness at the full turn rate of the knife.")]
		[SerializeField]
		private float randomnessAtFullTurn = 1f;

		[Tooltip("Time the pitch multiplier and the randomness take to catch up with the knife, in seconds, so the sound does not jump.")]
		[SerializeField]
		private float smoothTime = 0.1f;

		private float pitchMultiplier = 1f;
		private float pitchVelocity;
		private float randomness;
		private float randomnessVelocity;

		private void Reset()
		{
			knife = FindAnyObjectByType<KnifeController>();
			audioSource = GetComponent<ContinuousAudio>();
		}

		private void Update()
		{
			float targetPitch = Mathf.Pow(Mathf.Max(0.01f, knife.SpeedScale), speedToPitch);
			float turn = Mathf.Abs(knife.TurnRate) / Mathf.Max(0.01f, knife.MaxTurnRate);
			float targetRandomness = Mathf.Clamp01(turn) * randomnessAtFullTurn;

			pitchMultiplier = Mathf.SmoothDamp(pitchMultiplier, targetPitch, ref pitchVelocity, smoothTime, float.PositiveInfinity, Time.unscaledDeltaTime);
			randomness = Mathf.SmoothDamp(randomness, targetRandomness, ref randomnessVelocity, smoothTime, float.PositiveInfinity, Time.unscaledDeltaTime);

			audioSource.PitchMultiplier = pitchMultiplier;
			audioSource.Randomness = randomness;
		}
	}
}

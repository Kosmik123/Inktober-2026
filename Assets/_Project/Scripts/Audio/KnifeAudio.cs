using Inktober2026.Apple.Knife;
using UnityEngine;

namespace Inktober2026.Audio
{
	/// <summary>
	/// Connects the knife to a <see cref="ContinuousAudio"/>, so the player hears the moves: the pitch follows the speed of the knife
	/// (the throttle), and turning the knife sideways shifts the pitch noise (<see cref="ContinuousAudio.Randomness"/>), pans the sound towards the side of the turn and muffles it.
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

		[Tooltip("How far the sound moves to the side with the turn of the knife: 0 keeps it in the middle, 1 puts it fully on one side at the full turn rate. Negative values turn the direction around.")]
		[SerializeField]
		private float panAmount = 0.6f;

		[Tooltip("How much the sound is muffled by the turn of the knife: 0 not at all, 1 the full low-pass at the full turn rate.")]
		[SerializeField]
		private float muffleAmount = 0.5f;

		[Tooltip("Time the pitch multiplier and the randomness take to catch up with the knife, in seconds, so the sound does not jump.")]
		[SerializeField]
		private float smoothTime = 0.1f;

		private float pitchMultiplier = 1f;
		private float pitchVelocity;
		private float randomness;
		private float randomnessVelocity;
		private float signedTurn;
		private float signedTurnVelocity;

		private void Reset()
		{
			knife = FindAnyObjectByType<KnifeController>();
			audioSource = GetComponent<ContinuousAudio>();
		}

		private void Update()
		{
			float targetPitch = Mathf.Pow(Mathf.Max(0.01f, knife.SpeedScale), speedToPitch);
			float signedTarget = Mathf.Clamp(knife.TurnRate / Mathf.Max(0.01f, knife.MaxTurnRate), -1f, 1f);
			float turn = Mathf.Abs(signedTarget);
			float targetRandomness = Mathf.Clamp01(turn) * randomnessAtFullTurn;

			pitchMultiplier = Mathf.SmoothDamp(pitchMultiplier, targetPitch, ref pitchVelocity, smoothTime, float.PositiveInfinity, Time.unscaledDeltaTime);
			randomness = Mathf.SmoothDamp(randomness, targetRandomness, ref randomnessVelocity, smoothTime, float.PositiveInfinity, Time.unscaledDeltaTime);

			signedTurn = Mathf.SmoothDamp(signedTurn, signedTarget, ref signedTurnVelocity, smoothTime, float.PositiveInfinity, Time.unscaledDeltaTime);

			audioSource.PitchMultiplier = pitchMultiplier;
			audioSource.Pan = signedTurn * panAmount;
			audioSource.Muffle = Mathf.Abs(signedTurn) * muffleAmount;
			audioSource.Randomness = randomness;
		}
	}
}

using Inktober2026.Apple;
using Inktober2026.Apple.Knife;
using Inktober2026.Apple.Peel;
using UnityEngine;

namespace Inktober2026.Game
{
	/// <summary>
	/// Before the round (the Ready state) turns the apple slowly and at random, so the player can wait for the place they want to start the peel from.
	/// The round starts from the pose the apple has when the player starts it. The knife does not touch the apple until then.
	/// </summary>
	[RequireComponent(typeof(KnifeController))]
	public class ReadySpin : MonoBehaviour
	{
		[SerializeField]
		private GameFlow gameFlow;

		[SerializeField]
		private KnifeController knife;

		[Tooltip("Slowest speed of the turn, in degrees per second.")]
		[SerializeField]
		private float minSpeed = 8f;

		[Tooltip("Fastest speed of the turn, in degrees per second.")]
		[SerializeField]
		private float maxSpeed = 22f;

		[Tooltip("The axis and the speed head for new random ones after a random time between min and max, in seconds.")]
		[SerializeField]
		private float minChangeTime = 3f;

		[SerializeField]
		private float maxChangeTime = 7f;

		[Tooltip("How fast the axis and the speed reach the new ones: 1 is about a second.")]
		[SerializeField]
		private float sharpness = 0.7f;

		[Tooltip("The poles of the apple (the stalk and the calyx) stay at least this many degrees away from the knife, so the round cannot start on them.")]
		[Range(0f, 89f)]
		[SerializeField]
		private float poleClearance = 40f;

		private Tumble tumble;

		private void Reset()
		{
			gameFlow = FindAnyObjectByType<GameFlow>();
			knife = GetComponent<KnifeController>();
		}

		private void Awake()
		{
			if (gameFlow == null)
				gameFlow = FindAnyObjectByType<GameFlow>();

			tumble = new Tumble(minSpeed, maxSpeed, minChangeTime, maxChangeTime, sharpness);
		}

		private void Update()
		{
			if (gameFlow.State != GameState.Ready || knife.Surface == null)
				return;

			var surface = knife.Surface;
			surface.rotation = tumble.Step(Time.deltaTime) * surface.rotation;

			// The knife has not moved yet, so the point it is going to start from is the one between it and the centre of the apple.
			surface.rotation = PeelPole.KeepAway(knife.transform.position - surface.position, surface.up, poleClearance) * surface.rotation;
		}

		private void OnValidate()
		{
			minSpeed = Mathf.Max(0f, minSpeed);
			maxSpeed = Mathf.Max(minSpeed, maxSpeed);
			minChangeTime = Mathf.Max(0.01f, minChangeTime);
			maxChangeTime = Mathf.Max(minChangeTime, maxChangeTime);
			sharpness = Mathf.Max(0f, sharpness);
		}
	}
}

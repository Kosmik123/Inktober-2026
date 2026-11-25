using Inktober2026.Apple;
using Inktober2026.Apple.Knife;
using UnityEngine;

namespace Inktober2026.Game
{
	/// <summary>
	/// Before the round (the Ready state) turns the apple slowly and at random, so the player can wait for the place they want to start the peel from.
	/// The round starts from the pose the apple has when the player starts it. The knife lies on the apple the whole time.
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

			// The knife has to know the surface in its starting pose, before it is turned.
			knife.Initialize();

			float deltaTime = Time.deltaTime;
			knife.Surface.rotation = tumble.Step(deltaTime) * knife.Surface.rotation;
			knife.Settle(deltaTime);
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

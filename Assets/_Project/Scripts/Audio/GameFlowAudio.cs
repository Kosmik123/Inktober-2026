using Inktober2026.Game;
using UnityEngine;

namespace Inktober2026.Audio
{
	/// <summary>Connects the round to a <see cref="ContinuousAudio"/>: it plays while the round is on, is held by the pause and stops otherwise.</summary>
	[RequireComponent(typeof(ContinuousAudio))]
	public class GameFlowAudio : MonoBehaviour
	{
		[SerializeField]
		private GameFlow gameFlow;

		[SerializeField]
		private ContinuousAudio audioSource;

		private void Reset()
		{
			gameFlow = FindAnyObjectByType<GameFlow>();
			audioSource = GetComponent<ContinuousAudio>();
		}

		private void OnEnable()
		{
			gameFlow.OnStateChanged += GameFlow_OnStateChanged;
			GameFlow_OnStateChanged(gameFlow.State);
		}

		private void GameFlow_OnStateChanged(GameState state)
		{
			switch (state)
			{
				case GameState.Playing:
					audioSource.Play();
					break;
				case GameState.Paused:
					audioSource.Pause();
					break;
				default:
					audioSource.Stop();
					break;
			}
		}

		private void OnDisable()
		{
			gameFlow.OnStateChanged -= GameFlow_OnStateChanged;
		}
	}
}

using System.Collections.Generic;
using Inktober2026.Game;
using UnityEngine;
using UnityEngine.Audio;

namespace Inktober2026.Audio
{
	/// <summary>
	/// Plays the peel clip for as long as the round is on. The clip is short and is not a loop, so it is played again and again:
	/// the next voice starts a random time before the previous one ends and fades in while the previous one fades out,
	/// and the pitch of every voice wanders between a minimum and a maximum along Perlin noise, so the repetition is hard to notice.
	/// A pause fades the sound out and holds it; game over and the ready state fade it out and stop it.
	/// </summary>
	public class PeelSound : MonoBehaviour
	{
		private const float PerlinLow = 0.2f;
		private const float PerlinHigh = 0.8f;

		private class Voice
		{
			public AudioSource Source;
			public float Seed;
			public float Played;
			public float Overlap;
			public float FadeInDuration;
			public bool NextStarted;
		}

		[SerializeField]
		private GameFlow gameFlow;

		[SerializeField]
		private AudioClip clip;

		[SerializeField]
		private AudioMixerGroup output;

		[Range(0f, 1f)]
		[SerializeField]
		private float volume = 1f;

		[Header("Pitch")]
		[SerializeField]
		private float minPitch = 0.9f;

		[SerializeField]
		private float maxPitch = 1.1f;

		[Tooltip("How fast the pitch wanders: Perlin noise steps per second.")]
		[SerializeField]
		private float pitchChangeSpeed = 0.3f;

		[Header("Overlap")]
		[Tooltip("The next voice starts this many seconds (a random value between min and max) before the previous one ends.")]
		[SerializeField]
		private float minOverlap = 1f;

		[SerializeField]
		private float maxOverlap = 2.5f;

		[Tooltip("Time of the fade of the whole sound when the round pauses, ends or starts.")]
		[SerializeField]
		private float stateFadeDuration = 0.4f;

		private readonly List<Voice> voices = new List<Voice>();
		private readonly Stack<AudioSource> freeSources = new Stack<AudioSource>();
		private float master;
		private bool voicesPaused;

		private void Reset()
		{
			gameFlow = FindAnyObjectByType<GameFlow>();
		}

		private void Awake()
		{
			if (gameFlow == null)
				gameFlow = FindAnyObjectByType<GameFlow>();
		}

		private void OnValidate()
		{
			maxPitch = Mathf.Max(minPitch, maxPitch);
			maxOverlap = Mathf.Max(minOverlap, maxOverlap);
		}

		private void Update()
		{
			if (clip == null || gameFlow == null)
				return;

			bool playing = gameFlow.State == GameState.Playing;
			master = Mathf.MoveTowards(master, playing ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, stateFadeDuration));

			if (playing == false && master <= 0f)
			{
				HoldOrStop(gameFlow.State == GameState.Paused);
				return;
			}

			if (voicesPaused)
				SetVoicesPaused(false);

			if (playing && voices.Count == 0)
				StartVoice(0f);

			UpdateVoices(Time.deltaTime);
		}

		private void UpdateVoices(float deltaTime)
		{
			for (int i = voices.Count - 1; i >= 0; i--)
			{
				var voice = voices[i];
				var source = voice.Source;
				voice.Played += deltaTime;

				float pitch = SamplePitch(voice.Seed);
				source.pitch = pitch;
				float remaining = (clip.length - source.time) / pitch;

				if (remaining <= 0f || source.isPlaying == false)
				{
					ReleaseVoice(i);
					continue;
				}

				if (voice.NextStarted == false && remaining <= voice.Overlap && gameFlow.State == GameState.Playing)
				{
					voice.NextStarted = true;
					StartVoice(voice.Overlap);
				}

				source.volume = volume * master * LoopCrossfade.Gain(voice.Played, remaining, voice.FadeInDuration, voice.Overlap);
			}
		}

		private void StartVoice(float fadeInDuration)
		{
			var source = freeSources.Count > 0 ? freeSources.Pop() : CreateSource();
			var voice = new Voice
			{
				Source = source,
				Seed = Random.value * 1000f,
				FadeInDuration = fadeInDuration,
				// An overlap longer than a part of the clip would make voices pile up.
				Overlap = Mathf.Min(Random.Range(minOverlap, maxOverlap), clip.length * 0.45f / Mathf.Max(0.01f, minPitch)),
			};

			source.clip = clip;
			source.time = 0f;
			source.pitch = SamplePitch(voice.Seed);
			source.volume = 0f;
			source.Play();
			voices.Add(voice);
		}

		private float SamplePitch(float seed)
		{
			float noise = Mathf.PerlinNoise(seed, Time.time * pitchChangeSpeed);
			float t = Mathf.InverseLerp(PerlinLow, PerlinHigh, noise);
			return Mathf.Lerp(minPitch, maxPitch, t);
		}

		private AudioSource CreateSource()
		{
			var source = gameObject.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.loop = false;
			source.spatialBlend = 0f;
			source.outputAudioMixerGroup = output;
			return source;
		}

		private void ReleaseVoice(int index)
		{
			var source = voices[index].Source;
			source.Stop();
			freeSources.Push(source);
			voices.RemoveAt(index);
		}

		private void HoldOrStop(bool paused)
		{
			if (paused)
			{
				SetVoicesPaused(true);
				return;
			}

			for (int i = voices.Count - 1; i >= 0; i--)
				ReleaseVoice(i);
			voicesPaused = false;
		}

		private void SetVoicesPaused(bool paused)
		{
			if (voicesPaused == paused)
				return;

			voicesPaused = paused;
			foreach (var voice in voices)
			{
				if (paused)
					voice.Source.Pause();
				else
					voice.Source.UnPause();
			}
		}
	}
}

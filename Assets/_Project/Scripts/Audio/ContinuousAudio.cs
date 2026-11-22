using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

namespace Inktober2026.Audio
{
	/// <summary>
	/// Keeps one short clip sounding without end. The clip is not a loop, so it is played again and again:
	/// the next voice starts a random time before the previous one ends and fades in while the previous one fades out,
	/// and the pitch of every voice wanders around a mean along Perlin noise (time moves along one axis, <see cref="Randomness"/> shifts along the other), so the repetition is hard to notice.
	/// Knows nothing about the game: <see cref="Play"/>, <see cref="Pause"/> and <see cref="Stop"/> fade the whole sound in and out.
	/// </summary>
	public class ContinuousAudio : MonoBehaviour
	{
		private const float PerlinLow = 0.2f;
		private const float PerlinHigh = 0.8f;
		private const float MinPitch = 0.1f;
		private const float MaxPitch = 3f;

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
		private AudioClip clip;

		[SerializeField]
		private AudioMixerGroup output;

		[Range(0f, 1f)]
		[SerializeField]
		private float volume = 1f;

		[Header("Pitch")]
		[Tooltip("The mean pitch. The pitch of a voice wanders around it.")]
		[SerializeField]
		private float pitchCenter = 1f;

		[Tooltip("How far the pitch wanders from the mean, as a part of it: 0.1 is from 0.9 to 1.1 of the mean. 0 is no wandering.")]
		[SerializeField]
		private float pitchSpread = 0.1f;

		[Tooltip("How fast the pitch wanders in time: steps along the time axis of the Perlin noise per second.")]
		[SerializeField]
		private float pitchChangeSpeed = 0.3f;

		[Tooltip("How fast the Randomness moves along the other axis of the Perlin noise: steps per second at Randomness 1.")]
		[SerializeField]
		private float randomnessSpeed = 2f;

		[Header("Filter")]
		[Tooltip("Cutoff of the low-pass filter with no muffling, in Hz. Around the top of the hearing range, so the filter does nothing.")]
		[SerializeField]
		private float openCutoff = 22000f;

		[Tooltip("Cutoff of the low-pass filter with Muffle at 1, in Hz. Lower is more muffled.")]
		[SerializeField]
		private float mutedCutoff = 700f;

		[Header("Voices")]
		[Tooltip("Size of the pool. All the voices are created once, at the start, and reused, nothing is created while playing. Two are enough for one crossfade, the rest is a reserve.")]
		[Min(2)]
		[SerializeField]
		private int maxVoices = 3;

		[Header("Overlap")]
		[Tooltip("The next voice starts this many seconds (a random value between min and max) before the previous one ends.")]
		[SerializeField]
		private float minOverlap = 1f;

		[SerializeField]
		private float maxOverlap = 2.5f;

		[Tooltip("Time of the fade of the whole sound after Play, Pause and Stop.")]
		[SerializeField]
		private float fadeDuration = 0.4f;

		private enum Mode
		{
			Stopped,
			Playing,
			Paused,
		}

		[Header("States")]
		[Tooltip("Voices that sound now, for watching in the inspector. The pool has Max Voices children in all.")]
		[SerializeField]
		private int activeVoices;

		private readonly List<Voice> voices = new List<Voice>();
		private ObjectPool<AudioSource> sourcePool;
		private float master;
		private bool voicesPaused;
		private Mode mode;
		private float noiseTime;
		private float noiseShift;

		/// <summary>
		/// Multiplies the mean pitch, 1 changes nothing. It can be changed while the sound plays and applies at once, to the voices that already sound too.
		/// </summary>
		public float PitchMultiplier { get; set; } = 1f;

		/// <summary>
		/// How fast the pitch noise is shifted sideways, from 0 (it only moves with time) up. Raising it makes the pitch change in a different way,
		/// so something that raises it while the player acts is heard as a reaction to the player.
		/// </summary>
		public float Randomness { get; set; }

		/// <summary>Stereo position of the sound, from -1 (left) to 1 (right). Applies at once.</summary>
		public float Pan { get; set; }

		/// <summary>How muffled the sound is, from 0 (clear) to 1 (the low-pass cutoff is at its lowest). Applies at once.</summary>
		public float Muffle { get; set; }

		/// <summary>Starts the sound, or resumes it after <see cref="Pause"/>, with a fade in.</summary>
		public void Play()
		{
			mode = Mode.Playing;
		}

		/// <summary>Fades the sound out and holds it where it is, so <see cref="Play"/> goes on from there.</summary>
		public void Pause()
		{
			if (mode == Mode.Playing)
				mode = Mode.Paused;
		}

		/// <summary>Fades the sound out and drops all voices, so <see cref="Play"/> starts from the beginning.</summary>
		public void Stop()
		{
			mode = Mode.Stopped;
		}

		private void Awake()
		{
			sourcePool = new ObjectPool<AudioSource>(
				createFunc: CreateSource,
				actionOnGet: source => source.gameObject.SetActive(true),
				actionOnRelease: source =>
				{
					source.Stop();
					source.gameObject.SetActive(false);
				},
				actionOnDestroy: source => Destroy(source.gameObject),
				collectionCheck: true,
				defaultCapacity: maxVoices,
				maxSize: maxVoices);

			// Create all the voices now, so nothing is created while playing.
			var warmUp = new List<AudioSource>(maxVoices);
			for (int i = 0; i < maxVoices; i++)
				warmUp.Add(sourcePool.Get());
			foreach (var source in warmUp)
				sourcePool.Release(source);
		}

		private void OnValidate()
		{
			pitchCenter = Mathf.Max(0.01f, pitchCenter);
			pitchSpread = Mathf.Clamp01(pitchSpread);
			openCutoff = Mathf.Max(10f, openCutoff);
			mutedCutoff = Mathf.Clamp(mutedCutoff, 10f, openCutoff);
			maxOverlap = Mathf.Max(minOverlap, maxOverlap);
			maxVoices = Mathf.Max(2, maxVoices);
		}

		private void Update()
		{
			if (clip == null)
				return;

			bool playing = mode == Mode.Playing;
			master = Mathf.MoveTowards(master, playing ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration));

			if (playing == false && master <= 0f)
			{
				HoldOrStop(mode == Mode.Paused);
				return;
			}

			if (voicesPaused)
				SetVoicesPaused(false);

			if (playing && voices.Count == 0)
				StartVoice(0f);

			UpdateVoices(Time.deltaTime);
			activeVoices = voices.Count;
		}

		private void UpdateVoices(float deltaTime)
		{
			noiseTime += deltaTime * pitchChangeSpeed;
			noiseShift += deltaTime * randomnessSpeed * Mathf.Max(0f, Randomness);

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

				// An overlap longer than a part of the clip would make voices pile up.
				float overlap = Mathf.Min(voice.Overlap, clip.length / pitch * 0.45f);
				if (voice.NextStarted == false && remaining <= overlap && mode == Mode.Playing)
					voice.NextStarted = StartVoice(overlap);

				source.volume = volume * master * LoopCrossfade.Gain(voice.Played, remaining, voice.FadeInDuration, overlap);
				ApplyPanAndFilter(source);
			}
		}

		private bool StartVoice(float fadeInDuration)
		{
			// The pool is empty only when every voice still sounds. Then the next voice waits, the sound goes on with the ones that play.
			if (sourcePool.CountActive >= maxVoices)
				return false;

			var source = sourcePool.Get();
			var voice = new Voice
			{
				Source = source,
				Seed = Random.value * 1000f,
				FadeInDuration = fadeInDuration,
				Overlap = Random.Range(minOverlap, maxOverlap),
			};

			source.clip = clip;
			source.time = 0f;
			source.pitch = SamplePitch(voice.Seed);
			source.volume = 0f;
			source.Play();
			voices.Add(voice);
			return true;
		}

		private float SamplePitch(float seed)
		{
			// Every voice has its own place on the noise, the time moves along one axis and the randomness shifts along the other.
			float noise = Mathf.PerlinNoise(seed + noiseShift, noiseTime);
			float deviation = Mathf.InverseLerp(PerlinLow, PerlinHigh, noise) * 2f - 1f;
			float pitch = pitchCenter * PitchMultiplier * (1f + pitchSpread * Mathf.Clamp(deviation, -1f, 1f));
			return Mathf.Clamp(pitch, MinPitch, MaxPitch);
		}

		private void ApplyPanAndFilter(AudioSource source)
		{
			source.panStereo = Mathf.Clamp(Pan, -1f, 1f);

			// The cutoff is perceived on a logarithmic scale, so it is interpolated in it.
			float open = Mathf.Max(10f, openCutoff);
			float muted = Mathf.Clamp(mutedCutoff, 10f, open);
			source.GetComponent<AudioLowPassFilter>().cutoffFrequency = open * Mathf.Pow(muted / open, Mathf.Clamp01(Muffle));
		}

		// A filter works on the sound of the object it is on, so every voice gets an object of its own.
		private AudioSource CreateSource()
		{
			var voiceObject = new GameObject("Voice");
			voiceObject.transform.SetParent(transform, false);
			voiceObject.AddComponent<AudioLowPassFilter>();
			var source = voiceObject.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.loop = false;
			source.spatialBlend = 0f;
			source.outputAudioMixerGroup = output;
			return source;
		}

		private void ReleaseVoice(int index)
		{
			var source = voices[index].Source;
			sourcePool.Release(source);
			voices.RemoveAt(index);
			activeVoices = voices.Count;
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

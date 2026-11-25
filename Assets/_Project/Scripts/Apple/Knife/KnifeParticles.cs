using Inktober2026.Game;
using UnityEngine;

namespace Inktober2026.Apple.Knife
{
    public class KnifeParticles : MonoBehaviour
    {
        [SerializeField]
        private ParticleSystem particles;

        [SerializeField]
        private GameFlow gameFlow;

        [Header("States")]
        [SerializeField]
        private float emissionRate;

        private void Awake()
        {
            emissionRate = particles.emission.rateOverTime.constant;
        }

        private void OnEnable()
        {
            gameFlow.OnStateChanged += GameFlow_OnStateChanged;
        }

        private void GameFlow_OnStateChanged(GameState state)
        {
            bool isPlaying = state == GameState.Playing;
            var emission = particles.emission;
            if (particles.isPlaying == false && isPlaying)
                particles.Play();

            emission.rateOverTime = isPlaying ? emissionRate : 0;
        }

        private void OnDisable()
        {
            gameFlow.OnStateChanged -= GameFlow_OnStateChanged;
        }
    }
}

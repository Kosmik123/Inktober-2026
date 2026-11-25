using Inktober2026.Apple.Peel;
using Inktober2026.Game;
using UnityEngine;

namespace Inktober2026.Apple.Knife
{
    [RequireComponent(typeof(ParticleSystem))]
    public class KnifeParticles : MonoBehaviour
    {
        private ParticleSystem particles;

        [SerializeField]
        private GameFlow gameFlow;

        [SerializeField]
        private KnifeController knife;

        [SerializeField]
        private PeelTrail peelTrail;

        [SerializeField]
        private float baseEmissionRate = 10;

        private void Reset()
        {
            gameFlow = FindAnyObjectByType<GameFlow>();
            knife = GetComponentInParent<KnifeController>();
        }

        private void Awake()
        {
            particles = GetComponent<ParticleSystem>();
        }

        private void OnEnable()
        {
            gameFlow.OnStateChanged += GameFlow_OnStateChanged;
        }

        private void GameFlow_OnStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Paused:
                    particles.Pause();
                    break;

                case GameState.Playing:
                    particles.Play();
                    break;

                default:
                    particles.Stop();
                    break;
            }
            UpdateParticlesProperties();
        }

        private void Update()
        {
            UpdateParticlesProperties();
        }

        private void UpdateParticlesProperties()
        {
            float peelWidth = peelTrail.Width;
            var shape = particles.shape;
            shape.scale = new Vector3(peelWidth, shape.scale.y, shape.scale.z);

            var emission = particles.emission;
            emission.rateOverTime = baseEmissionRate * knife.SpeedScale;
        }

        private void OnDisable()
        {
            gameFlow.OnStateChanged -= GameFlow_OnStateChanged;
        }
    }
}

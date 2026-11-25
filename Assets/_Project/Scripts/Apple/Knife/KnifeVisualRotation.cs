using UnityEngine;

namespace Inktober2026.Apple.Knife
{
    public class KnifeVisualRotation : MonoBehaviour
    {
        [SerializeField]
        private KnifeController knife;

        [SerializeField]
        private Vector3 horizontalMoveRotationScale;
        
        [SerializeField]
        private Vector3 verticalMoveRotationScale;

        [SerializeField]
        private float maxTurnSpeed;

        [Header("States")]
        public float turnRate;
        public float throttle;

        private void Reset()
        {
            knife = GetComponentInParent<KnifeController>();
        }

        private void Update()
        {
            turnRate = knife.TurnRate;
            throttle = knife.Throttle;
            
            var horizontalRotation = turnRate * horizontalMoveRotationScale;
            var verticalRotation = throttle * verticalMoveRotationScale;

            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation, 
                Quaternion.Euler(horizontalRotation) * Quaternion.Euler(verticalRotation),
                maxTurnSpeed * Time.deltaTime);
        }
    }
}

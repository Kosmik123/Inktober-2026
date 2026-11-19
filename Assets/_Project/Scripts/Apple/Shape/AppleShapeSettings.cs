using System;
using UnityEngine;

namespace Inktober.Apple.Shape
{
    /// <summary>
    /// Describes the shape of the apple as a deformation of a sphere: the radius in every direction is the base radius
    /// times a factor made of a wider shoulder near the north pole, a dimple at each pole and a random unevenness.
    /// Angles are measured from the north pole (0 degrees) to the south pole (180 degrees). Depths are shares of the radius.
    /// </summary>
    [Serializable]
    public class AppleShapeSettings
    {
        [Tooltip("Radius of the sphere the apple is made from, in local units. 0.5 is the radius of the default Unity sphere.")]
        [SerializeField]
        private float radius = 0.5f;
        public float Radius => radius;

        [Header("Shoulder")]
        [Tooltip("How much wider the apple is around the shoulder (a share of the radius).")]
        [SerializeField]
        private float shoulderWidening = 0.07f;
        public float ShoulderWidening => shoulderWidening;

        [Tooltip("Angle from the north pole where the shoulder is the widest, in degrees.")]
        [SerializeField]
        private float shoulderAngle = 55f;
        public float ShoulderAngle => shoulderAngle;

        [Tooltip("How wide the shoulder is, in degrees (the standard deviation of the bump).")]
        [SerializeField]
        private float shoulderSpread = 45f;
        public float ShoulderSpread => shoulderSpread;

        [Header("Dimples")]
        [Tooltip("Depth of the dimple at the north pole (a share of the radius).")]
        [SerializeField]
        private float topDimpleDepth = 0.14f;
        public float TopDimpleDepth => topDimpleDepth;

        [Tooltip("Width of the dimple at the north pole, in degrees.")]
        [SerializeField]
        private float topDimpleSpread = 22f;
        public float TopDimpleSpread => topDimpleSpread;

        [Tooltip("Depth of the dimple at the south pole (a share of the radius).")]
        [SerializeField]
        private float bottomDimpleDepth = 0.08f;
        public float BottomDimpleDepth => bottomDimpleDepth;

        [Tooltip("Width of the dimple at the south pole, in degrees.")]
        [SerializeField]
        private float bottomDimpleSpread = 18f;
        public float BottomDimpleSpread => bottomDimpleSpread;

        [Header("Unevenness")]
        [Tooltip("Strength of the random unevenness of the surface (a share of the radius). It depends on the seed.")]
        [SerializeField]
        private float unevenness = 0.025f;
        public float Unevenness => unevenness;

        [Tooltip("Size of the unevenness: higher values give smaller and more frequent bumps.")]
        [SerializeField]
        private float unevennessScale = 1.3f;
        public float UnevennessScale => unevennessScale;

        public AppleShapeSettings()
        {
        }

        public AppleShapeSettings(float radius, float shoulderWidening, float shoulderAngle, float shoulderSpread, float topDimpleDepth, float topDimpleSpread, float bottomDimpleDepth, float bottomDimpleSpread, float unevenness, float unevennessScale)
        {
            this.radius = radius;
            this.shoulderWidening = shoulderWidening;
            this.shoulderAngle = shoulderAngle;
            this.shoulderSpread = shoulderSpread;
            this.topDimpleDepth = topDimpleDepth;
            this.topDimpleSpread = topDimpleSpread;
            this.bottomDimpleDepth = bottomDimpleDepth;
            this.bottomDimpleSpread = bottomDimpleSpread;
            this.unevenness = unevenness;
            this.unevennessScale = unevennessScale;
        }
    }
}

using UnityEngine;

namespace Inktober2026.Apple.Knife
{
    /// <summary>
    /// The second input axis as a lever from -1 (pulled all the way back) to 1 (pushed all the way forward), 0 in the middle.
    /// Pushing forward makes the knife faster and the peel wider, pulling back makes it slower and the peel narrower.
    /// The lever stays where it was left, unless a return rate is given.
    /// </summary>
    public class KnifeThrottle
    {
        /// <summary>Position of the lever, from -1 to 1.</summary>
        public float Value { get; private set; }

        /// <summary>Moves the lever by the given part of its whole range (1 is from the middle to the end).</summary>
        public void Add(float amount)
        {
            Value = Mathf.Clamp(Value + amount, -1f, 1f);
        }

        /// <summary>Lets the lever fall back towards the middle. The rate is the part of the range per second, 0 does nothing.</summary>
        public void Relax(float rate, float deltaTime)
        {
            Value = Mathf.MoveTowards(Value, 0f, Mathf.Max(0f, rate) * deltaTime);
        }

        public void Reset()
        {
            Value = 0f;
        }

        /// <summary>
        /// Turns the lever position into a multiplier: 1 in the middle, <paramref name="forward"/> at 1 and <paramref name="back"/> at -1,
        /// in a straight line in between.
        /// </summary>
        public static float Scale(float value, float back, float forward)
        {
            float clamped = Mathf.Clamp(value, -1f, 1f);
            return clamped >= 0f ? Mathf.Lerp(1f, forward, clamped) : Mathf.Lerp(1f, back, -clamped);
        }
    }
}

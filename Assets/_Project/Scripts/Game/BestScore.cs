using UnityEngine;

namespace Inktober2026.Game
{
    /// <summary>
    /// The longest peel ever made, in world units, kept in PlayerPrefs (in WebGL they live in the browser storage).
    /// </summary>
    public static class BestScore
    {
        private const string Key = "Inktober.Apple.BestLength";

        /// <summary>The longest peel so far, or 0 when nothing was saved yet.</summary>
        public static float Length => PlayerPrefs.GetFloat(Key, 0f);

        /// <summary>Saves the length when it is longer than the current record. Returns true for a new record.</summary>
        public static bool TrySet(float length)
        {
            if (length <= Length)
                return false;

            PlayerPrefs.SetFloat(Key, length);
            PlayerPrefs.Save();
            return true;
        }
    }
}

using UnityEngine;

namespace Inktober2026.Apple.Shape
{
    /// <summary>
    /// Where a point of the apple lies on its textures. The apple is mapped like the default Unity sphere
    /// (see <see cref="AppleMeshBuilder"/>), so the coordinates depend only on the direction from the center of the apple.
    /// </summary>
    public static class AppleUv
    {
        /// <summary>
        /// U goes around the Y axis from 0 at the -X side towards -Z, V is 1 at the north pole and 0 at the south pole.
        /// </summary>
        /// <param name="direction">Direction from the center of the apple, in its local space. It does not have to be a unit vector.</param>
        public static Vector2 FromDirection(Vector3 direction)
        {
            var unit = direction.normalized;

            float phi = Mathf.Atan2(-unit.z, -unit.x);
            if (phi < 0f)
                phi += 2f * Mathf.PI;

            float theta = Mathf.Acos(Mathf.Clamp(unit.y, -1f, 1f));
            return new Vector2(phi / (2f * Mathf.PI), 1f - theta / Mathf.PI);
        }
    }
}

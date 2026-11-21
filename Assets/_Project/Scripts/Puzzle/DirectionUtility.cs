using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Puzzle
{
    public static class DirectionUtility
    {
        private static readonly Vector3Int[] all =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.forward,
            Vector3Int.back,
        };

        /// <summary>The six axis directions a piece can move in.</summary>
        public static IReadOnlyList<Vector3Int> All => all;

        public static bool IsAxisDirection(Vector3Int direction)
        {
            return Mathf.Abs(direction.x) + Mathf.Abs(direction.y) + Mathf.Abs(direction.z) == 1;
        }
    }
}

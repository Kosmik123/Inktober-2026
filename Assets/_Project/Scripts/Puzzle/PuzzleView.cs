using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Puzzle
{
    /// <summary>
    /// Shows the arrows of the <see cref="PuzzleBoard"/> as objects under this one: it builds them when a grid is created
    /// and takes them away when a piece is removed. The relic cell sits at the local origin of this object.
    /// Every arrow gets its own mesh and its own color.
    /// </summary>
    public class PuzzleView : MonoBehaviour
    {
        private const float GoldenRatio = 0.618034f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField]
        private PuzzleBoard board;

        [SerializeField]
        private Material arrowMaterial;

        [Tooltip("Edge of the body of an arrow, in cells.")]
        [SerializeField, Range(0.2f, 0.9f)]
        private float thickness = 0.5f;

        [Tooltip("Radius of the base of the arrow head, in cells.")]
        [SerializeField, Range(0.2f, 0.5f)]
        private float headRadius = 0.42f;

        [Header("Colors")]
        [SerializeField, Range(0f, 1f)]
        private float saturation = 0.55f;

        [SerializeField, Range(0f, 1f)]
        private float brightness = 0.95f;

        private readonly Dictionary<ArrowPiece, GameObject> pieceObjects = new Dictionary<ArrowPiece, GameObject>();
        private MaterialPropertyBlock propertyBlock;

        private void Reset()
        {
            board = FindFirstObjectByType<PuzzleBoard>();
        }

        private void OnEnable()
        {
            board.OnGridCreated += Board_OnGridCreated;
            board.OnPieceRemoved += Board_OnPieceRemoved;
        }

        public GameObject GetPieceObject(ArrowPiece piece)
        {
            return pieceObjects.TryGetValue(piece, out var pieceObject) ? pieceObject : null;
        }

        /// <summary>Replaces the arrows shown with the ones of the grid.</summary>
        public void Show(ArrowGrid grid)
        {
            Clear();
            propertyBlock ??= new MaterialPropertyBlock();
            var offset = -(Vector3)grid.RelicCell;
            for (int i = 0; i < grid.Pieces.Count; i++)
            {
                var piece = grid.Pieces[i];
                var pieceObject = new GameObject($"Arrow {i}");
                pieceObject.transform.SetParent(transform, false);
                pieceObject.transform.localPosition = offset;

                var meshFilter = pieceObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = ArrowMeshUtility.Build(piece, thickness, headRadius);

                var meshRenderer = pieceObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = arrowMaterial;
                propertyBlock.SetColor(BaseColorId, Color.HSVToRGB(i * GoldenRatio % 1f, saturation, brightness));
                meshRenderer.SetPropertyBlock(propertyBlock);

                pieceObjects.Add(piece, pieceObject);
            }
        }

        public void Clear()
        {
            foreach (var pieceObject in pieceObjects.Values)
                DestroyPieceObject(pieceObject);

            pieceObjects.Clear();
        }

        /// <summary>Finds the piece of the grid that a ray in the world hits first, through the cells of the grid.</summary>
        public bool TryPick(Ray worldRay, ArrowGrid grid, out ArrowPiece piece)
        {
            var origin = transform.InverseTransformPoint(worldRay.origin) + (Vector3)grid.RelicCell;
            var direction = transform.InverseTransformDirection(worldRay.direction);
            return grid.TryRaycast(origin, direction, out piece);
        }

        /// <summary>Takes the object of the piece away.</summary>
        public void Remove(ArrowPiece piece)
        {
            if (pieceObjects.Remove(piece, out var pieceObject) == false)
                return;

            DestroyPieceObject(pieceObject);
        }

        private void Board_OnGridCreated(ArrowGrid grid)
        {
            Show(grid);
        }

        private void Board_OnPieceRemoved(ArrowPiece piece)
        {
            Remove(piece);
        }

        private static void DestroyPieceObject(GameObject pieceObject)
        {
            if (pieceObject == null)
                return;

            var meshFilter = pieceObject.GetComponent<MeshFilter>();
            if (Application.isPlaying)
            {
                Destroy(meshFilter.sharedMesh);
                Destroy(pieceObject);
                return;
            }

            DestroyImmediate(meshFilter.sharedMesh);
            DestroyImmediate(pieceObject);
        }

        private void OnDisable()
        {
            board.OnGridCreated -= Board_OnGridCreated;
            board.OnPieceRemoved -= Board_OnPieceRemoved;
        }
    }
}

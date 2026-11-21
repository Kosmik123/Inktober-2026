using System.Collections.Generic;
using UnityEngine;

namespace Inktober2026.Puzzle
{
    /// <summary>
    /// Shows the pieces of the <see cref="PuzzleBoard"/> as stones under this object: a stone for every cell of a piece,
    /// and a pointed one for its head. The stones are rigid; they only slide. A removed piece slides out of the grid along its own
    /// path, and a blocked piece bumps into what blocks it. The relic cell sits at the local origin of this object.
    /// </summary>
    public class PuzzleView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField]
        private PuzzleBoard board;

        [SerializeField]
        private Material stoneMaterial;

        [Header("Meshes")]
        [Tooltip("Mesh of a stone that is not the head. Stays empty to get a cube with chamfered edges.")]
        [SerializeField]
        private Mesh blockMesh;

        [Tooltip("Mesh of the head; it must point along +Z. Stays empty to get a pyramid with a thick base.")]
        [SerializeField]
        private Mesh headMesh;

        [Tooltip("Edge of a generated stone, in cells. Less than 1 leaves gaps between the stones.")]
        [SerializeField, Range(0.5f, 1f)]
        private float stoneSize = 0.92f;

        [Tooltip("How far the chamfer cuts into the edges of a generated stone, in cells.")]
        [SerializeField, Range(0.01f, 0.2f)]
        private float bevel = 0.07f;

        [Header("Colors")]
        [SerializeField]
        private Color stoneColor = new Color(0.55f, 0.55f, 0.54f);

        [Tooltip("The tint of a piece is one of these, washed out toward the stone color.")]
        [SerializeField]
        private Color[] tints =
        {
            new Color(0.80f, 0.42f, 0.30f),
            new Color(0.25f, 0.68f, 0.64f),
            new Color(0.80f, 0.64f, 0.32f),
            new Color(0.48f, 0.64f, 0.45f),
            new Color(0.42f, 0.50f, 0.68f),
            new Color(0.70f, 0.50f, 0.58f),
        };

        [Range(0f, 1f)]
        [SerializeField]
        private float tintStrength = 0.45f;

        [Tooltip("How much the brightness of a single stone differs from the rest of its piece.")]
        [Range(0f, 0.3f)]
        [SerializeField]
        private float brightnessVariation = 0.08f;

        [Header("Motion")]
        [Tooltip("How many cells a removed piece travels in a second.")]
        [SerializeField]
        private float slideSpeed = 10f;

        [Tooltip("How far a blocked piece moves toward what blocks it, in cells, when there is room.")]
        [SerializeField]
        private float bumpDistance = 0.3f;

        [Tooltip("The same, when the piece is right next to what blocks it.")]
        [SerializeField]
        private float bumpDistanceWhenTouching = 0.08f;

        [SerializeField]
        private float bumpDuration = 0.2f;

        private readonly Dictionary<ArrowPiece, GameObject> pieceObjects = new Dictionary<ArrowPiece, GameObject>();
        private readonly Dictionary<ArrowPiece, PieceMotion> bumps = new Dictionary<ArrowPiece, PieceMotion>();
        private readonly List<SlidingPiece> slides = new List<SlidingPiece>();
        private readonly List<ArrowPiece> finishedBumps = new List<ArrowPiece>();
        private ArrowGrid grid;
        private MaterialPropertyBlock propertyBlock;
        private Mesh generatedBlockMesh;
        private Mesh generatedHeadMesh;

        private void Reset()
        {
            board = FindFirstObjectByType<PuzzleBoard>();
        }

        private void OnEnable()
        {
            board.OnGridCreated += Board_OnGridCreated;
            board.OnPieceRemoved += Board_OnPieceRemoved;
            board.OnRemovalBlocked += Board_OnRemovalBlocked;
        }

        private void Update()
        {
            AdvanceSlides();
            AdvanceBumps();
        }

        public GameObject GetPieceObject(ArrowPiece piece)
        {
            return pieceObjects.TryGetValue(piece, out var pieceObject) ? pieceObject : null;
        }

        /// <summary>Replaces the pieces shown with the ones of the grid.</summary>
        public void Show(ArrowGrid newGrid)
        {
            Clear();
            grid = newGrid;
            propertyBlock ??= new MaterialPropertyBlock();
            if (blockMesh == null && generatedBlockMesh == null)
                generatedBlockMesh = StoneMeshUtility.BuildBlock(stoneSize, bevel);

            if (headMesh == null && generatedHeadMesh == null)
                generatedHeadMesh = StoneMeshUtility.BuildHead(stoneSize, stoneSize, stoneSize * 0.25f);

            var blockSource = blockMesh ? blockMesh : generatedBlockMesh;
            var headSource = headMesh ? headMesh : generatedHeadMesh;

            var offset = -(Vector3)grid.RelicCell;
            for (int i = 0; i < grid.Pieces.Count; i++)
            {
                var piece = grid.Pieces[i];
                var pieceObject = new GameObject($"Piece {i}");
                pieceObject.transform.SetParent(transform, false);
                pieceObject.transform.localPosition = offset;

                var tint = Color.Lerp(stoneColor, tints[i % tints.Length], tintStrength);
                for (int j = 0; j < piece.Cells.Count; j++)
                {
                    bool isHead = j == piece.Cells.Count - 1;
                    var stone = CreateStone(pieceObject.transform, isHead ? headSource : blockSource, isHead ? "Head" : $"Stone {j}");
                    stone.transform.localPosition = (Vector3)piece.Cells[j];
                    if (isHead)
                        stone.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, (Vector3)piece.Direction);

                    SetColor(stone.GetComponent<MeshRenderer>(), tint, i, j);
                }

                pieceObjects.Add(piece, pieceObject);
            }
        }

        public void Clear()
        {
            foreach (var pieceObject in pieceObjects.Values)
                DestroyObject(pieceObject);

            foreach (var slide in slides)
                DestroyObject(slide.PieceObject);

            pieceObjects.Clear();
            slides.Clear();
            bumps.Clear();
        }

        /// <summary>Finds the piece of the grid that a ray in the world hits first, through the cells of the grid.</summary>
        public bool TryPick(Ray worldRay, ArrowGrid pickedGrid, out ArrowPiece piece)
        {
            var origin = transform.InverseTransformPoint(worldRay.origin) + (Vector3)pickedGrid.RelicCell;
            var direction = transform.InverseTransformDirection(worldRay.direction);
            return pickedGrid.TryRaycast(origin, direction, out piece);
        }

        /// <summary>Takes the object of the piece away at once.</summary>
        public void Remove(ArrowPiece piece)
        {
            bumps.Remove(piece);
            if (pieceObjects.Remove(piece, out var pieceObject) == false)
                return;

            DestroyObject(pieceObject);
        }

        /// <summary>Sends the piece out of the grid along its own path; its object is destroyed when the last stone is out.</summary>
        public void SlideOut(ArrowPiece piece)
        {
            bumps.Remove(piece);
            if (pieceObjects.Remove(piece, out var pieceObject) == false)
                return;

            var path = new PiecePath(piece, grid.Size);
            var motion = PieceMotion.CreateSlide(path, GetStones(pieceObject), slideSpeed);
            slides.Add(new SlidingPiece(pieceObject, motion));
        }

        /// <summary>Pushes the piece a little toward what blocks it and back.</summary>
        public void Bump(ArrowPiece piece)
        {
            if (pieceObjects.TryGetValue(piece, out var pieceObject) == false)
                return;

            if (bumps.TryGetValue(piece, out var running))
                running.Advance(float.MaxValue);

            var path = new PiecePath(piece, grid.Size);
            float distance = CountFreeCellsAhead(piece) > 0 ? bumpDistance : bumpDistanceWhenTouching;
            bumps[piece] = PieceMotion.CreateBump(path, GetStones(pieceObject), distance, bumpDuration);
        }

        private int CountFreeCellsAhead(ArrowPiece piece)
        {
            int count = 0;
            var cell = piece.Head + piece.Direction;
            while (grid.IsInside(cell) && grid.IsEmpty(cell))
            {
                count++;
                cell += piece.Direction;
            }

            return count;
        }

        private GameObject CreateStone(Transform parent, Mesh mesh, string stoneName)
        {
            var stone = new GameObject(stoneName);
            stone.transform.SetParent(parent, false);
            stone.AddComponent<MeshFilter>().sharedMesh = mesh;
            stone.AddComponent<MeshRenderer>().sharedMaterial = stoneMaterial;
            return stone;
        }

        private void SetColor(MeshRenderer meshRenderer, Color tint, int pieceIndex, int stoneIndex)
        {
            // A fixed pseudo-random shade per stone, so a new round looks the same on every machine.
            float noise = Mathf.Repeat(Mathf.Sin(pieceIndex * 12.9898f + stoneIndex * 78.233f) * 43758.5453f, 1f) * 2f - 1f;
            var shade = tint * (1f + noise * brightnessVariation);
            shade.a = 1f;
            propertyBlock.SetColor(BaseColorId, shade);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private static Transform[] GetStones(GameObject pieceObject)
        {
            var stones = new Transform[pieceObject.transform.childCount];
            for (int i = 0; i < stones.Length; i++)
                stones[i] = pieceObject.transform.GetChild(i);

            return stones;
        }

        private void AdvanceSlides()
        {
            for (int i = slides.Count - 1; i >= 0; i--)
            {
                slides[i].Motion.Advance(Time.deltaTime);
                if (slides[i].Motion.IsFinished == false)
                    continue;

                DestroyObject(slides[i].PieceObject);
                slides.RemoveAt(i);
            }
        }

        private void AdvanceBumps()
        {
            if (bumps.Count == 0)
                return;

            finishedBumps.Clear();
            foreach (var pair in bumps)
            {
                pair.Value.Advance(Time.deltaTime);
                if (pair.Value.IsFinished)
                    finishedBumps.Add(pair.Key);
            }

            foreach (var piece in finishedBumps)
                bumps.Remove(piece);
        }

        private void Board_OnGridCreated(ArrowGrid newGrid)
        {
            Show(newGrid);
        }

        private void Board_OnPieceRemoved(ArrowPiece piece)
        {
            SlideOut(piece);
        }

        private void Board_OnRemovalBlocked(ArrowPiece piece)
        {
            Bump(piece);
        }

        private static void DestroyObject(GameObject pieceObject)
        {
            if (pieceObject == null)
                return;

            if (Application.isPlaying)
                Destroy(pieceObject);
            else
                DestroyImmediate(pieceObject);
        }

        private static void DestroyMesh(Mesh mesh)
        {
            if (mesh == null)
                return;

            if (Application.isPlaying)
                Destroy(mesh);
            else
                DestroyImmediate(mesh);
        }

        private void OnDisable()
        {
            board.OnGridCreated -= Board_OnGridCreated;
            board.OnPieceRemoved -= Board_OnPieceRemoved;
            board.OnRemovalBlocked -= Board_OnRemovalBlocked;
        }

        private void OnDestroy()
        {
            DestroyMesh(generatedBlockMesh);
            DestroyMesh(generatedHeadMesh);
        }

        private readonly struct SlidingPiece
        {
            public SlidingPiece(GameObject pieceObject, PieceMotion motion)
            {
                PieceObject = pieceObject;
                Motion = motion;
            }

            public GameObject PieceObject { get; }

            public PieceMotion Motion { get; }
        }
    }
}

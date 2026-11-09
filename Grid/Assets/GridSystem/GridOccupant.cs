using UnityEngine;

namespace GridTutorial
{
    [DefaultExecutionOrder(-100)]
    public sealed class GridOccupant : MonoBehaviour
    {
        [SerializeField] GridBoard board;
        [SerializeField] OccupantKind kind;
        [SerializeField] Vector2Int initialCell;
        [SerializeField] float elevation = .5f;
        public GridBoard Board => board;
        public Vector2Int Cell { get; private set; }
        public OccupantKind Kind => kind;
        public float Elevation => elevation;
        bool registered;
        void OnEnable()
        {
            if (!Application.isPlaying) return;
            var destination = registered ? Cell : initialCell;
            if (!board || !board.TryPlace(this, destination))
            {
                Debug.LogError($"{name}: Grid registration failed at {destination}. Check Board and overlapping cells.", this);
                return;
            }
            registered = true;
            transform.position = board.Center(Cell, elevation);
        }
        internal void SetCell(GridBoard value, Vector2Int cell) { board = value; Cell = cell; }
        void OnDisable() { if (board) board.Release(this); }
    }
}

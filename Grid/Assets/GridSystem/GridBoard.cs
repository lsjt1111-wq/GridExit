using System;
using UnityEngine;

namespace GridTutorial
{
    public enum OccupantKind { Obstacle, Monster, Player }

    public sealed class GridCell
    {
        public Vector2Int Coordinate { get; }
        public int Number { get; }
        public GridOccupant Occupant { get; internal set; }
        public GridBuilding Building { get; internal set; }
        public bool IsBlocked => Occupant != null || Building != null;
        public GridCell(Vector2Int coordinate, int number) { Coordinate = coordinate; Number = number; }
    }

    // Local XZ plane. Transform origin is the lower-left corner.
    [DefaultExecutionOrder(-200)]
    public sealed class GridBoard : MonoBehaviour
    {
        [SerializeField, Range(1, 50)] int width = 10;
        [SerializeField, Range(1, 50)] int height = 8;
        [SerializeField, Min(.1f)] float cellSize = 1;
        GridCell[,] cells;
        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public event Action Changed;
        void Awake() => EnsureCells();
        void EnsureCells()
        {
            if (cells != null) return;
            cells = new GridCell[width, height];
            for (int z = 0; z < height; z++)
                for (int x = 0; x < width; x++)
                    cells[x, z] = new GridCell(new Vector2Int(x, z), z * width + x + 1);
        }
        public bool Contains(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < width && p.y < height;
        public GridCell GetCell(Vector2Int p) { EnsureCells(); return Contains(p) ? cells[p.x, p.y] : null; }
        public Vector2Int WorldToCell(Vector3 world)
        {
            var p = transform.InverseTransformPoint(world);
            return new Vector2Int(Mathf.FloorToInt(p.x / cellSize), Mathf.FloorToInt(p.z / cellSize));
        }
        public Vector3 Center(Vector2Int p, float elevation = 0) => transform.TransformPoint(new Vector3((p.x + .5f) * cellSize, elevation, (p.y + .5f) * cellSize));
        public bool CanEnter(Vector2Int p, GridOccupant self = null)
        {
            var cell = GetCell(p);
            return cell != null && cell.Building == null && (cell.Occupant == null || cell.Occupant == self);
        }
        public bool CanBuild(Vector2Int anchor, Vector2Int size, out string reason)
        {
            reason = "Outside grid";
            if (size.x < 1 || size.y < 1 || !Contains(anchor) || !Contains(anchor + size - Vector2Int.one)) return false;
            for (int z = 0; z < size.y; z++) for (int x = 0; x < size.x; x++)
                if (GetCell(anchor + new Vector2Int(x, z)).IsBlocked) { reason = "Cell occupied"; return false; }
            reason = "Can build";
            return true;
        }
        internal bool RegisterBuilding(GridBuilding building, Vector2Int anchor, Vector2Int size)
        {
            if (!building || !CanBuild(anchor, size, out _)) return false;
            for (int z = 0; z < size.y; z++) for (int x = 0; x < size.x; x++)
                GetCell(anchor + new Vector2Int(x, z)).Building = building;
            Changed?.Invoke();
            return true;
        }
        internal void ReleaseBuilding(GridBuilding building, Vector2Int anchor, Vector2Int size)
        {
            if (cells == null) return;
            for (int z = 0; z < size.y; z++) for (int x = 0; x < size.x; x++)
            {
                var cell = GetCell(anchor + new Vector2Int(x, z));
                if (cell != null && cell.Building == building) cell.Building = null;
            }
            Changed?.Invoke();
        }
        public bool TryPlace(GridOccupant owner, Vector2Int destination)
        {
            if (!owner || !CanEnter(destination, owner)) return false;
            if (owner.Board != null && owner.Board != this) return false;
            Release(owner);
            GetCell(destination).Occupant = owner;
            owner.SetCell(this, destination);
            Changed?.Invoke();
            return true;
        }
        public void Release(GridOccupant owner)
        {
            if (cells == null || owner == null) return;
            if (owner.Board == this)
            {
                var cell = GetCell(owner.Cell);
                if (cell != null && cell.Occupant == owner) { cell.Occupant = null; Changed?.Invoke(); }
            }
        }
        void OnDrawGizmos()
        {
            Gizmos.color = Color.gray;
            for (int x = 0; x <= width; x++) Gizmos.DrawLine(transform.TransformPoint(x * cellSize, 0, 0), transform.TransformPoint(x * cellSize, 0, height * cellSize));
            for (int z = 0; z <= height; z++) Gizmos.DrawLine(transform.TransformPoint(0, 0, z * cellSize), transform.TransformPoint(width * cellSize, 0, z * cellSize));
        }
    }
}

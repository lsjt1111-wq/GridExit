using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridTutorial
{
    [RequireComponent(typeof(GridOccupant))]
    public sealed class GridMover : MonoBehaviour
    {
        [SerializeField, Min(.1f)] float speed = 3;
        GridOccupant occupant;
        public bool IsMoving { get; private set; }
        void Awake() => occupant = GetComponent<GridOccupant>();
        public bool MoveTo(Vector2Int destination)
        {
            if (IsMoving || !occupant.Board) return false;
            var path = GridPathfinder.Find(occupant.Board, occupant.Cell, destination, occupant);
            if (path.Count < 2) return false;
            StartCoroutine(Follow(path));
            return true;
        }
        IEnumerator Follow(List<Vector2Int> path)
        {
            IsMoving = true;
            var board = occupant.Board;
            for (int i = 1; i < path.Count; i++)
            {
                if (!occupant.isActiveAndEnabled) { IsMoving = false; yield break; }
                // Reserve the destination before interpolating; another unit cannot enter it.
                if (!board.TryPlace(occupant, path[i])) break;
                var target = board.Center(path[i], occupant.Elevation);
                while ((transform.position - target).sqrMagnitude > .00001f)
                {
                    if (!occupant.isActiveAndEnabled)
                    {
                        transform.position = target;
                        IsMoving = false;
                        yield break;
                    }
                    transform.position = Vector3.MoveTowards(transform.position, target, Mathf.Max(.1f, speed) * Time.deltaTime);
                    yield return null;
                }
                transform.position = target;
            }
            IsMoving = false;
        }
        void OnDisable()
        {
            StopAllCoroutines();
            IsMoving = false;
            if (occupant && occupant.Board) transform.position = occupant.Board.Center(occupant.Cell, occupant.Elevation);
        }
    }
}

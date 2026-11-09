using System.Collections.Generic;
using UnityEngine;

namespace GridTutorial
{
    public static class GridPathfinder
    {
        static readonly Vector2Int[] Directions = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
        static int Heuristic(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        // A*: four directions, equal cost. Result includes start and destination.
        public static List<Vector2Int> Find(GridBoard board, Vector2Int start, Vector2Int goal, GridOccupant self = null, bool planThroughBuildings = false)
        {
            var empty = new List<Vector2Int>();
            if (!board || !board.Contains(start)) return empty;
            var goalCell = board.GetCell(goal);
            bool attackableGoal = planThroughBuildings && goalCell != null && goalCell.Building != null && goalCell.Occupant == null;
            if (!board.CanEnter(goal, self) && !attackableGoal) return empty;
            var open = new List<Vector2Int> { start };
            var closed = new HashSet<Vector2Int>();
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            var cost = new Dictionary<Vector2Int, int> { [start] = 0 };
            while (open.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < open.Count; i++)
                    if (cost[open[i]] + Heuristic(open[i], goal) < cost[open[best]] + Heuristic(open[best], goal)) best = i;
                var current = open[best];
                open.RemoveAt(best);
                if (current == goal)
                {
                    var path = new List<Vector2Int> { current };
                    while (previous.TryGetValue(current, out var parent)) { current = parent; path.Add(current); }
                    path.Reverse();
                    return path;
                }
                closed.Add(current);
                foreach (var direction in Directions)
                {
                    var next = current + direction;
                    var cell = board.GetCell(next);
                    bool attackable = planThroughBuildings && cell != null && cell.Building != null && cell.Occupant == null;
                    if (closed.Contains(next) || (!board.CanEnter(next, self) && !attackable)) continue;
                    int candidate = cost[current] + 1;
                    if (cost.TryGetValue(next, out int old) && candidate >= old) continue;
                    previous[next] = current;
                    cost[next] = candidate;
                    if (!open.Contains(next)) open.Add(next);
                }
            }
            return empty;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace GridTutorial
{
    [RequireComponent(typeof(GridOccupant), typeof(GridMover))]
    public sealed class GridMonsterAttacker : MonoBehaviour
    {
        [SerializeField] GridOccupant player;
        [SerializeField, Min(1)] float damage = 25;
        [SerializeField, Min(.1f)] float attackInterval = 1;
        [SerializeField, Min(.05f)] float repathInterval = .25f;
        GridOccupant occupant;
        GridMover mover;
        GridActorAnimation animationView;
        float nextThink, nextAttack;
        public GridBuilding AttackTarget { get; private set; }
        static readonly Vector2Int[] Directions = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
        void Awake()
        {
            occupant = GetComponent<GridOccupant>();
            mover = GetComponent<GridMover>();
            animationView = GetComponent<GridActorAnimation>();
        }
        void OnEnable() { nextThink = nextAttack = 0; }
        void Update()
        {
            if (!player || !player.isActiveAndEnabled || !occupant.isActiveAndEnabled || !occupant.Board || player.Board != occupant.Board || mover.IsMoving || Time.time < nextThink) return;
            nextThink = Time.time + Mathf.Max(.05f, repathInterval);
            AttackTarget = null;
            var board = occupant.Board;
            // Stop next to the player. Player damage/combat is an extension point.
            if (Distance(occupant.Cell, player.Cell) <= 1) return;
            List<Vector2Int> best = null;
            // Prefer an open route; only plan destruction if all open routes fail.
            for (int pass = 0; pass < 2 && best == null; pass++)
                foreach (var direction in Directions)
                {
                    var goal = player.Cell + direction;
                    if (!board.CanEnter(goal, occupant)) continue;
                    var path = GridPathfinder.Find(board, occupant.Cell, goal, occupant, pass == 1);
                    if (path.Count > 0 && (best == null || path.Count < best.Count)) best = path;
                }
            // A building may itself occupy the cell next to the player.
            if (best == null)
            {
                foreach (var direction in Directions)
                {
                    var goal = player.Cell + direction;
                    var cell = board.GetCell(goal);
                    if (cell == null || !cell.Building) continue;
                    var path = GridPathfinder.Find(board, occupant.Cell, goal, occupant, true);
                    if (path.Count > 0 && (best == null || path.Count < best.Count)) best = path;
                }
            }
            if (best == null || best.Count < 2) return;
            var next = board.GetCell(best[1]);
            if (next.Building)
            {
                AttackTarget = next.Building;
                if (Time.time < nextAttack) return;
                nextAttack = Time.time + Mathf.Max(.1f, attackInterval);
                if (animationView)
                {
                    animationView.Face(AttackTarget.transform.position);
                    animationView.PlayAttack();
                }
                next.Building.TakeDamage(damage);
            }
            else mover.MoveTo(best[1]);
        }
        static int Distance(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}

using TMPro;
using UnityEngine;

namespace GridTutorial
{
    public sealed partial class GameManager
    {
        [SerializeField] Vector2Int exitCell = new Vector2Int(9, 7);
        [SerializeField, Min(1)] float timeLimit = 60;
        [SerializeField] TMP_Text timerText;
        public float Remaining { get; private set; }
        public float Elapsed { get; private set; }
        public string ResultReason { get; private set; }
        public int FinalScore { get; private set; }

        void Awake() => Remaining = timeLimit;

        void LateUpdate()
        {
            if (State != LoopState.Playing || Time.timeScale <= 0) return;
            Remaining = Mathf.Max(0, Remaining - Time.deltaTime);
            Elapsed = timeLimit - Remaining;
            if (timerText) timerText.text = $"남은 시간 {Remaining:0.0}초";
            if (Remaining <= 0)
            {
                Finish(false, "시간 초과");
                return;
            }
            var mover = player.GetComponent<GridMover>();
            if (mover.IsMoving) return;
            foreach (var monster in monsters)
            {
                if (!monster || !monster.isActiveAndEnabled) continue;
                var actor = monster.GetComponent<GridOccupant>();
                var otherMover = monster.GetComponent<GridMover>();
                if (!actor.isActiveAndEnabled || actor.Board != player.Board ||
                    otherMover.IsMoving) continue;
                var delta = actor.Cell - player.Cell;
                if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) <= 1)
                {
                    Finish(false, "몬스터에게 붙잡힘");
                    return;
                }
            }
            if (player.Cell == exitCell) Finish(true, "출구 도착");
        }
        void Finish(bool won, string reason)
        {
            if (State != LoopState.Playing || Time.timeScale <= 0) return;
            ResultReason = reason;
            FinalScore = won ? 1000 + Mathf.FloorToInt(Remaining) * 10 : 0;
            SetGameplay(false);
            hudPanel.SetActive(false);
            ChangeState(won ? LoopState.Win : LoopState.Lose);
            Debug.Log($"{State}: {ResultReason} / {FinalScore}점", this);
        }
    }
}


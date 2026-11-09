# 2. 플레이 중 승리·패배 조건

[이전: 시작 UI](GAME_LOOP_01_START_UI.md) | [다음: 결과창](GAME_LOOP_03_RESULT.md)

목표: Create Demo에서 이어서 추격을 추가하고 출구 도착은 승리, 포획 또는 시간 초과는 패배로 확정한다. 현재 GridMonsterAttacker는 건물을 공격하지만 플레이어 HP를 감소시키지 않는다. 따라서 이 실습의 패배는 HP 사망이 아닌 포획이다.

## 1. Scene과 몬스터 준비

1. 1강 Scene을 `Assets/Scenes/GridLoop_02_Rules.unity`로 Save As하고 Build Profiles의 Scene List에도 추가한다. Create Demo를 다시 실행하지 않는다.
2. 기존 Monster에 GridMover와 GridMonsterAttacker를 추가한다. GridMonsterAttacker의 Player에 기존 Player의 GridOccupant를 연결한다. Board는 기존 Grid Tutorial Demo의 GridBoard를 유지한다.
3. Monster GridMover의 Speed=1.5, Player Speed=3. 몬스터 Initial Cell=(7,5)를 유지한다. 기본 데모에는 장애물이 (4,0)~(4,5)에 있고 (4,3)이 비어 있다. 위쪽도 우회 가능하므로 길을 임의로 전부 막지 않는다.
4. GameManager의 Monsters 배열 Size=1, Element 0=Monster의 GridMonsterAttacker. 몬스터를 늘리면 모든 추격 컴포넌트를 중복 없이 추가한다. 빠진 몬스터는 메뉴에서도 움직이고 포획 판정에서 누락된다.
5. HUDPanel에 TimerText(TMP)를 추가한다. 상단 우측 Anchor/Pivot=(1,1), Pos=(-40,-40), Size=300×60, Raycast Target=Off로 둔다.

## 2. 출구 표시

1. 3D Object > Quad로 ExitMarker를 생성하고 Collider를 제거한다. GridOccupant는 붙이지 않는다. 출구는 플레이어가 들어갈 수 있는 빈 셀이어야 한다.
2. 보드가 원점·회전 0·Cell Size=1일 때 Position=(9.5,0.03,7.5), Rotation=(90,0,0), Scale=(0.8,0.8,0.8)로 둔다. 윗면이 안 보이면 X 회전을 -90으로 바꾼다.
3. 초록색 URP/Unlit 머티리얼을 연결한다. 출구 셀=(9,7)이다. 보드를 이동·회전했다면 위치는 `board.Center(new Vector2Int(9,7), .03f)`에 맞춘다.
4. HUD ObjectiveText를 `60초 안에 초록 출구 도착 / 상하좌우 포획 주의`로 바꾼다.

## 3. 승패 코드 추가

제공된 `Assets/GridSystem/Loop/GameManager.Rules.cs`를 확인한다. 아래 코드는 설명용이며 별도로 작성하지 않는다. 1강의 GameManager.cs와 같은 namespace 및 partial class이므로 하나의 컴포넌트로 합쳐지며, Rules 파일을 별도 컴포넌트로 붙일 필요가 없다.

```csharp
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
            if (State != LoopState.Playing) return;
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
            if (State != LoopState.Playing) return;
            ResultReason = reason;
            FinalScore = won ? 1000 + Mathf.FloorToInt(Remaining) * 10 : 0;
            SetGameplay(false);
            hudPanel.SetActive(false);
            ChangeState(won ? LoopState.Win : LoopState.Lose);
            Debug.Log($"{State}: {ResultReason} / {FinalScore}점", this);
        }
    }
}
```

GameManager Inspector에 새로 나타난 Exit Cell=(9,7), Time Limit=60, Timer Text=HUD의 TimerText를 연결한다. Console 컴파일 오류를 먼저 해결한다.

판정 우선순위는 **시간 초과 → 포획 → 출구**다. 한 프레임에 조건이 겹치면 앞의 조건이 승리하며 결과는 한 번만 확정한다. GridMover는 보간 전에 다음 셀을 예약하므로 Cell만 검사하면 출구에 닿기 전에 승리한다. 코드의 IsMoving 검사를 유지해야 한다. 포획은 양쪽 이동 완료 후 상하좌우 인접 거리 1 이하이며 대각선은 제외한다. 이동 도중 스쳐 지나가는 접촉 판정은 이 규칙에 포함하지 않는다.

Finish는 기록 확정 → 이동·추격·건설 중단 → 상태 이벤트 순으로 처리한다. GridOccupant 자체는 끄지 않아 셀 점유를 보존한다. Time.timeScale=0을 사용하지 않으며 다음 강의의 연출은 별도로 실행한다.

## 4. 재현 가능한 검증

설정은 Play를 끈 상태에서 바꾸고 다시 Play한다. 테스트 후 기본값을 복원한다.

| 테스트 | 설정 및 조작 | 기대 결과 |
| --- | --- | --- |
| 메뉴 대기 | 시작 전 5초 대기 | 추격 없음, 제한 시간 소비 없음 |
| 빠른 승리 | Exit Cell=(0,1), Marker=(0.5,0.03,1.5), 시작 후 (0,1) 클릭 | 이동 완료 후 Win, 양수 점수 |
| 기본 탈출 | 출구 (9,7) 복원, 몬스터를 피해 이동 | 출구 도착 후 Win |
| 포획 | Monster Initial Cell=(1,0), 시작 클릭 | Lose / 몬스터에게 붙잡힘 |
| 대각선 제외 | Monster=(1,1), Monster Mover Speed=0.1, 시작 직후 관찰 | 대각선 자체로는 포획되지 않음. 이후 상하좌우 접근하면 패배 가능 |
| 시간 초과 | Time Limit=3, Monster=(7,5), 이동하지 않음 | Lose / 시간 초과 |
| 중복 방지 | 결과 뒤 보드 클릭·10초 대기 | 추가 이동·판정·점수 변화 없음 |

이 단계는 Console로 결과를 확인한다. 결과 후 UI가 없는 것은 정상이며 3강에서 결과창을 연결한다. 테스트 값을 Player=(0,0), Monster=(7,5), Exit=(9,7), Time Limit=60, Monster Speed=1.5로 복원하고 저장한다.

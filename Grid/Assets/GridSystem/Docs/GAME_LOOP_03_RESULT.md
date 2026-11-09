# 3. 결과창 커스텀과 절차적 애니메이션

[이전: 승패 조건](GAME_LOOP_02_RULES.md) | [다음: Audio](GAME_LOOP_04_AUDIO.md)

목표: 같은 게임 Scene 위에 승패 결과를 표시하고 코드로 페이드·이동·확대·점수 카운트를 순서대로 실행한다. Animator, Animation Clip, 외부 Tween 패키지는 사용하지 않는다.

## 1. 결과 UI 배치

2강 Scene을 `Assets/Scenes/GridLoop_03_Result.unity`로 Save As하고 Scene List에 추가한다. GameCanvas의 마지막 자식에 ResultPanel을 만든다.

```text
GameManager                  기존 GameManager + GridResultScreen
GameCanvas
  StartPanel
  HUDPanel
  ResultPanel                Image: 전체 화면 검정 Alpha 0.65
    ResultCard               Image (Sliced) / CanvasGroup
      Title                  TMP
      Reason                 TMP
      Stats                  TMP
      Score                  TMP
      Buttons                CanvasGroup
        MainButton           Button / TMP: 메인으로
EventSystem
```

1. ResultPanel은 전체 Stretch, offsets=0, Raycast Target=On으로 뒤의 보드를 막는다. Scene 편집 시 비활성으로 저장한다.
2. ResultCard는 중앙 Anchor/Pivot, Size=760×640, Scale=1. 1강에서 Sprite로 바꾼 `Art/UI/Panel/panel-000.png`를 Source Image, Type=Sliced로 지정한다. CanvasGroup을 추가한다.
3. Title/Reason/Stats/Score는 중앙 Anchor, Width=660, Height=70. Pos Y를 각각 220/120/20/-80으로 둔다. 글꼴 크기는 각각 56/28/28/40부터 조정한다. Raycast Target은 모두 Off.
4. Buttons는 중앙 Pos Y=-220, Size=600×80에 CanvasGroup을 붙인다. MainButton은 360×80, Sliced Image와 Color Tint로 구성한다. On Click은 GameManager > ReturnToMenu()에 한 번 연결한다.
5. 부모와 자식 CanvasGroup의 Ignore Parent Groups=Off. 같은 alpha·위치·scale을 바꾸는 Animator는 붙이지 않는다. 배경 Dim Image는 ResultCard의 CanvasGroup 밖에 있어 카드가 투명한 동안에도 입력을 막는다.

## 2. 결과 화면 코드

제공된 `Assets/GridSystem/Loop/GridResultScreen.cs`를 **항상 활성인 GameManager 루트**에 붙인다. 아래 코드는 설명용이며 중복 작성하지 않는다. 비활성 ResultPanel에 붙이면 상태 이벤트를 받지 못하므로 위치를 지킨다.

```csharp
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace GridTutorial
{
    public sealed class GridResultScreen : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] GameObject panel;
        [SerializeField] RectTransform card;
        [SerializeField] CanvasGroup cardGroup, buttons;
        [SerializeField] TMP_Text title, reason, stats, score;
        [SerializeField] UnityEvent revealCue = new UnityEvent();
        Vector2 home;

        void Awake() => home = card.anchoredPosition;
        void OnEnable()
        {
            game.StateChanged += OnState;
            OnState(game.State);
        }
        void OnDisable()
        {
            game.StateChanged -= OnState;
            StopAllCoroutines();
        }
        void OnState(LoopState state)
        {
            StopAllCoroutines();
            if (state != LoopState.Win && state != LoopState.Lose)
            {
                panel.SetActive(false);
                return;
            }
            bool won = state == LoopState.Win;
            title.text = won ? "탈출 성공" : "탈출 실패";
            title.color = won ? new Color(.3f, 1, .7f) : new Color(1, .35f, .3f);
            reason.text = game.ResultReason;
            stats.text = $"플레이 시간 {game.Elapsed:0.0}초";
            score.text = "점수 0";
            cardGroup.alpha = buttons.alpha = 0;
            cardGroup.interactable = false;
            buttons.interactable = buttons.blocksRaycasts = false;
            card.localScale = Vector3.one * .9f;
            card.anchoredPosition = home + Vector2.down * 48;
            panel.SetActive(true);
            StartCoroutine(Reveal(game.FinalScore));
        }
        IEnumerator Reveal(int finalScore)
        {
            float elapsed = 0;
            while (elapsed < .3f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / .3f);
                float ease = 1 - Mathf.Pow(1 - t, 3);
                cardGroup.alpha = ease;
                card.localScale = Vector3.one * Mathf.Lerp(.9f, 1, ease);
                card.anchoredPosition = Vector2.Lerp(home + Vector2.down * 48, home, ease);
                yield return null;
            }
            cardGroup.alpha = 1;
            card.localScale = Vector3.one;
            card.anchoredPosition = home;
            revealCue.Invoke();
            elapsed = 0;
            while (elapsed < .6f)
            {
                elapsed += Time.unscaledDeltaTime;
                int shown = Mathf.RoundToInt(Mathf.Lerp(0, finalScore,
                    Mathf.Clamp01(elapsed / .6f)));
                score.text = $"점수 {shown:N0}";
                yield return null;
            }
            score.text = $"점수 {finalScore:N0}";
            elapsed = 0;
            while (elapsed < .2f)
            {
                elapsed += Time.unscaledDeltaTime;
                buttons.alpha = Mathf.Clamp01(elapsed / .2f);
                yield return null;
            }
            buttons.alpha = 1;
            cardGroup.interactable = true;
            buttons.interactable = buttons.blocksRaycasts = true;
        }
    }
}
```

Inspector의 Game=GameManager, Panel=ResultPanel, Card=ResultCard의 RectTransform, Card Group=그 CanvasGroup, Buttons=Buttons의 CanvasGroup을 연결한다. Title/Reason/Stats/Score에는 각 TMP를 넣는다. Reveal Cue는 4강까지 비워 둔다. GameManager와 GridResultScreen의 컴포넌트는 계속 활성으로 유지한다.

## 3. 커스텀하는 방법

승패별 Title 문자열과 color를 바꾸면 분위기가 달라진다. ResultCard Source Image를 교체할 때 새 Sprite에도 Border를 설정한다. `Art/UI/Border/panel-border-000.png`를 추가 프레임 Image로 겹쳐도 된다. 프레임은 Sprite/Sliced로 준비하고 Raycast Target을 끈다. 프레임을 카드 전체 Stretch로 배치하며 원본 패널의 중앙 장식은 별도 이미지로 분리하면 확장 시 덜 늘어난다.

현재 데이터는 실제 GameManager에 있는 결과 사유·시간·점수다. 처치 수나 HP처럼 수집하지 않은 수치를 임의로 표시하지 않는다. 항목 추가 시 게임 중 수집 → Finish에서 확정 → UI 표시 → 재시작 초기화를 함께 설계한다. 카운트 애니메이션은 복사한 finalScore만 사용하며 실제 기록을 바꾸지 않는다.

## 완료 확인

1. 2강의 빠른 승리·포획·시간 초과를 각각 재현한다. 사유, 제목, 색과 점수가 일치해야 한다.
2. 카드가 0.3초 동안 아래에서 올라오며 페이드·확대되고, 점수가 0.6초 카운트된 다음 버튼이 0.2초 페이드된다. 패배 점수 0도 같은 순서로 실행한다.
3. 연출 중 클릭해도 메인 버튼은 실행되지 않는다. 완료 후에는 같은 Scene을 재로드하여 시작 메뉴로 돌아간다.
4. 다시 시작하고 결과까지 3회 반복한다. 이벤트·연출 중복, 이전 결과 잔상, 누적 점수 변경이 없어야 한다.
5. 1280×720, 1920×1080과 좁은 화면에서 글자 잘림과 카드 밖 버튼을 확인한다. 카드 크기와 TMP 크기를 조정한다.

최종 기본값을 복원하고 ResultPanel=Off, GameManager/GameCanvas=On으로 Scene을 저장한다.

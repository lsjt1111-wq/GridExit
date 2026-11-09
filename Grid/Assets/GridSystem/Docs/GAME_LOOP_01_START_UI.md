# 1. 9-Sliced 게임 시작 버튼

[전체 순서](../../Docs/GAME_LOOP_SCENE_TUTORIAL.md) | [다음: 승패 조건](GAME_LOOP_02_RULES.md)

목표: Art의 UI로 모서리를 유지하며 크기를 바꾸는 시작 버튼을 만들고, GameManager의 `GameLoop()`를 호출한다.

현재 GameManager는 기본적으로 화면 전환 API를 사용합니다. 이 문서의 기존 즉시 시작 동작을 실습하려면 Inspector의 **Use Scene Transition을 끄세요**. 철문 전환 후 별도 게임 씬에서 시작하려면 [5. 씬 전환](GAME_LOOP_05_SCENE_TRANSITION.md)을 진행합니다. 아래 GameManager 예제는 기존 즉시 시작 흐름의 설명용이며, 현재 제공 코드는 전환 요청과 목적지 완료 처리가 추가되어 있습니다.

## 1. Create Demo 준비

1. 빈 Scene에서 `Tools > Grid Tutorial > Create Demo`를 한 번 실행한다. 이미 생성했다면 다시 실행하지 않는다.
2. `Assets/Scenes/GridLoop_01_Start.unity`로 저장하고 TMP Essential Resources를 가져온다.
3. 생성된 `Grid Tutorial Demo`는 Position/Rotation=0, Scale=1을 유지한다. 보드는 10×8, Player Initial Cell=(0,0), Monster=(7,5)이다.
4. `Grid Demo Camera`만 활성 카메라로 남긴다. Play에서 클릭 이동을 확인한 뒤 종료한다.
5. `Assets/GridSystem/Loop`에 모든 강의의 C#이 제공되어 있다. 중복 작성하지 않는다. 기존 GridTutorial.Runtime.asmdef의 TMP/uGUI 참조를 사용하므로 새 asmdef는 만들지 않는다.

## 2. UI 클릭의 보드 전달 차단

현재 `GridPathPreview.cs`에는 아래 수정이 반영되어 있다. 직접 다시 수정할 필요는 없으며, 수정 내용을 학습한다. 기존 코드에서는 Start에 있던 다음 검사를 제거했다.

```csharp
if ((buildController && buildController.IsBuilding) || (EventSystem.current && EventSystem.current.IsPointerOverGameObject())) return;
```

같은 클래스 Update의 `line.positionCount = 0;` 바로 다음에 위 검사를 배치했다. Start의 나머지 초기화는 유지한다. UI 위에서 Start가 조기 종료되어 mover 참조가 비는 문제도 함께 피한다. OnDisable에서는 남은 경로선을 지운다. 실습 입력은 마우스 기준이다.

## 3. Art 이미지의 9-Slice 설정

1. `Assets/GridSystem/Art/UI/Panel/panel-000.png`를 선택한다. 다른 Panel 변형으로 교체해도 된다.
2. Texture Type=Sprite (2D and UI), Sprite Mode=Single, Mesh Type=Full Rect, Pixels Per Unit=100, Generate Mip Maps=Off, Wrap Mode=Clamp로 Apply한다. 현재 원본은 일반 Texture일 수 있으므로 변환을 생략하지 않는다.
3. Sprite Editor에서 Border의 L/R/T/B 선을 모서리 장식 안쪽으로 이동한다. 각 16px부터 시도하되 실제 그림 크기와 모서리에 맞게 조정하고 Apply한다. Sprite Editor가 없으면 Package Manager에서 2D Sprite를 준비한다.
4. 네 모서리는 유지되고 위·아래는 가로, 좌·우는 세로, 중앙은 양방향으로 늘어난다. Border=0이면 9-Slice 효과가 없다.
5. 아래 Image의 Type을 Sliced로 설정한다. Transform Scale은 1로 유지하고 RectTransform Width/Height로 크기를 바꾼다. 320×88, 520×88, 520×160에서 Simple과 Sliced를 비교한다. Border 합보다 작은 크기는 피한다.

## 4. Canvas 구성

```text
Grid Tutorial Demo                  기존 데모 유지
GameManager                        GameManager.cs
GameCanvas                         Canvas / CanvasScaler / GraphicRaycaster
  StartPanel                       전체 Stretch Image
    MenuCard                       Image (Sliced)
      Title                        TMP: GRID ESCAPE
      StartButton                  Image (Sliced) / Button
        Text (TMP)                 START GAME
  HUDPanel                         빈 RectTransform
    ObjectiveText                  TMP: 출구에 도착하세요
EventSystem                        InputSystemUIInputModule
```

1. UI > Canvas: Render Mode=Screen Space - Overlay. Canvas Scaler=Scale With Screen Size, Reference Resolution=1920×1080, Match=0.5.
2. EventSystem은 하나만 둔다. InputSystemUIInputModule을 사용하고 Assign Default Actions로 기본 UI 액션을 지정한다.
3. StartPanel: 전체 Stretch, offsets=0, Image 검정 Alpha=0.55, Raycast Target=On. 메뉴 뒤의 보드 클릭을 막는다.
4. MenuCard: 중앙 Anchor/Pivot=(0.5,0.5), Size=640×360. Source Image=panel-000, Type=Sliced, Fill Center=On. 어두운 청록색을 적용한다.
5. Title은 위쪽 Pos Y=90, Size=560×80. Button - TextMeshPro로 StartButton을 만들고 Pos Y=-60, Size=440×96. 자신의 Image에도 panel-000과 Sliced를 적용한다.
6. Button Target Graphic=자신의 Image. Transition=Color Tint, Normal/Highlighted/Pressed/Disabled 색을 구별한다. 자식 TMP의 Raycast Target=Off. 한글을 쓰면 한글을 포함한 TMP Font Asset을 지정한다.
7. HUDPanel은 Image 없는 전체 Stretch RectTransform으로 만들고 ObjectiveText를 상단에 둔다. HUD 텍스트의 Raycast Target=Off. 전체 화면 투명 Image가 남으면 보드 입력을 가로막는다.

## 5. GameManager 작성

제공된 `Assets/GridSystem/Loop/GameManager.cs`를 확인한다. 아래 코드는 설명용이며 별도로 작성하지 않는다. partial은 2강의 승패 코드와 하나의 클래스로 합쳐지는 선언이다.

```csharp
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GridTutorial
{
    public enum LoopState { Ready, Playing, Win, Lose }
    public sealed partial class GameManager : MonoBehaviour
    {
        [SerializeField] GridOccupant player;
        [SerializeField] GridPathPreview pathPreview;
        [SerializeField] GridMonsterAttacker[] monsters = Array.Empty<GridMonsterAttacker>();
        [SerializeField] GridBuildController buildController;
        [SerializeField] GameObject startPanel, hudPanel;
        public LoopState State { get; private set; } = LoopState.Ready;
        public event Action<LoopState> StateChanged;

        void Start()
        {
            SetGameplay(false);
            startPanel.SetActive(true);
            hudPanel.SetActive(false);
        }
        public void GameLoop()
        {
            if (State != LoopState.Ready) return;
            startPanel.SetActive(false);
            hudPanel.SetActive(true);
            SetGameplay(true);
            ChangeState(LoopState.Playing);
        }
        void ChangeState(LoopState value)
        {
            State = value;
            StateChanged?.Invoke(value);
        }
        void SetGameplay(bool active)
        {
            pathPreview.enabled = active;
            player.GetComponent<GridMover>().enabled = active;
            if (buildController)
            {
                if (!active) buildController.CancelBuild();
                buildController.enabled = active;
            }
            foreach (var monster in monsters)
            {
                if (!monster) continue;
                monster.enabled = active;
                monster.GetComponent<GridMover>().enabled = active;
            }
        }
        public void ReturnToMenu()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
```

1. 항상 활성인 루트 오브젝트 GameManager에 동명 컴포넌트를 추가한다.
2. Player=생성된 Player의 GridOccupant, Path Preview=기존 Path Preview, Start Panel/Hud Panel=각 패널을 연결한다. Monsters는 Size=0으로 둔다. 기본 데모에는 건설이 없으므로 Build Controller는 비워 둔다.
3. StartButton > On Click > + → Hierarchy의 GameManager를 드래그 → GameManager > `GameLoop()`를 Runtime Only로 한 번 연결한다. 스크립트 에셋을 드래그하지 않는다.
4. File > Build Profiles의 Scene List에 이 Scene을 추가하고 활성화한다. ReturnToMenu는 같은 Scene을 재로드하여 점유·메뉴·타이머를 함께 초기화한다. 이후 Save As한 Scene도 각각 목록에 추가한다.

## 완료 확인

- Play 진입 직후 메뉴가 보이고 Player는 이동하지 않는다.
- 시작 버튼 클릭 후 메뉴가 사라지고 HUD와 클릭 이동이 활성화된다. 연타해도 한 번만 시작한다.
- 1280×720과 1920×1080에서 모서리와 글자·클릭 범위가 유지된다.
- 클릭이 안 되면 EventSystem, UI 입력 액션, GraphicRaycaster, On Click 대상, 버튼 위의 Raycast Image를 확인한다.

승패 코드도 함께 제공되므로 시작 후 60초가 지나거나 출구에 도착하면 Console에 결과가 출력된다. 추격과 결과 UI는 다음 단계에서 연결한다. Scene을 저장하고 2강으로 진행한다.

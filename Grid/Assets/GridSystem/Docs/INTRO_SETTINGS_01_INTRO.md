# 1. Intro 화면 구성

[전체 순서](INTRO_SETTINGS_TUTORIAL.md) | [다음: AudioMixer](INTRO_SETTINGS_02_AUDIO.md)

목표: 로고와 타이틀 메뉴를 만들고 기존 철문 전환 뒤에 게임을 시작합니다. 이 단계의 Intro는 로고·타이틀 화면이며, 동영상 재생이나 추가 Scene 로딩을 요구하지 않습니다.

## 1. Scene 준비

1. Play를 끄고 기존 메뉴 Scene을 저장합니다.
2. 기존 `GameManager`의 Start Panel/HUD Panel, Player/Path Preview, Monsters 참조를 유지합니다. 새 UI 때문에 기존 StartPanel 자체를 삭제하면 안 됩니다.
3. `Tools > Grid Tutorial > Intro Settings > Add Frontend To Current Scene`을 실행합니다.
4. 도구 없이 만들려면 Hierarchy에서 루트 `Create Empty` → 이름 `Grid Frontend` → `GridFrontend` 컴포넌트를 추가합니다.
5. Game에 현재 Scene의 `GameManager`를 연결합니다. Title=`ZOMBIE SURVIVE`, Logo Seconds=`1.5`, Show Intro=On으로 설정합니다. Font는 TMP Font Asset을 지정하거나 기본값을 사용합니다.
6. `Grid Frontend`를 StartPanel/ResultPanel 밑에 두지 않습니다. 항상 활성인 루트로 유지해야 ESC 설정이 동작합니다.

## 2. Play에서 생성되는 UI

```text
Grid Frontend                         GridFrontend
  Grid Frontend Canvas                Overlay / Scaler / GraphicRaycaster
    Intro                             로고와 건너뛰기 안내
    Main Menu                         타이틀, 시작, 설정, 종료, 버전
    Settings                          사운드와 화면·편의 설정
    Display Confirmation              KEEP / REVERT / 15초 카운트다운
    Quit Confirmation                 CANCEL / QUIT
    Text                              FPS 표시
Grid Settings (Persistent)             자동 생성 / DontDestroyOnLoad
EventSystem                           없을 때만 자동 생성
```

1. Canvas는 Screen Space - Overlay, Sort Order=100, Reference Resolution=1280×900, Match=0.5입니다.
2. 패널 배경 Image가 전체 화면의 포인터 입력을 받습니다. 장식 TMP의 Raycast Target은 꺼져 있습니다.
3. 기존 EventSystem이 있으면 재사용합니다. `InputSystemUIInputModule`과 기본 UI Actions가 연결되어 있어야 합니다. 중복 EventSystem은 만들지 않습니다.
4. 자동 UI는 기본 TMP 글꼴로 표시할 수 있는 영문을 사용합니다. 한글로 변경하려면 한글 Font Asset을 Font 필드에 연결하고 `Build`의 표시 문자열을 수정합니다.
5. 색·크기·배치는 `GridFrontend.Build/BuildSettings/Box/Label`에서 조절합니다. 이 구현은 Play 때 UI를 만드는 방식입니다. Hierarchy에서 Play 중 바꾼 배치는 저장되지 않습니다.

## 3. 시작과 기존 화면 전환

1. 새 START GAME은 `GridFrontend.StartGame()` → `GameManager.GameLoop()` 순서로 실행합니다. 기존 씬 전환 API를 다시 구현하지 않습니다.
2. GameManager의 Use Scene Transition=On과 기존 Gameplay Scene/Transition Mode를 유지합니다. 기존 철문 설정은 [5단계](GAME_LOOP_05_SCENE_TRANSITION.md)를 따릅니다.
3. 전환 중에는 `IsStarting`이 true이므로 설정 재진입과 시작 연타를 차단합니다.
4. 전환이 실패해 GameManager가 Ready로 돌아오면 Intro 메뉴를 다시 보여 줍니다.
5. 게임 Scene 도착 중에는 Intro를 보여 주지 않습니다. 별도 게임 Scene에 연결한 Frontend는 Show Intro=Off로 설정합니다.
6. 기존 StartPanel이 새 메뉴 아래에 남아 있어도 됩니다. StartPanel을 참조하는 기존 GameManager 로직은 그대로 사용합니다. 화면 전환이 시작되면 새 메뉴가 내려가고 기존 전환 화면이 표시됩니다.

## 4. 설정과 종료

1. 메뉴의 SETTINGS로 설정을 엽니다. 게임 중에는 ESC로 엽니다.
2. Playing 상태에서만 기존 Time Scale을 보관하고 0으로 바꿉니다. 닫을 때 보관한 값과 커서 상태를 복원합니다.
3. 기존 Grid 이동 입력·건설 입력·몬스터 판단·승패 판단에는 Time Scale=0 가드를 추가했습니다. 설정 중 건설이나 승패가 진행되지 않습니다.
4. 다른 사용자 스크립트가 `unscaledDeltaTime`으로 게임을 진행한다면 같은 일시정지 조건을 연결합니다. Unity 전체 스크립트를 강제로 중지하는 방식은 아닙니다.
5. 종료는 확인창의 QUIT를 눌렀을 때 실행합니다. Editor에서는 로그만 출력합니다. Windows 빌드에서는 `Application.Quit()`를 호출합니다.
6. 키보드 방향키/Tab으로 UI를 선택하고 Enter로 실행할 수 있습니다. 설정 확인창 뒤의 설정 항목은 확인이 끝날 때까지 비활성화됩니다.

## 완료 확인

| 확인 | 기대 결과 |
| --- | --- |
| 첫 진입 | 로고 1.5초 → 타이틀 메뉴 |
| 로고 중 Enter/클릭/ESC | 즉시 메뉴 진입 |
| 시작 연타 | 기존 전환 요청 한 번만 처리 |
| 설정 중 보드 클릭·B/R 입력 | 이동·건설·회전 없음 |
| 게임 중 설정 10초 대기 | 게임 타이머·몬스터 진행 정지 |
| ESC 취소 | 음량 복원, 게임과 커서 상태 복원 |
| 종료 취소 | 메인 메뉴 선택으로 복귀 |
| 화면 전환 실패 | 다시 시작 가능한 메뉴 복귀 |

Scene을 저장하고 [AudioMixer 설정](INTRO_SETTINGS_02_AUDIO.md)으로 진행합니다.

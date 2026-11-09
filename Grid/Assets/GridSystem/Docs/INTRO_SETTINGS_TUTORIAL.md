# Intro 화면과 Setting 구성

[전체 튜토리얼](README.md) | [이전: 철문 화면 전환](GAME_LOOP_05_SCENE_TRANSITION.md) | [1. Intro 구성](INTRO_SETTINGS_01_INTRO.md)

목표: 기존 Grid 게임 루프에 로고 → 타이틀 메뉴 → 게임 시작 흐름과, AudioMixer 기반 사운드·Windows 화면·편의 설정을 추가합니다. Unity 6.4 / URP / TextMeshPro / Input System 기준입니다.

## 진행 순서

| 단계 | 구성 | 문서 |
| --- | --- | --- |
| 1 | 로고, 게임 타이틀, 시작·설정·종료, 기존 철문 전환 유지 | [Intro](INTRO_SETTINGS_01_INTRO.md) |
| 2 | Master / Music / SFX / UI Mixer 그룹, 로그 음량, 자동 라우팅 | [AudioMixer 설정](INTRO_SETTINGS_02_AUDIO.md) |
| 3 | 해상도·창 모드·품질·VSync·FPS, 화면 변경 확인/복구 | [화면과 추가 설정](INTRO_SETTINGS_03_DISPLAY.md) |

## 1. 준비

1. Play를 종료하고 작업할 메뉴 Scene을 엽니다. 기존 예시는 `Assets/GridSystem/Scene/m.unity`입니다. 작업 전 Scene을 저장합니다.
2. TMP Essential Resources를 준비합니다. 기존 `GameManager`, Player, HUD, AudioRoot 참조는 유지합니다.
3. `Frontend/GridFrontend.cs`, `Frontend/GridSettings.cs`, `Editor/GridFrontendMenu.cs`는 제공된 코드입니다. 문서의 코드를 별도 클래스로 중복 작성하지 않습니다.
4. Console의 컴파일 오류를 먼저 해결합니다. 새 asmdef는 필요하지 않습니다.

## 2. 빠르게 연결하기

1. `Tools > Grid Tutorial > Intro Settings > Create Audio Mixer`를 실행합니다.
2. `Assets/GridSystem/Resources/GridAudioMixer.mixer`와 Master / Music / SFX / UI 그룹 및 네 개의 Exposed Parameter가 만들어졌는지 확인합니다. 기존 같은 경로의 Mixer는 덮어쓰지 않습니다.
3. `Tools > Grid Tutorial > Intro Settings > Add Frontend To Current Scene`을 실행합니다. `Grid Frontend`가 선택됩니다. 같은 Scene에 이미 있으면 기존 오브젝트를 선택합니다.
4. Inspector의 Game에 이 Scene의 `GameManager`가 들어 있는지 확인합니다. Title=`ZOMBIE SURVIVE`, Logo Seconds=`1.5`, Show Intro=On으로 둡니다.
5. Scene을 저장합니다. Play에서 생성된 UI는 Play 종료 시 없어지므로, 저장 대상은 `Grid Frontend`와 그 Inspector 값입니다.
6. 별도 게임 Scene을 사용하는 경우 그 Scene에도 `Grid Frontend`를 추가하고 **Show Intro=Off**로 저장합니다. ESC 설정은 계속 동작합니다. Additive 전환 연출 Scene에는 추가하지 않습니다.

Mixer 메뉴는 Unity Editor 내부 생성 API를 사용합니다. 해당 버전에서 실패하면 Console 안내에 따라 [2단계의 수동 절차](INTRO_SETTINGS_02_AUDIO.md)를 사용합니다. 부분 생성된 Mixer는 삭제하지 않으므로 그룹·파라미터를 확인하여 마무리합니다.

## 3. 추가 항목 선정 이유

| 항목 | 선정 이유 | 실제 처리 |
| --- | --- | --- |
| UI 음량 분리 | 버튼 소리를 게임 효과음과 별도로 조절 | UI Mixer 그룹 |
| 품질 프리셋 | 저사양 PC에서도 실행 가능 | 프로젝트의 Quality 목록 사용 |
| VSync / FPS 제한 | 화면 찢어짐과 GPU 사용량 관리 | VSync 우선순위 안내 |
| FPS 표시 | 적용 결과를 즉시 확인 | 실시간 FPS 텍스트 |
| 백그라운드 음소거 | Alt+Tab 시 소리 제어 | 포커스 상실 시 Master 감쇠 |
| 적용·취소·기본값 | 실수 복구와 설정 영구 저장 | 임시 복사본 + PlayerPrefs |
| 화면 변경 확인 | 잘못된 해상도에서도 복구 | 실시간 15초 제한 |

카메라는 별도 CameraSystem을 사용하므로 마우스 감도·FOV·화면 흔들림을 실제 카메라 연결 없이 설정 항목으로만 추가하지 않습니다. 언어 변경도 번역 테이블 없이 선택지만 만들지 않습니다.

## 4. 파일 역할

| 파일 | 역할 |
| --- | --- |
| `Frontend/GridFrontend.cs` | Intro, 메뉴, 설정 UI 생성, 일시정지, 임시값, 화면 복구 |
| `Frontend/GridSettings.cs` | 앱당 하나의 설정 소유자, 저장, Mixer, 그래픽 적용 |
| `Editor/GridFrontendMenu.cs` | 메뉴 Scene 연결과 Mixer 생성 도구 |
| `Loop/GameManager.cs` | 기존 시작 전환, `IsStarting`으로 중복 시작·설정 진입 방지 |
| `Loop/GridLoopAudio.cs` | 기존 소스를 Music/UI/SFX 그룹으로 연결 |

`GridSettings`는 실행 시 자동 생성되고 Scene 이동 후에도 유지됩니다. Scene에 같은 컴포넌트를 수동 추가할 필요가 없습니다. 설정 JSON 키는 `GridTutorial.Settings.v1`이며 다른 PlayerPrefs를 삭제하지 않습니다.

## 완료 확인

- [ ] 로고를 기다리거나 Enter/클릭/ESC로 건너뛰면 메인 메뉴가 보입니다.
- [ ] 시작 버튼은 기존 `GameManager.GameLoop()`를 실행합니다.
- [ ] 메뉴 설정과 게임 중 ESC 설정이 열립니다.
- [ ] Mixer 각 그룹 음량, 저장·취소, 해상도 자동 복구를 다음 단계에서 확인합니다.

소스 컴파일과 실제 플레이 검증은 구분합니다. 화면 모드·Alt+Tab·종료는 Windows standalone 빌드에서 최종 확인합니다.

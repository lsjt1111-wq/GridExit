# 3. Windows 화면과 추가 설정

[이전: AudioMixer](INTRO_SETTINGS_02_AUDIO.md) | [전체 순서](INTRO_SETTINGS_TUTORIAL.md)

목표: 실제 지원 해상도와 프로젝트 품질 목록을 사용하고, 잘못된 화면 변경을 복구하면서 설정을 저장합니다.

## 1. 화면 설정 UI

| 항목 | 선택값 | 적용 API |
| --- | --- | --- |
| Resolution | 지원 해상도 + 현재/저장 해상도, 중복 제거 | Screen.SetResolution |
| Window Mode | Windowed / Borderless / Exclusive Fullscreen | FullScreenMode |
| Quality | 현재 프로젝트의 Quality 프리셋 | QualitySettings.SetQualityLevel |
| VSync | On / Off | QualitySettings.vSyncCount |
| FPS Limit | 30 / 60 / 120 / 144 / Unlimited | Application.targetFrameRate |
| Show FPS | On / Off | unscaledDeltaTime 기반 표시 |
| Mute Unfocused | On / Off | Mixer Master 감쇠 |

1. 설정의 선택 버튼을 누를 때마다 다음 값으로 바뀝니다. 키보드로 선택 후 Enter도 같은 동작입니다.
2. Refresh Rate 중복은 같은 해상도로 묶습니다. 이번 단계에서는 별도 주사율 선택을 제공하지 않고 OS/Unity의 선택을 사용합니다.
3. Windowed는 창 크기 변경, Borderless는 데스크톱 전체 화면 창, Exclusive는 Windows 독점 전체 화면입니다. Borderless의 실제 출력 크기는 모니터 해상도를 따를 수 있습니다.
4. `Screen.SetResolution`은 즉시 완료되지 않고 프레임이 끝날 때 적용됩니다. KEEP을 누르면 확인한 실제 Screen 크기와 모드를 저장합니다. [Unity 공식 API](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/screen/setresolution)
5. Editor의 Game 뷰 크기는 Windows 플레이어 창과 다릅니다. 실제 창 모드 동작은 Build And Run으로 확인합니다.

## 2. 품질 설정

1. Edit > Project Settings > Quality를 엽니다.
2. Windows에서 사용할 품질 프리셋과 각 단계에 연결된 URP Asset을 확인합니다.
3. 예를 들어 Low는 그림자·MSAA를 줄이고, High는 필요한 그림자 품질을 높입니다. 공용 URP Asset 하나를 모든 단계가 공유하면 일부 품질 차이가 없을 수 있습니다.
4. 설정 UI는 QualitySettings.names를 그대로 표시합니다. Low/Medium/High라는 가짜 고정 목록을 만들지 않습니다.
5. Quality 변경 후 VSync 값을 다시 적용합니다. 프리셋에 저장된 VSync가 사용자 선택을 덮어쓰는 문제를 피합니다.
6. 데스크톱에서 VSync=On이면 FPS 제한보다 디스플레이 동기화가 우선입니다. UI도 이 우선순위를 표시합니다. 60 FPS 제한을 비교할 때 VSync를 Off로 바꿉니다.

## 3. 적용·취소·기본값

```text
설정 열기 → Saved 복사본(draft)
  Slider → 음량만 미리 적용
  CANCEL / ESC → Saved 복원 → 창 닫기
  DEFAULTS → draft 기본값 + 음량 미리보기 (아직 저장하지 않음)
  APPLY
    화면 크기/모드 동일 → 그래픽 적용 + 저장 → 닫기
    화면 크기/모드 변경 → 실제 화면 적용 → 15초 확인
      KEEP → 실제 화면 상태 저장 → 닫기
      REVERT / ESC / 시간 초과 → 이전 실제 화면 + Saved 복원 → 설정 유지
```

1. 저장은 버전 키 `GridTutorial.Settings.v1`의 JSON 한 항목입니다. 다른 튜토리얼의 PlayerPrefs를 지우지 않습니다.
2. 저장값 로드 후 볼륨, 창 모드, 품질 인덱스, FPS 후보, 해상도 범위를 검증합니다. 모니터·Quality 목록이 바뀌어도 인덱스 범위를 벗어나지 않게 합니다.
3. 기본값은 음량 Master=100%, Music/SFX/UI=80%, 1280×720, Borderless, 품질 인덱스 최대 2, VSync=On, FPS=60, FPS 표시=Off, 백그라운드 음소거=On입니다. 기기 크기와 품질 수에 맞게 보정됩니다.
4. 화면 변경 전에는 저장값 외에 실제 Screen 크기/모드도 보관합니다. OS 단축키로 화면 모드를 바꾼 경우에도 변경 직전 상태로 돌아갑니다.
5. 카운트다운에는 Time.realtimeSinceStartup을 사용합니다. 설정 창이 Time Scale=0이어도 15초 후 복구합니다. 백그라운드 실행도 켜 두어 Alt+Tab 상태에서도 확인 시간이 흐릅니다.
6. 화면 확인 중에는 설정 항목을 조작하지 못하도록 CanvasGroup의 Interactable을 끕니다.
7. 일반적인 화면 변경 실패는 REVERT/타임아웃으로 복구합니다. 프로세스 강제 종료 시 미확인 화면값은 저장되지 않습니다. 이미 저장한 모드가 특정 GPU/모니터에서 열리지 않는 경우에는 해당 PlayerPrefs 키를 지우는 복구 절차가 필요합니다.

## 4. 빌드 확인 순서

1. 메뉴와 게임 Scene을 File > Build Profiles > Scene List에 등록합니다. 기존 전환 Scene도 [화면 전환 튜토리얼](../../ScreenTransition/TUTORIAL.md)대로 포함합니다.
2. Windows 빌드에서 Intro → Settings → Windowed 1280×720 → APPLY를 누릅니다.
3. 확인창의 REVERT로 변경 전 크기·모드로 돌아오는지 확인합니다.
4. 다시 변경하고 15초간 기다립니다. 자동 복구되고 변경값이 저장되지 않아야 합니다.
5. 다시 변경하고 KEEP을 누른 뒤 게임을 종료·재실행합니다. 확인한 설정이 유지되어야 합니다.
6. VSync Off + FPS=30으로 제한을 확인한 뒤 VSync On의 동작과 비교합니다. 실제 FPS는 기기 성능을 넘지 못합니다.
7. 게임 중 ESC로 설정을 열고 대기합니다. 게임 타이머, 몬스터, 건설 입력은 정지하고 UI·화면 확인 카운트다운은 동작해야 합니다.

## 완료 확인

| 확인 | 기대 결과 |
| --- | --- |
| 해상도 버튼 순환 | 동일 해상도 중복 없음 |
| 창/테두리 없는 전체 화면/독점 전체 화면 | Windows 빌드에서 전환 |
| KEEP | 확인한 실제 화면값 저장 |
| REVERT / ESC / 15초 | 변경 전 화면 복구, 미저장 |
| 기본값 후 CANCEL | 기존 설정 보존 |
| 음량 변경 후 APPLY | 재실행 후 유지 |
| 게임 중 설정 닫기 | 기존 Time Scale·커서 상태 복구 |
| FPS 표시 On/Off | APPLY 후 표시 상태 변경 |
| Scene 전환 반복 | 설정 소유자 한 개 유지, 음량 유지 |

검증 기록에는 Editor 컴파일, Play 모드, Windows 빌드를 구분해서 남깁니다. 코드 컴파일 성공만으로 실제 모니터의 모든 화면 모드가 검증된 것은 아닙니다.

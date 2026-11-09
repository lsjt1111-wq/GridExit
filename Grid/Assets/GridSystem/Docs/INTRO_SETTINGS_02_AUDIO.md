# 2. AudioMixer 사운드 설정

[이전: Intro](INTRO_SETTINGS_01_INTRO.md) | [전체 순서](INTRO_SETTINGS_TUTORIAL.md) | [다음: 화면·편의 설정](INTRO_SETTINGS_03_DISPLAY.md)

목표: AudioSource의 개별 볼륨은 원래 믹싱용으로 유지하고, 사용자 음량은 Mixer 그룹의 dB로 제어합니다.

## 1. Mixer 생성

1. Play를 끕니다.
2. `Tools > Grid Tutorial > Intro Settings > Create Audio Mixer`를 실행합니다.
3. Project에서 `Assets/GridSystem/Resources/GridAudioMixer.mixer`를 더블클릭합니다.
4. 아래 그룹과 Exposed Parameter를 확인합니다. 경로와 대소문자가 코드와 일치해야 합니다.

```text
Master                  MasterVolume
  Music                 MusicVolume
  SFX                   SfxVolume
  UI                    UIVolume
```

UI를 SFX의 자식으로 두면 SFX=0일 때 UI도 꺼집니다. 이 튜토리얼은 UI와 SFX를 독립적으로 조절하므로 둘을 Master 바로 아래에 둡니다.

## 2. 수동 생성하기

도구를 쓰지 않거나 내부 API 변경으로 생성에 실패했을 때 다음 순서로 구성합니다.

1. Project의 `Assets/GridSystem` 아래 `Resources` 폴더를 만듭니다. 같은 이름이 있으면 재사용합니다.
2. 폴더 안에서 Create > Audio Mixer로 이름 `GridAudioMixer`를 만듭니다.
3. Audio Mixer 창에서 Master를 선택하고 자식 그룹 Music, SFX, UI를 만듭니다. 모든 그룹은 Master의 직접 자식입니다.
4. Master 그룹의 Volume을 우클릭 → Expose to script를 선택합니다. Exposed Parameters에서 이름을 `MasterVolume`으로 바꿉니다.
5. Music의 Volume도 노출하고 `MusicVolume`으로 바꿉니다. SFX는 `SfxVolume`, UI는 `UIVolume`입니다.
6. Snapshot의 그룹 볼륨은 모두 0 dB로 둡니다. Pitch나 효과 파라미터 대신 **그룹 Volume**을 노출했는지 확인합니다.
7. Mixer를 저장합니다. 같은 `Resources/GridAudioMixer` 이름의 Mixer를 여러 폴더에 만들지 않습니다.

## 3. AudioSource 연결

기존 `GridLoopAudio.Start()`는 실행 시 아래 라우팅을 적용합니다.

| 기존 참조 | Mixer Output | 소스 볼륨 예시 |
| --- | --- | --- |
| Music | Music | 0.35 |
| UI | UI | 0.8 |
| Feet | SFX | 0.3 |

1. AudioRoot의 Music/UI/Feet 참조는 기존대로 지정합니다. 이 세 소스는 실행 시 그룹이 자동 연결됩니다.
2. 새로 추가한 좀비·무기·건설 소리는 해당 AudioSource의 Output에 SFX를 직접 연결합니다. 새로운 BGM은 Music, 메뉴 클릭은 UI에 연결합니다.
3. Source Volume은 클립 사이의 기본 음량 균형입니다. 사용자 Master/Music/SFX/UI 음량과 곱해지는 구조이므로 소스 볼륨을 매번 1로 바꿀 필요가 없습니다.
4. 이전 [4. Audio](GAME_LOOP_04_AUDIO.md)의 선택 실습으로 만든 Music/SFX Slider가 있다면 제거하거나 연결을 해제합니다. 기존 `SetMusicVolume/SetSfxVolume`은 Source Volume을 바꾸므로 새 Mixer 설정과 함께 사용자 볼륨 UI로 사용하지 않습니다.
5. 기존 UI Source에는 클릭뿐 아니라 Win/Lose/Start 이벤트 소리도 재생됩니다. 따라서 해당 소리는 현재 UI 음량을 따릅니다. 전투 효과음으로 분류하려면 별도 SFX Source와 재생 함수를 연결합니다.
6. AudioListener는 활성 카메라에 하나만 유지합니다. 믹서 설정을 위해 AudioListener를 추가하지 않습니다.

## 4. Slider와 로그 음량

1. 네 Slider 범위는 0~1입니다. 화면에는 0~100%로 표시합니다.
2. `20 * Log10(value)`로 dB를 구합니다. 예를 들어 1=0 dB, 0.5≈-6.02 dB, 0.1=-20 dB입니다.
3. 0에서는 Log10을 계산하지 않고 -80 dB로 처리합니다.
4. AudioMixer.SetFloat는 초기 Awake가 아닌 Start 이후에 호출합니다. `GridSettings`는 Awake에서 저장값을 읽고 Start에서 Mixer에 적용합니다. [Unity 공식 API](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/audio/audiomixer/setfloat)
5. Slider를 움직이면 즉시 미리 듣기만 합니다. APPLY로 저장하고, CANCEL/ESC로 이전 저장값을 다시 적용합니다.
6. Mute Unfocused=On이면 Alt+Tab 시 Master가 -80 dB로 내려갑니다. 포커스 복귀 시 현재 미리보기 또는 저장 음량으로 복원됩니다. 저장된 Master 값 자체를 0으로 바꾸지 않습니다.

## 완료 확인

| 확인 | 기대 결과 |
| --- | --- |
| Master=0 | 연결된 전체 소리 감쇠 |
| Music=0 | BGM만 감쇠, UI/발소리는 유지 |
| SFX=0 | 발소리만 감쇠, BGM/UI 유지 |
| UI=0 | 기존 UI Source의 소리만 감쇠 |
| 음량 변경 후 CANCEL | 창을 열기 전 저장 음량 복원 |
| APPLY 후 Scene 이동·재실행 | 저장 음량 유지 |
| Alt+Tab과 복귀 | 음소거 옵션에 따라 Master만 일시 변경 |
| 사운드 출력 확인 | Audio Mixer 미터에서 대응 그룹에 신호 표시 |

음량이 바뀌지 않으면 Resources 경로 → Exposed Parameter 이름 → AudioSource Output → Game 뷰 Mute Audio → 활성 Listener 순서로 확인합니다. BGM 클립은 기존 프로젝트에서 지정한 것을 사용하며 새 음악 파일은 추가하지 않습니다.

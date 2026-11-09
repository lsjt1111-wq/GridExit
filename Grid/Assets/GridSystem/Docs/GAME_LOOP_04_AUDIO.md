# 4. 각 요소에 Audio 추가

[이전: 결과창](GAME_LOOP_03_RESULT.md) | [전체 순서](../../Docs/GAME_LOOP_SCENE_TUTORIAL.md)

목표: 버튼, 게임 시작, 이동, 승패, 결과 카드 등장에 소리를 붙인다. 게임 상태의 소리는 상태 이벤트에서, UI 소리는 버튼 이벤트에서 재생하여 프레임마다 중복 출력하지 않는다.

## 1. Scene과 클립 준비

1. 3강 Scene을 `Assets/Scenes/GridLoop_04_Audio.unity`로 Save As하고 Scene List에 추가한다.
2. `Assets/GridSystem/Art/SFX/Audio`의 클립을 Inspector에서 미리 듣는다. 다음 파일은 실제 폴더에 있다. 아래 역할은 실습용 선택이며 듣고 다른 클립으로 교체해도 된다. License.txt도 에셋과 함께 보관한다.

| 요소 | 예시 클립 | 호출 위치 |
| --- | --- | --- |
| 버튼 위 진입 | click_001.ogg | Pointer Enter |
| 시작 버튼 클릭 | click_002.ogg | Button On Click |
| 게임 시작 | confirmation_001.ogg | Playing 상태 진입 |
| 승리 | confirmation_002.ogg | Win 상태 진입 |
| 패배 | error_001.ogg | Lose 상태 진입 |
| 결과 카드 등장 완료 | bong_001.ogg | GridResultScreen.Reveal Cue |
| 메뉴 복귀 | back_001.ogg | 복귀 버튼 |
| 이동 중 일정 간격 | drop_001.ogg | 이동 중 0.3초 간격 |

이 팩의 UI 효과음을 이동 리듬 확인용으로 사용한다. 실제 발소리로 쓰려면 발소리 AudioClip으로 교체한다. BGM은 이 목록의 짧은 UI 효과음을 반복하지 말고 별도로 준비한 음악을 지정한다. 음악이 없으면 Music Clip을 비워도 나머지 실습은 동작한다.

3. 긴 BGM은 Load Type=Streaming을 검토하고 Loop 구간을 확인한다. 짧은 효과음은 Decompress On Load, Preload Audio Data=On으로 시작한다. 실제 메모리·품질에 맞춰 조정한다.
4. Grid Demo Camera에 AudioListener를 하나 추가한다. Scene 전체에서 활성 AudioListener는 하나만 유지한다. Create Demo 카메라에는 기본적으로 Listener가 없다.

## 2. AudioSource 배치

```text
AudioRoot                     GridLoopAudio (항상 활성)
  MusicSource                 AudioSource
  UISource                    AudioSource
Grid Tutorial Demo
  Player
    FootSource                AudioSource
```

| AudioSource | 설정 |
| --- | --- |
| MusicSource | Play On Awake=Off, Loop=On, Spatial Blend=0, Volume=0.35 |
| UISource | Play On Awake=Off, Loop=Off, Spatial Blend=0, Volume=0.8 |
| FootSource | Play On Awake=Off, Loop=Off, Spatial Blend=0, Volume=0.3 |

탑다운 카메라는 Player에서 높이 14만큼 떨어져 있으므로 발소리도 우선 2D로 설정해 들리는지 확인한다. 이후 3D로 바꾸려면 Spatial Blend=1, Min Distance=5, Max Distance=30부터 조정하고 보드 양 끝에서 감쇠를 확인한다. UI·BGM은 2D를 유지한다. AudioRoot를 StartPanel/ResultPanel 아래에 두면 패널을 끌 때 재생이 끊기므로 루트로 만든다.

## 3. 재생 코드

제공된 `Assets/GridSystem/Loop/GridLoopAudio.cs`를 AudioRoot에 붙인다. 아래 코드는 설명용이며 중복 작성하지 않는다.

```csharp
using System.Collections;
using UnityEngine;

namespace GridTutorial
{
    public sealed class GridLoopAudio : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] GridMover playerMover;
        [SerializeField] AudioSource music, ui, feet;
        [SerializeField] AudioClip musicClip, hover, click, start;
        [SerializeField] AudioClip win, lose, reveal, back, foot;
        float nextFoot;
        bool returning;

        void OnEnable() => game.StateChanged += OnState;
        void OnDisable() => game.StateChanged -= OnState;
        void Start()
        {
            if (musicClip)
            {
                music.clip = musicClip;
                music.loop = true;
                music.Play();
            }
        }
        void OnState(LoopState state)
        {
            nextFoot = 0;
            if (state == LoopState.Playing) Play(start);
            else if (state == LoopState.Win) Play(win);
            else if (state == LoopState.Lose) Play(lose);
            if (state != LoopState.Playing) feet.Stop();
        }
        void Update()
        {
            if (game.State != LoopState.Playing || !playerMover.IsMoving) return;
            if (Time.time < nextFoot) return;
            nextFoot = Time.time + .3f;
            if (foot) feet.PlayOneShot(foot);
        }
        void Play(AudioClip clip)
        {
            if (clip) ui.PlayOneShot(clip);
        }
        public void PlayHover() => Play(hover);
        public void PlayClick() => Play(click);
        public void PlayReveal() => Play(reveal);
        public void SetMusicVolume(float value) => music.volume = Mathf.Clamp01(value);
        public void SetSfxVolume(float value)
        {
            ui.volume = Mathf.Clamp01(value);
            feet.volume = Mathf.Clamp01(value) * .4f;
        }
        public void ReturnWithSound()
        {
            if (returning) return;
            returning = true;
            StartCoroutine(ReturnRoutine());
        }
        IEnumerator ReturnRoutine()
        {
            Play(back);
            yield return new WaitForSecondsRealtime(back ? back.length : .15f);
            game.ReturnToMenu();
        }
    }
}
```

Inspector에 Game=GameManager, Player Mover=Player의 GridMover, Music/UI/Feet=각 AudioSource, 클립 필드=표의 실제 AudioClip을 연결한다. AudioSource 자체를 클립 필드에 넣지 않는다. 음악을 비워도 세 AudioSource 참조는 모두 연결한다.

이동음은 현재 GridMover에 도착 이벤트가 없어서 IsMoving을 관찰하는 간단한 방식이다. 애니메이션 발 접지와 정확히 동기화하려면 이후 Animation Event에서 별도의 재생 메서드를 호출하고 이 Update의 이동음 재생은 제거한다. 두 경로를 동시에 사용하면 중복 발소리가 난다.

## 4. UI와 결과 연출 연결

1. StartButton의 On Click에 AudioRoot > GridLoopAudio.PlayClick()을 **첫 항목**, 기존 GameManager.GameLoop()를 두 번째 항목으로 둔다. GameLoop는 한 번만 연결한다.
2. StartButton에 EventTrigger를 추가하고 Pointer Enter에 AudioRoot > PlayHover()를 연결한다. Pointer Down/Click에 같은 소리를 다시 연결하지 않는다. 게임패드 포커스 이동음은 별도 Select 이벤트가 필요하다.
3. GameManager 루트의 GridResultScreen > Reveal Cue에 AudioRoot > PlayReveal()를 연결한다. 카드 등장 완료에 한 번 실행되며 점수 카운트 매 프레임에는 소리를 재생하지 않는다.
4. MainButton의 기존 GameManager.ReturnToMenu() 연결을 **제거**하고 AudioRoot > ReturnWithSound() 하나로 교체한다. 메뉴 복귀음을 끝까지 들은 뒤 Scene을 재로드한다. 두 함수를 함께 연결하면 즉시 재로드되어 소리가 잘린다.
5. 승패 클립은 On Click에 연결하지 않는다. StateChanged가 Playing/Win/Lose 진입 시 각각 재생한다. 2강 Finish의 상태 가드가 결과음 중복을 방지한다.
6. 선택 실습: HUD에 Music/SFX Slider를 만들고 Min=0, Max=1, Whole Numbers=Off, 초기값=각각 0.35/0.8. On Value Changed의 **Dynamic float**에 SetMusicVolume/SetSfxVolume을 연결한다. 초기 FootSource Volume도 0.32로 맞춘다. 이 예제의 볼륨은 Scene 재로드 시 기본값으로 돌아온다. 영구 저장이 필요하면 PlayerPrefs 저장·불러오기와 Slider 표시 동기화를 함께 추가한다.

## 완료 확인

| 확인 | 기대 결과 |
| --- | --- |
| 메뉴에서 마우스 진입·클릭 | Hover/Click 각 한 번, 시작 후 Confirmation 한 번 |
| 메뉴 대기·결과 화면 | 발소리 없음 |
| Player 연속 이동 | 이동 중에만 약 0.3초 간격 발소리 |
| Win / 포획 / 시간 초과 | 해당 결과음 한 번, 결과 카드 완료 시 Reveal 한 번 |
| 메인 버튼 연타 | 복귀음과 Scene 재로드 각각 한 번 |
| SFX Slider=0 | UI·발소리 무음, BGM 유지 |
| Music Slider=0 | BGM 무음, 효과음 유지 |
| 재시작 3회 | AudioRoot나 Listener 누적 없음 |

소리가 안 나면 Game 뷰 Mute Audio, OS 출력 장치, Listener 활성, Source Mute/Volume, Clip 참조, 3D 거리 순으로 확인한다. 클리핑이 나면 Source 볼륨을 낮추고 긴 클립의 중첩을 줄인다. 모든 AudioSource가 Play On Awake=Off인지 확인한다.

제출용으로 시작 화면, GameManager 참조, 승리·패배 결과창을 캡처하고 시작→이동→결과 애니메이션→메뉴 복귀를 소리와 함께 녹화한다. ResultPanel=Off로 저장하고 실제 Play에서 최종 검증한다.

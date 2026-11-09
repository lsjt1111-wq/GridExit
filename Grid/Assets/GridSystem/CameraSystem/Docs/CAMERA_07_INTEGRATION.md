# 07. 게임 루프 통합과 최종 확인

## 최종 Hierarchy 예시

```text
CameraSystem [Controller, Look, Interactor, Projection, LensController]
  Grid Demo Camera [기존 Base Camera, AudioListener]
    LensOverlayCamera [Overlay Camera, Listener 없음]
      LensCanvas [Screen Space Camera]
        CondensationImage [RawImage, CameraLens]
  Environment [CameraVolumeDriver]
    BaseGlobalVolume
    TopViewGlobalVolume
    FirstPersonGlobalVolume
    EvidenceGlobalVolume [EvidenceVolume 레이어]
  Output [CameraOutputController]
    EvidenceCamera [Base Camera, 평소 enabled=false]
Player [기존 GridOccupant, GridMover]
  EyeAnchor
Clue_Box [BoxCollider, InspectableTarget]
HumidZone [BoxCollider Trigger, CondensationZone]
기존 Canvas/HUDPanel
  Reticle
  InspectPrompt
  InspectPanel/Description/CloseButton
  PhotoPreview
```

## 입력과 게임 규칙

| 상황 | 기대 동작 |
| --- | --- |
| Ready | 시작 UI만 사용, Tab/E/P/이동/건설 무시 |
| Playing 탑뷰 | 좌클릭 이동, B 건설, 휠 줌, Tab 전환, F 닦기 |
| 이동 중 Tab | 경로 완료까지 대기, 추가 이동·건설 차단, Tab으로 요청 취소 |
| 화면 가림 | 이동·건설·조사·모드 재전환 차단 |
| 1인칭 | 마우스 회전, 우클릭 확대, E 조사, P 사진, Tab 복귀 |
| 조사 패널 | 커서 해제, 버튼/Esc로 닫기, 회전·전환·추가 조사 차단 |
| Win/Lose | 전환 요청 폐기, 탑뷰 복구, 패널 닫기, 커서 해제 |

기존 몬스터와 시간은 조사 중에도 계속 진행합니다. 60초 제한으로 실습하기 어렵다면 GameManager의 Time Limit을 연습 Scene에서만 늘립니다. 게임 루프가 없는 최소 Scene에서는 Controller의 Game Manager를 비워 항상 Playing처럼 실습할 수 있습니다.

## 순서대로 검증

1. 시작 전 Tab/B/E/P와 바닥 클릭이 동작하지 않는지 확인합니다.
2. 시작 후 기존 장애물 우회와 건설·파괴가 유지되는지 확인합니다.
3. 이동 중 Tab→취소→다시 Tab을 반복합니다. 도착 후 Player Transform과 GridOccupant.Cell이 같은 위치인지 확인합니다.
4. 건설 미리보기 중 Tab을 눌러 미리보기와 건설 상태가 정리되는지 확인합니다.
5. 전환 중 연타해도 모드가 꼬이지 않고 AudioListener 중복 경고가 없는지 확인합니다.
6. 1인칭에서 좌클릭/B로 이동·건설이 되지 않아야 합니다.
7. 단서 거리 밖, 벽 뒤, Trigger Collider, 대상 비활성화를 각각 확인합니다.
8. 단서 패널을 열었다 닫은 뒤 커서와 시선 회전이 정상인지 확인합니다.
9. 습도 구역에서 시점을 반복 전환해도 습기가 초기화되지 않는지 확인합니다.
10. 사진 두 장을 차례로 찍고 마지막 사진만 남는지 확인합니다.
11. 1인칭·전환 중·조사 중 각각 시간 초과를 발생시켜 결과 화면이 정상인지 확인합니다.
12. 재시작 후 초기 상태가 복구되는지 확인합니다. Window 포커스를 잃으면 입력과 커서 잠금이 해제되는지도 확인합니다.

## Frame Debugger와 Profiler

- Frame Debugger에서 월드 Base → 렌즈 Overlay의 실제 순서와 후처리 위치를 확인합니다.
- Overlay가 월드 전체를 다시 그리지 않아야 합니다. CameraLens 레이어만 그려야 합니다.
- EvidenceCamera는 사진 요청 때만 렌더링해야 합니다.
- 기본 Scene과 비교하여 CPU/GPU 프레임 시간, 카메라 렌더링, UI overdraw, 사진 촬영 순간 비용을 측정합니다.
- 16:9, 16:10, 좁은 창에서 보드 맞춤을 재실행하고 렌즈 화면 덮기·조사 패널·사진 비율을 확인합니다.
- 목표 기기가 정해지면 렌즈 해상도, 사진 크기, Volume 효과를 그 기기의 측정 결과로 조절합니다.

## 구현 범위와 이후 확장

완성되는 기능은 제자리 1인칭 관찰, 그리드 이동 연결, 가림 전환, 직교 맞춤과 줌, 조사, Overlay 결로와 닦기, 모드 Volume 혼합, 최근 사진 한 장입니다.

1인칭 WASD 이동을 추가하려면 GridOccupant의 점유 갱신·벽 충돌·이동 예약 규칙을 먼저 설계해야 합니다. Player Transform만 움직이는 스크립트를 덧붙이지 마세요. 이동 중 즉시 시점 전환도 GridMover의 안전한 정지 API 없이 OnDisable로 대체하지 않습니다.

굴절/블러, 연속적인 투영 전환, 조사 일시정지, 여러 사진 영구 저장은 별도 심화 단계입니다. 자동 Scene 구성이나 외부 FPS 시스템 연결을 기본 단계의 전제로 두지 않습니다.

## 공식 참고

- [Camera의 다섯 설정 그룹](https://docs.unity3d.com/kr/6000.0/Manual/urp/camera-component-reference.html)
- [Base/Overlay와 후처리 범위](https://docs.unity.com/en-us/engine/6000.3/manual/cameras/urp/camera-types-and-render-type/introduction)
- [카메라별 Volume 구성](https://docs.unity3d.com/kr/current/Manual/urp/cameras/apply-different-post-proc-to-cameras.html)

설정명과 Renderer 지원 여부는 설치된 URP 17.4.0 Inspector에서 최종 확인합니다.

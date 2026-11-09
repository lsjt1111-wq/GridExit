# 01. 탑뷰 이동과 1인칭 시점 전환

## 목표

기존 GridCamera를 그대로 사용하면서 탑뷰에서는 클릭 이동, 1인칭에서는 위치를 유지한 관찰을 합니다. 이동 중 Tab은 전체 경로 도착 후 처리됩니다. 01~02를 마치면 기본 탐험 순환이 완성됩니다.

## 사용할 코드

- `Runtime/ExplorationCameraController.cs`: 모드, 전환 대기, 화면 가림, 기존 입력 연결.
- `Runtime/FirstPersonLook.cs`: 마우스 시선 회전과 우클릭 확대.
- `GridTutorial.CameraSystem.asmdef`: GridTutorial, Input System, URP/Core, TMP/UI 참조.

기존 GameManager는 시작·결과 상태를 관리합니다. CameraController는 StateChanged와 현재 State를 확인하고 GridPathPreview/BuildController의 enabled를 조정합니다. GridMover는 모드 전환으로 끄지 않습니다. 끄면 기존 OnDisable이 이동을 중단하고 셀 중심으로 정렬하기 때문입니다.

## Scene 직접 구성

1. 기존 Gameplay Scene을 열고 `File > Save As`로 `Assets/GridSystem/CameraSystem/CameraTutorial.unity`에 저장합니다.
2. Hierarchy에 빈 `CameraSystem`을 만듭니다. Transform은 Position/Rotation=0, Scale=1입니다.
3. 기존 `Grid Demo Camera`를 CameraSystem 자식으로 옮깁니다. Inspector에서 기존 월드 위치와 회전이 유지됐는지 확인합니다.
4. 이 Camera의 Projection을 Orthographic으로 유지합니다. 기본 예시 구도는 Position=(5,14,4), Rotation=(90,0,0), Size=6.5, Near/Far=0.3/100입니다. 보드 크기가 다르면 기존 구도를 유지합니다.
5. Player 아래 빈 `EyeAnchor`를 만듭니다. 월드 위치가 발밑 지면에서 약 1.6m 위가 되도록 Scene 뷰에서 이동합니다. 기존 Player의 Scale/Elevation에 따라 local Y가 달라집니다.
6. EyeAnchor의 월드 회전은 (0,0,0), 앞 방향은 +Z로 시작합니다. 애니메이션 모델의 자식이 아니라 Player 루트의 자식으로 둡니다.
7. CameraSystem에 `ExplorationCameraController`, `FirstPersonLook`을 추가합니다.

## Inspector 연결

| ExplorationCameraController 필드 | 연결 |
| --- | --- |
| View Camera | 기존 Grid Demo Camera |
| Eye Anchor | Player/EyeAnchor |
| Mover | Player의 GridMover |
| Game Manager | 기존 GameManager |
| Path Preview | 기존 Path Preview의 GridPathPreview |
| Build Controller | 기존 Grid Build Controller; 건설이 없으면 비움 |
| Projection | 03까지 비움 |
| Look | CameraSystem의 FirstPersonLook |
| First Person Hidden Renderers | Player 몸통 Renderer만; Capsule이면 MeshRenderer, 모델이면 필요한 SkinnedMeshRenderer |
| Transition Duration | 0.4 |
| Show Hint | 체크 |

FirstPersonLook의 Controller에 CameraSystem의 Controller를 연결하고 Projection은 비워 둡니다. Sensitivity=0.12, Pitch Limit=80으로 시작합니다.

기존 GridPathPreview와 GridBuildController의 View Camera가 같은 Camera인지 다시 확인합니다. GameManager의 Path Preview/Build Controller도 위와 동일한 인스턴스여야 합니다. 다른 카메라 추적 스크립트가 이 Camera를 함께 조작하면 끕니다.

씬에 AudioListener는 하나만 둡니다. Player를 통째로 끄거나 Collider/GridOccupant를 제거하지 않습니다.

## 실행

1. Play 후 기존 시작 버튼을 누릅니다.
2. 빈 칸을 클릭해 이동합니다.
3. 이동 중 Tab을 누르면 대기 안내가 표시됩니다. 다시 Tab을 누르면 요청이 취소됩니다.
4. 다시 요청하고 도착하면 화면이 잠깐 어두워진 뒤 1인칭으로 바뀝니다.
5. 마우스로 둘러보고 우클릭을 유지해 확대합니다. Tab으로 원래 탑뷰에 복귀합니다.
6. 건설 미리보기 중 전환하면 미리보기가 제거되는지 확인합니다.

전환 중에는 클릭 이동과 건설이 차단됩니다. 게임 시간과 몬스터는 계속 진행합니다. 관찰 중에도 공격받을 수 있습니다.

## 통과 조건과 오류

- 시작 전 Tab 무시, Playing에서만 전환, 결과에서는 커서 해제.
- 1인칭 클릭으로 Player가 움직이지 않고 B가 건설을 켜지 않음.
- 탑뷰 복귀 시 기존 위치·회전·Size 복원.
- 몸통이 보이면 Hidden Renderers를 확인합니다. Renderer만 연결합니다.
- 화면이 바닥 안이면 EyeAnchor의 월드 높이와 카메라 Near를 확인합니다.
- 마우스가 회전하지 않으면 Game 창 포커스, Look 참조, Input System 패키지를 확인합니다.
- Console의 필수 참조 오류가 나면 Play를 끄고 참조를 채운 뒤 재시작합니다.

다음: [02. 1인칭 관찰과 조사](CAMERA_02_FIRST_PERSON_INSPECTION.md)

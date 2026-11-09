# 02. 1인칭 관찰과 조사

## 목표와 코드

화면 중앙으로 2.5m 안의 단서를 보고 E로 조사합니다. `FirstPersonInteractor`는 가장 가까운 Collider를 먼저 확인하므로 벽이 앞에 있으면 뒤 단서는 조사하지 못합니다. `InspectableTarget`은 제목·내용과 최초 조사 이벤트를 관리합니다.

## 단서 만들기

1. `3D Object > Cube`를 만들고 `Clue_Box`로 이름을 바꿉니다.
2. Player가 접근할 수 있는 셀 옆에 배치합니다. 크기는 약 (0.3,0.3,0.3), 높이는 시선에서 보이는 곳으로 맞춥니다.
3. BoxCollider는 유지하고 Is Trigger는 끕니다.
4. `InspectableTarget`을 추가하고 Display Name과 Description에 단서 이름과 설명을 입력합니다.
5. 장식용 단서는 GridOccupant 없이 둘 수 있습니다. 길을 막아야 한다면 기존 Grid 튜토리얼에 따라 GridOccupant를 추가합니다. Collider만으로 Grid 경로가 차단되지는 않습니다.
6. Player 본인 Collider가 있는 오브젝트에 `ExplorationPlayer` 레이어를 만들고 지정합니다. 자식 전체에 적용할지는 Collider와 시각 오브젝트를 확인해 결정합니다.

## UI 만들기

기존 HUDPanel 아래에 다음을 직접 만듭니다. EventSystem은 기존 것을 사용합니다.

```text
HUDPanel
  Reticle           [UI > Text - TextMeshPro, 텍스트 +, 중앙]
  InspectPrompt     [TMP, 중앙 아래]
  InspectPanel      [UI > Panel, 처음 비활성]
    Description     [TMP, 자동 줄바꿈]
    CloseButton     [UI > Button]
```

1. Reticle의 RectTransform Anchor를 중앙, Position=(0,0), 크기=(30,30)으로 설정합니다.
2. InspectPrompt는 중앙 아래 약 50px 위치, 크기=(500,50)으로 둡니다.
3. InspectPanel은 화면 중앙, 약 (650,350), 배경 불투명도를 0.9로 설정합니다.
4. Description은 패널 내부에 여백 30px을 두고 늘립니다. 글꼴에 한글이 포함돼야 합니다.
5. Reticle, Prompt, Description의 Raycast Target을 끕니다. 패널 배경과 버튼은 유지합니다.
6. Controller가 있는 CameraSystem에 `FirstPersonInteractor`를 추가합니다. 이 스크립트를 비활성 패널에 붙이지 않습니다.

| FirstPersonInteractor | 연결 |
| --- | --- |
| Controller | CameraSystem의 Controller |
| Distance | 2.5 |
| Hit Mask | Everything에서 ExplorationPlayer만 제외; 벽·장애물·단서는 포함 |
| Prompt / Description | InspectPrompt / Description |
| Panel / Reticle | InspectPanel / Reticle |

CloseButton의 On Click에 FirstPersonInteractor를 드래그하고 `Close()`를 선택합니다. Esc도 패널을 닫습니다.

## 실행과 코드 읽기

1. 단서 근처로 탑뷰 이동하고 Tab으로 관찰합니다.
2. 중앙에 단서를 두면 E 안내가 나옵니다. E를 누르면 설명 패널이 열리고 커서가 풀립니다.
3. 패널 중에는 시선 회전과 모드 전환을 막습니다. 닫으면 회전이 복구됩니다.
4. InspectableTarget의 On First Inspected에는 최초 발견 시 실행할 UI나 사운드의 공개 메서드를 연결할 수 있습니다. 같은 대상 재조사로 이벤트가 중복 실행되지는 않습니다.

조사 Ray는 CameraController가 눈 위치를 적용한 다음 LateUpdate에서 계산합니다. QueryTriggerInteraction.Ignore를 사용하므로 단서 Collider는 Trigger가 아니어야 합니다. UI를 연결하지 않아도 Console에 설명을 출력해 기본 판정을 확인할 수 있습니다.

## 통과 조건

- 2.5m 밖에서는 안내와 조사가 불가합니다.
- 단서 앞에 일반 Cube 벽을 두면 단서 안내가 사라집니다. 벽 레이어를 Hit Mask에서 빼면 안 됩니다.
- 본인 Capsule이 Ray를 막지 않습니다.
- 대상 비활성화 또는 시선 이탈 시 안내가 사라집니다.
- 패널을 연 상태에서 패배하면 패널이 닫히고 커서가 풀립니다.

다음: [03. Projection 직접 설계](CAMERA_03_PROJECTION.md)

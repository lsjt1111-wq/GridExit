# 7단계부터: Grid BuildSystem 직접 세팅하기

이 문서는 [README.md](README.md)의 Grid, Player, Monster, PathPreview가 있는 Scene에서 이어집니다. 에디터 자동 생성 메뉴 없이 Hierarchy와 Inspector에서 직접 세팅합니다. 모든 설정은 Play를 끈 상태에서 진행하고 Scene을 저장하세요.

목표 흐름은 **Build 명령 → 마우스 위치에 건물 미리보기 → 초록/빨강 설치 판정 → 클릭 설치 → 몬스터가 막힌 길의 바리케이드 공격 → 파괴 후 통과**입니다.

## 7. 기존 FPS 코드와 연결되는 개념

기존 `CoverCombat/FPS/Scripts/FpsBuildGrid.cs`, `FpsBuildDefinition.cs`, `FpsGridBuilder.cs`의 정의/판정/미리보기 분리를 참고했습니다. 새 코드는 `GridTutorial` 네임스페이스에서 현재 GridBoard를 사용합니다.

| 역할 | 새 코드 | 책임 |
| --- | --- | --- |
| 건물 설계 데이터 | GridBuildDefinition | Renderer Prefab, 셀 너비·깊이, 체력, 모델 보정 |
| 설치 판정과 생성 | GridBuildSystem | Grid 전체 footprint 검사, 실제 건물 생성 |
| 플레이어 Build 명령 | GridBuildController | B/UI 입력, 초록·빨강 미리보기, R 회전, 클릭 설치 |
| 설치된 건물 | GridBuilding | 점유한 셀, 현재 체력, 피해와 파괴 |
| 몬스터 행동 | GridMonsterAttacker | 플레이어 추적, 막힌 바리케이드 공격 |

FPS의 준비 페이즈·크레딧·아이콘·지형 Raycast에 대한 의존성은 이 실습에 추가하지 않았습니다. 현재는 Grid 평면 위에서 셀 점유로 판정합니다. 모든 장애물·캐릭터는 GridOccupant로 등록하고, 건물은 GridBuilding으로 등록해야 판정에 반영됩니다.

## 8. Renderer 전용 3D 프리팹 만들기

1. Hierarchy에서 `3D Object > Cube`를 만듭니다. 이름을 `BarricadeVisual`로 바꿉니다.
2. Transform을 Position=(0,0,0), Rotation=(0,0,0), Scale=(1,1,1)로 설정합니다.
3. URP/Unlit 머티리얼을 만들고 갈색으로 설정한 뒤 Cube에 연결합니다. 조명 없이도 실제 설치 모델을 확인할 수 있습니다.
4. Cube의 Box Collider를 제거합니다. 최종 충돌체는 BuildSystem이 따로 만듭니다.
5. Project에서 `Assets/GridSystem/Prefabs` 폴더를 만들고 Cube를 그 폴더로 드래그해 프리팹으로 저장합니다.
6. Hierarchy에 남은 Cube는 삭제합니다. 이후 정의에는 Project의 프리팹 에셋을 연결합니다.

이 프리팹은 MeshFilter/MeshRenderer 또는 SkinnedMeshRenderer 등 **모양만 표현하는 오브젝트**입니다. GridOccupant, GridBuilding, Rigidbody, 게임플레이 스크립트는 넣지 않습니다. 자식 Renderer가 여러 개여도 지원합니다. 미리보기는 모든 Renderer의 머티리얼을 색상용 머티리얼로 바꿉니다. 생성 시 Collider와 Behaviour를 비활성화하므로 애니메이션이나 프리팹 자체 스크립트 실행에 의존하지 않는 모델을 사용하세요.

## 9. ScriptableObject로 Grid 크기 설계

1. Project에서 `Assets/GridSystem/Definitions` 폴더를 만듭니다.
2. 폴더에서 우클릭 → `Create > Grid Tutorial > Build Definition`을 선택하고 이름을 `Barricade_2x1`로 지정합니다.
3. 에셋을 선택하고 다음 값을 입력합니다.

| 필드 | 값 | 의미 |
| --- | --- | --- |
| Display Name | Barricade | 설치 오브젝트 이름 |
| Renderer Prefab | BarricadeVisual 프리팹 | 실제 보일 3D 모델 |
| Width | 2 | 회전 전 X방향 셀 수 |
| Depth | 1 | 회전 전 Z방향 셀 수 |
| Collision Height | 1 | 생성할 충돌체의 높이 |
| Max Health | 100 | 건물 한 개의 체력 |
| Visual Offset | (0,0.5,0) | 모델을 지면 위로 올리는 보정 |
| Visual Euler | (0,0,0) | 모델의 기본 방향 보정 |
| Visual Scale | (2,1,1) | 단위 Cube를 2m×1m×1m로 표시 |

**Width/Depth와 Visual Scale은 서로 다른 설정입니다.** 전자는 논리적인 셀 점유, 후자는 모델 크기입니다. 모델 크기를 footprint에 맞춰 자동 변경하지 않습니다. 이 예시는 Cell Size=1 기준입니다. Cell Size=2라면 같은 2×1 건물의 가로·깊이는 4m×2m가 되므로 모델 Scale도 조절해야 합니다.

마우스로 가리키는 셀은 건물 footprint의 **왼쪽 아래 Anchor**입니다. Anchor=(2,3), 크기=(2,1)이면 (2,3), (3,3)을 점유하고 모델 중심은 (3,0,3.5)에 배치됩니다. R을 누르면 크기가 (1,2)로 바뀌고 같은 Anchor에서 (2,3), (2,4)를 점유합니다. 회전 시 프리팹도 Y축으로 90도 회전합니다.

## 10. GridBoard에 BuildSystem 부착

1. Hierarchy의 `Grid`를 선택합니다.
2. `Add Component > GridBuildSystem`을 추가합니다.
3. 기존 GridBoard, GridVisual은 유지합니다. Grid의 Scale은 (1,1,1)로 둡니다.

GridBuildSystem은 같은 오브젝트의 GridBoard를 사용하므로 Board 필드를 따로 연결하지 않습니다. 셀마다 다음 정보가 함께 저장됩니다.

```text
GridCell
 ├─ Coordinate / Number
 ├─ Occupant : 한 칸짜리 Player / Monster / Obstacle
 └─ Building : 여러 셀에서 공유하는 GridBuilding 참조
```

둘 중 하나라도 있으면 설치를 거부합니다. Player의 일반 A*도 Building이 들어 있는 셀은 통과하지 않습니다. 하나의 건물이 2칸을 차지해도 체력은 하나이며, 어느 쪽에서 공격해도 같은 건물에 피해가 들어갑니다.

## 11. 플레이어의 Build 명령과 미리보기 연결

1. Project에서 `BuildPreview` 머티리얼을 만듭니다. Shader=`Universal Render Pipeline/Unlit`으로 지정합니다. 불투명 머티리얼로 시작해도 됩니다.
2. Hierarchy에 빈 오브젝트 `BuildController`를 만듭니다.
3. `GridBuildController`를 추가하고 아래 참조를 연결합니다.

| 필드 | 연결 대상 |
| --- | --- |
| Build System | Hierarchy의 Grid (GridBuildSystem) |
| Selected Definition | Project의 Barricade_2x1 에셋 |
| View Camera | 기존 GridCamera |
| Preview Material | Project의 BuildPreview 머티리얼 |
| Show Hint | 체크 |

4. 기존 `PathPreview` 오브젝트를 선택합니다. GridPathPreview의 새 **Build Controller** 필드에 Hierarchy의 BuildController를 연결합니다. 설치 중 이동 클릭과 경로선 표시를 중지하는 연결입니다.
5. Scene을 저장하고 Play를 누릅니다. Game 뷰에 포커스를 주고 B를 누릅니다.

| 입력 | 동작 |
| --- | --- |
| B | 설치 모드 시작 / 종료 |
| 마우스 이동 | 셀에 맞춰 3D 모델과 footprint 이동 |
| R | 2×1 ↔ 1×2 회전 |
| 왼쪽 클릭 | 설치 가능할 때 설치. 연속 설치 모드 유지 |
| 오른쪽 클릭 / Esc | 설치 취소, 일반 이동 모드 복귀 |

초록색은 모든 점유 셀이 Grid 안이고 비어 있다는 의미입니다. 빨간색은 Grid 밖 또는 기존 점유 정보와 겹친 상태입니다. 모델과 얇은 footprint Cube를 함께 색칠합니다. 바닥 평면을 가리킬 수 없거나 UI 위에 있으면 미리보기를 숨깁니다. 원본 모델/머티리얼 에셋을 직접 바꾸지 않고 런타임 머티리얼 복사본만 색칠합니다.

확인 순서:

1. 빈 셀 → 초록색인지 확인합니다.
2. Player/Monster/Obstacle 셀과 겹치기 → 빨간색이고 클릭해도 생성되지 않아야 합니다.
3. 맨 오른쪽 열에서 2×1 선택 → 일부가 밖으로 나가므로 빨간색이어야 합니다.
4. R 회전 후 위쪽으로 여유가 있으면 초록색으로 바뀌어야 합니다.
5. 설치 후 같은 위치를 가리키면 빨간색이어야 합니다. GridVisual에서 점유한 각 셀에 Build가 표시됩니다.
6. Esc 후 빈 셀을 클릭하면 기존 Player 이동이 동작해야 합니다.

### 선택 실습: 화면의 Build 버튼 연결

B키 대신 버튼으로 시작하려면 다음 과정을 추가합니다.

1. Hierarchy 우클릭 → `UI > Button - TextMeshPro`를 만듭니다. Canvas와 EventSystem이 없다면 함께 생성됩니다.
2. Canvas의 Render Mode를 `Screen Space - Overlay`로 둡니다. 버튼을 화면 위쪽에 배치하고 텍스트를 `Build`로 바꿉니다.
3. Button 컴포넌트의 `On Click ()`에 `+`를 누릅니다.
4. 대상에 Hierarchy의 BuildController를 드래그합니다.
5. 함수에서 `GridBuildController > BeginBuild()`를 선택합니다.
6. 새 Input System을 사용하는 Scene의 EventSystem에는 `InputSystemUIInputModule`을 사용합니다. Standalone Input Module만 있다면 교체하고, UI 모듈의 기본 Actions가 비어 있다면 Assign Default Actions로 연결합니다.

UI 위에서 클릭할 때는 설치와 이동을 모두 무시합니다. 여러 건물 버튼을 만들려면 각 버튼에서 `SelectDefinition(GridBuildDefinition)`을 선택하고 해당 에셋을 인자로 지정합니다. 취소 버튼은 `CancelBuild()`에 연결합니다.

## 12. 몬스터가 바리케이드 공격하기

1. Hierarchy의 `Monster`를 선택합니다. 기존 GridOccupant의 Kind=`Monster`, Board=`Grid`를 확인합니다.
2. `GridMonsterAttacker`를 추가합니다. 필요한 GridMover가 없으면 자동 추가됩니다.
3. 다음 값을 설정합니다.

| 필드 | 값 |
| --- | --- |
| Player | Hierarchy의 Player (GridOccupant) |
| Damage | 25 |
| Attack Interval | 1 |
| Repath Interval | 0.25 |
| GridMover의 Speed | 2 |

몬스터는 먼저 플레이어 옆 셀까지 **건물을 부수지 않는 경로**를 찾습니다. 우회할 수 있으면 우회합니다. 모든 우회 경로가 막히면 건물 셀을 파괴 가능한 경로 후보로 포함하여 다시 탐색합니다. 다음 칸에 건물이 있으면 이동하지 않고 인접 칸에서 공격합니다. 이 특별 경로는 몬스터의 판단에만 사용하며 GridMover의 통과 판정은 그대로 유지합니다.

몬스터는 이 실습에서 플레이어 옆에 도착하면 정지합니다. 플레이어 체력/피해, 공격 애니메이션, 보상, 설치 비용은 이후 게임 규칙을 붙이는 지점입니다. 공격은 물리 Collider의 거리 대신 셀 상하좌우 인접 여부를 사용합니다.

### 공격과 파괴를 확실히 재현하는 배치

우회 경로가 열려 있으면 몬스터가 공격하지 않으므로, 아래와 같이 완전히 막힌 통로를 구성합니다.

1. Play를 끕니다. 기존 다섯 Obstacle을 모두 비활성화합니다.
2. Player의 Initial Cell=(2,3), Position=(2.5,1,3.5)로 설정합니다.
3. Monster의 Initial Cell=(7,3), Position=(7.5,0.5,3.5)로 설정합니다.
4. 장애물 Cube를 X=4열의 Z=0,1,2,5,6,7에 여섯 개 만듭니다. 각각 GridOccupant의 Board=`Grid`, Kind=`Obstacle`, Elevation=0.5로 설정합니다.

| Initial Cell | Transform Position |
| --- | --- |
| (4,0) | (4.5,0.5,0.5) |
| (4,1) | (4.5,0.5,1.5) |
| (4,2) | (4.5,0.5,2.5) |
| (4,5) | (4.5,0.5,5.5) |
| (4,6) | (4.5,0.5,6.5) |
| (4,7) | (4.5,0.5,7.5) |

5. **Monster의 GridMonsterAttacker 컴포넌트 체크를 해제**하여 AI를 잠시 멈춥니다.
6. Play 후 B → R로 1×2로 회전하고 Anchor=(4,3)에 바리케이드를 설치합니다. (4,3)과 (4,4)를 막으면 X=4열 전체가 벽이 됩니다.
7. Esc로 설치 모드를 끝내고 Monster의 GridMonsterAttacker를 활성화합니다.
8. Monster가 벽 앞까지 이동하여 바리케이드만 공격하는지 확인합니다.
9. Hierarchy의 Grid 아래 생성된 Barricade를 선택하고 GridBuilding의 Health가 100→75→50→25→0으로 줄어드는지 확인합니다. 공격 연출은 아직 없으므로 이 수치로 확인합니다.
10. 파괴 시 건물이 사라지고 두 셀의 Build 표시가 모두 없어지며, Monster가 열린 통로로 이동하여 Player 옆에 멈추면 완료입니다.

## 13. 흐름과 코드 읽기

```text
BeginBuild() / B
  → 정의 데이터를 읽어 미리보기 생성
  → WorldToCell로 Anchor 획득
  → IsAvailable → CanBuild로 footprint 전체 검사
  → 초록 / 빨강 갱신
  → 왼쪽 클릭 → TryBuild에서 재검사
  → GridBuilding 생성 → 각 셀에 동일한 건물 참조 등록

Monster
  → 열린 경로 탐색
  → 없으면 건물을 포함한 경로 계획
  → 다음 셀이 Building이면 TakeDamage
  → Health=0 → 점유한 모든 셀 해제 후 건물 삭제
  → 재탐색 후 이동 재개
```

별도의 공격 시스템을 연결하려면 `GridBuilding.TakeDamage(float)`를 호출합니다. `HealthChanged` 이벤트로 HP 게이지를 갱신할 수 있습니다. 건물을 삭제하거나 비활성화하면 점유 셀을 해제합니다. 살아 있는 건물을 다시 활성화하면 재등록하며, 다른 오브젝트가 점유 중이면 다시 비활성화합니다.

## 14. 현 단계의 범위와 확인 사항

- 건물 실체는 GridBoard의 자식이며 원래 3D 모델은 건물의 자식입니다. 건물에 GridOccupant를 추가하지 않습니다.
- 기존 A* 이동과 GridVisual은 건물 정보에 대응합니다. GridPathPreview의 Build Controller 참조는 꼭 연결하세요.
- 설치 가능 여부는 셀 점유로 판정합니다. 미등록 Collider, 절벽, 지면 유무, 모델이 footprint 밖으로 나온 부분은 판정하지 않습니다.
- 설치 거리·자금·건설 시간·업그레이드는 미구현입니다. GridBuildSystem.IsAvailable이 추가 규칙의 진입점입니다.
- 이동을 시작한 캐릭터는 다음 셀을 예약합니다. 출발 셀은 해제되므로 보간 중 시각적 겹침까지 방지하려면 출발 셀의 추가 예약이 필요합니다.
- 몬스터의 파괴 경로는 걸음 수로 비교합니다. 건물 HP를 이동 비용으로 비교하는 전술 판단은 미구현입니다.
- 에디터/플레이어 조건 컴파일과 Unity 실행·시각 확인은 별개입니다. 위 설치 판정 및 공격 재현 절차를 Scene에서 확인하세요.

| 문제 | 확인 |
| --- | --- |
| B로 시작되지 않음 | Game 포커스, BuildController 참조, 정의의 Renderer Prefab과 수치, Console 경고 |
| 항상 빨간색 | footprint 전체가 Grid 안인지, 점유자가 없는지, Grid Scale이 1인지 확인 |
| 색이 바뀌지 않음 | Preview Material이 URP/Unlit인지 확인. 전용 머티리얼 연결 |
| 설치 클릭에 Player도 이동 | GridPathPreview의 Build Controller 참조 확인 |
| 모델 위치나 크기가 어긋남 | Visual Offset / Euler / Scale과 Prefab 피벗 조절 |
| 몬스터가 건물을 공격하지 않음 | 우회 가능한지 먼저 확인. 공격 재현용 벽 배치와 Player 참조 확인 |
| HP 감소 확인이 어려움 | Play 중 생성된 건물의 GridBuilding Health 확인 |

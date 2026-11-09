# GridSystem 단계별 튜토리얼

**Intro · 설정 화면:** [로고·타이틀·AudioMixer·Windows 화면 설정](INTRO_SETTINGS_TUTORIAL.md). 기존 게임 루프와 철문 전환에 이어서 진행합니다.

**시작 버튼에 씬 전환 적용:** [5. 철문 화면 전환 후 게임 시작](GAME_LOOP_05_SCENE_TRANSITION.md). 공용 API의 설치·코드 사용법은 [화면 전환 튜토리얼](../../ScreenTransition/TUTORIAL.md)에 있습니다.

Unity 6 / URP / TextMeshPro / Input System 기준의 독립 실습 코드입니다. 기존 FPS 시스템과 별개로 `GridTutorial` 네임스페이스를 사용합니다. 각 단계를 끝낸 뒤 다음 컴포넌트를 추가하며 진행합니다.

## Unity 씬에서 직접 세팅하기

아래 과정은 `Tools > Grid Tutorial > Create Demo`를 사용하지 않습니다. Hierarchy에서 오브젝트를 만들고 Inspector에서 직접 컴포넌트와 참조를 연결합니다. 모든 값은 **Play를 끈 상태**에서 입력하고 Scene을 저장하세요.

Grid 선과 셀 번호는 GridVisual이 Play 시 생성합니다. 셀마다 TextMeshPro를 수동 배치하거나 Canvas를 만들 필요는 없습니다. 편집 중에는 Scene의 Gizmos 선과 좌표로 배치를 확인합니다.

### A. 빈 Scene과 TMP 준비

1. `File > New Scene`에서 빈 Scene을 만들고 `Assets/GridSystem` 아래에 `GridTutorial.unity`로 저장합니다.
2. `Window > TextMeshPro > Import TMP Essential Resources`를 실행합니다. 이미 가져왔다면 생략합니다.
3. Console에 컴파일 오류가 없는지 확인합니다. 기존 Scene을 사용한다면 카메라는 하나만 활성화하세요.

### B. Grid 오브젝트 생성

1. Hierarchy 빈 공간에서 우클릭 → `Create Empty`를 선택하고 이름을 `Grid`로 바꿉니다.
2. Transform을 다음과 같이 설정합니다.

| 항목 | X | Y | Z |
| --- | --- | --- | --- |
| Position | 0 | 0 | 0 |
| Rotation | 0 | 0 | 0 |
| Scale | 1 | 1 | 1 |

3. `Add Component`로 `GridBoard`를 추가합니다. 검색에서 찾기 어렵다면 Project의 `GridBoard.cs`를 Inspector로 드래그합니다.
4. Width=`10`, Height=`8`, Cell Size=`1`을 설정합니다.
5. Scene 뷰의 `Gizmos`를 켭니다. Grid는 월드 X=0~10, Z=0~8 영역에 표시됩니다. 셀 좌표 (0,0)의 중심은 (0.5,0,0.5)입니다.

### C. 머티리얼과 Game 뷰 시각화 설정

1. Project의 GridSystem 폴더에서 우클릭 → `Create > Material`로 `GridLine`을 만듭니다.
2. Shader를 `Universal Render Pipeline/Unlit`으로 선택합니다. Base Map 색상은 밝은 청록색으로 설정합니다. 이 머티리얼은 조명이 없어도 보입니다.
3. 같은 방식으로 `PathLine` 머티리얼을 만들고 Base Map 색상은 노란색으로 설정합니다.
4. Grid 오브젝트에 `GridVisual`을 추가하고 다음 값을 연결합니다.

| GridVisual 항목 | 설정 |
| --- | --- |
| Line Material | Project의 GridLine 머티리얼을 드래그 |
| Font | TMP Font Asset 지정. 비워두면 TMP 기본 글꼴 사용 |
| Line Color | 밝은 청록색 |
| Show Numbers | 체크 |
| Show Occupants | 체크 |

URP/Unlit에서는 선의 색상이 머티리얼 색상으로 보일 수 있으므로 GridLine과 PathLine의 Base Map 색상으로 구분합니다. 이동 방향은 경로선의 굵은 출발점에서 가는 도착점 쪽으로 읽습니다.

### D. 바닥 생성

1. Hierarchy 우클릭 → `3D Object > Cube`를 만들고 이름을 `Floor`로 바꿉니다.
2. Transform을 다음과 같이 설정합니다. 부모는 없는 상태를 기준으로 합니다.

| 항목 | X | Y | Z |
| --- | --- | --- | --- |
| Position | 5 | -0.1 | 4 |
| Rotation | 0 | 0 | 0 |
| Scale | 10 | 0.1 | 8 |

3. `FloorMaterial`을 만들고 Shader=`Universal Render Pipeline/Unlit`, Base Map=짙은 회색으로 설정한 뒤 Floor의 Mesh Renderer에 연결합니다.

바닥 윗면은 Y=-0.05이므로 Grid 선과 번호를 가리지 않습니다. Floor에는 GridOccupant를 추가하지 않습니다.

### E. 위에서 내려다보는 카메라 설정

1. Hierarchy 우클릭 → `Camera`를 생성하고 이름을 `GridCamera`로 바꿉니다. 기존 Main Camera를 사용한다면 이름 변경 없이 같은 값을 설정해도 됩니다.
2. Transform과 Camera를 다음과 같이 설정합니다.

| 항목 | 값 |
| --- | --- |
| Position | (5, 14, 4) |
| Rotation | (90, 0, 0) |
| Scale | (1, 1, 1) |
| Projection | Orthographic |
| Size | 6.5 |
| Near / Far Clipping Planes | 0.3 / 100 |
| Culling Mask | Everything |

3. Game 뷰 비율을 `16:9`로 설정합니다. 이 카메라에서는 +X가 화면 오른쪽, +Z가 화면 위쪽입니다.

카메라는 뒤의 View Camera 필드에 직접 연결하므로 MainCamera 태그가 필수는 아닙니다.

### F. 플레이어와 몬스터 배치

1. `3D Object > Capsule`을 만들고 이름을 `Player`로 바꿉니다.
2. Player에 `GridOccupant`와 `GridMover`를 추가합니다.
3. `3D Object > Sphere`를 만들고 이름을 `Monster`로 바꿉니다. Monster에는 `GridOccupant`만 추가합니다.
4. 아래 표대로 값을 설정합니다. Board에는 **Hierarchy의 Grid 오브젝트**를 드래그합니다.

| 오브젝트 | Transform Position | Scale | Board | Kind | Initial Cell X / Y | Elevation |
| --- | --- | --- | --- | --- | --- | --- |
| Player | (0.5, 1, 0.5) | (0.7, 0.7, 0.7) | Grid | Player | 0 / 0 | 1 |
| Monster | (7.5, 0.5, 5.5) | (0.7, 0.7, 0.7) | Grid | Monster | 7 / 5 | 0.5 |

Initial Cell의 **Y는 월드 높이가 아니라 Grid의 Z방향 좌표**입니다. Play 시 위치는 Initial Cell과 Elevation을 기준으로 자동 정렬됩니다. 편집 중에도 같은 위치로 보이도록 Transform Position을 함께 입력합니다.

5. Player의 GridMover에서 Speed=`3`을 설정합니다.
6. Player와 Monster에 각각 초록색·빨간색 URP/Unlit 머티리얼을 만들어 연결하면 조명 없이도 구분할 수 있습니다. Rigidbody는 추가하지 않습니다.

### G. 우회 경로를 만들 장애물 배치

1. `3D Object > Cube`를 만들고 이름을 `Obstacle_4_0`으로 바꿉니다.
2. Scale=(0.9,1,0.9)로 설정하고 GridOccupant를 추가합니다.
3. Board=`Grid`, Kind=`Obstacle`, Elevation=`0.5`로 설정합니다.
4. 복제하여 아래 다섯 개를 만듭니다. 각 복제본의 **Initial Cell도 반드시 변경**합니다.

| 이름 | Initial Cell X / Y | Transform Position |
| --- | --- | --- |
| Obstacle_4_0 | 4 / 0 | (4.5, 0.5, 0.5) |
| Obstacle_4_1 | 4 / 1 | (4.5, 0.5, 1.5) |
| Obstacle_4_2 | 4 / 2 | (4.5, 0.5, 2.5) |
| Obstacle_4_4 | 4 / 4 | (4.5, 0.5, 4.5) |
| Obstacle_4_5 | 4 / 5 | (4.5, 0.5, 5.5) |

(4,3)은 비워둡니다. Player가 벽 너머로 이동할 때 통과할 틈입니다. 장애물에 회색 URP/Unlit 머티리얼을 연결합니다. Collider만 배치하면 경로 탐색이 장애물로 인식하지 않으므로 GridOccupant를 꼭 연결합니다.

### H. 경로 미리보기 연결

1. Hierarchy에서 `Create Empty`로 `PathPreview`를 만듭니다. Transform은 Position/Rotation=(0,0,0), Scale=(1,1,1)로 둡니다.
2. `GridPathPreview`를 추가합니다. 필요한 `Line Renderer`가 자동 추가됩니다.
3. 다음 참조를 드래그하여 연결합니다.

| GridPathPreview 항목 | 연결 대상 |
| --- | --- |
| Actor | Hierarchy의 Player (GridOccupant) |
| View Camera | Hierarchy의 GridCamera (Camera) |
| Line Material | Project의 PathLine 머티리얼 |

Line Renderer의 점 개수·좌표·굵기는 실행 중 스크립트가 설정합니다. Inspector에서 경로 점을 직접 입력하지 않아도 됩니다. 별도 Input Actions 에셋이나 PlayerInput 컴포넌트도 필요하지 않습니다.

### I. Hierarchy와 실행 확인

완성된 기본 구조는 다음과 같습니다. 아래는 모두 Scene 루트에 두어도 됩니다.

```text
Grid                  [GridBoard, GridVisual]
Floor                 [Mesh Renderer, Box Collider]
GridCamera            [Camera]
Player                [GridOccupant, GridMover]
Monster               [GridOccupant]
Obstacle_4_0          [GridOccupant]
Obstacle_4_1          [GridOccupant]
Obstacle_4_2          [GridOccupant]
Obstacle_4_4          [GridOccupant]
Obstacle_4_5          [GridOccupant]
PathPreview           [Line Renderer, GridPathPreview]
```

1. Scene을 저장하고 Play를 누릅니다.
2. Game에서 Grid 선과 1~80 번호를 확인합니다. Scene에서는 Gizmos를 켜 좌표를 확인합니다.
3. Game 뷰를 클릭해 포커스를 주고, 플레이어 오른쪽 벽 너머의 빈 셀 위에 마우스를 둡니다. 틈을 통과하는 경로선이 나타나야 합니다.
4. 왼쪽 클릭하면 Player가 경로를 따라 이동합니다. 이동 중에는 미리보기와 추가 클릭을 무시합니다.
5. 도착 후 해당 셀에 Player 정보가 표시되는지 확인합니다. 장애물·Monster 셀이나 Grid 밖을 가리키면 경로가 사라져야 합니다.
6. Hierarchy에서 장애물 하나를 비활성화하면 셀 점유가 해제되어 통과할 수 있습니다.

Play 중 Grid 아래에 생기는 `Grid Runtime Visuals`와 그 안의 `Grid Line`, `Cell 1` 등의 오브젝트는 자동 생성 결과입니다. Play 종료 후 사라지는 것이 정상입니다.

### J. 설정 문제 확인

| 증상 | 확인할 항목 |
| --- | --- |
| Add Component에서 스크립트가 검색되지 않음 | Console 컴파일 오류, 파일명과 클래스명 일치 여부 확인 |
| 번호가 보이지 않음 | TMP Essential Resources, GridVisual의 Font, Show Numbers 확인 |
| 선이 분홍색 | 머티리얼 Shader를 URP/Unlit으로 설정하고 Line Material에 연결 |
| 오브젝트가 어둡게 보임 | 설명대로 URP/Unlit 머티리얼을 연결하거나 Scene 조명 추가 |
| Grid가 화면에 잘림 | 카메라 Position/Rotation, Orthographic Size, Game 비율 확인 |
| 번호나 선이 바닥에 가려짐 | Floor의 위치와 스케일을 D단계 값으로 설정 |
| 경로가 전혀 보이지 않음 | Actor, View Camera, Line Material 연결과 Player의 GridMover 확인 |
| 클릭해도 이동하지 않음 | Game 뷰 포커스, 빈 목적 셀, 이동 중인지 확인 |
| 등록 실패 오류 | 모든 GridOccupant의 Board, Initial Cell 범위, 중복 좌표 확인 |
| 장애물을 그대로 통과함 | 장애물의 GridOccupant와 Kind, Initial Cell 확인. Transform만 옮겨서는 셀 정보가 바뀌지 않음 |

아래부터는 직접 구성한 Scene을 기준으로 코드의 역할과 학습 순서를 설명합니다.

## 1. Grid 데이터와 좌표: GridBoard.cs

빈 오브젝트에 GridBoard만 추가합니다. Width=10, Height=8, Cell Size=1로 시작합니다. Transform의 원점은 왼쪽 아래 모서리이며, 로컬 +X는 오른쪽, +Z는 위쪽입니다. 먼저 회전 0, 스케일 1로 실습하세요. 상부 카메라가 +Z를 화면 위로 보도록 배치해야 화면에서도 같은 방향입니다.

셀 좌표는 0부터, 셀 번호는 1부터 시작합니다.

```text
z=2   21 22 23 24 ... 30
z=1   11 12 13 14 ... 20
z=0    1  2  3  4 ... 10
       x=0 →
```

`번호 = z * Width + x + 1`. `WorldToCell`은 월드 위치를 로컬 위치로 바꾼 뒤 floor로 셀을 구합니다. 음수 좌표와 오른쪽/위쪽 끝 경계는 Grid 밖입니다. `Center`는 셀 중심의 월드 위치를 돌려줍니다. `GetCell`은 범위 밖이면 null을 반환합니다.

확인: Scene에서 (0,0), (9,0), (0,7) 좌표를 찾습니다. Grid를 옮겨도 좌표와 선이 함께 이동하는지 봅니다. Width/Height/Cell Size는 Play 전에 정합니다. 실행 중 크기 변경은 지원하지 않습니다.

## 2. Game 뷰 선과 TMP 번호: GridVisual.cs

같은 오브젝트에 GridVisual을 추가합니다. URP/Unlit 머티리얼을 만들어 Line Material에 연결합니다. 번호 글꼴은 Font에 지정하거나 TMP 기본 글꼴을 사용합니다. 바닥은 Grid 평면보다 조금 아래에 둡니다.

`Debug.DrawLine` 대신 LineRenderer로 각 경계선을 만듭니다. 각 셀 중심에는 Canvas가 필요 없는 3D TextMeshPro를 생성하고 X축으로 90도 회전하여 위에서 읽게 합니다. Scene 좌표는 `Editor/GridBoardGizmos.cs`의 `Handles.Label`이 담당합니다. 좌표 표시와 생성 메뉴는 에디터 전용 어셈블리에 있어 플레이어 빌드에 포함되지 않습니다.

확인: Play 후 Hierarchy의 Grid Runtime Visuals 아래에서 Cell 1 오브젝트를 선택합니다. TextMeshPro 컴포넌트가 있고, Game에서 번호와 선이 보이는지 확인합니다. 번호는 1~80이어야 합니다. 글꼴이 없으면 Console 오류 안내에 따라 TMP 리소스를 가져오세요. 선이 분홍색이면 URP 머티리얼을 확인합니다.

## 3. 셀에 오브젝트 정보 저장: GridOccupant.cs

장애물 Cube, 몬스터 Sphere, 플레이어 Capsule을 만들고 각각 GridOccupant를 붙입니다. Board에 Grid를 연결하고 Kind와 Initial Cell을 지정합니다. Capsule의 Elevation은 1, Cube/Sphere는 .5부터 시작합니다.

한 셀은 하나의 GridOccupant 참조를 저장합니다. 참조를 통해 실제 GameObject와 추가된 캐릭터 컴포넌트에 접근할 수 있습니다. 세 종류 모두 이동을 막습니다. GridOccupant가 활성화될 때 셀에 등록하고 비활성화될 때 해제합니다. 중복 셀이나 범위 밖 등록은 Console 오류를 냅니다. 콜라이더를 자동 탐색하는 방식이 아니므로, 장애물에도 반드시 GridOccupant가 필요합니다.

```csharp
var cell = board.GetCell(new UnityEngine.Vector2Int(2, 3));
if (cell != null && cell.Occupant != null)
    UnityEngine.Debug.Log(cell.Occupant.Kind + ": " + cell.Occupant.gameObject.name);
```

확인: 같은 셀에 두 개를 배치하면 오류가 나고, 먼저 등록된 하나만 유지됩니다. 장애물을 비활성화하면 해당 셀의 텍스트와 점유가 해제됩니다. 외부에서 Transform만 이동시키면 점유 정보는 갱신되지 않습니다. 논리적 이동은 TryPlace 또는 GridMover를 사용합니다.

## 4. A* 길찾기: GridPathfinder.cs

상하좌우 네 방향, 한 칸 비용 1을 사용합니다. 맨해튼 거리 `abs(dx)+abs(dz)`를 예상 비용 H로 사용하고, 시작부터 실제 비용 G와 합한 F가 가장 작은 셀부터 탐색합니다. previous로 이전 셀을 기록하고 도착하면 역추적합니다.

`Find(board, start, goal, actor)`의 반환 목록은 출발 셀과 도착 셀을 모두 포함합니다. 막힌 목적지·범위 밖·도달 불가는 빈 목록, 출발=도착은 원소 하나입니다. actor를 전달하면 자신의 점유 셀은 탐색 가능합니다. 대각선 이동과 몬스터 공격은 이 실습의 범위에 포함하지 않습니다.

확인: (4,0)~(4,5)에 장애물을 두되 (4,3)을 비우고 (0,0)에서 (8,0)까지 탐색합니다. 경로가 틈을 돌아가야 합니다. 틈과 위쪽 우회까지 막으면 빈 경로가 나와야 합니다.

## 5. 이동 방향 사전 표시: GridPathPreview.cs

별도 빈 오브젝트에 GridPathPreview를 추가합니다. Actor, View Camera, Line Material을 연결합니다. LineRenderer는 자동으로 추가됩니다. 마우스 Ray와 Grid 평면의 교점을 셀로 변환하므로 바닥 Collider 없이도 선택할 수 있습니다.

경로의 셀 중심들을 LineRenderer에 순서대로 넣습니다. 시작 쪽이 굵고 끝 쪽이 가늘어지는 선이 이동 순서를 나타냅니다. Grid 밖이나 점유된 셀에는 선을 숨깁니다. 경로는 마우스 이동과 점유 변경을 반영하도록 매 프레임 계산합니다. 입력은 새 Input System 우선이며 Legacy만 활성화된 경우도 지원합니다.

확인: 장애물 위에서는 선이 사라지고, 뒤쪽 빈 셀에서는 우회 경로가 나타나야 합니다. Player가 현재 서 있는 셀은 길이가 없으므로 이동하지 않습니다.

## 6. Grid 경로를 따라 이동: GridMover.cs

플레이어에 GridMover를 추가합니다. 클릭하면 현재 상태로 경로를 다시 구하고 각 셀 중심까지 MoveTowards로 이동합니다. 이동 중에는 새 클릭과 미리보기를 무시합니다.

한 칸을 이동하기 직전에 목적 셀을 점유하고 이전 셀을 해제합니다. 이후 경로에 새 장애물이 생기면 해당 칸 앞에서 정지합니다. 자동 재탐색은 하지 않으므로 다시 클릭해 새 경로를 요청합니다. 이동 도중 비활성화하면 코루틴을 중단하고 논리 셀 중심으로 정렬합니다.

확인: 클릭한 목적지에 도착하면 Player 정보도 그 셀에 나타나야 합니다. 이동 도중 다음 경로 셀에 다른 점유자를 등록하면 충돌 대신 정지해야 합니다.

## 학습 이후 확장과 제한

- GridOccupant는 한 칸 크기 캐릭터·장애물용입니다. 여러 셀을 차지하는 건물은 다음 [BuildSystem 튜토리얼](BUILD_TUTORIAL.md)의 GridBuilding을 사용합니다.
- 셀 예약 순간 이전 셀은 비워집니다. 다수 유닛의 보간 중 물리적 겹침까지 막으려면 출발·목적 셀 동시 예약 및 교통 제어가 필요합니다.
- 번호·좌표는 소규모 학습용입니다. 큰 맵에서는 TMP 풀링과 표시 범위 제한, 우선순위 큐 A*, 목표/점유 변경 시 경로 갱신 방식으로 개선합니다.
- 위 수동 설정으로 Scene을 구성한 뒤, TMP 리소스와 카메라 출력은 Unity에서 아래 항목으로 최종 확인하세요.

## 완료 확인

1. Unity Console 컴파일 오류가 없는지 확인합니다.
2. Game에서 선과 1~80 번호, Scene에서 (x,z) 좌표를 확인합니다.
3. 장애물 우회, 도달 불가, Grid 밖 클릭, 자기 셀 클릭을 확인합니다.
4. 플레이어 도착 위치와 셀의 Player 정보가 일치하는지 확인합니다.
5. 장애물 비활성화 후 통과, 재활성화 후 차단을 확인합니다.

## 다음 단계: Grid BuildSystem

[BUILD_TUTORIAL.md](BUILD_TUTORIAL.md)에서 ScriptableObject 건물 설계, 3D 프리팹 연결, 초록/빨강 설치 미리보기, 회전·설치, 몬스터의 바리케이드 공격과 파괴 후 셀 해제를 순서대로 진행합니다. 기존 수동 설정 Scene에 컴포넌트를 추가하는 방식이며 에디터 자동 생성 메뉴를 사용하지 않습니다.

## 모듈 독립성과 의존성

Grid 이동·점유·길찾기·설치·미리보기·건물 체력·몬스터 공격의 구현은 모두 GridSystem 내부에 있습니다. CoverCombat/FPS 코드는 설계 참고 대상이며 실행/컴파일 참조 대상이 아닙니다. FpsGameplayFlow, FpsBuildGrid, Starter Assets, UnityFramework, NavMesh를 요구하지 않습니다.

`GridTutorial.Runtime.asmdef`는 Unity 패키지 어셈블리만 명시적으로 참조합니다. 따라서 Assembly-CSharp의 외부 사용자 클래스를 직접 참조할 수 없습니다. `Editor/GridTutorial.Editor.asmdef`는 Runtime만 참조하는 에디터 전용 어셈블리입니다. UnityEditor API는 Editor 폴더에만 있습니다.

| 의존성 | 용도 / 준비 |
| --- | --- |
| UnityEngine 및 기본 모듈 | Transform, Renderer, Collider, LineRenderer, 코루틴, 안내용 IMGUI |
| com.unity.ugui / Unity.TextMeshPro | TMP 번호와 UnityEngine.EventSystems UI 클릭 차단. TMP Essential Resources 필요 |
| com.unity.ugui / UnityEngine.UI | EventSystem. Build UI 버튼은 선택 사항이지만 UI 어셈블리는 코드 의존성 |
| com.unity.inputsystem / Unity.InputSystem | 새 입력 시스템. 현재 asmdef가 명시 참조하므로 패키지는 설치되어 있어야 함 |
| URP 및 호환 머티리얼 | 이 프로젝트의 선·설치 미리보기·모델 렌더링. C#에서 URP API를 직접 참조하지는 않음 |

Legacy 입력 분기도 있지만, 현재 배포 구성에서는 Input System 패키지를 제거하면 asmdef 참조도 별도로 조정해야 합니다. Unity 패키지까지 포함해 무의존인 모듈은 아닙니다.

씬과 Inspector가 연결하는 에셋은 코드와 별개입니다. 폴더만 복사해서 재사용하려면 Scene, Definitions, Prefabs, Materials 및 직접 만든 텍스처를 GridSystem 하위에 보관하고, 다른 폴더의 캐릭터 스크립트가 포함된 프리팹을 연결하지 마세요. 기본 TMP 글꼴을 쓰면 GridSystem 밖의 TMP Essential Resources를 사용하므로 대상 프로젝트에서도 가져와야 합니다. 빈 Scene에서 이 문서의 순서로 다시 구성할 수 있습니다.

## 게임 루프 실습

[GameLoop 4단계 튜토리얼](../../Docs/GAME_LOOP_SCENE_TUTORIAL.md): Create Demo에서 시작하여 Art의 9-Sliced 시작 버튼, GameManager.GameLoop 연결, 탈출·포획·시간 초과, 결과창 절차적 애니메이션과 Audio를 구성합니다. 필요한 코드는 각 문서에 포함되어 있습니다.



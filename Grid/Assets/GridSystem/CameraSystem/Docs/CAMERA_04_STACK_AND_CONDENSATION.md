# 04. Stack Overlay와 탑뷰 렌즈 결로

## 목표

탑뷰를 정찰 카메라 영상으로 해석합니다. 습한 구역에서 렌즈에 결로가 늘어나고 F로 닦습니다. 1인칭에서는 정찰 렌즈가 보이지 않아 직접 관찰할 이유가 생깁니다.

기본 구현은 반투명 마스크 합성입니다. 실제 배경의 굴절·블러는 구현하지 않습니다. `LensCondensationController`가 작은 노이즈 마스크를 런타임 생성하므로 이미지 파일 없이 실습할 수 있습니다.

## URP와 레이어 준비

1. `Edit > Project Settings > Graphics`와 현재 Quality에서 사용하는 URP Asset을 확인합니다.
2. Asset의 Renderer List에서 현재 Camera의 Renderer가 Universal Renderer인지 확인합니다. 기존 Renderer 에셋은 바꾸지 말고 호환되는 기존 설정을 사용합니다.
3. `CameraLens` 레이어를 새로 만듭니다.
4. Base Camera의 Culling Mask에서 CameraLens만 제외합니다. 기존 월드·UI 레이어는 유지합니다.

## Overlay 직접 만들기

1. 기존 GridCamera의 자식으로 Camera를 만들고 이름을 `LensOverlayCamera`로 바꿉니다.
2. local Position/Rotation=0, Scale=1로 설정합니다.
3. AudioListener가 추가되어 있다면 이 자식 Camera에서 제거합니다.
4. Render Type=Overlay, Renderer=Base와 같은 호환 Universal Renderer, Projection=Orthographic, Size=1, Near/Far=0.1/10으로 설정합니다.
5. Culling Mask는 CameraLens만 선택합니다. Clear Depth=체크, Render Shadows=해제, Post Processing=해제로 시작합니다.
6. Base Camera의 Stack 목록에 이 Overlay Camera를 직접 추가합니다. Overlay는 Stack에 없으면 화면을 그리지 않습니다.

## 렌즈 Canvas 만들기

1. LensOverlayCamera 아래 `UI > Canvas`를 만들고 `LensCanvas`로 이름을 바꿉니다. EventSystem이 이미 있으면 중복 생성하지 않습니다.
2. Render Mode=`Screen Space - Camera`, Render Camera=LensOverlayCamera, Plane Distance=1로 설정합니다.
3. Canvas Scaler는 Scale With Screen Size, Reference Resolution=(1920,1080)으로 설정합니다.
4. Canvas 아래 `UI > Raw Image`를 만들고 `CondensationImage`로 이름을 바꿉니다.
5. Anchor Presets에서 Stretch 양방향을 선택하고 Left/Right/Top/Bottom을 모두 0으로 설정합니다.
6. Canvas와 RawImage의 Layer를 CameraLens로 설정합니다. Color=흰색, Raycast Target=해제로 설정합니다. Texture는 비워 둡니다.
7. LensCanvas는 Screen Space Overlay로 두지 않습니다. 이 장에서는 Camera Stack을 거쳐 렌즈를 합성해야 합니다.

기존 시작/결과/조사 HUD는 Screen Space Overlay Canvas로 유지하면 렌즈 위에 읽기 쉽게 표시됩니다.

## 습도 구역과 코드 연결

1. CameraSystem에 LensCondensationController를 추가합니다.
2. Controller=기존 Controller, Player=Player 루트, Overlay Camera=LensOverlayCamera, Lens=CondensationImage를 연결합니다.
3. Dry Rate=0.015, Wipe Duration=1, Wipe Cooldown=3, Initial Moisture=0으로 설정합니다.
4. 빈 `HumidZone`을 만들어 플레이어가 이동할 구역에 배치합니다. BoxCollider를 추가하고 Is Trigger를 켭니다.
5. `CondensationZone`을 추가하고 Accumulation Per Second=0.08로 설정합니다.
6. BoxCollider의 Size와 Center를 조정해 플레이어 중심 위치가 들어가는 높이까지 덮습니다. 예: Size=(3,4,3).
7. LensCondensationController의 Zones 배열에 HumidZone을 연결합니다.

구역은 로컬 Box 내부 판정으로 읽습니다. Trigger 이벤트에 의존하지 않아 Rigidbody가 필요하지 않습니다. 습기는 가장 강한 구역의 증가율을 사용하고 구역 밖에서 서서히 줄어듭니다.

## 실행과 설계 규칙

1. 탑뷰로 HumidZone에 이동하고 약 10초 기다립니다. 가장자리에 흰 습기가 늘어나는지 확인합니다.
2. F를 누르면 1초 동안 습기가 감소합니다. 닦기 완료 후 3초의 재사용 대기 시간이 있습니다.
3. Tab으로 1인칭에 들어가면 렌즈가 숨겨집니다. 다시 탑뷰에 오면 남은 습기가 보입니다.
4. 1인칭에서도 습도는 계속 계산됩니다. 시점 전환으로 상태를 초기화할 수 없습니다.
5. 닦기는 시각 효과만 제어합니다. 이동 명령을 막지 않으며, 결로 영역 안의 클릭도 기존 Grid 규칙으로 처리합니다.

더 어려운 버전에서는 닦기 중 입력 금지나 결로로 가린 셀 선택 금지를 추가할 수 있습니다. 현재 코드에 그런 판정이 있다고 가정하지 마세요.

## 통과 조건과 문제 해결

- 초기 습기를 0.8로 설정하면 구역 진입 없이 Overlay를 즉시 검증할 수 있습니다.
- 렌즈가 안 보이면 Stack 등록, CameraLens 레이어, Overlay Culling Mask, Canvas Render Camera를 확인합니다.
- 월드가 두 번 보이면 Overlay가 월드 레이어를 그리고 있는지 확인합니다.
- 클릭이 막히면 CondensationImage의 Raycast Target을 확인합니다.
- 1인칭에서 마스크가 남으면 LensController의 Overlay Camera 참조를 확인합니다.
- 다른 화면 비율에서도 RawImage가 화면 전체를 덮어야 합니다.

## 다음 확장: 실제 굴절

Opaque Texture는 투명 물체까지 포함한 최종 화면이 아닙니다. 실제 굴절·블러를 만들려면 URP 17 Render Graph 기반 Full Screen Pass 또는 Renderer Feature에서 필요한 시점의 화면 텍스처를 읽어야 합니다. 이 작업은 기본 Stack 실습과 분리합니다. 결로 마스크와 Global Volume만으로 국소적인 배경 굴절이 자동 생성되지는 않습니다.

다음: [05. Global Volume](CAMERA_05_GLOBAL_VOLUME.md)

# 03. Projection 직접 설계

## 목표와 코드

`CameraProjectionSettings`로 Orthographic 보드 맞춤, 휠 줌, 1인칭 FOV/Clip 설정을 분리합니다. 처음부터 투영 행렬을 직접 변경하지 않고 Inspector 설정과 수학 관계를 확인합니다.

## 직접 연결

1. Play를 끄고 CameraSystem에 CameraProjectionSettings를 추가합니다.
2. View Camera에 기존 GridCamera, Board에 기존 GridBoard를 연결합니다.
3. Margin=0.5, Zoom Limits=(2,20), First Person Fov=65, Observation Fov=40, First Person Near=0.05, Far=100으로 설정합니다.
4. Controller와 FirstPersonLook 양쪽 Projection 필드에 이 컴포넌트를 연결합니다.
5. Game 창을 16:9로 설정합니다. 컴포넌트 우측 메뉴에서 `Fit Top View To Board`를 실행합니다.
6. Scene에서 Camera가 보드 중심 위를 바라보고 Game에서 전체 보드가 보이는지 확인하고 저장합니다.

Fit은 편집 상태에서 직접 실행합니다. 런타임 전환 중 실행하지 않습니다. 현재 구현은 맞춤을 한 번 수행하며 창 크기가 바뀔 때 자동 재맞춤하지 않습니다. 새 화면 비율을 선택한 뒤 Play를 끄고 다시 실행하여 비교하세요. 기존 구도를 유지하고 싶다면 Fit을 실행하지 않아도 됩니다.

## 계산 이해

직교 카메라의 Size는 보이는 세로 길이의 절반입니다. 보드 Scale=1, 수직 카메라라면:

```text
width  = Board.Width  × CellSize
depth  = Board.Height × CellSize
size   = max(depth / 2, width / (2 × aspect)) + margin
```

10×8, 16:9에서는 margin=0.5일 때 max(4,2.8125)+0.5=4.5입니다. 코드는 실제 네 모서리를 카메라 좌표로 변환하므로 보드 회전과 스케일도 반영합니다. Grid 이동 자체는 기존 GridBoard 지원 범위를 따릅니다.

Perspective에서 기준 평면의 세로 절반 길이는 대략 `distance × tan(verticalFov / 2)`입니다. 거리가 10이고 FOV가 60도면 약 5.77입니다. 이 관계로 두 투영의 기준 크기를 비교할 수 있지만 깊이가 다른 모든 물체의 크기를 동시에 맞출 수는 없습니다.

## 전환 방식

Controller는 0.4초 동안 화면을 가리고 중간 지점에서 Projection과 위치를 바꿉니다. 직교→원근 변경이 순간적으로 보여 생기는 튐을 가림으로 처리합니다. 현재 코드는 연속적인 카메라 비행이나 projectionMatrix 보간을 구현하지 않습니다.

휠은 탑뷰 Size만 바꾸며, 우클릭은 1인칭 FOV만 바꿉니다. 줌은 시야를 바꾸며 조사 Distance는 그대로입니다. Near/Far 역시 조사 거리나 몬스터 AI 범위를 바꾸지 않습니다.

## 통과 조건

- 16:9와 좁은 창에서 각각 Fit을 수행하면 보드가 들어옵니다.
- 휠 줌은 지정한 최소·최대 Size를 지킵니다.
- 우클릭 확대 후 탑뷰로 복귀하면 이전 탑뷰 Size가 복원됩니다.
- 가까운 벽 잘림이 있으면 Near와 EyeAnchor 위치를 확인합니다. 너무 작은 Near는 깊이 정밀도에 불리하므로 필요한 수준까지만 줄입니다.

다음: [04. Stack과 렌즈 결로](CAMERA_04_STACK_AND_CONDENSATION.md)

# 06. Output Texture로 관찰 사진 남기기

## 목표와 코드

1인칭에서 P를 누르면 독립 Base Camera로 정사각형 사진을 촬영해 HUD에 표시합니다. `CameraOutputController`는 URP의 endCameraRendering 이벤트로 렌더 완료를 확인하고 카메라를 끕니다. 최근 사진 한 장만 보관합니다.

## RenderTexture 생성

1. Project에서 `CameraSystem/RenderTextures` 폴더를 만듭니다.
2. 우클릭 `Create > Rendering > Render Texture` 또는 Create 메뉴에서 Render Texture를 검색합니다.
3. 이름은 `ObservationPhoto`, 크기는 512×512, Anti-Aliasing은 None/1 sample로 설정합니다.
4. Color Format은 일반 RGBA8 계열, Depth Buffer는 24bit 이상으로 시작합니다. Inspector 표기는 Unity 버전에 따라 다를 수 있습니다.
5. 이 에셋은 코드가 생성하거나 해제하지 않습니다. Scene의 카메라와 UI가 함께 참조합니다.

## 사진 Camera 생성

1. CameraSystem 아래 빈 Output을 만들고 자식으로 `EvidenceCamera`를 생성합니다.
2. AudioListener를 제거합니다.
3. Render Type=Base, Projection=Perspective, Stack은 비워 둡니다. 메인 Base의 Stack에도 추가하지 않습니다.
4. Output > Output Texture/Target Texture에 ObservationPhoto를 연결합니다.
5. Culling Mask에서 CameraLens와 UI, 필요 시 ExplorationPlayer를 제외하고 월드·단서·벽을 포함합니다.
6. Volume Mask는 EvidenceVolume만 선택합니다. 화면 효과를 쓰려면 Post Processing을 켭니다.
7. EvidenceVolume 레이어의 별도 Global Volume을 만들고 원하는 사진용 Profile을 연결하거나, 후처리 없이 시작합니다.
8. Camera 컴포넌트의 enabled 체크를 끕니다. GameObject 자체는 활성 상태로 둡니다. 촬영 시 컴포넌트만 켭니다.

## UI와 코드 연결

1. 기존 HUDPanel 아래 RawImage를 만들고 `PhotoPreview`로 이름을 바꿉니다.
2. 오른쪽 아래 Anchor, 크기=(220,220), 적절한 여백으로 정사각형을 만듭니다.
3. Raycast Target을 끄고 Texture는 비워 둡니다. 첫 촬영 전에는 스크립트가 Image를 숨깁니다.
4. 별도 빈 `PhotoFrame` UI를 화면 중앙에 정사각형으로 표시하고 싶으면 네 개의 얇은 Image로 테두리만 만듭니다. 배경은 투명, 모든 Raycast Target은 끕니다. 1인칭 전용 프레임은 Reticle의 자식으로 두면 조사 패널 중에도 숨겨집니다.
5. Output에 CameraOutputController를 추가합니다.

| 필드 | 연결 |
| --- | --- |
| Controller | CameraSystem의 Controller |
| Evidence Camera | 새 EvidenceCamera |
| Output Texture | ObservationPhoto |
| Photo Preview | PhotoPreview |
| Timeout | 2 |

Controller의 View Camera를 Evidence Camera에도 넣으면 안 됩니다. 카메라를 복제했다면 복제본에 남은 다른 제어 스크립트는 제거합니다.

## 사진 프레임 이해

사진은 1:1 비율이며 16:9 메인 화면의 좌우 전체를 담지 않습니다. 같은 Vertical FOV를 사용하므로 화면 높이를 기준으로 중앙 정사각형 영역에 대응합니다. 프레임의 세로 크기를 Game 화면 높이에 맞추고 정사각형으로 유지하면 기본 사진 구도를 미리 확인할 수 있습니다. 고정 300px 프레임은 실제 촬영 범위와 일치하지 않으므로 단순 장식과 구분하세요.

촬영 시 Camera 위치·회전·FOV·Clip만 복사합니다. Culling Mask와 Volume Mask는 사진 Camera의 독립 설정을 유지합니다. 사진에 메인 HUD와 정찰 렌즈가 찍히지 않는 이유입니다.

## 실행과 통과 조건

1. 1인칭으로 단서를 보고 P를 누릅니다.
2. 우측 아래 사진이 보이고 EvidenceCamera의 enabled가 다시 꺼지는지 확인합니다.
3. 다른 방향을 찍으면 기존 사진이 덮어써집니다. 사진은 실시간 CCTV처럼 계속 갱신되지 않습니다.
4. 조사 패널이 열렸거나 탑뷰이면 촬영이 무시됩니다.
5. 사진을 찍어도 InspectableTarget.Inspected가 바뀌지 않습니다. 조사 완료는 E 입력으로만 처리합니다.
6. 결과 전환 또는 렌더 완료 지연 시 요청을 취소합니다. 카메라가 계속 켜져 있지 않은지 확인합니다.

검은 화면이면 EvidenceCamera GameObject 활성 여부, Output Texture, Culling Mask, 조명, 렌더 파이프라인 오류를 확인합니다. 기본 코드는 Camera.Render를 직접 호출하지 않습니다.

## 확장 과제

- 여러 사진: 각 사진을 별도 텍스처로 복사하고 개수 제한과 메모리 해제를 추가해야 합니다.
- CCTV: 선택한 피드 Camera만 활성화하고 RenderTexture를 모니터 Material에 표시합니다.
- 분할 화면: 별도 Base Camera의 Viewport Rect를 조정합니다. 사진 기능과 구분해 실습합니다.
- 탑뷰를 방해하는 결로 규칙을 유지하려면 깨끗한 실시간 미니맵을 무제한 제공하지 않습니다.

다음: [07. 통합 확인](CAMERA_07_INTEGRATION.md)

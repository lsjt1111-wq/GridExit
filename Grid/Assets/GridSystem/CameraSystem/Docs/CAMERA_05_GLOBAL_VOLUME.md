# 05. Global Volume으로 시점 분위기 구분

## 목표와 코드

탑뷰는 차가운 정찰 영상, 1인칭은 현장 색감으로 표현합니다. `CameraVolumeDriver`는 Volume.weight만 조절하므로 원본 Profile 에셋에 런타임 값을 기록하지 않습니다.

## 레이어와 카메라

1. `CameraWorldVolume`, `EvidenceVolume` 레이어를 만듭니다.
2. 기존 Base Camera의 Rendering > Post Processing을 켭니다.
3. Environment > Volume Mask는 CameraWorldVolume을 포함하도록 설정합니다. 기본 Scene의 Volume도 사용하려면 그 Volume 레이어를 의도적으로 포함합니다.
4. Volume Trigger에 Player 또는 EyeAnchor를 연결합니다.
5. Global Volume은 위치와 관계없이 적용됩니다. Trigger 위치의 차이는 이후 Local Volume을 만들 때 확인할 수 있습니다.

## Profile 직접 구성

CameraSystem 아래 빈 `Environment`를 만들고 아래 오브젝트를 생성합니다. `GameObject > Volume > Global Volume` 메뉴 또는 Add Component > Volume을 사용합니다. 각각 Is Global을 체크하고 Layer=CameraWorldVolume으로 설정합니다.

| 오브젝트 | Priority | 초기 Weight | Profile과 Override 예시 |
| --- | --- | --- | --- |
| BaseGlobalVolume | 0 | 1 | 공통 Color Adjustments, 약한 Bloom |
| TopViewGlobalVolume | 10 | 1 | Color Adjustments: Saturation=-35, Color Filter=옅은 청록; Vignette Intensity=0.3; Film Grain Intensity=0.2 |
| FirstPersonGlobalVolume | 10 | 0 | Color Adjustments: Saturation=0, Color Filter=흰색; Vignette Intensity=0.12; Film Grain Intensity=0 |

1. Profile의 New를 눌러 각각 별도 Profile을 만들고 `CameraSystem/Profiles`로 옮깁니다. Profiles 폴더는 직접 만듭니다.
2. Add Override에서 위 효과를 추가합니다.
3. 제어할 각 값 왼쪽 Override 체크박스를 켭니다. 숫자만 입력하고 체크하지 않으면 적용되지 않습니다.
4. 두 모드 Profile에는 같은 항목을 명시해 이전 모드의 효과가 남는 것처럼 보이는 상황을 줄입니다.
5. Environment에 CameraVolumeDriver를 추가하고 Controller, Top View Volume, First Person Volume을 연결합니다. Blend Speed=5로 시작합니다.

색과 강도는 출발값입니다. 어두운 단서도 읽을 수 있도록 실제 Scene에서 조절하세요. BaseGlobalVolume은 선택 사항이며 모드 Volume 두 개만으로 시작해도 됩니다.

## Stack과 후처리 순서 확인

Overlay의 Post Processing은 꺼 둡니다. Base에서 월드 후처리 후 렌즈를 합성하는 구성을 시작점으로 삼습니다. 실제 처리 위치는 선택 Renderer와 효과에 따라 Frame Debugger에서 확인합니다. Overlay의 후처리를 켜면 앞서 합성된 화면에도 효과가 적용될 수 있으므로 카메라 분리만으로 완전한 격리를 기대하지 않습니다.

HUD는 기존 Screen Space Overlay Canvas에서 유지합니다. 렌즈 결로의 위치별 마스크는 LensController가 처리하며 Global Volume은 화면 전체 분위기를 담당합니다. Global Volume은 습도 구역 판정이 아닙니다.

## 실행과 통과 조건

1. 탑뷰에서 정찰 색감과 Vignette가 보이는지 확인합니다.
2. 1인칭으로 전환하면 Top weight가 0, FirstPerson weight가 1로 변하는지 Play 중 Inspector에서 확인합니다.
3. 반복 전환 후 Play를 종료하면 Profile의 수치가 원래 값 그대로인지 확인합니다.
4. 효과가 없으면 Post Processing, Volume Mask, Layer, Profile, Override 체크를 순서대로 확인합니다.
5. 전체가 과하게 어두우면 기존 Global Volume과 Priority가 중첩되는지 확인합니다.

다음: [06. Output과 관찰 사진](CAMERA_06_OUTPUT.md)

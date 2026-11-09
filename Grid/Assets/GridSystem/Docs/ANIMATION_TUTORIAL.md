# GridSystem 애니메이션 실습: 캐릭터 → 바리케이드 → 피격 Flash

이 문서는 현재 GridSystem 위에 애니메이션을 연결하는 실습 설계서다. **아래 C# 코드는 이미 프로젝트에 생성·연결되어 있다.** 코드 블록은 구조를 학습하기 위한 예제이므로 같은 클래스를 다시 생성하거나 기존 호출을 중복 삽입하지 않는다. 실제 파일에는 참조 누락 방어와 이벤트 재구독 처리도 추가되어 있다. Unity Inspector에서 모델·클립·프리팹을 연결하는 과정은 직접 진행한다.

### 코드 생성 완료 상태

| 실제 파일 | 연결 상태 |
| --- | --- |
| `Presentation/GridActorAnimation.cs` | 캐릭터 루트에 추가 후 Animator/VisualPivot 연결. 컴포넌트 메뉴에 Attack/Hit/Death 테스트 제공 |
| `Presentation/GridBuildingFeedback.cs` | GridBuildSystem이 설치된 건물 루트에 자동 추가 |
| `Presentation/GridHitFlash.cs` | 건물 생성 시 자동 추가, 피격 이벤트에서 Pulse 호출 |
| `UI/GridDamagePopup.cs` | 학습자가 만드는 World Space Canvas 프리팹에 추가 |
| `Shaders/GridHitFlash.shader` | Shader 메뉴의 `GridTutorial/HitFlash`. 3장의 Graph 제작 대신 바로 사용 가능 |
| `GridBuilding.cs` | 실제 감소량을 보내는 Damaged 이벤트 적용 |
| `GridMonsterAttacker.cs` | 실제 공격 시 방향 전환과 PlayAttack 호출 적용 |
| `GridBuildSystem.cs` | Popup/Camera 필드와 피격 연출 생성 적용 |

남은 연결: GridBuildSystem의 Damage Popup Prefab/Feedback Camera, 캐릭터의 Animator/Avatar/Override, FBX Visual과 Definition, 피격 Material의 텍스처다. FeedbackPivot이 없는 기존 비주얼은 컨테이너 자체를 흔드는 방식으로 동작한다. 프리뷰에는 피격 컴포넌트를 생성하지 않는다.

제공된 HLSL 셰이더는 BaseMap/Normal/Metallic/Smoothness, 실시간 조명, 그림자와 Flash를 지원하는 실습용이다. 베이크 라이트맵·MaskMap·전체 URP Lit 옵션은 구현 범위에 포함하지 않았다. 3장의 Shader Graph와 프로퍼티 이름은 동일하다. C#은 프로젝트의 Unity 컴파일 응답 파일을 사용해 컴파일을 확인했다. 셰이더 임포트와 시각 결과는 Unity에서 확인한다.

기준 환경: 프로젝트의 Unity `6000.4.2f1`, URP `17.4.0`, uGUI `2.0.0`에 포함된 TextMeshPro. 먼저 [그리드 이동](README.md), [건설과 몬스터 공격](BUILD_TUTORIAL.md)을 완료한다. 각 장을 마칠 때 Scene과 Prefab을 저장한다. Play 중 수정한 Inspector 값은 종료하면 사라진다.

## 0. 현재 프로젝트와 완성할 구조

사용자가 지칭한 Zombie 모델은 실제로 `Assets/GridSystem/Models/Monster`에 있다. 폴더를 이동할 필요는 없다.

| 현재 파일/폴더 | 실습 용도 |
| --- | --- |
| `Models/Monster/Warzombie F Pedroso.fbx` | 좀비 모델과 Avatar |
| 같은 폴더의 `@Zombie Idle/Walk/Attack/Reaction Hit/Death.fbx` | 5종 애니메이션 |
| `Models/Player` | 비어 있음. 학습자가 모델과 5종 클립을 직접 임포트 |
| `Models/Barricade/FBX/Water_Barricade_lod0.fbx` | 바리케이드 대표 모델 |
| `Models/Barricade/Texture` | BaseColor, Normal, MaskMap, Scattering 및 외형 변형 |
| `Definition/Barricade.asset` | 기존 2×1, HP 100 정의 |
| `Prefabs/BarricadeVisual.prefab`, `Mat/Barricade.mat` | 기존 비주얼·머티리얼. 복제해서 실습 |

추가할 폴더는 `Animation/Clips`, `Animation/Controllers`, `Presentation`, `UI`, `Shaders`다. 모두 GridSystem 아래에 만들고 C#의 namespace는 `GridTutorial`로 통일한다. 기존 Runtime asmdef에 TMP와 UI 참조가 있으므로 별도 asmdef는 만들지 않는다.

```text
논리: GridMover / GridMonsterAttacker / GridBuilding
                       ↓ 실제 이동·공격·확정 피해
표현: GridActorAnimation / GridBuildingFeedback
                       ↓
      Animator Override / 자식 Transform / TMP Popup / Flash
```

설계 원칙은 논리 루트와 그래픽 자식의 분리다. GridMover만 캐릭터 위치를 이동하고, 건물 루트만 셀 점유와 Collider를 가진다. 애니메이션은 자식 모델에만 적용한다. 피격 연출 때문에 점유 셀이나 충돌체가 흔들리면 안 된다.

현재 범위도 확인한다. 몬스터는 건물을 공격하지만 플레이어 인접 시 멈추며, 플레이어 HP·공격·사망 시스템은 아직 없다. 1장의 Hit/Death는 공통 표현 API와 수동 검증까지 제공한다. 플레이어 전투를 추가할 때 그 API를 호출하면 된다. Override Controller는 AI나 공격 규칙을 복제하는 기능이 아니라 **같은 표현 상태 기계에서 클립을 교체하는 기능**이다.

## GridSystem 애니메이션 실습: 캐릭터 → 바리케이드 → 피격 Flash

1. [1. 모델과 리깅 애니메이션](ANIMATION_TUTORIAL_01_CHARACTER.md)
2. [2. Rig 없이 바리케이드 Transform 애니메이션과 TMP 팝업](ANIMATION_TUTORIAL_02_BARRICADE.md)
3. [3. 공통 머티리얼과 커스텀 Flash 셰이더](ANIMATION_TUTORIAL_03_FLASH.md)

## 4. 완성 확인과 문제 해결

| 확인 | 합격 조건 |
| --- | --- |
| 좀비·Player 이동 | 같은 ActorBase, 별도 Avatar/Override로 Idle과 Move 전환 |
| 길이가 다른 Attack/Hit | normalized Exit Time으로 복귀, 의도치 않은 Trigger 잔류 없음 |
| Root Motion | 셀 중심 도착 위치와 모델이 일치 |
| Death | 표현 API로 Death 진입, 실제 전투 추가 시 이동·AI·점유도 중단 |
| 바리케이드 외관 | BaseMap/Normal 표시, 분홍색 오류와 바닥 관통 없음 |
| Definition | B/R/설치, 2×1⇔1×2, 미리보기 색상 정상 |
| 피격 | HP 감소 1회당 흔들림·TMP·Flash 1회 |
| 공유 Material | 옆의 피격되지 않은 바리케이드는 빛나지 않음 |
| 연속 피격 | 종료 후 원래 자세와 Flash=0 복원 |
| 마지막 타격 | 본체 소멸·셀 해제 후에도 Popup 수명 유지 |
| UI | 카메라를 향하고 숫자를 읽을 수 있으며 건설 클릭 방해 없음 |
| 컴파일 | GridTutorial.Runtime 내부에서 외부 게임 코드 참조 없이 컴파일 |

| 증상 | 확인 위치 |
| --- | --- |
| T-Pose/움직이지 않음 | Avatar 유효성, Override 5개 슬롯, Animator 참조, 리그 종류 |
| 공중 보행/발 미끄러짐 | elevation, VisualPivot, Root Motion, Bake, GridMover 속도 |
| 공격 동작 없음 | 실제 공격 블록의 PlayAttack 호출, Trigger 이름 |
| 바리케이드 흔들림 없음 | CreateVisual의 Behaviour 비활성화, 루트에 스크립트 추가, 직접 자식 FeedbackPivot |
| Popup이 사라짐 | 피격 대상 자식으로 생성했는지, Prefab/TMP/Camera 참조 |
| 모든 개체가 빛남 | sharedMaterial 직접 수정 여부, PropertyBlock 적용 대상 |
| Flash 없음 | 커스텀 Shader 할당, Reference 문자열, Renderer의 모든 슬롯 |
| 건물 우회 | 현재 AI 사양. 양 끝까지 벽을 막아 재현 |

문서 작성 시에는 소스·모델 목록·메타데이터·Definition·Unity/Package 설정과의 정합성을 확인했다. FBX의 Humanoid 변환, 클립의 외관, Shader Graph 생성, Play 모드 최종 검증은 실습 시 Unity에서 진행한다.

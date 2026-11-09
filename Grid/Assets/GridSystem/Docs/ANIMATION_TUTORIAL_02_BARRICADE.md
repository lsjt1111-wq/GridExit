# 2. Rig 없이 바리케이드 Transform 애니메이션과 TMP 팝업

[Overview](ANIMATION_TUTORIAL.md) | [Previous](ANIMATION_TUTORIAL_01_CHARACTER.md) | [Next](ANIMATION_TUTORIAL_03_FLASH.md)

이 문서는 현재 GridSystem 위에 애니메이션을 연결하는 실습 설계서다. **아래 C# 코드는 이미 프로젝트에 생성·연결되어 있다.** 코드 블록은 구조를 학습하기 위한 예제이므로 같은 클래스를 다시 생성하거나 기존 호출을 중복 삽입하지 않는다. 실제 파일에는 참조 누락 방어와 이벤트 재구독 처리도 추가되어 있다. Unity Inspector에서 모델·클립·프리팹을 연결하는 과정은 직접 진행한다.

### 2-1. 텍스처를 머티리얼에 매핑

1. `Mat/Barricade.mat`을 복제해 `Barricade_Used.mat`을 만든다. Shader=`Universal Render Pipeline/Lit`, Surface=`Opaque`로 시작한다.
2. `Water_Barricade_Used_1_BaseColor.png`를 Base Map에 넣고 Tint=White로 둔다. BaseColor 텍스처의 sRGB는 On이다.
3. `Water_Barricade_Used_1_Normal.png`의 Texture Type을 Normal map으로 바꾸고 Apply한 뒤 Normal Map에 넣는다. 노멀 강도는 1부터 시작한다.
4. MaskMap은 컬러가 아닌 데이터이므로 sRGB=Off다. **파일 이름만으로 채널 규약을 확정할 수 없다.** 공급자 설명 또는 R/G/B/A 단독 Preview로 규약을 확인한다. HDRP형 R=Metallic/G=AO/B=Detail/A=Smoothness가 확인되면 URP Metallic Map에는 R/A를 사용하고, AO는 G를 읽는 Occlusion Map에 연결한다. 규약이 다르면 채널을 재패킹한다. Roughness 맵은 Smoothness로 쓸 때 `1-Roughness` 변환이 필요하다.
5. 규약을 확인하지 못했다면 MaskMap은 비워 두고 Metallic=0, Smoothness=0.25로 먼저 완료한다. Scattering은 일반 URP Lit에 동일 이름 슬롯이 없으므로 이 기초 단계에서는 연결하지 않는다.
6. lod0 FBX를 Scene으로 드래그하고 모든 MeshRenderer의 Material 슬롯을 확인해 만든 머티리얼을 연결한다. 다른 LOD 파일들을 동시에 겹쳐 넣지 않는다. 이번 단계는 lod0 하나로 진행한다.

### 2-2. 시각 전용 프리팹과 Definition

다음 구조를 만들어 `Prefabs/Barricade_FBX_Visual.prefab`으로 저장한다.

```text
Barricade_FBX_Visual       ← Definition의 Offset/Scale이 적용되는 컨테이너
  FeedbackPivot           ← localPosition=0, localRotation=identity, localScale=1
    Water_Barricade_lod0   ← 피벗/크기/정면 보정, MeshRenderer
```

이 프리팹에는 Animator, Collider, Rigidbody, 게임플레이 스크립트를 붙이지 않는다. FeedbackPivot의 원점은 모델의 바닥 중앙에 둔다. FBX의 보정은 가장 안쪽 모델에서 처리한다.

1. 기존 `Definition/Barricade.asset`을 복제해 `Barricade_FBX.asset`을 만든다. Renderer Prefab에 새 Visual을 넣는다.
2. Width=2, Depth=1, Max Health=100, Collision Height=1을 시작값으로 둔다. 모델 실측 높이에 따라 Collision Height를 조정한다.
3. 기존 Visual Scale=(2,1,1), Offset=(0,0.5,0)은 큐브용 값이다. FBX에서는 Scale=(1,1,1), Offset=(0,0,0)에서 시작해 2×1 셀 안에 맞춘다. 단위가 다르면 FBX Model Scale Factor 또는 모델 자식의 균일 Scale을 조절한다. footprint와 시각 크기는 자동 연동되지 않는다.
4. GridBuildController의 Selected Definition에 새 정의를 넣는다. B → 미리보기 → R → 설치를 확인한다. 회전 전후 모델과 BoxCollider가 해당 셀 안에 들어가는지 확인한다.

**현재 코드의 중요한 연결 규칙:** GridBuildDefinition.CreateVisual은 하위 Behaviour를 모두 끈다. GridBuildSystem은 GridBuilding 루트를 런타임에 새로 만들며 rendererPrefab 자체를 게임플레이 루트로 사용하지 않는다. 따라서 피격 스크립트는 Visual 프리팹에 넣어도 동작하지 않는다. 생성 완료 후 별도로 루트에 붙인다. 프리뷰에는 이 스크립트를 붙이지 않는다.

### 2-3. 실제 피해를 이벤트로 전달

`GridBuilding.cs`에 기존 HealthChanged를 유지하면서 다음 이벤트를 추가한다.

```csharp
public event Action<float> Damaged; // 방금 실제로 감소한 HP
```

TakeDamage 본문을 다음으로 교체한다. 사망 비활성화 전에 이벤트를 발행하므로 마지막 타격 팝업도 생성할 수 있다.

```csharp
if (!registered || damage <= 0 || float.IsNaN(damage) || float.IsInfinity(damage)) return;
float applied = Mathf.Min(Health, damage);
Health -= applied;
Damaged?.Invoke(applied);
HealthChanged?.Invoke(Health);
if (Health <= 0) { gameObject.SetActive(false); Destroy(gameObject); }
```

예: 남은 HP 10에 공격 25가 들어오면 팝업은 실제 감소량 10이다. 공격 원래 수치 25를 보여주고 싶다면 이벤트에 원본과 적용값을 별도 필드로 전달하도록 확장한다.

### 2-4. TMP World Space 팝업 프리팹

1. TMP Essential Resources가 없으면 Window > TextMeshPro에서 임포트한다.
2. Scene에 Canvas를 만들고 Render Mode=`World Space`로 둔다. RectTransform 크기 200×80, Scale=(0.01,0.01,0.01)로 시작한다. GraphicRaycaster는 제거한다.
3. 자식으로 UI > Text - TextMeshPro를 만들고 Stretch, 가운데 정렬, Font Size=48, Color=노랑, Raycast Target=Off로 설정한다. 텍스트는 `25`로 두고 카메라에서 읽히는지 확인한다.
4. 아래 `UI/GridDamagePopup.cs`를 Canvas 루트에 붙이고 TMP 자식을 연결한 뒤 `UI/DamagePopup.prefab`으로 저장한다. Scene의 임시 Canvas는 삭제한다.

```csharp
using TMPro;
using UnityEngine;

namespace GridTutorial
{
    public sealed class GridDamagePopup : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] float lifetime = .7f;
        Camera viewCamera;
        Vector3 start;
        float age;
        bool initialized;
        public void Setup(float amount, Vector3 position, Camera camera)
        {
            start = position; transform.position = position;
            viewCamera = camera; age = 0;
            initialized = true;
            label.text = amount.ToString("0.#");
            label.alpha = 1;
        }
        void LateUpdate()
        {
            if (!initialized) return;
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Mathf.Max(.01f, lifetime));
            transform.position = start + Vector3.up * (.8f * t);
            if (viewCamera) transform.rotation = viewCamera.transform.rotation;
            label.alpha = 1 - t;
            if (t >= 1) Destroy(gameObject);
        }
    }
}
```

피격 객체의 자식으로 생성하지 않는다. 마지막 타격에 건물이 즉시 파괴되어도 독립된 팝업은 수명 동안 남는다. 기초 실습은 Instantiate/Destroy를 사용하고, 대량 전투에서는 이후 풀링한다.

### 2-5. 본 없이 자식 Transform을 흔들기

`Presentation/GridBuildingFeedback.cs`를 만든다. 이 장에서는 Transform과 Popup을 구현하고 3장에서 Flash를 추가한다.

```csharp
using System.Collections;
using UnityEngine;

namespace GridTutorial
{
    public sealed class GridBuildingFeedback : MonoBehaviour
    {
        GridBuilding building;
        Transform pivot;
        Vector3 restPosition, restScale;
        Quaternion restRotation;
        GridDamagePopup popup;
        Camera cameraView;
        Coroutine reaction;
        public void Initialize(GridBuilding owner, Transform feedbackPivot,
                               GridDamagePopup popupPrefab, Camera camera)
        {
            building = owner; pivot = feedbackPivot;
            popup = popupPrefab; cameraView = camera;
            restPosition = pivot.localPosition;
            restScale = pivot.localScale;
            restRotation = pivot.localRotation;
            building.Damaged += OnDamaged;
        }
        void OnDamaged(float amount)
        {
            var box = GetComponent<BoxCollider>();
            var top = box.bounds.center + Vector3.up * (box.bounds.extents.y + .3f);
            if (popup) Instantiate(popup).Setup(amount, top, cameraView);
            if (reaction != null) StopCoroutine(reaction);
            Restore();
            reaction = StartCoroutine(React());
        }
        IEnumerator React()
        {
            const float duration = .22f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = elapsed / duration;
                float wave = Mathf.Sin(t * Mathf.PI * 4) * (1 - t);
                pivot.localPosition = restPosition + Vector3.right * (.04f * wave);
                pivot.localRotation = restRotation * Quaternion.Euler(0, 0, 5 * wave);
                pivot.localScale = Vector3.Scale(restScale,
                    new Vector3(1 + .04f * wave, 1 - .04f * wave, 1));
                yield return null;
            }
            Restore(); reaction = null;
        }
        void Restore()
        {
            if (!pivot) return;
            pivot.localPosition = restPosition;
            pivot.localRotation = restRotation;
            pivot.localScale = restScale;
        }
        void OnDisable()
        {
            if (building) building.Damaged -= OnDamaged;
            StopAllCoroutines(); reaction = null; Restore();
        }
    }
}
```

연속 피격에도 기준 자세를 다시 취득하지 않는다. 위 코드는 초기화할 때 자세를 저장하고, 재피격 시 그 자세로 되돌려 연출을 처음부터 재생한다. 루트 위치와 Collider는 변경하지 않는다. 마지막 타격에는 건물이 즉시 사라지므로 흔들림은 보이지 않고 Popup만 남는다. 파괴 연출을 남기려면 별도 파편 Visual을 생성한다.

### 2-6. 생성 처리에 연결

이 실습에서는 FBX 바리케이드용 Scene을 사용한다. `GridBuildSystem`에 다음 필드를 추가하고 Grid Inspector에서 Popup Prefab과 Camera를 연결한다. Preview 생성 과정은 그대로 사용한다.

```csharp
[SerializeField] GridDamagePopup damagePopupPrefab;
[SerializeField] Camera feedbackCamera;
```

TryBuild의 `item.CreateVisual(root.transform);`을 다음으로 변경한다.

```csharp
var visual = item.CreateVisual(root.transform);
```

BoxCollider의 center/size 설정 후, `building = candidate;` 앞에 추가한다.

```csharp
var pivot = visual.transform.Find("FeedbackPivot");
if (pivot)
    root.AddComponent<GridBuildingFeedback>().Initialize(
        candidate, pivot, damagePopupPrefab,
        feedbackCamera ? feedbackCamera : Camera.main);
else
    Debug.LogWarning("FeedbackPivot missing: hit presentation not attached", visual);
```

이렇게 기존 비주얼 전용 프리팹 규칙과 미리보기 동작을 유지한다. 여러 건물로 확장할 때는 Pivot 이름·Popup·흔들림 강도를 Definition의 표현 프로필로 묶는다. 건물 재활성화 기능을 추가한다면 초기화 완료 여부를 저장하고 OnEnable/OnDisable에서 이벤트 구독과 해제를 대칭으로 관리한다. 이 예제는 현재의 생성부터 파괴까지 수명 주기를 대상으로 한다.

### 2-7. 공격 재현과 확인

1. 너비 10인 보드에서 몬스터를 (2,2), 플레이어를 (2,6)에 둔다.
2. z=4 행을 x=0,2,4,6,8 Anchor의 2×1 바리케이드 5개로 막는다. 기존 Occupant와 겹치지 않는지 확인한다.
3. 몬스터의 Player 참조를 연결하고 Play한다. 현재 AI는 우회로가 있으면 건물을 공격하지 않으므로 보드 양 끝까지 막는다.
4. 공격마다 HP가 25 줄고 모델만 흔들리며 머리 위로 25가 떠오른다. 네 번째에 건물이 사라지고 셀이 해제된다. 마지막 25는 건물 소멸 후에도 보인다.
5. 간격을 잠시 0.15초로 줄여 연속 피격 시 자세 복원만 확인한다. Attack 클립 재발행 제한은 별도이므로 정상 간격으로 복원하고 저장한다.

[Overview](ANIMATION_TUTORIAL.md) | [Previous](ANIMATION_TUTORIAL_01_CHARACTER.md) | [Next](ANIMATION_TUTORIAL_03_FLASH.md)

# 3. 공통 머티리얼과 커스텀 Flash 셰이더

[Overview](ANIMATION_TUTORIAL.md) | [Previous](ANIMATION_TUTORIAL_02_BARRICADE.md)

이 문서는 현재 GridSystem 위에 애니메이션을 연결하는 실습 설계서다. **아래 C# 코드는 이미 프로젝트에 생성·연결되어 있다.** 코드 블록은 구조를 학습하기 위한 예제이므로 같은 클래스를 다시 생성하거나 기존 호출을 중복 삽입하지 않는다. 실제 파일에는 참조 누락 방어와 이벤트 재구독 처리도 추가되어 있다. Unity Inspector에서 모델·클립·프리팹을 연결하는 과정은 직접 진행한다.

### 3-1. 공통화 범위

모든 대상에 동일한 Flash 프로퍼티를 가진 커스텀 Shader Graph를 사용한다. 같은 텍스처의 바리케이드는 하나의 Material 에셋을 공유한다. 텍스처가 다른 모델은 같은 Shader를 사용하는 별도 Material로 만든다. 하나의 Material에 서로 다른 BaseMap을 동시에 저장할 수는 없다. 완전히 하나의 Material로 통합하려면 후속 실습에서 아틀라스/Texture Array와 UV를 설계한다.

### 3-2. URP Lit Shader Graph 제작

1. `Shaders`에서 Create > Shader Graph > URP > Lit Shader Graph를 선택해 `GridHitFlash`를 만든다. 자체 Flash 계산을 가진 커스텀 셰이더다. Graph Inspector에서 Universal/Lit, Opaque를 확인한다.
2. Blackboard에 다음 Exposed 프로퍼티를 만든다. Reference 문자열을 정확히 입력한다.

| Display / Reference | Type | Default |
| --- | --- | --- |
| Base Map / `_BaseMap` | Texture2D | White |
| Base Color / `_BaseColor` | Color | White |
| Normal Map / `_NormalMap` | Texture2D (Normal 설정) | 기본 Normal |
| Normal Strength / `_NormalStrength` | Float | 1 |
| Metallic / `_Metallic` | Slider 0〜1 | 0 |
| Smoothness / `_Smoothness` | Slider 0〜1 | 0.25 |
| Flash Color / `_FlashColor` | Color HDR | White |
| Flash Amount / `_FlashAmount` | Slider 0〜1 | 0 |

3. Sample Texture 2D에서 BaseMap을 UV0로 읽는다. RGB에 BaseColor RGB를 Multiply하여 기본색 C를 만든다.
4. Saturate(FlashAmount)를 t로 사용해 Lerp(C, FlashColor.rgb, t)를 Fragment의 Base Color에 연결한다.
5. `FlashColor.rgb × t × 2`를 Emission에 연결한다. Bloom은 선택 사항이며 Emission만으로도 색 변화를 확인할 수 있다.
6. NormalMap은 Sample Texture 2D의 Type=`Normal`로 읽고 Normal Strength 노드를 거쳐 Normal (Tangent Space)에 연결한다. Metallic/Smoothness도 각 Fragment 블록에 연결한다.
7. Save Asset을 누른다. 공통 Material `Mat/Barricade_HitFlash.mat`을 만들고 Shader에 이 Graph를 지정한다. 2장의 BaseColor와 Normal을 다시 연결한다. 처음에는 MaskMap 없이 수치를 사용하고 채널 규약을 확인한 뒤 Graph를 확장한다.
8. 이 Material을 FBX Visual의 모든 대상 Material 슬롯에 지정한다. Inspector에서 FlashAmount를 0→1→0으로 바꿔 확인한다. 마지막에는 반드시 0으로 돌린다.

URP/Lit에서 Shader를 변경하면 이름이 다른 프로퍼티는 자동 승계되지 않는다. 텍스처와 Normal 설정을 다시 확인한다.

### 3-3. 피격 개체만 Flash 적용

공유 Material에 직접 SetFloat하면 모든 개체가 동시에 빛난다. `Presentation/GridHitFlash.cs`를 만들고 MaterialPropertyBlock으로 Renderer별 값을 설정한다.

```csharp
using UnityEngine;

namespace GridTutorial
{
    public sealed class GridHitFlash : MonoBehaviour
    {
        Renderer[] targets;
        MaterialPropertyBlock block;
        float remaining;
        const float Duration = .12f;
        static readonly int Amount = Shader.PropertyToID("_FlashAmount");
        public void Initialize(Transform visual)
        {
            targets = visual.GetComponentsInChildren<Renderer>(true);
            block = new MaterialPropertyBlock();
            Write(0);
        }
        public void Pulse() { remaining = Duration; Write(1); }
        void Update()
        {
            if (remaining <= 0) return;
            remaining = Mathf.Max(0, remaining - Time.deltaTime);
            Write(remaining / Duration);
        }
        void Write(float value)
        {
            if (targets == null) return;
            foreach (var target in targets)
            {
                if (!target) continue;
                // Material 슬롯별 기존 값을 보존하며 Flash만 갱신한다.
                for (int i = 0; i < target.sharedMaterials.Length; i++)
                {
                    block.Clear();
                    target.GetPropertyBlock(block, i);
                    block.SetFloat(Amount, value);
                    target.SetPropertyBlock(block, i);
                }
            }
        }
        void OnDisable() { remaining = 0; Write(0); }
    }
}
```

2-6에서 Feedback을 추가하기 직전에 다음을 삽입한다.

```csharp
root.AddComponent<GridHitFlash>().Initialize(visual.transform);
```

GridBuildingFeedback의 OnDamaged 맨 앞에 다음을 추가한다.

```csharp
GetComponent<GridHitFlash>()?.Pulse();
```

이제 확정된 피격 이벤트 하나에서 흔들림·숫자·Flash가 함께 시작된다. 연속 피격 시 remaining을 매번 0.12로 되돌린다. Flash는 Update 하나로 갱신한다. Renderer.material을 호출하지 않아 불필요한 Material 복제도 만들지 않는다.

MaterialPropertyBlock을 사용하는 Renderer는 SRP Batcher 호환성을 잃으므로 대량 배치 시 Profiler로 비교한다. 개체별 표현을 간단히 실습하기 위한 선택이며 배칭 성능을 개선한다고 가정하지 않는다. [Unity 배칭과 프로퍼티 문서](https://docs.unity.com/en-us/engine/6000.3/manual/analysis/graphics-performance-profiling/reduce-draw-calls/draw-call-batching/properties)

캐릭터로 확장할 때는 루트에 GridHitFlash를 붙이고 모델 Transform으로 Initialize한다. 앞으로 추가할 캐릭터 HP의 확정 피격 이벤트에서 Pulse와 PlayHit를 호출한다. 피격이 성립한 대상이 연출을 시작하도록 연결한다.

[Overview](ANIMATION_TUTORIAL.md) | [Previous](ANIMATION_TUTORIAL_02_BARRICADE.md)

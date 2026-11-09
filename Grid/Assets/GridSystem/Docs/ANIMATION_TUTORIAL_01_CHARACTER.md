# 1. 모델과 리깅 애니메이션

[Overview](ANIMATION_TUTORIAL.md) | [Next](ANIMATION_TUTORIAL_02_BARRICADE.md)

이 문서는 현재 GridSystem 위에 애니메이션을 연결하는 실습 설계서다. **아래 C# 코드는 이미 프로젝트에 생성·연결되어 있다.** 코드 블록은 구조를 학습하기 위한 예제이므로 같은 클래스를 다시 생성하거나 기존 호출을 중복 삽입하지 않는다. 실제 파일에는 참조 누락 방어와 이벤트 재구독 처리도 추가되어 있다. Unity Inspector에서 모델·클립·프리팹을 연결하는 과정은 직접 진행한다.

### 1-1. 좀비 FBX를 Humanoid로 준비

1. Project에서 좀비 기본 FBX를 선택한다. Inspector의 Rig에서 Animation Type=`Humanoid`, Avatar Definition=`Create From This Model`로 바꾸고 Apply한다. 현재 FBX 메타데이터는 이 실습용 Humanoid 구성이 아니므로 이 단계를 생략하지 않는다.
2. Configure에서 골반·척추·머리·양팔·양다리 매핑과 T-Pose를 확인한다. 유효한 Avatar 표시를 확인하고 Done한다. 자동 매핑이 실패하면 필수 본을 직접 지정한다.
3. 5개 애니메이션 FBX는 Rig=`Humanoid`, Avatar Definition=`Copy From Other Avatar`, Source=기본 좀비 FBX의 Avatar로 지정하고 Apply한다. 같은 스켈레톤의 분리 파일에만 이 Avatar를 복사한다.
4. Animation 탭에서 클립 이름을 아래처럼 정리한다. FBX를 펼쳐 재생 아이콘이 있는 AnimationClip을 Preview에서 확인한다. FBX 전체를 Motion 슬롯에 넣지 않는다.

| 슬롯 이름 | 소스 | Loop Time |
| --- | --- | --- |
| Idle | Zombie Idle | On |
| Move | Zombie Walk | On |
| Attack | Zombie Attack | Off |
| Hit | Zombie Reaction Hit | Off |
| Death | Zombie Death | Off |

5. Root Transform Rotation, Position Y, Position XZ의 Bake Into Pose를 켜서 시작한다. 걷기 Preview에서 제자리 이동 여부와 발 높이를 확인한다. 이동 거리는 GridMover가 담당한다. Death의 바닥 관통 여부도 별도로 확인한다.
6. Scene의 Animator에서 Apply Root Motion을 끈다. Root Motion을 처리하는 OnAnimatorMove도 추가하지 않는다.

Humanoid로 유효하게 변환할 수 없는 몬스터는 Generic으로 별도 Avatar/클립 세트를 구성한다. Generic 클립은 본 경로가 맞아야 하므로 서로 다른 스켈레톤을 클립만 바꿔 리타기팅할 수 없다. 공통 상태 구조는 공유할 수 있지만 임의의 리그 호환성을 Override가 해결하지는 않는다.

### 1-2. 하나의 공통 Animator Controller

1. `Animation/Controllers`에서 Create > Animator Controller로 `ActorBase.controller`를 만든다.
2. Override 표의 키를 안정적으로 유지하도록 `Animation/Clips`에 서로 다른 빈 AnimationClip 5개를 만든다: `Slot_Idle`, `Slot_Move`, `Slot_Attack`, `Slot_Hit`, `Slot_Death`.
3. Controller에 Idle, Move, Attack, Hit, Death 상태를 만들고 각각 해당 Slot 클립을 Motion으로 지정한다. 기본 상태는 Idle이다. 모든 캐릭터는 완성된 Override를 사용한다. 빈 슬롯 Controller는 직접 재생하지 않는다.
4. Parameters를 아래와 같이 만든다. 철자와 대소문자를 일치시킨다.

| 이름 | 형식 | 의미 |
| --- | --- | --- |
| Moving | Bool | 실제 수평 이동 여부 |
| Attack | Trigger | 공격 1회 시작 |
| Hit | Trigger | 피격 1회 시작 |
| Dead | Bool | 사망 표현 고정 |

5. 다음 표대로 Transition을 만든다. 단위는 normalized time으로 통일한다. Fixed Duration을 끄고 Duration=`0.05`로 시작한다.

| From → To | 조건 | Has Exit Time / Exit Time |
| --- | --- | --- |
| Idle → Move | Moving=true, Dead=false | Off |
| Move → Idle | Moving=false, Dead=false | Off |
| Idle → Attack, Move → Attack | Attack, Dead=false | Off |
| Attack → Move | Moving=true, Dead=false | On / 0.95 |
| Attack → Idle | Moving=false, Dead=false | On / 0.95 |
| Any State → Hit | Hit, Dead=false | Off |
| Hit → Move | Moving=true, Dead=false | On / 0.95 |
| Hit → Idle | Moving=false, Dead=false | On / 0.95 |
| Any State → Death | Dead=true | Off |

Any State의 Death 전이를 Hit보다 위로 배치하고 Can Transition To Self는 두 전이 모두 끈다. Death에는 출구가 없다. Attack/Hit 복귀 전이는 Interruption Source=`Current State`로 시작하고 사망 시 즉시 Death로 전환되는지 확인한다. Hit가 Attack을 중단해도 이 실습의 이미 확정된 피해는 취소되지 않는다.

짧은 클립으로 Override했을 때도 종료 조건이 유효하도록 normalized Exit Time을 쓴다. [Unity Animator Override Controller 문서](https://docs.unity.com/en-us/engine/6000.3/manual/animation-section/animation-mecanim/animator-override-controller)

### 1-3. 좀비 Override와 프리팹

1. Create > Animator Override Controller로 `Zombie.overrideController`를 만든다. Controller에 `ActorBase`를 지정한다.
2. Slot 5개를 해당 FBX의 실제 클립으로 모두 교체한다. 같은 클립을 여러 Slot의 원본으로 쓰지 않는다. 원본 클립이 같으면 독립적인 교체 슬롯으로 구분되지 않는다.
3. 기존 몬스터의 Sphere는 제거하거나 MeshRenderer만 끄고 다음 구조를 만든다.

```text
MonsterRoot                   ← GridOccupant, GridMover, GridMonsterAttacker
  VisualPivot                 ← 모델의 발 높이/전방 보정
    ZombieModel               ← Animator, SkinnedMeshRenderer와 본
```

4. Animator는 모델의 스켈레톤을 제어할 수 있는 FBX 루트에 둔다. Avatar=좀비 Avatar, Controller=Zombie Override, Apply Root Motion=Off. VisualPivot으로 크기·방향을 보정한다. 모델마다 전방을 +Z에 맞춘다.
5. GridOccupant의 elevation은 기존 구체/캡슐 기준 기본값 0.5다. 발 피벗 모델은 elevation=0으로 바꾸거나 VisualPivot의 Y를 -0.5로 보정한다. 두 방법을 동시에 적용하지 않는다.

### 1-4. 실제 그리드 이동을 읽는 공통 스크립트

`Presentation/GridActorAnimation.cs`를 만들고 아래를 작성한다. 루트의 실제 월드 좌표 차이를 읽으므로 셀을 미리 예약하는 GridOccupant.Cell을 이동 방향으로 사용하지 않는다.

```csharp
using UnityEngine;

namespace GridTutorial
{
    public sealed class GridActorAnimation : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] Transform visualPivot;
        Vector3 previous;
        bool dead;
        void OnEnable() { previous = transform.position; }
        void LateUpdate()
        {
            var delta = transform.position - previous;
            previous = transform.position;
            var up = transform.up;
            var flat = Vector3.ProjectOnPlane(delta, up);
            bool moving = !dead && flat.sqrMagnitude > 0.0000001f;
            animator.SetBool("Moving", moving);
            if (moving) visualPivot.rotation = Quaternion.LookRotation(flat, up);
        }
        public void Face(Vector3 position)
        {
            var d = Vector3.ProjectOnPlane(position - transform.position, transform.up);
            if (d.sqrMagnitude > .0001f)
                visualPivot.rotation = Quaternion.LookRotation(d, transform.up);
        }
        public void PlayAttack() { if (!dead) animator.SetTrigger("Attack"); }
        public void PlayHit() { if (!dead) animator.SetTrigger("Hit"); }
        public void PlayDeath()
        {
            dead = true;
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Hit");
            animator.SetBool("Moving", false);
            animator.SetBool("Dead", true);
        }
        [ContextMenu("Test Hit")] void TestHit() { if (Application.isPlaying) PlayHit(); }
        [ContextMenu("Test Death")] void TestDeath() { if (Application.isPlaying) PlayDeath(); }
    }
}
```

MonsterRoot에 붙이고 Animator와 VisualPivot을 연결한다. Play 모드에서 이동·정지와 컴포넌트 메뉴의 Test Hit/Death를 확인한다. 이 컴포넌트는 연출만 제어한다. 실제 사망 처리에서는 먼저 AI와 GridMover를 끄고, GridOccupant를 비활성화해 점유를 해제한 뒤 PlayDeath를 호출한다. 루트는 사망 클립이 끝날 때까지 유지한다. 풀링을 추가한다면 dead와 Animator를 초기화하는 별도 Reset API가 필요하다.

`GridMonsterAttacker`에 `GridActorAnimation animationView;` 필드를 추가하고 기존 Awake 끝에 `animationView = GetComponent<GridActorAnimation>();`을 넣는다. 실제 공격 블록의 `next.Building.TakeDamage(damage);` 바로 앞에 다음을 넣는다.

```csharp
if (animationView)
{
    animationView.Face(AttackTarget.transform.position);
    animationView.PlayAttack();
}
```

attackInterval은 교체한 Attack 클립의 재생 시간과 블렌드 시간보다 길게 시작한다. 예를 들어 클립 1.2초면 간격 1.3초부터 검증한다. 공격 중 재발행한 Trigger는 다음 공격까지 남을 수 있으므로 짧은 간격으로 무조건 발행하지 않는다.

**이번 장의 피해 시점은 기존 코드와 동일하게 공격 시작 순간이다.** 클립의 손이 닿는 순간에 피해를 주려면 후속 단계에서 공격 시작 시 대상·피해·1회 소비 토큰을 저장하고, 각 Override 클립의 Animation Event를 Animator 오브젝트의 수신 스크립트가 받아 판정한다. 이벤트 시 대상 생존·동일 보드·인접·토큰 미소비를 재검증한다. 기존 즉시 TakeDamage 호출은 그때 제거한다. 클립별 이벤트 위치가 다르고, 중단·사망 시 예약 취소가 필요하므로 클립 교체만으로 전투 타이밍까지 자동 호환된다고 가정하지 않는다.

### 1-5. Player FBX를 직접 임포트하는 실습

1. 사용 가능한 Humanoid 캐릭터의 **모델 포함 FBX 1개**와 Idle/Walk 또는 Run/Attack/Hit/Death 클립 FBX를 준비한다. `Models/Player`로 파일을 드래그한다. 외부 에셋의 사용 조건을 확인한다.
2. 모델은 Humanoid/Create From This Model로 Avatar를 만든다. 이 플레이어의 동일 스켈레톤 클립에는 플레이어 Avatar를 Copy한다. 서로 다른 Humanoid 원본에서 가져온 클립은 각 원본에서 유효한 Humanoid Avatar를 구성하고 리타기팅을 검증한다. 좀비 Avatar를 플레이어 FBX에 무조건 복사하지 않는다.
3. Loop/Bake 설정은 좀비와 동일하게 적용한다. `Player.overrideController`를 만들고 Controller=`ActorBase`, 다섯 Slot을 플레이어 클립으로 교체한다.
4. PlayerRoot의 GridOccupant/GridMover와 기존 GridPathPreview의 Actor 참조는 유지한다. 캡슐 그래픽을 끄고 `VisualPivot/PlayerModel`을 추가한다. 캐릭터용 Rigidbody 물리 이동과 NavMeshAgent는 추가하지 않는다.
5. PlayerModel Animator에 플레이어 Avatar와 Player Override를 넣고 Root Motion을 끈다. PlayerRoot에 동일한 GridActorAnimation을 붙여 참조를 연결한다.
6. 클릭 이동, 정지, 방향 전환, Test Hit/Death를 확인한다. Attack은 전투 명령 코드가 PlayAttack을 호출해야 재생된다. 필요하면 같은 방식의 ContextMenu 테스트를 추가한다.
7. 세 번째 몬스터도 동일한 구조와 Base Controller를 사용하고 새 Override 및 자기 Avatar로 구성한다. 속도 차이는 GridMover, 보행 클립의 재생 보정은 Animator의 Move State Speed로 조정한다. 종별로 다른 속도 보정이 필요하면 공통 Float 파라미터와 프로필을 후속 추가한다.

완료 기준: 모델/Avatar/Override/비주얼 보정 이외에 Controller 전이나 공통 표현 코드를 복제하지 않고 두 캐릭터가 이동한다. 발 미끄러짐은 실제 이동 속도와 클립 보행 속도를 맞춰 줄인다.

[Overview](ANIMATION_TUTORIAL.md) | [Next](ANIMATION_TUTORIAL_02_BARRICADE.md)

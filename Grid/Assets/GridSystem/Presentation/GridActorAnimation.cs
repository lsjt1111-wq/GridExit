using UnityEngine;

namespace GridTutorial
{
    public sealed class GridActorAnimation : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] Transform visualPivot;
        Vector3 previous;
        bool dead;
        bool Ready => animator && animator.isActiveAndEnabled && animator.runtimeAnimatorController;
        void Reset()
        {
            animator = GetComponentInChildren<Animator>(true);
            if (animator) visualPivot = animator.transform;
        }
        void Awake()
        {
            if (!animator) animator = GetComponentInChildren<Animator>(true);
            if (!visualPivot && animator && animator.transform != transform)
                visualPivot = animator.transform;
            if (visualPivot == transform) visualPivot = null;
        }
        void OnEnable() { previous = transform.position; }
        void LateUpdate()
        {
            var delta = transform.position - previous;
            previous = transform.position;
            if (!Ready) return;
            var up = transform.up;
            var flat = Vector3.ProjectOnPlane(delta, up);
            bool moving = !dead && flat.sqrMagnitude > 0.0000001f;
            animator.SetBool("Moving", moving);
            if (moving && visualPivot) visualPivot.rotation = Quaternion.LookRotation(flat, up);
        }
        public void Face(Vector3 position)
        {
            var d = Vector3.ProjectOnPlane(position - transform.position, transform.up);
            if (visualPivot && d.sqrMagnitude > .0001f)
                visualPivot.rotation = Quaternion.LookRotation(d, transform.up);
        }
        public void PlayAttack() { if (!dead && Ready) animator.SetTrigger("Attack"); }
        public void PlayHit() { if (!dead && Ready) animator.SetTrigger("Hit"); }
        public void PlayDeath()
        {
            dead = true;
            if (!Ready) return;
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Hit");
            animator.SetBool("Moving", false);
            animator.SetBool("Dead", true);
        }
        [ContextMenu("Test Attack")] void TestAttack() { if (Application.isPlaying) PlayAttack(); }
        [ContextMenu("Test Hit")] void TestHit() { if (Application.isPlaying) PlayHit(); }
        [ContextMenu("Test Death")] void TestDeath() { if (Application.isPlaying) PlayDeath(); }
    }
}

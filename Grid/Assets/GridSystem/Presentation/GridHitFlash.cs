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

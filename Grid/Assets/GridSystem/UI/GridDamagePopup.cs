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
        void Awake() { if (!label) label = GetComponentInChildren<TMP_Text>(true); }
        public void Setup(float amount, Vector3 position, Camera camera)
        {
            start = position; transform.position = position;
            viewCamera = camera; age = 0;
            initialized = true;
            if (label)
            {
                label.text = amount.ToString("0.#");
                label.alpha = 1;
            }
        }
        void LateUpdate()
        {
            // Scene templates stay untouched until a damage popup is initialized.
            if (!initialized) return;
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Mathf.Max(.01f, lifetime));
            transform.position = start + Vector3.up * (.8f * t);
            if (viewCamera) transform.rotation = viewCamera.transform.rotation;
            if (label) label.alpha = 1 - t;
            if (t >= 1) Destroy(gameObject);
        }
    }
}

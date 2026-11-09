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
        bool subscribed;
        GridHitFlash flash;
        public void Initialize(GridBuilding owner, Transform feedbackPivot,
                               GridDamagePopup popupPrefab, Camera camera)
        {
            Unsubscribe();
            building = owner; pivot = feedbackPivot;
            flash = GetComponent<GridHitFlash>();
            popup = popupPrefab; cameraView = camera;
            restPosition = pivot.localPosition;
            restScale = pivot.localScale;
            restRotation = pivot.localRotation;
            Subscribe();
        }
        void OnEnable() => Subscribe();
        void Subscribe()
        {
            if (!subscribed && building && isActiveAndEnabled)
            { building.Damaged += OnDamaged; subscribed = true; }
        }
        void Unsubscribe()
        {
            if (subscribed && building) building.Damaged -= OnDamaged;
            subscribed = false;
        }
        void OnDamaged(float amount)
        {
            if (flash) flash.Pulse();
            var box = GetComponent<BoxCollider>();
            var top = box ? box.bounds.center + Vector3.up * (box.bounds.extents.y + .3f)
                          : transform.position + Vector3.up;
            if (popup) Instantiate(popup).Setup(amount, top, cameraView);
            if (!pivot) return;
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
            Unsubscribe();
            StopAllCoroutines(); reaction = null; Restore();
        }
    }
}

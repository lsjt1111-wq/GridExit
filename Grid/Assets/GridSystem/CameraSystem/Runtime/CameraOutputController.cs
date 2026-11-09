using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace GridTutorial.Cameras
{
    public sealed class CameraOutputController : MonoBehaviour
    {
        [SerializeField] ExplorationCameraController controller;
        [SerializeField] Camera evidenceCamera;
        [SerializeField] RenderTexture outputTexture;
        [SerializeField] RawImage photoPreview;
        [SerializeField, Min(.1f)] float timeout = 2;
        bool capturing;
        float startedAt;
        public bool HasPhoto { get; private set; }
        void OnEnable() => RenderPipelineManager.endCameraRendering += OnCameraRendered;
        void Start()
        {
            if (evidenceCamera) evidenceCamera.enabled = false;
            if (photoPreview) { photoPreview.raycastTarget = false; photoPreview.enabled = false; }
        }
        void LateUpdate()
        {
            if (capturing && (!controller || !controller.CanObserve || Time.realtimeSinceStartup - startedAt > timeout)) CancelCapture();
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame) Capture();
        }
        public void Capture()
        {
            if (!isActiveAndEnabled || capturing || !controller || !controller.CanObserve || !evidenceCamera || !outputTexture) return;
            if (evidenceCamera == controller.ViewCamera) { Debug.LogError("Evidence Camera must be a separate Base camera.", this); return; }
            var source = controller.ViewCamera;
            evidenceCamera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            evidenceCamera.orthographic = false;
            evidenceCamera.fieldOfView = source.fieldOfView;
            evidenceCamera.nearClipPlane = source.nearClipPlane; evidenceCamera.farClipPlane = source.farClipPlane;
            evidenceCamera.targetTexture = outputTexture;
            evidenceCamera.aspect = (float)outputTexture.width / Mathf.Max(1, outputTexture.height);
            capturing = true; startedAt = Time.realtimeSinceStartup;
            evidenceCamera.enabled = true;
        }
        void OnCameraRendered(ScriptableRenderContext context, Camera camera)
        {
            if (!capturing || camera != evidenceCamera) return;
            capturing = false; evidenceCamera.enabled = false; HasPhoto = true;
            if (photoPreview) { photoPreview.texture = outputTexture; photoPreview.enabled = true; }
        }
        void CancelCapture() { capturing = false; if (evidenceCamera && (!controller || evidenceCamera != controller.ViewCamera)) evidenceCamera.enabled = false; }
        void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnCameraRendered;
            CancelCapture();
        }
    }
}

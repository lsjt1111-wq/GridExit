using UnityEngine;
using UnityEngine.InputSystem;

namespace GridTutorial.Cameras
{
    public enum ExplorationView { TopView, FirstPerson }

    // Runs before GridBuildController (-50) and GridPathPreview (0).
    [DefaultExecutionOrder(-150)]
    public sealed class ExplorationCameraController : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] Transform eyeAnchor;
        [SerializeField] GridMover mover;
        [SerializeField] GameManager gameManager;
        [SerializeField] GridPathPreview pathPreview;
        [SerializeField] GridBuildController buildController;
        [SerializeField] CameraProjectionSettings projection;
        [SerializeField] FirstPersonLook look;
        [SerializeField] Renderer[] firstPersonHiddenRenderers = System.Array.Empty<Renderer>();
        [SerializeField, Min(.05f)] float transitionDuration = .4f;
        [SerializeField] bool showHint = true;
        Vector3 topPosition;
        Quaternion topRotation;
        float topSize, topNear, topFar, timer;
        bool[] rendererStates;
        bool pending, transitioning, switched, panelOpen, focused = true;
        bool initialOrthographic, initialized;
        float initialFov;
        public ExplorationView Mode { get; private set; }
        public Camera ViewCamera => viewCamera;
        public bool IsPlaying => !gameManager || gameManager.State == LoopState.Playing;
        public bool CanObserve => isActiveAndEnabled && focused && IsPlaying && Mode == ExplorationView.FirstPerson && !transitioning && !panelOpen;
        public bool CanCommand => isActiveAndEnabled && focused && IsPlaying && IsTopView && !pending && !transitioning && !panelOpen;
        public bool IsTopView => Mode == ExplorationView.TopView;
        public bool PanelOpen => panelOpen;

        void Awake()
        {
            if (!viewCamera || !eyeAnchor || !mover || !pathPreview)
            {
                Debug.LogError("Camera tutorial: assign Camera, Eye Anchor, Mover and Path Preview.", this);
                enabled = false;
                return;
            }
            SaveTop();
            initialOrthographic = viewCamera.orthographic;
            initialFov = viewCamera.fieldOfView;
            initialized = true;
            rendererStates = new bool[firstPersonHiddenRenderers.Length];
            for (int i = 0; i < rendererStates.Length; i++)
                if (firstPersonHiddenRenderers[i]) rendererStates[i] = firstPersonHiddenRenderers[i].enabled;
        }
        void OnEnable() { if (gameManager) gameManager.StateChanged += OnLoopChanged; }
        void OnLoopChanged(LoopState state)
        {
            if (state != LoopState.Playing) ResetView();
            ApplyPermissions();
        }
        void Update()
        {
            if (!viewCamera || !mover) return;
            if (!IsPlaying) { if (Mode != ExplorationView.TopView || transitioning || pending) ResetView(); ApplyPermissions(); return; }
            if (focused && Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) RequestToggle();
            if (pending && !mover.IsMoving) { pending = false; BeginTransition(); }
            if (transitioning)
            {
                timer += Time.unscaledDeltaTime;
                if (!switched && timer >= transitionDuration * .5f) { switched = true; SwitchView(); }
                if (timer >= transitionDuration) transitioning = false;
            }
            if (focused && IsTopView && !pending && !transitioning && !panelOpen && projection)
                projection.Zoom(viewCamera, Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0);
            ApplyPermissions();
        }
        void LateUpdate()
        {
            if (Mode == ExplorationView.FirstPerson && eyeAnchor && viewCamera)
                viewCamera.transform.SetPositionAndRotation(eyeAnchor.position, look ? look.ViewRotation : eyeAnchor.rotation);
        }
        public void RequestToggle()
        {
            if (!focused || !IsPlaying || transitioning || panelOpen || !viewCamera || !eyeAnchor || !mover) return;
            if (pending) { pending = false; ApplyPermissions(); return; }
            if (IsTopView && mover.IsMoving) pending = true;
            else BeginTransition();
            ApplyPermissions();
        }
        void BeginTransition()
        {
            if (buildController) buildController.CancelBuild();
            transitioning = true; switched = false; timer = 0;
        }
        void SaveTop()
        {
            topPosition = viewCamera.transform.position; topRotation = viewCamera.transform.rotation;
            topSize = viewCamera.orthographicSize; topNear = viewCamera.nearClipPlane; topFar = viewCamera.farClipPlane;
        }
        void SwitchView()
        {
            if (IsTopView)
            {
                SaveTop(); Mode = ExplorationView.FirstPerson;
                viewCamera.orthographic = false;
                viewCamera.nearClipPlane = .05f;
                viewCamera.fieldOfView = 65;
                if (projection) projection.ApplyFirstPerson(viewCamera);
                if (look) look.ResetLook(eyeAnchor.rotation);
                HideBody(true);
            }
            else RestoreTop();
        }
        void RestoreTop()
        {
            Mode = ExplorationView.TopView;
            if (viewCamera)
            {
                viewCamera.transform.SetPositionAndRotation(topPosition, topRotation);
                viewCamera.orthographic = initialOrthographic; viewCamera.fieldOfView = initialFov;
                viewCamera.orthographicSize = topSize;
                viewCamera.nearClipPlane = topNear; viewCamera.farClipPlane = topFar;
            }
            HideBody(false);
        }
        void HideBody(bool hide)
        {
            if (rendererStates == null) return;
            for (int i = 0; i < rendererStates.Length; i++)
                if (firstPersonHiddenRenderers[i]) firstPersonHiddenRenderers[i].enabled = hide ? false : rendererStates[i];
        }
        public void SetPanelOpen(bool value) { panelOpen = value; ApplyPermissions(); }
        void ApplyPermissions()
        {
            bool allow = CanCommand;
            if (pathPreview && pathPreview.enabled != allow) pathPreview.enabled = allow;
            if (buildController && buildController.enabled != allow) buildController.enabled = allow;
            bool locked = CanObserve;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
        void ResetView() { pending = transitioning = panelOpen = false; RestoreTop(); }
        void OnApplicationFocus(bool value) { focused = value; ApplyPermissions(); }
        void OnDisable()
        {
            if (gameManager) gameManager.StateChanged -= OnLoopChanged;
            if (initialized) { ResetView(); ApplyPermissions(); }
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        void OnGUI()
        {
            if (showHint && IsPlaying) GUI.Label(new Rect(12, 70, 700, 25), pending ? "Waiting for arrival | Tab: Cancel" : IsTopView ? "Top view | LMB: Move | Tab: Observe | Wheel: Zoom | F: Wipe" : "First person | Mouse: Look | RMB: Zoom | E: Inspect | Tab: Return | P: Photo");
            if (!transitioning) return;
            float alpha = 1 - Mathf.Abs(Mathf.Clamp01(timer / transitionDuration) * 2 - 1);
            var previous = GUI.color; int depth = GUI.depth;
            GUI.depth = -10000; GUI.color = new Color(0, 0, 0, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous; GUI.depth = depth;
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace ScreenTransitions
{
    public class ShelterDoorTransition : MonoBehaviour, ISceneTransitionDriver, IInPlaceTransitionDriver
    {
        public Camera targetCamera;
        public Camera doorCamera;
        public Transform leftDoor;
        public Transform rightDoor;
        [Tooltip("Overlay scene instance: remain in the additive scene and dispose on unload.")]
        public bool sceneScoped;
        [Header("Perspective doorway")]
        [Range(35, 75)] public float doorFieldOfView = 50;
        [Range(120, 175)] public float swingDegrees = 155;
        public GameObject previewWorldA;
        public GameObject previewWorldB;
        [Min(0.1f)] public float closeSeconds = 1.2f;
        [Min(0.1f)] public float openSeconds = 1.8f;
        [Min(0)] public float closedHoldSeconds = 0.65f;

        [Tooltip("Player input/controller components to disable during transition. Do not include this component.")]
        public Behaviour[] inputBehaviours;
        public Canvas[] hideCanvases;
        [Tooltip("Hide scene UI during the transition, including destination canvases, and restore their previous state.")]
        public bool hideAllSceneCanvases;
        public UnityEvent onBegin = new UnityEvent();
        public UnityEvent onCovered = new UnityEvent();
        public UnityEvent onComplete = new UnityEvent();
        public bool showDemoControls = true;
        public string nextScene;
        public bool IsRunning { get; private set; }
        public float Openness { get; protected set; } = 1;
        private RenderTexture texture;
        private bool[] inputStates, canvasStates;
        private Camera maskedCamera;
        private int oldMask;
        private bool tintAlternate;
        private static ShelterDoorTransition owner;
        private Transform leftHinge, rightHinge;
        private Transform doorway;
        private Transform[] frameParts;
        protected bool opening;
        protected virtual string PreviewTitle => "SHELTER // PRIMITIVE DOOR TRANSITION";
        protected virtual bool HasStageReferences => doorCamera != null && leftDoor != null && rightDoor != null;
        private bool localRunning;
        private System.Func<IEnumerator> localInitialize;
        private System.Action localCompleted;
        private System.Action<string> localFailed;
        private string initializationError;
        private readonly System.Collections.Generic.Dictionary<Canvas, bool> sceneCanvasStates = new System.Collections.Generic.Dictionary<Canvas, bool>();

        protected virtual void Awake()
        {
            if (!sceneScoped)
            {
                if (owner != null && owner != this) { Destroy(gameObject); return; }
                owner = this;
                SceneTransition.Register(this);
                DontDestroyOnLoad(gameObject);
            }
            SceneManager.sceneLoaded += OnSceneLoaded;
            BuildPerspectiveRig();
            doorCamera.gameObject.SetActive(false);
            SetOpen(1);
        }

        public void Preview() => Begin(null);
        public void TransitionToScene(string sceneName) => SceneTransition.TryLoadScene(sceneName);
        public bool TryLoadScene(string sceneName) => Begin(sceneName);

        public bool TryRunInPlace(System.Func<IEnumerator> initialize, System.Action completed, System.Action<string> failed)
        {
            if (!sceneScoped || IsRunning) return false;
            localRunning = true;
            localInitialize = initialize;
            localCompleted = completed;
            localFailed = failed;
            initializationError = null;
            if (Begin(null)) return true;
            localRunning = false;
            localInitialize = null;
            localCompleted = null;
            localFailed = null;
            return false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsRunning) return;
            var camera = FindGameCamera();
            if (camera != null) BindCamera(camera);
            HideSceneCanvases();
        }

        private static Camera FindGameCamera()
        {
            if (Camera.main != null && Camera.main.targetTexture == null) return Camera.main;
            Camera result = null;
            foreach (var camera in Camera.allCameras)
            {
                if (camera.cameraType != CameraType.Game || camera.targetTexture != null || !camera.isActiveAndEnabled) continue;
                if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var data) && data.renderType == CameraRenderType.Overlay) continue;
                if (result == null || camera.depth > result.depth) result = camera;
            }
            return result;
        }

        private bool Begin(string sceneName)
        {
            if (IsRunning || !isActiveAndEnabled) return false;
            if (!string.IsNullOrEmpty(sceneName) && !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("Add the destination scene to Build Profiles / Scene List: " + sceneName, this);
                return false;
            }
            if (targetCamera == null || !targetCamera.isActiveAndEnabled) targetCamera = FindGameCamera();
            if (targetCamera == null || !HasStageReferences)
            { Debug.LogError("Shelter door references or an active game camera are missing.", this); return false; }
            IsRunning = true;
            StartCoroutine(Play(sceneName));
            return true;
        }

        private void BindCamera(Camera camera)
        {
            RestoreMask();
            targetCamera = camera;
            maskedCamera = camera;
            oldMask = camera.cullingMask;
            camera.cullingMask &= ~(1 << gameObject.layer);
            ScreenTransitionFeature.TargetCamera = camera;
        }

        private void RestoreMask()
        {
            if (maskedCamera != null) maskedCamera.cullingMask = oldMask;
            maskedCamera = null;
        }

        private IEnumerator Play(string sceneName)
        {
            opening = false;
            SceneTransition.SetPhase(TransitionPhase.Closing);
            BindCamera(targetCamera);
            HideSceneCanvases();
            SetInputLocked(true);
            EnsureTexture();
            doorCamera.gameObject.SetActive(true);
            ScreenTransitionFeature.Overlay = texture;
            onBegin.Invoke();
          
            yield return Animate(1, 0, closeSeconds);
            // Let the fully closed geometry reach the target before changing scenes.
            yield return new WaitForEndOfFrame();
            SceneTransition.SetPhase(TransitionPhase.Covered);
            onCovered.Invoke();
          
            float holdUntil = Time.realtimeSinceStartup + closedHoldSeconds;
            if (!string.IsNullOrEmpty(sceneName))
            {
                SceneTransition.SetPhase(TransitionPhase.Loading);
                AsyncOperation operation;
                try { operation = SceneManager.LoadSceneAsync(sceneName); }
                catch (System.Exception exception)
                {
                    Cleanup();
                    SceneTransition.NotifyFailed(this, exception.Message);
                    yield break;
                }
                if (operation == null)
                {
                    Cleanup();
                    SceneTransition.NotifyFailed(this, "Scene loading did not return an operation.");
                    yield break;
                }
                if (operation != null)
                    while (!operation.isDone) yield return null;
                // Allow destination Start() to initialise its camera and Ready gameplay state.
                yield return null;
                var camera = FindGameCamera();
                if (camera == null)
                {
                    Cleanup();
                    SceneTransition.NotifyFailed(this, "Destination scene has no active game camera.");
                    yield break;
                }
                BindCamera(camera);
                HideSceneCanvases();
            }
            else if (localRunning)
            {
                yield return InitializeWhileCovered();
                if (initializationError != null)
                {
                    Cleanup();
                    EndLocal(initializationError);
                    yield break;
                }
                var camera = FindGameCamera();
                if (camera == null)
                {
                    Cleanup();
                    EndLocal("Initialisation removed the active game camera.");
                    yield break;
                }
                BindCamera(camera);
                HideSceneCanvases();
            }
            else
            {
                tintAlternate = !tintAlternate;
                targetCamera.backgroundColor = tintAlternate ? new Color(0.18f, 0.29f, 0.35f) : new Color(0.35f, 0.23f, 0.14f);
                if (previewWorldA != null) previewWorldA.SetActive(!tintAlternate);
                if (previewWorldB != null) previewWorldB.SetActive(tintAlternate);
            }
            while (Time.realtimeSinceStartup < holdUntil) yield return null;
            opening = true;
            SceneTransition.SetPhase(TransitionPhase.Opening);
            yield return Animate(0, 1, openSeconds);
            Cleanup();
            if (localRunning) EndLocal(null);
            else SceneTransition.NotifyCompleted(this, SceneManager.GetActiveScene());
            onComplete.Invoke();
           
        }

        private IEnumerator InitializeWhileCovered()
        {
            var stack = new System.Collections.Generic.Stack<IEnumerator>();
            try
            {
                IEnumerator routine = null;
                try { routine = localInitialize?.Invoke(); }
                catch (System.Exception exception) { initializationError = exception.Message; }
                if (routine != null) stack.Push(routine);
                // Flatten nested coroutines so callback exceptions are caught and UI can be restored.
                while (stack.Count > 0 && initializationError == null)
                {
                    var current = stack.Peek();
                    bool more = false;
                    object yielded = null;
                    try { more = current.MoveNext(); if (more) yielded = current.Current; }
                    catch (System.Exception exception) { initializationError = exception.Message; }
                    if (initializationError != null) break;
                    if (!more) { stack.Pop(); DisposeInitialization(current); }
                    else if (yielded is IEnumerator nested) stack.Push(nested);
                    else yield return yielded;
                }
            }
            finally
            {
                while (stack.Count > 0) DisposeInitialization(stack.Pop());
            }
        }

        private void DisposeInitialization(IEnumerator routine)
        {
            try { (routine as System.IDisposable)?.Dispose(); }
            catch (System.Exception exception) { initializationError = initializationError ?? exception.Message; }
        }

        private void EndLocal(string error)
        {
            var completed = localCompleted;
            var failed = localFailed;
            localRunning = false;
            localInitialize = null;
            localCompleted = null;
            localFailed = null;
            if (error == null) completed?.Invoke();
            else failed?.Invoke(error);
        }

        private IEnumerator Animate(float from, float to, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                SetOpen(Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, elapsed / Mathf.Max(0.01f, duration))));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            SetOpen(to);
        }

        protected virtual void SetOpen(float value)
        {
            Openness = value;
            if (leftHinge == null || rightHinge == null) return;
            float aspect = texture != null ? (float)texture.width / texture.height : (float)Mathf.Max(1, Screen.width) / Mathf.Max(1, Screen.height);
            // Fit at the far face too: no uncovered top/side strips when fully closed.
            float halfHeight = 5.3f * Mathf.Tan(doorFieldOfView * Mathf.Deg2Rad * 0.5f) * 1.06f;
            float halfWidth = halfHeight * aspect;
            float width = halfWidth + 0.06f;
            leftDoor.localScale = rightDoor.localScale = new Vector3(width, halfHeight * 2, 0.42f);
            leftDoor.localPosition = new Vector3(width * 0.5f, 0, 0);
            rightDoor.localPosition = new Vector3(-width * 0.5f, 0, 0);
            leftHinge.localPosition = new Vector3(-halfWidth, 0, 0);
            rightHinge.localPosition = new Vector3(halfWidth, 0, 0);
            leftHinge.localRotation = Quaternion.Euler(0, -swingDegrees * value, 0);
            rightHinge.localRotation = Quaternion.Euler(0, swingDegrees * value, 0);
            doorCamera.orthographic = false;
            doorCamera.fieldOfView = doorFieldOfView;
            doorCamera.farClipPlane = 100;
            float travel = opening ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.12f, 1, value)) : 0;
            doorCamera.transform.localPosition = new Vector3(0, Mathf.Sin(travel * Mathf.PI) * 0.06f, Mathf.Lerp(-5, 1.2f, travel));
            doorCamera.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(travel * Mathf.PI * 2) * 0.65f);
            if (frameParts != null)
            {
                frameParts[0].localPosition = new Vector3(-halfWidth - 0.22f, 0, 0.8f);
                frameParts[1].localPosition = new Vector3(halfWidth + 0.22f, 0, 0.8f);
                frameParts[0].localScale = frameParts[1].localScale = new Vector3(0.44f, halfHeight * 2 + 0.8f, 2.4f);
                frameParts[2].localPosition = new Vector3(0, halfHeight + 0.22f, 0.8f);
                frameParts[3].localPosition = new Vector3(0, -halfHeight - 0.22f, 0.8f);
                frameParts[2].localScale = frameParts[3].localScale = new Vector3(halfWidth * 2 + 0.9f, 0.44f, 2.4f);
            }
        }

        protected virtual void BuildPerspectiveRig()
        {
            if (leftDoor == null || rightDoor == null || doorCamera == null) return;
            leftHinge = new GameObject("Left Hinge").transform;
            rightHinge = new GameObject("Right Hinge").transform;
            leftHinge.SetParent(transform, false);
            rightHinge.SetParent(transform, false);
            leftDoor.SetParent(leftHinge, false);
            rightDoor.SetParent(rightHinge, false);
            doorway = new GameObject("Doorway depth frame").transform;
            doorway.SetParent(transform, false);
            frameParts = new Transform[4];
            var surface = leftDoor.GetComponent<Renderer>().sharedMaterial;
            for (int i = 0; i < frameParts.Length; i++)
            {
                var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                part.name = "Doorway reveal " + i;
                part.layer = gameObject.layer;
                part.transform.SetParent(doorway, false);
                part.GetComponent<Renderer>().sharedMaterial = surface;
                Destroy(part.GetComponent<Collider>());
                frameParts[i] = part.transform;
            }
        }

        private void EnsureTexture()
        {
            int width = Mathf.Max(1, Screen.width), height = Mathf.Max(1, Screen.height);
            if (texture != null && texture.width == width && texture.height == height) return;
            doorCamera.targetTexture = null;
            if (texture != null) { texture.Release(); Destroy(texture); }
            texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = "Shelter Door Overlay" };
            texture.Create();
            doorCamera.targetTexture = texture;
            doorCamera.aspect = (float)width / height;
            ScreenTransitionFeature.Overlay = texture;
        }

        protected virtual void LateUpdate()
        {
            if (!IsRunning) return;
            HideSceneCanvases();
            EnsureTexture();
            SetOpen(Openness);
        }

        private void HideSceneCanvases()
        {
            if (!hideAllSceneCanvases) return;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.InstanceID))
            {
                if (canvas.transform.IsChildOf(transform)) continue;
                if (!sceneCanvasStates.ContainsKey(canvas)) sceneCanvasStates.Add(canvas, canvas.enabled);
                canvas.enabled = false;
            }
        }

        private void SetInputLocked(bool locked)
        {
            if (locked)
            {
                inputStates = new bool[inputBehaviours?.Length ?? 0];
                canvasStates = new bool[hideCanvases?.Length ?? 0];
                for (int i = 0; i < inputStates.Length; i++)
                    if (inputBehaviours[i] != null && inputBehaviours[i] != this)
                    { inputStates[i] = inputBehaviours[i].enabled; inputBehaviours[i].enabled = false; }
                for (int i = 0; i < canvasStates.Length; i++)
                    if (hideCanvases[i] != null) { canvasStates[i] = hideCanvases[i].enabled; hideCanvases[i].enabled = false; }
            }
            else
            {
                for (int i = 0; i < (inputStates?.Length ?? 0); i++)
                    if (inputBehaviours[i] != null && inputBehaviours[i] != this) inputBehaviours[i].enabled = inputStates[i];
                for (int i = 0; i < (canvasStates?.Length ?? 0); i++)
                    if (hideCanvases[i] != null) hideCanvases[i].enabled = canvasStates[i];
                inputStates = canvasStates = null;
            }
        }

        private void Cleanup()
        {
            ScreenTransitionFeature.Overlay = null;
            ScreenTransitionFeature.ClosedCoverage = 0;
            ScreenTransitionFeature.TargetCamera = null;
            if (doorCamera != null) doorCamera.gameObject.SetActive(false);
            RestoreMask();
            SetInputLocked(false);
            foreach (var entry in sceneCanvasStates)
                if (entry.Key != null) entry.Key.enabled = entry.Value;
            sceneCanvasStates.Clear();
            IsRunning = false;
            if (!sceneScoped) SceneTransition.SetPhase(TransitionPhase.Idle);
        }

        protected virtual void OnDisable()
        {
            if (!sceneScoped && owner != this) return;
            StopAllCoroutines();
            Cleanup();
            if (localRunning) EndLocal("Overlay driver was disabled during transition.");
            else if (!sceneScoped) SceneTransition.NotifyFailed(this, "Transition was interrupted because the driver was disabled.");
        }
        protected virtual void OnDestroy()
        {
            if (!sceneScoped && owner != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Cleanup();
            if (!sceneScoped) SceneTransition.Unregister(this);
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (!sceneScoped) owner = null;
        }

        protected virtual void OnGUI()
        {
            if (!showDemoControls || IsRunning) return;
            GUILayout.BeginArea(new Rect(20, 20, 340, 150), GUI.skin.box);
            GUILayout.Label(PreviewTitle);
            if (GUILayout.Button("Preview: Close > Hold > Open", GUILayout.Height(32))) Preview();
            nextScene = GUILayout.TextField(nextScene ?? "");
            GUI.enabled = !string.IsNullOrWhiteSpace(nextScene);
            if (GUILayout.Button("Load scene", GUILayout.Height(28))) TransitionToScene(nextScene);
            GUI.enabled = true;
            GUILayout.EndArea();
        }
    }
}

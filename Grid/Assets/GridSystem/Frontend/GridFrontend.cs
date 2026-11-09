using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GridTutorial
{
    /// <summary>Place on an always-active root in menu and gameplay scenes.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class GridFrontend : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] TMP_FontAsset font;
        [SerializeField] string title = "ZOMBIE SURVIVE";
        [SerializeField, Min(0)] float logoSeconds = 1.5f;
        [SerializeField] bool showIntro = true;
        Canvas canvas;
        GameObject intro, menu, settings, confirmation, quit;
        TMP_Text status, countdown, fpsText;
        Button startButton, settingsButton, keepButton;
        readonly List<Action> refresh = new List<Action>();
        readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        GridSettingsData draft, beforeDisplay;
        bool opened, pending, paused;
        float previousTimeScale, deadline, logoDeadline, frameTime;
        CursorLockMode oldLock;
        bool oldCursor;
        GameObject oldSelection;
        GridSettings Store => GridSettings.Instance;

        void Start()
        {
            if (!game) game = FindAnyObjectByType<GameManager>();
            Build();
            logoDeadline = Time.unscaledTime + logoSeconds;
            bool ready = showIntro && game && game.State == LoopState.Ready && !game.IsStarting;
            intro.SetActive(ready && logoSeconds > 0);
            menu.SetActive(ready && logoSeconds <= 0);
            if (ready) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (menu.activeSelf) Select(startButton);
        }
        void Update()
        {
            if (!canvas) return;
            bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (intro.activeSelf && (Time.unscaledTime >= logoDeadline || escape ||
                (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) ||
                (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)))
            { intro.SetActive(false); menu.SetActive(true); Select(startButton); escape = false; }
            if (game && (game.State != LoopState.Ready || game.IsStarting))
            { intro.SetActive(false); menu.SetActive(false); }
            if (showIntro && game && game.State == LoopState.Ready && !game.IsStarting && !intro.activeSelf)
                menu.SetActive(!opened && !quit.activeSelf);
            if (escape)
            {
                if (pending) RevertDisplay();
                else if (quit.activeSelf) { quit.SetActive(false); menu.SetActive(true); Select(startButton); }
                else if (opened) Cancel();
                else if (!intro.activeSelf && (!game || !game.IsStarting)) OpenSettings();
            }
            if (pending)
            {
                float left = deadline - Time.realtimeSinceStartup;
                countdown.text = $"Keep these display settings? ({Mathf.CeilToInt(Mathf.Max(0, left))}s)";
                if (left <= 0) RevertDisplay();
            }
            frameTime = Mathf.Lerp(frameTime, Time.unscaledDeltaTime, .08f);
            fpsText.gameObject.SetActive(Store.ShowFps);
            fpsText.text = $"{1f / Mathf.Max(.001f, frameTime):0} FPS";
        }
        void OnDisable()
        {
            if (pending && Store) RevertDisplay();
            if (opened) { if (Store) Store.Restore(); ReleasePause(); opened = false; }
        }
        void OnDestroy() { if (canvas) Destroy(canvas.gameObject); }

        public void OpenSettings()
        {
            if (opened || (game && game.IsStarting)) return;
            opened = true;
            oldSelection = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            oldLock = Cursor.lockState; oldCursor = Cursor.visible;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            previousTimeScale = Time.timeScale;
            paused = game && game.State == LoopState.Playing;
            if (paused) Time.timeScale = 0;
            draft = Store.Saved.Copy();
            Refresh(); settings.SetActive(true); status.text = "Audio previews immediately. Apply to save.";
            menu.SetActive(false);
            Select(settingsButton);
        }
        void Refresh() { foreach (var action in refresh) action(); }
        void Cancel()
        {
            if (pending) RevertDisplay();
            Store.Restore(); Close();
        }
        void ReleasePause()
        {
            if (paused) Time.timeScale = previousTimeScale;
            paused = false;
            Cursor.lockState = oldLock; Cursor.visible = oldCursor;
        }
        void Close()
        {
            settings.SetActive(false); confirmation.SetActive(false);
            opened = false; ReleasePause();
            menu.SetActive(showIntro && game && game.State == LoopState.Ready && !game.IsStarting);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(oldSelection);
        }
        void Apply()
        {
            if (pending) return;
            GridSettings.Sanitize(draft);
            beforeDisplay = Store.Saved.Copy();
            // Restore the actual current window, even after an OS Alt+Enter change.
            beforeDisplay.width = Screen.width; beforeDisplay.height = Screen.height;
            int actualMode = Array.IndexOf(GridSettings.Modes, Screen.fullScreenMode);
            beforeDisplay.mode = actualMode < 0 ? 1 : actualMode;
            bool changed = beforeDisplay.width != draft.width || beforeDisplay.height != draft.height || beforeDisplay.mode != draft.mode;
            Store.Apply(draft, changed);
            if (!changed) { Store.Commit(draft); Close(); return; }
            pending = true; deadline = Time.realtimeSinceStartup + 15;
            settings.GetComponent<CanvasGroup>().interactable = false;
            confirmation.SetActive(true); Select(keepButton);
        }
        void KeepDisplay()
        {
            if (!pending) return;
            // SetResolution is asynchronous. Save the observed result after user confirmation.
            draft.width = Screen.width; draft.height = Screen.height;
            int mode = Array.IndexOf(GridSettings.Modes, Screen.fullScreenMode);
            if (mode >= 0) draft.mode = mode;
            pending = false; settings.GetComponent<CanvasGroup>().interactable = true; Store.Commit(draft); Close();
        }
        void RevertDisplay()
        {
            if (!pending) return;
            pending = false; settings.GetComponent<CanvasGroup>().interactable = true; Store.Apply(beforeDisplay, true); Store.Restore();
            draft = Store.Saved.Copy(); Refresh();
            confirmation.SetActive(false); status.text = "Display reverted. No changes saved.";
            Select(settingsButton);
        }
        void StartGame()
        {
            if (!game || opened || game.IsStarting) return;
            game.GameLoop();
        }
        void ExitGame()
        {
#if UNITY_EDITOR
            Debug.Log("Quit requested. Application.Quit runs in a standalone build.");
            quit.SetActive(false); menu.SetActive(true); Select(startButton);
#else
            Application.Quit();
#endif
        }
        static void Select(Selectable item)
        { if (EventSystem.current && item) EventSystem.current.SetSelectedGameObject(item.gameObject); }

        void Build()
        {
            if (!EventSystem.current)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var root = new GameObject("Grid Frontend Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 900); scaler.matchWidthOrHeight = .5f;
            intro = Panel("Intro", root.transform); Label(intro.transform, title + "\n\nA GRID SURVIVAL GAME\n\nEnter / Click to skip", 0, 0, 980, 300, 40);
            menu = Panel("Main Menu", root.transform);
            Label(menu.transform, title, 0, 210, 1000, 100, 48);
            Label(menu.transform, "SURVIVE. BUILD. ESCAPE.", 0, 130, 800, 50, 22);
            startButton = Button(menu.transform, "START GAME", 0, 30, StartGame);
            Button(menu.transform, "SETTINGS", 0, -45, OpenSettings);
            Button(menu.transform, "QUIT", 0, -120, () => { quit.SetActive(true); menu.SetActive(false); Select(quit.GetComponentInChildren<Button>()); });
            Label(menu.transform, "v" + Application.version, 0, -245, 600, 40, 18);
            BuildSettings(root.transform);
            quit = Panel("Quit Confirmation", root.transform);
            Label(quit.transform, "Quit the game?", 0, 80, 800, 80, 36);
            Button(quit.transform, "CANCEL", -210, -40, () => { quit.SetActive(false); menu.SetActive(true); Select(startButton); });
            Button(quit.transform, "QUIT", 210, -40, ExitGame); quit.SetActive(false);
            fpsText = Label(root.transform, "", -510, 425, 240, 45, 20);
            fpsText.raycastTarget = false;
            intro.SetActive(false); menu.SetActive(false); settings.SetActive(false); confirmation.SetActive(false);
        }
        void BuildSettings(Transform root)
        {
            settings = Panel("Settings", root); settings.AddComponent<CanvasGroup>();
            Label(settings.transform, "SETTINGS", 0, 400, 700, 60, 36);
            Volume("MASTER", 320, () => draft.master, v => draft.master = v);
            Volume("MUSIC", 265, () => draft.music, v => draft.music = v);
            Volume("SFX", 210, () => draft.sfx, v => draft.sfx = v);
            Volume("UI", 155, () => draft.ui, v => draft.ui = v);
            BuildResolutions();
            settingsButton = Cycle("Resolution", 90,
                () => draft.width + " x " + draft.height,
                () => {
                    int i = resolutions.FindIndex(r => r.x == draft.width && r.y == draft.height);
                    var r = resolutions[(i + 1) % resolutions.Count]; draft.width = r.x; draft.height = r.y;
                });
            Cycle("Window Mode", 35, () => new[] { "Windowed", "Borderless", "Exclusive Fullscreen" }[draft.mode], () => draft.mode = (draft.mode + 1) % 3);
            Cycle("Quality", -20, () => QualitySettings.names[draft.quality], () => draft.quality = (draft.quality + 1) % QualitySettings.names.Length);
            Cycle("VSync", -75, () => draft.vSync ? "ON (FPS limit overridden)" : "OFF", () => draft.vSync = !draft.vSync);
            int[] rates = { 30, 60, 120, 144, -1 };
            Cycle("FPS Limit", -130, () => draft.fps < 0 ? "Unlimited" : draft.fps.ToString(), () => draft.fps = rates[(Array.IndexOf(rates, draft.fps) + 1) % rates.Length]);
            Cycle("Show FPS", -185, () => draft.showFps ? "ON" : "OFF", () => draft.showFps = !draft.showFps);
            Cycle("Mute Unfocused", -240, () => draft.muteUnfocused ? "ON" : "OFF", () => { draft.muteUnfocused = !draft.muteUnfocused; Store.PreviewAudio(draft); });
            status = Label(settings.transform, "", 0, -305, 1160, 55, 20);
            Button(settings.transform, "DEFAULTS", -400, -380, () => { draft = GridSettings.Defaults(); Refresh(); Store.PreviewAudio(draft); });
            Button(settings.transform, "CANCEL", 0, -380, Cancel);
            Button(settings.transform, "APPLY", 400, -380, Apply);
            confirmation = Panel("Display Confirmation", root);
            countdown = Label(confirmation.transform, "", 0, 90, 1100, 100, 32);
            keepButton = Button(confirmation.transform, "KEEP", -210, -40, KeepDisplay);
            Button(confirmation.transform, "REVERT", 210, -40, RevertDisplay);
        }
        void BuildResolutions()
        {
            var set = new HashSet<Vector2Int>();
            foreach (var r in Screen.resolutions) if (r.width >= 640 && r.height >= 480) set.Add(new Vector2Int(r.width, r.height));
            set.Add(new Vector2Int(Mathf.Max(640, Screen.width), Mathf.Max(480, Screen.height)));
            var saved = Store.Saved; set.Add(new Vector2Int(saved.width, saved.height));
            if (Screen.currentResolution.width >= 1280 && Screen.currentResolution.height >= 720) set.Add(new Vector2Int(1280, 720));
            resolutions.AddRange(set); resolutions.Sort((a,b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
        }
        void Volume(string name, float y, Func<float> get, Action<float> set)
        {
            var label = Label(settings.transform, name, -360, y, 350, 45, 22);
            var track = Box(name + " Slider", settings.transform, 170, y, 560, 30, new Color(.12f,.17f,.19f));
            var fill = Box("Fill", track.transform, 0, 0, 560, 30, new Color(.2f,.6f,.5f));
            fill.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);
            var handle = Box("Handle", track.transform, 0, 0, 26, 42, Color.white);
            var slider = track.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
            slider.fillRect = fill.GetComponent<RectTransform>(); slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>(); slider.direction = Slider.Direction.LeftToRight;
            slider.onValueChanged.AddListener(v => { if (draft == null) return; set(v); label.text = name + "  " + Mathf.RoundToInt(v * 100) + "%"; Store.PreviewAudio(draft); });
            refresh.Add(() => { slider.SetValueWithoutNotify(get()); label.text = name + "  " + Mathf.RoundToInt(get() * 100) + "%"; });
        }
        Button Cycle(string name, float y, Func<string> get, Action next)
        {
            Label(settings.transform, name, -360, y, 350, 45, 22);
            Button button = null;
            button = Button(settings.transform, "", 170, y, () => { next(); Refresh(); });
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(560, 46);
            refresh.Add(() => button.GetComponentInChildren<TMP_Text>().text = get());
            return button;
        }
        GameObject Panel(string name, Transform parent)
        {
            var go = Box(name, parent, 0, 0, 0, 0, new Color(.025f,.04f,.055f,.985f));
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            return go;
        }
        GameObject Box(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
            rect.anchoredPosition = new Vector2(x,y); rect.sizeDelta = new Vector2(w,h);
            go.GetComponent<Image>().color = color; return go;
        }
        TMP_Text Label(Transform parent, string text, float x, float y, float w, float h, float size)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
            rect.anchoredPosition = new Vector2(x,y); rect.sizeDelta = new Vector2(w,h);
            var label = go.GetComponent<TextMeshProUGUI>(); if (font) label.font = font;
            label.text = text; label.fontSize = size; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return label;
        }
        Button Button(Transform parent, string text, float x, float y, UnityEngine.Events.UnityAction action)
        {
            var go = Box(text, parent, x,y,360,58,new Color(.12f,.24f,.25f));
            var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>(); button.onClick.AddListener(action);
            var colors = button.colors; colors.highlightedColor = new Color(.55f,1,.8f); colors.selectedColor = colors.highlightedColor; button.colors = colors;
            Label(go.transform,text,0,0,350,50,22); return button;
        }
    }
}




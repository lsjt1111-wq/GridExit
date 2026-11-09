using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace GridTutorial
{
    [Serializable]
    public sealed class GridSettingsData
    {
        public float master = 1, music = .8f, sfx = .8f, ui = .8f;
        public int width = 1280, height = 720, mode = 1, quality, fps = 60;
        public bool vSync = true, showFps, muteUnfocused = true;
        public GridSettingsData Copy() => (GridSettingsData)MemberwiseClone();
    }

    // One settings owner for the entire application, including additive transition scenes.
    public sealed class GridSettings : MonoBehaviour
    {
        const string Key = "GridTutorial.Settings.v1";
        public static GridSettings Instance { get; private set; }
        public GridSettingsData Saved { get; private set; }
        public AudioMixer Mixer { get; private set; }
        public bool MutedByFocus { get; private set; }
        GridSettingsData active;
        bool started;
        public static readonly FullScreenMode[] Modes = {
            FullScreenMode.Windowed, FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (!Instance) new GameObject("Grid Settings (Persistent)").AddComponent<GridSettings>();
        }

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Mixer = Resources.Load<AudioMixer>("GridAudioMixer");
            Saved = Defaults();
            try
            {
                if (PlayerPrefs.HasKey(Key))
                    JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(Key), Saved);
            }
            catch (Exception e) { Debug.LogWarning("Settings reset: " + e.Message); }
            Sanitize(Saved);
            active = Saved.Copy();
        }

        void Start()
        {
            started = true;
            Apply(active, true);
            if (!Mixer) Debug.LogWarning("Create Resources/GridAudioMixer.mixer; see INTRO_SETTINGS_02_AUDIO.md.");
        }

        public static GridSettingsData Defaults() => new GridSettingsData {
            width = 1280, height = 720,
            quality = Mathf.Min(2, Mathf.Max(0, QualitySettings.names.Length - 1))
        };

        public static void Sanitize(GridSettingsData d)
        {
            d.master = Volume(d.master); d.music = Volume(d.music);
            d.sfx = Volume(d.sfx); d.ui = Volume(d.ui);
            d.width = Mathf.Clamp(d.width, 640, Mathf.Max(640, Screen.currentResolution.width));
            d.height = Mathf.Clamp(d.height, 480, Mathf.Max(480, Screen.currentResolution.height));
            d.mode = Mathf.Clamp(d.mode, 0, Modes.Length - 1);
            d.quality = Mathf.Clamp(d.quality, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            if (d.fps != 30 && d.fps != 60 && d.fps != 120 && d.fps != 144 && d.fps != -1) d.fps = 60;
        }
        static float Volume(float v) => float.IsNaN(v) || float.IsInfinity(v) ? 1 : Mathf.Clamp01(v);
        public static float Decibels(float value) => value <= .0001f ? -80 : Mathf.Log10(value) * 20;

        public void PreviewAudio(GridSettingsData d)
        {
            active.master = d.master; active.music = d.music; active.sfx = d.sfx; active.ui = d.ui;
            active.muteUnfocused = d.muteUnfocused;
            if (started) ApplyAudio();
        }
        void ApplyAudio()
        {
            MutedByFocus = active.muteUnfocused && !Application.isFocused;
            Set("MasterVolume", MutedByFocus ? 0 : active.master);
            Set("MusicVolume", active.music); Set("SfxVolume", active.sfx); Set("UIVolume", active.ui);
        }
        void Set(string parameter, float value)
        {
            if (Mixer && !Mixer.SetFloat(parameter, Decibels(value)))
                Debug.LogWarning("AudioMixer exposed parameter missing: " + parameter);
        }
        void OnApplicationFocus(bool focused) { if (started) ApplyAudio(); }

        public void Apply(GridSettingsData d, bool display)
        {
            Sanitize(d);
            active = d.Copy();
            QualitySettings.SetQualityLevel(d.quality, true);
            QualitySettings.vSyncCount = d.vSync ? 1 : 0;
            Application.targetFrameRate = d.fps;
            // Continue UI/countdown while focus is lost. Audio is muted via the master mixer.
            Application.runInBackground = true;
            if (started) ApplyAudio();
            if (display) Screen.SetResolution(d.width, d.height, Modes[d.mode]);
        }
        public void Commit(GridSettingsData d)
        {
            Sanitize(d);
            Saved = d.Copy();
            active = Saved.Copy();
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Saved));
            PlayerPrefs.Save();
        }
        public void Restore() => Apply(Saved.Copy(), false);
        public bool ShowFps => active != null && active.showFps;
        public void Route(AudioSource source, string group)
        {
            if (!source || !Mixer) return;
            foreach (var match in Mixer.FindMatchingGroups(group))
                if (match.name == group) { source.outputAudioMixerGroup = match; return; }
            Debug.LogWarning("AudioMixer group missing: " + group);
        }
    }
}


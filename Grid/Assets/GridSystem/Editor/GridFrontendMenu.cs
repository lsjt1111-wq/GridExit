using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

namespace GridTutorial.Editor
{
    public static class GridFrontendMenu
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        const string MixerPath = "Assets/GridSystem/Resources/GridAudioMixer.mixer";

        [MenuItem("Tools/Grid Tutorial/Intro Settings/Add Frontend To Current Scene")]
        static void AddFrontend()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var existing = UnityEngine.Object.FindAnyObjectByType<GridFrontend>(FindObjectsInactive.Include);
            if (existing) { Selection.activeGameObject = existing.gameObject; return; }
            var root = new GameObject("Grid Frontend");
            Undo.RegisterCreatedObjectUndo(root, "Add Grid Frontend");
            var frontend = Undo.AddComponent<GridFrontend>(root);
            var serialized = new SerializedObject(frontend);
            serialized.FindProperty("game").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            serialized.ApplyModifiedProperties();
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
        }

        [MenuItem("Tools/Grid Tutorial/Intro Settings/Create Audio Mixer")]
        static void CreateMixer()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (existing) { Selection.activeObject = existing; return; }
            if (!AssetDatabase.IsValidFolder("Assets/GridSystem/Resources")) AssetDatabase.CreateFolder("Assets/GridSystem", "Resources");
            // Unity exposes runtime mixing publicly, but authoring is an Editor-internal API.
            // Keep all reflection here; runtime code uses only supported AudioMixer APIs.
            var controllerType = typeof(AudioImporter).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
            if (controllerType == null)
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                { controllerType = assembly.GetType("UnityEditor.Audio.AudioMixerController"); if (controllerType != null) break; }
            try
            {
                if (controllerType == null) throw new MissingMemberException("AudioMixerController");
                var mixer = (AudioMixer)controllerType.GetMethod("CreateMixerControllerAtPath", Flags).Invoke(null, new object[] { MixerPath });
                var master = controllerType.GetProperty("masterGroup", Flags).GetValue(mixer);
                var groups = new object[4]; groups[0] = master;
                string[] names = { "Master", "Music", "SFX", "UI" };
                for (int i = 1; i < groups.Length; i++)
                {
                    groups[i] = controllerType.GetMethod("CreateNewGroup", Flags).Invoke(mixer, new object[] { names[i], false });
                    controllerType.GetMethod("AddChildToParent", Flags).Invoke(mixer, new[] { groups[i], master });
                }
                var property = controllerType.GetProperty("exposedParameters", Flags);
                Type element = property.PropertyType.GetElementType();
                var exposed = Array.CreateInstance(element, 4);
                string[] parameters = { "MasterVolume", "MusicVolume", "SfxVolume", "UIVolume" };
                for (int i = 0; i < 4; i++)
                {
                    object entry = Activator.CreateInstance(element);
                    element.GetField("name", Flags).SetValue(entry, parameters[i]);
                    object guid = groups[i].GetType().GetMethod("GetGUIDForVolume", Flags).Invoke(groups[i], null);
                    element.GetField("guid", Flags).SetValue(entry, guid);
                    exposed.SetValue(entry, i);
                }
                property.SetValue(mixer, exposed);
                EditorUtility.SetDirty(mixer); AssetDatabase.SaveAssets();
                Selection.activeObject = mixer;
                Debug.Log("GridAudioMixer created: Master / Music / SFX / UI. See INTRO_SETTINGS_02_AUDIO.md.");
            }
            catch (Exception e)
            {
                Debug.LogError("Mixer authoring API differs in this Editor. Use the manual steps in INTRO_SETTINGS_02_AUDIO.md. " + e.GetBaseException().Message);
                // Keep a partially created asset available for manual completion; do not overwrite user edits.
            }
        }
    }
}


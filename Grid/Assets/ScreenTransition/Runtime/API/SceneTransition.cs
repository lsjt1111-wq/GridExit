using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScreenTransitions
{
    public enum TransitionPhase { Idle, Closing, Covered, Loading, Opening, Unloading }

    public interface ISceneTransitionDriver
    {
        bool IsRunning { get; }
        bool TryLoadScene(string sceneName);
    }

    public interface IInPlaceTransitionDriver
    {
        bool TryRunInPlace(Func<IEnumerator> initialize, Action completed, Action<string> failed);
    }

    /// <summary>Scene-independent entry point. Lazily creates the persistent driver prefab.</summary>
    public static class SceneTransition
    {
        public const string ResourcePath = "ScreenTransition/ShelterDoorTransition";
        public const string AdditiveScenePath = "Assets/ScreenTransition/Scenes/ShelterDoorOverlay.unity";
        private static bool inPlaceRequest;
        private static int requestVersion;
        private static ISceneTransitionDriver driver;
        private static string destination;
        private static Action<Scene> completion;
        public static string ArrivalTag { get; private set; }
        public static bool IsBusy => inPlaceRequest || !string.IsNullOrEmpty(destination) || (DriverAlive && driver.IsRunning);
        public static TransitionPhase Phase { get; private set; }
        public static event Action<Scene> Completed;
        public static event Action<string> Failed;

        private static bool DriverAlive => driver is MonoBehaviour behaviour && behaviour != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            driver = null;
            inPlaceRequest = false;
            requestVersion = 0;
            destination = null;
            completion = null;
            ArrivalTag = null;
            Phase = TransitionPhase.Idle;
            Completed = null;
            Failed = null;
        }

        public static bool IsArrivingAt(Scene scene, string arrivalTag = null)
        {
            return !string.IsNullOrEmpty(destination) &&
                (arrivalTag == null || string.Equals(ArrivalTag, arrivalTag, StringComparison.Ordinal)) &&
                (string.Equals(scene.path, destination, StringComparison.Ordinal) ||
                 string.Equals(scene.name, destination, StringComparison.Ordinal));
        }

        /// <returns>False for busy, invalid scene, missing prefab, or missing source camera. No direct-load fallback.</returns>
        public static bool TryLoadScene(string sceneName, Action<Scene> afterReveal = null, string arrivalTag = null)
        {
            if (IsBusy) return false;
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("SceneTransition: add the scene to the active Build Profile Scene List: " + sceneName);
                return false;
            }
            if (!DriverAlive)
            {
                var prefab = Resources.Load<GameObject>(ResourcePath);
                if (prefab == null)
                {
                    Debug.LogError("SceneTransition: shared prefab missing. Run Tools > Screen Transition > Install Shared API.");
                    return false;
                }
                UnityEngine.Object.Instantiate(prefab);
                if (!DriverAlive)
                {
                    Debug.LogError("SceneTransition: prefab must contain an active ShelterDoorTransition driver.");
                    return false;
                }
            }
            destination = sceneName;
            requestVersion++;
            completion = afterReveal;
            ArrivalTag = arrivalTag;
            Phase = TransitionPhase.Closing;
            if (driver.TryLoadScene(sceneName)) return true;
            destination = null;
            completion = null;
            ArrivalTag = null;
            Phase = TransitionPhase.Idle;
            return false;
        }

        /// <summary>Load only the overlay scene additively. Initialise the existing scene while covered.</summary>
        public static bool TryRunInPlace(Action initialize, Action afterReveal = null, string overlayScene = AdditiveScenePath)
        {
            return TryRunInPlaceRoutine(() => InvokeAction(initialize), afterReveal, overlayScene);
        }

        private static IEnumerator InvokeAction(Action action)
        {
            action?.Invoke();
            yield break;
        }

        /// <summary>Coroutine initialisation may span frames; the doors stay closed until it completes.</summary>
        public static bool TryRunInPlaceRoutine(Func<IEnumerator> initialize, Action afterReveal = null, string overlayScene = AdditiveScenePath)
        {
            if (IsBusy) return false;
            if (string.IsNullOrWhiteSpace(overlayScene) || !Application.CanStreamedLevelBeLoaded(overlayScene))
            {
                Debug.LogError("SceneTransition: install the additive overlay and add it to the active Build Profile: " + overlayScene);
                return false;
            }
            if (SceneManager.GetSceneByPath(overlayScene).isLoaded || SceneManager.GetSceneByName(overlayScene).isLoaded)
            {
                Debug.LogError("SceneTransition: the overlay scene is already loaded. It must be owned by this request.");
                return false;
            }
            inPlaceRequest = true;
            destination = null;
            ArrivalTag = null;
            completion = scene => afterReveal?.Invoke();
            Phase = TransitionPhase.Loading;
            int version = ++requestVersion;
            var host = new GameObject("Additive Transition Request");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<AdditiveTransitionRunner>().Begin(version, SceneManager.GetActiveScene(), overlayScene, initialize);
            return true;
        }

        internal static void FinishInPlace(int version, Scene source, string error)
        {
            if (!inPlaceRequest || version != requestVersion) return;
            inPlaceRequest = false;
            if (error != null) { Fail(error); return; }
            Complete(source);
        }

        // Driver lifecycle hooks. Application code normally uses TryLoadScene / Completed.
        public static void Register(ISceneTransitionDriver value) => driver = value;
        public static void SetPhase(TransitionPhase phase) => Phase = phase;

        public static void NotifyCompleted(ISceneTransitionDriver sender, Scene scene)
        {
            if (inPlaceRequest || !ReferenceEquals(driver, sender) || string.IsNullOrEmpty(destination)) return;
            Complete(scene);
        }

        private static void Complete(Scene scene)
        {
            var callback = completion;
            destination = null;
            completion = null;
            ArrivalTag = null;
            Phase = TransitionPhase.Idle;
            // Invoke subscribers independently so one consumer cannot prevent another game from starting.
            if (Completed != null)
                foreach (Action<Scene> listener in Completed.GetInvocationList())
                    try { listener(scene); } catch (Exception exception) { Debug.LogException(exception); }
            try { callback?.Invoke(scene); } catch (Exception exception) { Debug.LogException(exception); }
        }

        public static void NotifyFailed(ISceneTransitionDriver sender, string reason)
        {
            if (inPlaceRequest || !ReferenceEquals(driver, sender) || string.IsNullOrEmpty(destination)) return;
            Fail(reason);
        }

        private static void Fail(string reason)
        {
            destination = null;
            completion = null;
            ArrivalTag = null;
            Phase = TransitionPhase.Idle;
            Debug.LogError("SceneTransition: " + reason);
            if (Failed != null)
                foreach (Action<string> listener in Failed.GetInvocationList())
                    try { listener(reason); } catch (Exception exception) { Debug.LogException(exception); }
        }

        public static void Unregister(ISceneTransitionDriver sender)
        {
            if (!ReferenceEquals(driver, sender)) return;
            NotifyFailed(sender, "Transition driver was destroyed.");
            driver = null;
        }
    }
}

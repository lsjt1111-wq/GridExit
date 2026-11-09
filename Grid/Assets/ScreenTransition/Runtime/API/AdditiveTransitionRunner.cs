using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScreenTransitions
{
    // Lives outside the overlay so it can wait for that scene to unload before reporting completion.
    public sealed class AdditiveTransitionRunner : MonoBehaviour
    {
        private bool finished;
        private int version;
        private Scene source;

        public void Begin(int requestVersion, Scene sourceScene, string overlayPath, Func<IEnumerator> initialize)
        {
            version = requestVersion;
            source = sourceScene;
            StartCoroutine(Run(overlayPath, initialize));
        }

        private IEnumerator Run(string overlayPath, Func<IEnumerator> initialize)
        {
            string error = null;
            AsyncOperation load = null;
            try { load = SceneManager.LoadSceneAsync(overlayPath, LoadSceneMode.Additive); }
            catch (Exception exception) { error = exception.Message; }
            if (load == null && error == null) error = "Overlay loading returned no operation.";
            if (load != null) while (!load.isDone) yield return null;

            var overlay = SceneManager.GetSceneByPath(overlayPath);
            if (!overlay.IsValid()) overlay = SceneManager.GetSceneByName(overlayPath);
            if (error == null && (!overlay.IsValid() || !overlay.isLoaded)) error = "Overlay scene did not load.";

            IInPlaceTransitionDriver localDriver = null;
            if (error == null)
            {
                foreach (var root in overlay.GetRootGameObjects())
                    foreach (var component in root.GetComponentsInChildren<MonoBehaviour>())
                        if (component is IInPlaceTransitionDriver candidate) { localDriver = candidate; break; }
                if (localDriver == null) error = "Overlay scene requires an active scene-scoped transition driver.";
            }

            if (error == null)
            {
                bool revealed = false;
                bool accepted = false;
                try { accepted = localDriver.TryRunInPlace(initialize, () => revealed = true, reason => error = reason); }
                catch (Exception exception) { error = exception.Message; }
                if (!accepted && error == null) error = "Overlay driver rejected the request.";
                while (!revealed && error == null)
                {
                    if (!source.IsValid() || !source.isLoaded) { error = "Source scene was unloaded during in-place transition."; break; }
                    if (localDriver is MonoBehaviour behaviour && behaviour == null) { error = "Overlay driver was destroyed."; break; }
                    yield return null;
                }
            }

            // Never change or unload the source scene. Keep IsBusy until overlay disposal completes.
            if (overlay.IsValid() && overlay.isLoaded)
            {
                SceneTransition.SetPhase(TransitionPhase.Unloading);
                AsyncOperation unload = null;
                try { unload = SceneManager.UnloadSceneAsync(overlay); }
                catch (Exception exception) { error = error ?? exception.Message; }
                if (unload == null) error = error ?? "Overlay unloading returned no operation.";
                else while (!unload.isDone) yield return null;
            }
            finished = true;
            SceneTransition.FinishInPlace(version, source, error);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (!finished) SceneTransition.FinishInPlace(version, source, "Additive transition runner was interrupted.");
        }
    }
}

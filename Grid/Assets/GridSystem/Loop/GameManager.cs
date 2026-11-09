using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using ScreenTransitions;
using UnityEngine.Events;

namespace GridTutorial
{
    public enum LoopState { Ready, Playing, Win, Lose }
    public enum GridStartTransitionMode { LoadScene, AdditiveInPlace }
    public sealed partial class GameManager : MonoBehaviour
    {
        [SerializeField] GridOccupant player;
        [SerializeField] GridPathPreview pathPreview;
        [SerializeField] GridMonsterAttacker[] monsters = Array.Empty<GridMonsterAttacker>();
        [SerializeField] GridBuildController buildController;
        [SerializeField] GameObject startPanel, hudPanel;
        [Header("Start button scene transition")]
        [SerializeField] bool useSceneTransition = true;
        [SerializeField] GridStartTransitionMode transitionMode;
        [Tooltip("Additive transition stage scene. Leave empty to use the default shelter doors.")]
        [SerializeField] string inPlaceOverlayScene = SceneTransition.AdditiveScenePath;
        [Tooltip("Invoked while fully covered in AdditiveInPlace mode. Connect scene-local setup here.")]
        [SerializeField] UnityEvent initializeInPlace = new UnityEvent();
        [Tooltip("Empty: reload this saved scene, then start playing after the door has opened.")]
        [SerializeField] string gameplayScene;
        bool startRequested;
        bool awaitingArrival;
        public LoopState State { get; private set; } = LoopState.Ready;
        public event Action<LoopState> StateChanged;
        public bool IsStarting => startRequested || awaitingArrival;

        void OnEnable()
        {
            SceneTransition.Completed += OnTransitionCompleted;
            SceneTransition.Failed += OnTransitionFailed;
        }
        void OnDisable()
        {
            SceneTransition.Completed -= OnTransitionCompleted;
            SceneTransition.Failed -= OnTransitionFailed;
        }

        void Start()
        {
            SetGameplay(false);
            awaitingArrival = SceneTransition.IsArrivingAt(gameObject.scene, "Grid.StartGame");
            startPanel.SetActive(!awaitingArrival);
            hudPanel.SetActive(false);
        }
        public void GameLoop()
        {
            if (State != LoopState.Ready || startRequested || awaitingArrival) return;
            if (!useSceneTransition) { StartGameplay(); return; }
            if (transitionMode == GridStartTransitionMode.AdditiveInPlace)
            {
                startRequested = SceneTransition.TryRunInPlace(InitializeInPlace, () =>
                {
                    if (this == null) return;
                    startRequested = false;
                    StartGameplay();
                }, overlayScene: string.IsNullOrWhiteSpace(inPlaceOverlayScene) ? SceneTransition.AdditiveScenePath : inPlaceOverlayScene);
                return;
            }
            string destination = string.IsNullOrWhiteSpace(gameplayScene) ? gameObject.scene.path : gameplayScene;
            startRequested = SceneTransition.TryLoadScene(destination, arrivalTag: "Grid.StartGame");
        }
        void InitializeInPlace()
        {
            SetGameplay(false);
            startPanel.SetActive(false);
            hudPanel.SetActive(false);
            Remaining = timeLimit;
            Elapsed = 0;
            ResultReason = null;
            FinalScore = 0;
            initializeInPlace.Invoke();
        }
        void OnTransitionCompleted(Scene scene)
        {
            if (!awaitingArrival || scene != gameObject.scene) return;
            awaitingArrival = false;
            StartGameplay();
        }
        void OnTransitionFailed(string reason)
        {
            if (State != LoopState.Ready) return;
            startRequested = awaitingArrival = false;
            startPanel.SetActive(true);
        }
        void StartGameplay()
        {
            if (State != LoopState.Ready) return;
            startPanel.SetActive(false);
            hudPanel.SetActive(true);
            SetGameplay(true);
            ChangeState(LoopState.Playing);
        }
        void ChangeState(LoopState value)
        {
            State = value;
            StateChanged?.Invoke(value);
        }
        void SetGameplay(bool active)
        {
            pathPreview.enabled = active;
            player.GetComponent<GridMover>().enabled = active;
            if (buildController)
            {
                if (!active) buildController.CancelBuild();
                buildController.enabled = active;
            }
            foreach (var monster in monsters)
            {
                if (!monster) continue;
                monster.enabled = active;
                monster.GetComponent<GridMover>().enabled = active;
            }
        }
        public void ReturnToMenu()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}


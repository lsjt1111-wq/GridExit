using UnityEngine;
using UnityEngine.Events;

namespace ScreenTransitions
{
    /// <summary>Attach anywhere and connect LoadScene() to Button.onClick.</summary>
    public sealed class SceneTransitionButton : MonoBehaviour
    {
        [Tooltip("Scene name or full Assets/.../*.unity path registered in the active Build Profile.")]
        [SerializeField] private string destinationScene;
        public UnityEvent onRequestAccepted = new UnityEvent();
        public UnityEvent onRequestRejected = new UnityEvent();

        public void LoadScene()
        {
            if (SceneTransition.TryLoadScene(destinationScene)) onRequestAccepted.Invoke();
            else onRequestRejected.Invoke();
        }
    }
}

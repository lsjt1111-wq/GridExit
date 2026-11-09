using UnityEngine;
using UnityEngine.Events;

namespace GridTutorial.Cameras
{
    public sealed class InspectableTarget : MonoBehaviour
    {
        [SerializeField] string displayName = "Clue";
        [SerializeField, TextArea(3, 10)] string description = "Write your observation here.";
        [SerializeField] UnityEvent onFirstInspected = new UnityEvent();
        public string DisplayName => displayName;
        public string Description => description;
        public bool Inspected { get; private set; }
        public void Inspect()
        {
            if (Inspected) return;
            Inspected = true; onFirstInspected.Invoke();
        }
    }
}

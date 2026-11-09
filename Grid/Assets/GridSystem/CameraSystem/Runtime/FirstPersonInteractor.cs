using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GridTutorial.Cameras
{
    public sealed class FirstPersonInteractor : MonoBehaviour
    {
        [SerializeField] ExplorationCameraController controller;
        [SerializeField, Min(.1f)] float distance = 2.5f;
        [Tooltip("Include walls AND clues. Exclude the player's own collider layer.")]
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] TMP_Text prompt, description;
        [SerializeField] GameObject panel, reticle;
        InspectableTarget target;
        void Start() { if (panel) panel.SetActive(false); }
        void LateUpdate()
        {
            if (!controller) return;
            if (!controller.IsPlaying || controller.IsTopView) Close();
            if (controller.PanelOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
            target = null;
            if (controller.CanObserve)
            {
                var ray = controller.ViewCamera.ViewportPointToRay(new Vector3(.5f, .5f));
                if (Physics.Raycast(ray, out var hit, distance, hitMask, QueryTriggerInteraction.Ignore))
                {
                    var candidate = hit.collider.GetComponentInParent<InspectableTarget>();
                    if (candidate && candidate.isActiveAndEnabled) target = candidate;
                }
            }
            if (reticle) reticle.SetActive(controller.CanObserve);
            if (prompt) prompt.text = target ? $"E: {target.DisplayName}" : "";
            if (target && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                // Panel is optional: without one, print the result and keep controls usable.
                var selected = target;
                selected.Inspect();
                if (description) description.text = selected.DisplayName + "\n\n" + selected.Description;
                if (panel && controller.IsPlaying) { panel.SetActive(true); controller.SetPanelOpen(true); }
                else Debug.Log(selected.Description, selected);
            }
        }
        public void Close()
        {
            if (panel) panel.SetActive(false);
            if (controller && controller.PanelOpen) controller.SetPanelOpen(false);
        }
        void OnDisable() { Close(); if (prompt) prompt.text = ""; if (reticle) reticle.SetActive(false); }
    }
}

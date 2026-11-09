using UnityEngine;

namespace ScreenTransitions
{
    /// <summary>Reuse the transition lifecycle, but animate the imported chest's lid BONE.</summary>
    public sealed class TreasureChestTransition : ShelterDoorTransition
    {
        [Header("TreasureBox rig")]
        public Transform treasureBox;
        public Transform lidHinge;
        [Tooltip("TreasureChest01 ships with the lid standing open. B-lid X=90 closes this model.")]
        public Vector3 closedLidEuler = new Vector3(90, 0, 0);
        public Vector3 openLidEuler = new Vector3(-15, 0, 0);
        [Header("Camera inside the chest (stage-local coordinates)")]
        public Vector3 cameraInsidePosition = new Vector3(0, 0.22f, 0.03f);
        public Vector3 cameraInsideEuler = new Vector3(-90, 0, 0);
        public Vector3 cameraExitPosition = new Vector3(0, 1.65f, 0.45f);
        public Vector3 cameraExitEuler = Vector3.zero;
        [Range(25, 80)] public float chestFieldOfView = 50;
        [Tooltip("Fill tiny gaps in the fully closed model with an opaque matte. Only enabled at the closed endpoint.")]
        public bool sealClosedFrame = true;

        protected override string PreviewTitle => "TREASUREBOX // RIG HINGE TRANSITION";
        protected override bool HasStageReferences => doorCamera != null && treasureBox != null && lidHinge != null;

        protected override void BuildPerspectiveRig()
        {
            // Keep the original skinning hierarchy and bind poses intact. Never reparent B-lid.
            if (treasureBox == null || lidHinge == null) return;
            foreach (var renderer in treasureBox.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                renderer.updateWhenOffscreen = true;
            if (!lidHinge.IsChildOf(treasureBox))
                Debug.LogError("Lid Hinge must be the B-lid bone under TreasureBox/Rig.", this);
        }

        protected override void SetOpen(float value)
        {
            Openness = Mathf.Clamp01(value);
            if (!HasStageReferences) return;
            lidHinge.localRotation = Quaternion.Slerp(Quaternion.Euler(closedLidEuler),
                Quaternion.Euler(openLidEuler), Openness);
            doorCamera.orthographic = false;
            doorCamera.fieldOfView = chestFieldOfView;
            doorCamera.nearClipPlane = 0.01f;
            doorCamera.farClipPlane = 20;
            // Closing stays inside; opening lifts the viewer out so no chest edges remain at the end.
            float travel = opening ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.35f, 1, Openness)) : 0;
            doorCamera.transform.localPosition = Vector3.Lerp(cameraInsidePosition, cameraExitPosition, travel);
            doorCamera.transform.localRotation = Quaternion.Slerp(Quaternion.Euler(cameraInsideEuler),
                Quaternion.Euler(cameraExitEuler), travel);
            // This static render state is published only while the stage owns the active transition.
            if (IsRunning) ScreenTransitionFeature.ClosedCoverage = sealClosedFrame && Openness <= 0.0001f ? 1 : 0;
        }
    }
}

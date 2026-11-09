using UnityEngine;
using UnityEngine.InputSystem;

namespace GridTutorial.Cameras
{
    [DefaultExecutionOrder(-100)]
    public sealed class FirstPersonLook : MonoBehaviour
    {
        [SerializeField] ExplorationCameraController controller;
        [SerializeField] CameraProjectionSettings projection;
        [SerializeField, Min(.001f)] float sensitivity = .12f;
        [SerializeField, Range(10, 89)] float pitchLimit = 80;
        float yaw, pitch;
        public Quaternion ViewRotation => Quaternion.Euler(pitch, yaw, 0);
        public void ResetLook(Quaternion rotation)
        {
            yaw = rotation.eulerAngles.y;
            pitch = Mathf.Clamp(Mathf.DeltaAngle(0, rotation.eulerAngles.x), -pitchLimit, pitchLimit);
        }
        void Update()
        {
            if (!controller || !controller.CanObserve || Mouse.current == null) return;
            Vector2 delta = Mouse.current.delta.ReadValue() * sensitivity;
            yaw += delta.x; pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
            float fov = Mouse.current.rightButton.isPressed ? (projection ? projection.ObservationFov : 40) : (projection ? projection.FirstPersonFov : 65);
            controller.ViewCamera.fieldOfView = Mathf.Lerp(controller.ViewCamera.fieldOfView, fov, 1 - Mathf.Exp(-12 * Time.unscaledDeltaTime));
        }
    }
}

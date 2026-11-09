using UnityEngine;

namespace GridTutorial.Cameras
{
    public sealed class CameraProjectionSettings : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] GridBoard board;
        [SerializeField, Min(0)] float margin = .5f;
        [SerializeField] Vector2 zoomLimits = new Vector2(2, 20);
        [SerializeField, Range(20, 100)] float firstPersonFov = 65;
        [SerializeField, Range(10, 80)] float observationFov = 40;
        [SerializeField, Min(.01f)] float firstPersonNear = .05f;
        [SerializeField, Min(1)] float far = 100;
        public float FirstPersonFov => firstPersonFov;
        public float ObservationFov => observationFov;
        [ContextMenu("Fit Top View To Board")]
        public void FitTopView()
        {
            if (!board || !viewCamera) return;
            var t = board.transform;
            Vector3 center = t.TransformPoint(new Vector3(board.Width * board.CellSize / 2, 0, board.Height * board.CellSize / 2));
            var rotation = Quaternion.LookRotation(-t.up, t.forward);
            float height = Mathf.Max(10, Vector3.Distance(viewCamera.transform.position, center));
            viewCamera.transform.SetPositionAndRotation(center + t.up * height, rotation);
            float halfX = 0, halfY = 0;
            for (int x = 0; x <= 1; x++) for (int z = 0; z <= 1; z++)
            {
                var corner = t.TransformPoint(new Vector3(x * board.Width * board.CellSize, 0, z * board.Height * board.CellSize));
                var local = Quaternion.Inverse(rotation) * (corner - center);
                halfX = Mathf.Max(halfX, Mathf.Abs(local.x)); halfY = Mathf.Max(halfY, Mathf.Abs(local.y));
            }
            viewCamera.orthographic = true;
            viewCamera.orthographicSize = Mathf.Max(halfY, halfX / Mathf.Max(.01f, viewCamera.aspect)) + margin;
            viewCamera.nearClipPlane = .3f; viewCamera.farClipPlane = Mathf.Max(far, height + 10);
        }
        public void Zoom(Camera camera, float scroll)
        {
            if (!camera.orthographic || scroll == 0) return;
            camera.orthographicSize = Mathf.Clamp(camera.orthographicSize - scroll * .005f, Mathf.Max(.1f, zoomLimits.x), Mathf.Max(zoomLimits.x, zoomLimits.y));
        }
        public void ApplyFirstPerson(Camera camera)
        {
            camera.orthographic = false; camera.fieldOfView = firstPersonFov;
            camera.nearClipPlane = firstPersonNear; camera.farClipPlane = Mathf.Max(firstPersonNear + 1, far);
        }
    }
}

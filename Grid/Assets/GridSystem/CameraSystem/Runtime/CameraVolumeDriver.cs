using UnityEngine;
using UnityEngine.Rendering;

namespace GridTutorial.Cameras
{
    public sealed class CameraVolumeDriver : MonoBehaviour
    {
        [SerializeField] ExplorationCameraController controller;
        [SerializeField] Volume topViewVolume, firstPersonVolume;
        [SerializeField, Min(.1f)] float blendSpeed = 5;
        float originalTop, originalFirst;
        void OnEnable()
        {
            if (topViewVolume) originalTop = topViewVolume.weight;
            if (firstPersonVolume) originalFirst = firstPersonVolume.weight;
        }
        void Update()
        {
            if (!controller) return;
            float top = controller.IsTopView ? 1 : 0;
            if (topViewVolume) topViewVolume.weight = Mathf.MoveTowards(topViewVolume.weight, top, blendSpeed * Time.unscaledDeltaTime);
            if (firstPersonVolume) firstPersonVolume.weight = Mathf.MoveTowards(firstPersonVolume.weight, 1 - top, blendSpeed * Time.unscaledDeltaTime);
        }
        void OnDisable()
        {
            if (topViewVolume) topViewVolume.weight = originalTop;
            if (firstPersonVolume) firstPersonVolume.weight = originalFirst;
        }
    }
}

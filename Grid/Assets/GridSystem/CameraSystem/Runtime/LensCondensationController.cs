using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GridTutorial.Cameras
{
    public sealed class LensCondensationController : MonoBehaviour
    {
        [SerializeField] ExplorationCameraController controller;
        [SerializeField] Transform player;
        [SerializeField] CondensationZone[] zones;
        [SerializeField] Camera overlayCamera;
        [SerializeField] RawImage lens;
        [SerializeField, Min(0)] float dryRate = .015f;
        [SerializeField, Min(.1f)] float wipeDuration = 1;
        [SerializeField, Min(0)] float wipeCooldown = 3;
        [SerializeField, Range(0, 1)] float initialMoisture;
        float moisture, wipeRemaining, cooldown;
        Texture2D generatedMask;
        Texture originalTexture;
        Color originalColor;
        bool originalRaycast;
        public float Moisture => moisture;
        void Awake()
        {
            moisture = initialMoisture;
            if (!lens) return;
            originalTexture = lens.texture; originalColor = lens.color; originalRaycast = lens.raycastTarget;
            // An asset-free mask for the first Stack lesson; replace with your own texture later.
            generatedMask = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            generatedMask.name = "Tutorial Lens Mask"; generatedMask.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float u = x / 127f, v = y / 127f;
                float edge = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Vector2.Distance(new Vector2(u, v), Vector2.one * .5f) - .15f) / .5f));
                float noise = Mathf.PerlinNoise(u * 14, v * 14);
                generatedMask.SetPixel(x, y, new Color(.83f, .9f, .92f, edge * (.4f + .6f * noise)));
            }
            generatedMask.Apply(); lens.texture = generatedMask; lens.raycastTarget = false;
        }
        void Update()
        {
            if (!controller) return;
            bool show = controller.isActiveAndEnabled && controller.IsPlaying && controller.IsTopView;
            if (overlayCamera) overlayCamera.enabled = show;
            if (controller.IsPlaying && player)
            {
                cooldown = Mathf.Max(0, cooldown - Time.deltaTime);
                if (controller.CanCommand && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) Wipe();
                if (wipeRemaining > 0)
                {
                    wipeRemaining -= Time.deltaTime;
                    moisture = Mathf.MoveTowards(moisture, 0, Time.deltaTime / Mathf.Max(.1f, wipeDuration));
                }
                else
                {
                    float rate = 0;
                    if (zones != null) foreach (var zone in zones) if (zone && zone.Contains(player.position)) rate = Mathf.Max(rate, zone.Rate);
                    moisture = Mathf.Clamp01(moisture + (rate > 0 ? rate : -dryRate) * Time.deltaTime);
                }
            }
            if (lens) lens.color = new Color(1, 1, 1, moisture * .9f);
        }
        public void Wipe()
        {
            if (!controller || !controller.CanCommand || cooldown > 0) return;
            wipeRemaining = Mathf.Max(.1f, wipeDuration); cooldown = wipeRemaining + wipeCooldown;
        }
        void OnDisable()
        {
            if (overlayCamera) overlayCamera.enabled = false;
            if (lens) lens.color = new Color(1, 1, 1, 0);
        }
        void OnDestroy()
        {
            if (lens) { lens.texture = originalTexture; lens.color = originalColor; lens.raycastTarget = originalRaycast; }
            if (generatedMask) Destroy(generatedMask);
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GridTutorial
{
    [DefaultExecutionOrder(-50)]
    public sealed class GridBuildController : MonoBehaviour
    {
        [SerializeField] GridBuildSystem buildSystem;
        [SerializeField] GridBuildDefinition selectedDefinition;
        [SerializeField] Camera viewCamera;
        [Tooltip("URP/Unlit material. A runtime copy is tinted green/red.")]
        [SerializeField] Material previewMaterial;
        [SerializeField] bool showHint = true;
        GameObject preview;
        Material runtimeMaterial;
        bool rotated;
        public bool IsBuilding { get; private set; }
        public bool CanPlace { get; private set; }
        public string Status { get; private set; } = "B: Build";
        public GridBuilding LastBuilt { get; private set; }

        // Callable from a UI Button's On Click event.
        public void BeginBuild()
        {
            CancelBuild();
            if (!buildSystem || !selectedDefinition || !selectedDefinition.IsValid || !previewMaterial)
            { Status = "Assign Build System, valid Definition and Preview Material"; Debug.LogWarning(Status, this); return; }
            if (!viewCamera) viewCamera = Camera.main;
            if (!viewCamera) { Status = "Assign View Camera"; return; }
            runtimeMaterial = new Material(previewMaterial);
            preview = new GameObject("Build Preview");
            preview.SetActive(false);
            selectedDefinition.CreateVisual(preview.transform, runtimeMaterial);
            var footprint = GameObject.CreatePrimitive(PrimitiveType.Cube);
            footprint.name = "Footprint";
            footprint.transform.SetParent(preview.transform, false);
            footprint.GetComponent<Collider>().enabled = false;
            footprint.GetComponent<Renderer>().sharedMaterial = runtimeMaterial;
            // Footprint follows the same 90-degree rotation as the renderer.
            footprint.transform.localPosition = new Vector3(0, .04f, 0);
            footprint.transform.localScale = new Vector3(selectedDefinition.width * buildSystem.Board.CellSize, .03f, selectedDefinition.depth * buildSystem.Board.CellSize);
            IsBuilding = true;
            Status = "Hover to place | R: Rotate | RMB/Esc: Cancel";
        }
        public void SelectDefinition(GridBuildDefinition definition) { selectedDefinition = definition; BeginBuild(); }
        public void CancelBuild()
        {
            IsBuilding = false; CanPlace = false; rotated = false;
            if (preview) { preview.SetActive(false); Destroy(preview); }
            if (runtimeMaterial) Destroy(runtimeMaterial);
            Status = "B: Build";
        }
        void Update()
        {
            Vector2 pointer = default;
            bool begin = false, cancel = false, rotate = false, click = false, hasPointer = false;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            begin = keyboard != null && keyboard.bKey.wasPressedThisFrame;
            cancel = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            rotate = keyboard != null && keyboard.rKey.wasPressedThisFrame;
            if (Mouse.current != null)
            {
                pointer = Mouse.current.position.ReadValue(); hasPointer = true;
                click = Mouse.current.leftButton.wasPressedThisFrame;
                cancel |= Mouse.current.rightButton.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            pointer = Input.mousePosition; hasPointer = true;
            begin = Input.GetKeyDown(KeyCode.B); cancel = Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1);
            rotate = Input.GetKeyDown(KeyCode.R); click = Input.GetMouseButtonDown(0);
#endif
            if (begin) { if (IsBuilding) CancelBuild(); else BeginBuild(); }
            if (!IsBuilding) return;
            if (cancel) { CancelBuild(); return; }
            CanPlace = false;
            if (!buildSystem || !viewCamera || !selectedDefinition || !preview) { CancelBuild(); return; }
            if (rotate) rotated = !rotated;
            if (!hasPointer || (EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
            { preview.SetActive(false); Status = "Point at the grid"; return; }
            var board = buildSystem.Board;
            var ray = viewCamera.ScreenPointToRay(pointer);
            if (!new Plane(board.transform.up, board.transform.position).Raycast(ray, out float distance))
            { preview.SetActive(false); Status = "Point at the grid"; return; }
            var anchor = board.WorldToCell(ray.GetPoint(distance));
            var size = selectedDefinition.Footprint(rotated);
            var localCenter = new Vector3((anchor.x + size.x * .5f) * board.CellSize, 0, (anchor.y + size.y * .5f) * board.CellSize);
            preview.transform.SetPositionAndRotation(board.transform.TransformPoint(localCenter), board.transform.rotation * Quaternion.Euler(0, rotated ? 90 : 0, 0));
            preview.SetActive(true);
            CanPlace = buildSystem.IsAvailable(selectedDefinition, anchor, rotated, out string reason);
            var color = CanPlace ? Color.green : Color.red;
            runtimeMaterial.color = color;
            if (runtimeMaterial.HasProperty("_BaseColor")) runtimeMaterial.SetColor("_BaseColor", color);
            Status = $"{selectedDefinition.displayName} {size.x}x{size.y}: {reason} | LMB: Place | R: Rotate | Esc: Cancel";
            if (click && CanPlace && buildSystem.TryBuild(selectedDefinition, anchor, rotated, out var building)) LastBuilt = building;
        }
        void OnGUI()
        {
            if (showHint) GUI.Label(new Rect(12, Screen.height - 55, Screen.width - 24, 50), Status);
        }
        void OnDisable() => CancelBuild();
    }
}

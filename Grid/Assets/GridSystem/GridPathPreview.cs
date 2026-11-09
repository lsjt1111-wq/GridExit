using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GridTutorial
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class GridPathPreview : MonoBehaviour
    {
        [SerializeField] GridOccupant actor;
        [SerializeField] Camera viewCamera;
        [SerializeField] Material lineMaterial;
        [SerializeField] GridBuildController buildController;
        LineRenderer line;
        GridMover mover;
        void Start()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = lineMaterial;
            line.startWidth = .09f; line.endWidth = .025f;
            line.startColor = Color.green; line.endColor = Color.yellow;
            line.positionCount = 0;
            if (!viewCamera) viewCamera = Camera.main;
            if (actor) mover = actor.GetComponent<GridMover>();
        }
        void OnDisable()
        {
            if (line) line.positionCount = 0;
        }
        void Update()
        {
            line.positionCount = 0;
            if ((buildController && buildController.IsBuilding) || (EventSystem.current && EventSystem.current.IsPointerOverGameObject())) return;
            if (!actor || !actor.isActiveAndEnabled || !actor.Board || !viewCamera || !mover || mover.IsMoving) return;
            Vector2 mouse;
            bool clicked;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null) return;
            mouse = Mouse.current.position.ReadValue();
            clicked = Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            mouse = Input.mousePosition;
            clicked = Input.GetMouseButtonDown(0);
#else
            return;
#endif
#if ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            var board = actor.Board;
            var plane = new Plane(board.transform.up, board.transform.position);
            var ray = viewCamera.ScreenPointToRay(mouse);
            if (!plane.Raycast(ray, out float distance)) return;
            var target = board.WorldToCell(ray.GetPoint(distance));
            var path = GridPathfinder.Find(board, actor.Cell, target, actor);
            line.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++) line.SetPosition(i, board.Center(path[i], .07f));
            if (clicked && path.Count > 1) mover.MoveTo(target);
#endif
        }
    }
}

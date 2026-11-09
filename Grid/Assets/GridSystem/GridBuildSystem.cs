using UnityEngine;

namespace GridTutorial
{
    [RequireComponent(typeof(GridBoard))]
    public sealed class GridBuildSystem : MonoBehaviour
    {
        [SerializeField] GridDamagePopup damagePopupPrefab;
        [SerializeField] Camera feedbackCamera;
        GridBoard board;
        public GridBoard Board => board ? board : board = GetComponent<GridBoard>();
        public bool IsAvailable(GridBuildDefinition item, Vector2Int anchor, bool rotated, out string reason)
        {
            reason = "Invalid definition / renderer prefab";
            if (!item || !item.IsValid) return false;
            reason = "Grid must have scale 1";
            if ((Board.transform.lossyScale - Vector3.one).sqrMagnitude > .0001f) return false;
            return Board.CanBuild(anchor, item.Footprint(rotated), out reason);
        }
        public bool TryBuild(GridBuildDefinition item, Vector2Int anchor, bool rotated, out GridBuilding building)
        {
            building = null;
            if (!IsAvailable(item, anchor, rotated, out _)) return false;
            var size = item.Footprint(rotated);
            var root = new GameObject(item.displayName);
            root.transform.SetParent(Board.transform, false);
            root.transform.localPosition = new Vector3((anchor.x + size.x * .5f) * Board.CellSize, 0, (anchor.y + size.y * .5f) * Board.CellSize);
            root.transform.localRotation = Quaternion.Euler(0, rotated ? 90 : 0, 0);
            var candidate = root.AddComponent<GridBuilding>();
            if (!candidate.Initialize(Board, item, anchor, rotated)) { Destroy(root); return false; }
            var visual = item.CreateVisual(root.transform);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, item.collisionHeight * .5f, 0);
            collider.size = new Vector3(item.width * Board.CellSize, item.collisionHeight, item.depth * Board.CellSize);
            root.AddComponent<GridHitFlash>().Initialize(visual.transform);
            var pivot = visual.transform.Find("FeedbackPivot");
            // Older visual-only prefabs can react through their container too.
            if (!pivot) pivot = visual.transform;
            root.AddComponent<GridBuildingFeedback>().Initialize(
                candidate, pivot, damagePopupPrefab,
                feedbackCamera ? feedbackCamera : Camera.main);
            building = candidate;
            return true;
        }
    }
}

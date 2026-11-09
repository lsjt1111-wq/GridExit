using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GridTutorial
{
    [RequireComponent(typeof(GridBoard))]
    public sealed class GridVisual : MonoBehaviour
    {
        [SerializeField] Material lineMaterial;
        [SerializeField] TMP_FontAsset font;
        [SerializeField] Color lineColor = new Color(.4f, .8f, .9f);
        [SerializeField] bool showNumbers = true;
        [SerializeField] bool showOccupants = true;
        GridBoard board;
        GameObject root;
        readonly List<TextMeshPro> labels = new List<TextMeshPro>();
        void Start()
        {
            board = GetComponent<GridBoard>();
            root = new GameObject("Grid Runtime Visuals");
            root.transform.SetParent(transform, false);
            for (int x = 0; x <= board.Width; x++)
                CreateLine(new Vector3(x * board.CellSize, .015f, 0), new Vector3(x * board.CellSize, .015f, board.Height * board.CellSize));
            for (int z = 0; z <= board.Height; z++)
                CreateLine(new Vector3(0, .015f, z * board.CellSize), new Vector3(board.Width * board.CellSize, .015f, z * board.CellSize));
            if (showNumbers)
            {
                var selectedFont = font ? font : TMP_Settings.defaultFontAsset;
                if (!selectedFont) Debug.LogError("GridVisual: Import TMP Essential Resources or assign a TMP Font Asset.", this);
                else for (int z = 0; z < board.Height; z++) for (int x = 0; x < board.Width; x++)
                {
                    var go = new GameObject($"Cell {z * board.Width + x + 1}");
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = new Vector3((x + .5f) * board.CellSize, .025f, (z + .5f) * board.CellSize);
                    go.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    var text = go.AddComponent<TextMeshPro>();
                    text.font = selectedFont;
                    text.fontSize = 2.5f * board.CellSize;
                    text.alignment = TextAlignmentOptions.Center;
                    text.rectTransform.sizeDelta = new Vector2(board.CellSize * .95f, board.CellSize * .9f);
                    labels.Add(text);
                }
            }
            board.Changed += Refresh;
            Refresh();
        }
        void CreateLine(Vector3 a, Vector3 b)
        {
            var go = new GameObject("Grid Line");
            go.transform.SetParent(root.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = lineMaterial;
            line.startColor = line.endColor = lineColor;
            line.startWidth = line.endWidth = .02f * board.CellSize;
            line.positionCount = 2;
            line.SetPosition(0, a); line.SetPosition(1, b);
        }
        void Refresh()
        {
            for (int i = 0; i < labels.Count; i++)
            {
                var cell = board.GetCell(new Vector2Int(i % board.Width, i / board.Width));
                var occupant = cell.Occupant;
                labels[i].text = cell.Number.ToString() + (showOccupants && cell.Building ? "\nBuild" : showOccupants && occupant ? "\n" + occupant.Kind : "");
                labels[i].color = cell.Building ? Color.cyan : !occupant ? Color.white : occupant.Kind == OccupantKind.Obstacle ? Color.gray : occupant.Kind == OccupantKind.Monster ? Color.red : Color.green;
            }
        }
        void OnDestroy() { if (board) board.Changed -= Refresh; if (root) Destroy(root); }
    }
}

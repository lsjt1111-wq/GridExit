using UnityEditor;
using UnityEngine;

namespace GridTutorial.Editor
{
    public static class GridBoardGizmos
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        static void DrawCoordinates(GridBoard board, GizmoType gizmoType)
        {
            for (int z = 0; z < board.Height; z++)
                for (int x = 0; x < board.Width; x++)
                    Handles.Label(board.Center(new Vector2Int(x, z)), $"({x}, {z})");
        }
    }
}

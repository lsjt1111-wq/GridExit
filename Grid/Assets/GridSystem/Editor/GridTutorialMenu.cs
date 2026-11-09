using UnityEditor;
using UnityEngine;

namespace GridTutorial.Editor
{
    public static class GridTutorialMenu
    {
        [MenuItem("Tools/Grid Tutorial/Create Demo")]
        static void CreateDemo()
        {
            var root = new GameObject("Grid Tutorial Demo");
            Undo.RegisterCreatedObjectUndo(root, "Create Grid Tutorial Demo");
            var board = root.AddComponent<GridBoard>();
            var visual = root.AddComponent<GridVisual>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (!shader) { Debug.LogError("No supported unlit shader found. Assign line materials manually."); return; }
            const string materialPath = "Assets/GridSystem/GridTutorialLine.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            Assign(visual, "lineMaterial", material);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition = new Vector3(5, -.1f, 4);
            floor.transform.localScale = new Vector3(10, .1f, 8);
            var actor = CreateOccupant(root.transform, board, "Player", OccupantKind.Player, new Vector2Int(0, 0), PrimitiveType.Capsule);
            actor.gameObject.AddComponent<GridMover>();
            CreateOccupant(root.transform, board, "Monster", OccupantKind.Monster, new Vector2Int(7, 5), PrimitiveType.Sphere);
            for (int z = 0; z < 6; z++) if (z != 3)
                CreateOccupant(root.transform, board, "Obstacle", OccupantKind.Obstacle, new Vector2Int(4, z), PrimitiveType.Cube);
            var preview = new GameObject("Path Preview");
            preview.transform.SetParent(root.transform, false);
            var controller = preview.AddComponent<GridPathPreview>();
            Assign(controller, "actor", actor);
            Assign(controller, "lineMaterial", material);
            var cameraObject = new GameObject("Grid Demo Camera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(5, 14, 4);
            cameraObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 6.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.08f, .1f, .14f);
            Assign(controller, "viewCamera", camera);
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            Debug.Log("Grid demo created. Save the scene, import TMP Essential Resources, then press Play. Hover to preview; left-click to move.");
        }
        static GridOccupant CreateOccupant(Transform parent, GridBoard board, string name, OccupantKind kind, Vector2Int cell, PrimitiveType primitive)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(parent, false);
            float elevation = primitive == PrimitiveType.Capsule ? 1 : .5f;
            go.transform.position = board.Center(cell, elevation);
            go.transform.localScale = Vector3.one * .7f;
            var occupant = go.AddComponent<GridOccupant>();
            var serialized = new SerializedObject(occupant);
            serialized.FindProperty("board").objectReferenceValue = board;
            serialized.FindProperty("kind").enumValueIndex = (int)kind;
            serialized.FindProperty("initialCell").vector2IntValue = cell;
            serialized.FindProperty("elevation").floatValue = elevation;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return occupant;
        }
        static void Assign(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

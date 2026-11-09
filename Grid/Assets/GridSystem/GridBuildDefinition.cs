using UnityEngine;

namespace GridTutorial
{
    [CreateAssetMenu(menuName = "Grid Tutorial/Build Definition", fileName = "Barricade")]
    public sealed class GridBuildDefinition : ScriptableObject
    {
        public string displayName = "Barricade";
        [Tooltip("Visual-only prefab: MeshRenderer/SkinnedMeshRenderer, no gameplay scripts.")]
        public GameObject rendererPrefab;
        [Min(1)] public int width = 2, depth = 1;
        [Min(.1f)] public float collisionHeight = 1;
        [Min(1)] public float maxHealth = 100;
        public Vector3 visualOffset = new Vector3(0, .5f, 0);
        public Vector3 visualEuler;
        public Vector3 visualScale = new Vector3(2, 1, 1);
        public Vector2Int Footprint(bool rotated) => rotated ? new Vector2Int(depth, width) : new Vector2Int(width, depth);
        public bool IsValid => rendererPrefab && rendererPrefab.GetComponentInChildren<Renderer>(true) && width > 0 && depth > 0 && collisionHeight > 0 && maxHealth > 0;

        internal GameObject CreateVisual(Transform parent, Material overrideMaterial = null)
        {
            var visual = Instantiate(rendererPrefab, parent);
            visual.SetActive(true);
            visual.transform.localPosition = visualOffset;
            visual.transform.localEulerAngles = visualEuler;
            visual.transform.localScale = visualScale;
            foreach (var behaviour in visual.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled = false;
            foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var body in visual.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.detectCollisions = false; }
            if (overrideMaterial)
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++) materials[i] = overrideMaterial;
                    renderer.sharedMaterials = materials;
                }
            return visual;
        }
    }
}
